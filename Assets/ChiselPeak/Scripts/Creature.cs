using System.Collections.Generic;
using UnityEngine;

namespace ChiselPeak
{
    /// Kindle: a giant ember salamander, sitting upright, frozen asleep inside the ice. Faces -z.
    public class Creature : MonoBehaviour
    {
        struct Ell { public Vector3 c, r; }
        readonly List<Ell> ells = new List<Ell>();
        Transform body, head, jaw, tailRoot;
        readonly List<Transform> lids = new List<Transform>();
        readonly List<Vector3> lidOpen = new List<Vector3>();
        Material crestMat, eyeMat;
        public bool Awake { get; private set; }
        float wakeT;

        static readonly Color Orange = new Color(1f, 0.52f, 0.1f);
        static readonly Color Stripe = new Color(0.78f, 0.3f, 0.05f);
        static readonly Color Belly = new Color(1f, 0.82f, 0.5f);
        static readonly Color Crest = new Color(1f, 0.8f, 0.2f);

        public Vector3 HeadPos => head.position;

        public void Build()
        {
            var O = Art.Lit(Orange, 0.35f, 0.45f);
            var D = Art.Lit(Stripe, 0.3f);
            var B = Art.Lit(Belly, 0.3f, 0.2f);
            crestMat = Art.Lit(Crest, 0.4f, 0.25f);
            eyeMat = Art.Lit(new Color(1f, 0.97f, 0.85f), 0.8f);
            var K = Art.Lit(new Color(0.08f, 0.05f, 0.1f), 0.9f);
            var M = Art.Lit(new Color(0.32f, 0.05f, 0.07f), 0.2f);
            var W = Art.Lit(Color.white, 0.5f);

            body = new GameObject("Body").transform; body.SetParent(transform, false);

            Blob(body, new Vector3(0, 3.1f, 0.3f), new Vector3(5.4f, 5.6f, 4.4f), O, "Torso");
            Blob(body, new Vector3(0, 2.8f, -1.5f), new Vector3(3.8f, 4.2f, 1.7f), B, "Belly");
            for (int s = -1; s <= 1; s += 2)
            {
                Blob(body, new Vector3(2.1f * s, 1.0f, -0.6f), new Vector3(2.3f, 2.0f, 3.0f), O, "Thigh");
                for (int t = -1; t <= 1; t++)
                    Blob(body, new Vector3(2.1f * s + 0.55f * t, 0.32f, -2.05f), new Vector3(0.6f, 0.55f, 0.75f), B, "Toe", false);
                Blob(body, new Vector3(2.55f * s, 3.0f, -1.1f), new Vector3(1.4f, 2.7f, 1.4f), O, "Arm");
                Blob(body, new Vector3(2.3f * s, 1.8f, -1.85f), new Vector3(1.25f, 0.95f, 1.15f), B, "Hand");
                // stripes on the flanks
                for (int k = 0; k < 3; k++)
                    Blob(body, new Vector3(2.45f * s, 4.4f - k * 1.2f, 0.9f + k * 0.15f), new Vector3(0.5f, 0.55f, 1.6f), D, "Stripe", false,
                        new Vector3(0, 0, -18 * s));
            }
            // back spikes along the spine
            for (int k = 0; k < 5; k++)
            {
                float a = k / 4f;
                var pos = new Vector3(0, Mathf.Lerp(5.3f, 1.6f, a), Mathf.Lerp(1.8f, 2.6f, Mathf.Sin(a * Mathf.PI) * 0.6f + a * 0.4f));
                Art.Part(Art.Cone, body, pos, new Vector3(0.9f, 1.3f, 0.9f) * Mathf.Lerp(1f, 0.7f, a), crestMat,
                    new Vector3(Mathf.Lerp(60, 110, a), 0, 0), "Spike");
            }

            // tail curling round the right side to the front
            tailRoot = new GameObject("Tail").transform; tailRoot.SetParent(body, false);
            for (int k = 0; k < 8; k++)
            {
                float a = k / 7f;
                float ang = Mathf.Lerp(150f, 290f, a) * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(ang) * -3.3f, 0.65f - a * 0.15f, Mathf.Sin(ang) * 2.8f + 0.3f);
                float sz = Mathf.Lerp(1.6f, 0.6f, a);
                Blob(tailRoot, pos, new Vector3(sz, sz * 0.85f, sz), k % 2 == 0 ? O : D, "Tail" + k);
            }

            // one solid body the player can't walk through; on Ignore Raycast so swings pass to the ice
            var solid = new GameObject("BodyCollider");
            solid.transform.SetParent(transform, false);
            solid.layer = 2;
            var cap = solid.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0, 3.5f, 0f); cap.radius = 2.6f; cap.height = 7f;

            head = new GameObject("Head").transform; head.SetParent(transform, false);
            head.localPosition = new Vector3(0, 6.0f, -0.4f);
            Blob(head, new Vector3(0, 0.5f, 0), new Vector3(4.4f, 3.0f, 3.6f), O, "Skull");
            Blob(head, new Vector3(0, 0.05f, -1.45f), new Vector3(3.6f, 1.6f, 1.9f), O, "Snout");
            Blob(head, new Vector3(0, -0.28f, -2.2f), new Vector3(2.7f, 0.32f, 0.5f), M, "Mouth", false);
            jaw = new GameObject("Jaw").transform; jaw.SetParent(head, false); jaw.localPosition = new Vector3(0, -0.4f, -0.2f);
            Blob(jaw, new Vector3(0, -0.3f, -1.15f), new Vector3(3.3f, 1.1f, 2.3f), B, "LowerJaw");
            for (int t = -2; t <= 2; t++)
                if (t != 0) Art.Part(Art.Cone, jaw, new Vector3(t * 0.45f, 0.15f, -2.1f), new Vector3(0.22f, 0.3f, 0.22f), W, default, "Tooth");
            for (int s = -1; s <= 1; s += 2)
            {
                Blob(head, new Vector3(1.15f * s, 1.15f, -1.4f), new Vector3(1.15f, 1.25f, 0.95f), eyeMat, "Eye", false);
                Blob(head, new Vector3(1.15f * s, 1.1f, -1.86f), new Vector3(0.55f, 0.75f, 0.25f), K, "Pupil", false);
                Blob(head, new Vector3(1.35f * s, 1.35f, -1.9f), new Vector3(0.18f, 0.18f, 0.1f), W, "Glint", false);
                var lid = Blob(head, new Vector3(1.15f * s, 1.15f, -1.45f), new Vector3(1.3f, 1.4f, 1.2f), O, "Lid", false).transform;
                lids.Add(lid); lidOpen.Add(new Vector3(1.15f * s, 1.95f, -1.2f));
                Blob(head, new Vector3(0.55f * s, 0.35f, -2.38f), new Vector3(0.25f, 0.18f, 0.12f), D, "Nostril", false);
                // swept horns
                Art.Part(Art.Cone, head, new Vector3(1.6f * s, 1.5f, 0.6f), new Vector3(0.8f, 2.0f, 0.8f), crestMat, new Vector3(-35, 0, -40 * s), "Horn");
                Art.Part(Art.Cone, head, new Vector3(2.1f * s, 0.2f, 0.2f), new Vector3(0.55f, 1.3f, 0.55f), crestMat, new Vector3(0, 0, -100 * s), "Cheek");
                for (int k = 0; k < 2; k++)
                    Blob(head, new Vector3(1.4f * s, 1.9f - k * 0.5f, 0.6f + k * 0.3f), new Vector3(0.45f, 0.3f, 0.9f), D, "HeadStripe", false);
            }
            Art.Part(Art.Cone, head, new Vector3(0, 1.75f, 0.3f), new Vector3(1.0f, 1.6f, 1.0f), crestMat, new Vector3(-20, 0, 0), "Crest");
        }

        GameObject Blob(Transform parent, Vector3 pos, Vector3 size, Material m, string name, bool sdf = true, Vector3 euler = default)
        {
            var g = Art.Part(Art.Sphere, parent, pos, size, m, euler, name);
            g.GetComponent<MeshRenderer>().receiveShadows = false;
            if (sdf)
            {
                ells.Add(new Ell { c = transform.InverseTransformPoint(g.transform.position), r = size * 0.5f });
            }
            return g;
        }

        /// Approximate distance from a world point to Kindle's surface (negative inside).
        public float Distance(Vector3 world)
        {
            var p = transform.InverseTransformPoint(world);
            float best = 1e9f;
            foreach (var e in ells)
            {
                var q = p - e.c;
                var n = new Vector3(q.x / e.r.x, q.y / e.r.y, q.z / e.r.z);
                float d = (n.magnitude - 1f) * Mathf.Min(e.r.x, Mathf.Min(e.r.y, e.r.z));
                if (d < best) best = d;
            }
            return best * transform.localScale.x;
        }

        public void Wake()
        {
            if (Awake) return;
            Awake = true; wakeT = 0;
        }

        void Update()
        {
            if (!Awake) return;
            wakeT += Time.deltaTime;
            float open = Mathf.SmoothStep(0, 1, Mathf.Clamp01((wakeT - 0.3f) / 0.35f));
            for (int i = 0; i < lids.Count; i++)
            {
                var closed = new Vector3(lids[i].localPosition.x, 1.15f, -1.45f);
                lids[i].localPosition = Vector3.Lerp(closed, lidOpen[i], open);
                lids[i].localScale = Vector3.Lerp(new Vector3(1.3f, 1.4f, 1.2f), new Vector3(1.3f, 0.5f, 1.2f), open);
            }
            // roar: jaw drops, head lifts
            float roar = Mathf.Clamp01((wakeT - 0.8f) / 0.25f) * (1 - Mathf.Clamp01((wakeT - 2.4f) / 0.4f));
            jaw.localEulerAngles = new Vector3(32f * roar, 0, 0);
            float breathe = Mathf.Sin(wakeT * 2.2f);
            head.localPosition = new Vector3(0, 6.0f + roar * 0.5f + breathe * 0.08f, -0.4f - roar * 0.3f);
            head.localEulerAngles = new Vector3(-14f * roar + breathe * 2f, Mathf.Sin(wakeT * 0.9f) * 6f * (1 - roar), 0);
            body.localScale = new Vector3(1 + breathe * 0.015f, 1 + breathe * 0.02f, 1 + breathe * 0.015f);
            tailRoot.localEulerAngles = new Vector3(0, Mathf.Sin(wakeT * 3f) * 6f, 0);
            float glow = 0.25f + roar * 1.2f + Mathf.Max(0, Mathf.Sin(wakeT * 4f)) * 0.2f;
            crestMat.SetColor("_EmissionColor", Crest * glow);
            if (roar > 0.5f && Fx.I != null && Time.frameCount % 3 == 0)
                Fx.I.Steam(head.TransformPoint(new Vector3(0, 0.2f, -2.4f)), head.TransformDirection(new Vector3(0, 0.3f, -1f)).normalized, 2);
        }
    }
}
