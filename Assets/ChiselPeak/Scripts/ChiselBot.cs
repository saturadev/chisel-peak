using UnityEngine;

namespace ChiselPeak
{
    /// A simple player bot for demo footage: walks up to the ice, picks an exposed chunk near
    /// the front (low ones once the tools get heavy, so the ice above collapses), aims and swings,
    /// crafts as soon as it can, and backs off to watch Kindle wake up.
    public class ChiselBot : MonoBehaviour
    {
        public bool Active;
        PlayerRig p;
        IceChunk target;
        Vector3 aimPoint;
        float retargetAt, t0 = -1;
        int lastTool;
        float lastHp, stuckSince;
        readonly System.Collections.Generic.HashSet<IceChunk> skip = new System.Collections.Generic.HashSet<IceChunk>();

        void Start() { p = GetComponent<PlayerRig>(); }

        void Update()
        {
            var g = ChiselGame.I;
            if (!Active || g == null || p == null) { if (p != null) p.BotDriven = false; return; }
            p.BotDriven = true;
            if (t0 < 0) t0 = Time.time;
            float t = Time.time - t0;

            if (g.Won)
            {
                p.BotSwing = false;
                var head = g.Kindle.HeadPos;
                var spot = g.Block.transform.position + new Vector3(0.5f, 0, -13.5f);
                var toSpot = spot - transform.position; toSpot.y = 0;
                p.BotMove = toSpot.magnitude > 0.4f ? Vector3.ClampMagnitude(toSpot * 0.5f, 1f) : Vector3.zero;
                LookAt(head + Vector3.down * 2.2f, 3f);
                return;
            }

            if (g.CanCraft) g.Craft();
            if (g.ToolIndex != lastTool) { lastTool = g.ToolIndex; target = null; }

            if (target != null && !target.Falling)
            {
                if (target.Hp != lastHp || p.BotMove.sqrMagnitude > 0.01f) { lastHp = target.Hp; stuckSince = Time.time; }
                else if (Time.time - stuckSince > 2f) { skip.Add(target); target = null; }
            }
            if (target == null || target.Falling || Time.time > retargetAt)
            {
                var prev = target;
                target = Pick(g, t);
                if (target != prev && target != null) { lastHp = target.Hp; stuckSince = Time.time; }
                retargetAt = Time.time + 0.6f;
                if (target != null) aimPoint = Face(target);
            }

            var eye = p.EyePos;
            if (target == null)
            {
                var c = g.Block.transform.position + Vector3.up * 1.5f;
                var dir = c - eye; dir.y = 0;
                p.BotMove = dir.normalized;
                p.BotSwing = false;
                LookAt(c, 8f);
                return;
            }

            aimPoint = Face(target);
            var to = aimPoint - eye;
            var flatTo = new Vector3(to.x, 0, to.z);
            float want = p.Tool.Reach * 0.85f;
            float d = to.magnitude;
            Vector3 move = Vector3.zero;
            if (d > want + 0.3f) move = flatTo.normalized;
            else if (d < want - 0.6f) move = -flatTo.normalized * 0.7f;
            // don't hug Kindle: keep a few metres from its body so the shot stays readable
            float kd = g.Kindle.Distance(eye) * g.Block.transform.lossyScale.x;
            if (kd < 1.4f)
            {
                var away = eye - g.Kindle.transform.position; away.y = 0;
                move = move + away.normalized * 0.5f;
            }
            p.BotMove = Vector3.ClampMagnitude(move, 1f);
            float err = LookAt(aimPoint, 12f);
            p.BotSwing = d <= p.Tool.Reach - 0.15f && err < 7f;
        }

        Vector3 Face(IceChunk c)
        {
            var pos = c.transform.position;
            var dir = p.EyePos - pos; dir.y *= 0.3f;
            return pos + dir.normalized * 0.45f;
        }

        float LookAt(Vector3 point, float speed)
        {
            var dir = point - p.EyePos;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(dir.y, new Vector2(dir.x, dir.z).magnitude) * Mathf.Rad2Deg;
            float k = 1f - Mathf.Exp(-speed * Time.deltaTime);
            p.Yaw = Mathf.LerpAngle(p.Yaw, yaw, k);
            pitch = Mathf.Clamp(pitch, -28f, 32f);
            p.Pitch = Mathf.Lerp(p.Pitch, pitch, k);
            return Mathf.Abs(Mathf.DeltaAngle(p.Yaw, yaw)) + Mathf.Abs(p.Pitch - pitch);
        }

        IceChunk Pick(ChiselGame g, float t)
        {
            return Pick(g, t, true) ?? Pick(g, t, false);
        }

        IceChunk Pick(ChiselGame g, float t, bool front)
        {
            var b = g.Block;
            var eye = p.EyePos;
            float wanderX = Mathf.Sin(t * 0.35f) * 2.4f;
            float preferH = g.ToolIndex >= 2 ? 1.0f : 2.0f;
            IceChunk best = null; float bestScore = float.MaxValue;
            foreach (var c in b.Live)
            {
                var pos = c.transform.position;
                if (front && (pos.z > -0.5f || pos.y > 4.2f)) continue;   // stay on the front side, within a comfortable look
                if (!front && pos.y > 6f) continue;
                if (!b.IsExposed(c) || skip.Contains(c)) continue;
                float score = Vector3.Distance(eye, pos) * 1.0f + Mathf.Abs(pos.y - preferH) * 0.8f + Mathf.Abs(pos.x - wanderX) * 0.45f;
                if (score >= bestScore) continue;
                // must be the first thing the swing would hit
                var dir = pos - eye;
                if (Physics.Raycast(eye, dir.normalized, out var hit, dir.magnitude + 0.6f, ~((1 << PlayerRig.ViewLayer) | (1 << 2)), QueryTriggerInteraction.Ignore)
                    && hit.collider.gameObject != c.gameObject) continue;
                best = c; bestScore = score;
            }
            return best;
        }
    }
}
