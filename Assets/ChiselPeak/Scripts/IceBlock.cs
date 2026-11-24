using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChiselPeak
{
    /// The giant ice dome around Kindle: a voxel grid of IceChunks.
    /// Support: after chunks break, a flood fill starts from chunks that stand on the ground,
    /// on a stable chunk or on Kindle, and spreads sideways at most MaxOverhang cells inside each layer.
    /// Every chunk it doesn't reach falls as a rigidbody. Technique adapted from
    /// ElasticSea/unity-fracture (MIT): anchored chunks flood-fill, unreached chunks unfreeze.
    public class IceBlock : MonoBehaviour
    {
        public const int NX = 11, NY = 8, NZ = 9;
        public const float RX = 5.35f, RY = 7.9f, RZ = 4.45f, Shape = 2.6f;
        public const int MaxOverhang = 2;
        public const float ChunkHp = 4f;

        readonly IceChunk[,,] grid = new IceChunk[NX, NY, NZ];
        readonly bool[,,] creatureCell = new bool[NX, NY, NZ];
        readonly int[,] dist = new int[NX, NZ];
        readonly Queue<Vector2Int> queue = new Queue<Vector2Int>();
        public readonly List<IceChunk> Live = new List<IceChunk>();
        readonly List<IceChunk> hitList = new List<IceChunk>();
        readonly List<float> hitDist = new List<float>();

        public int Total { get; private set; }
        public int Removed { get; private set; }
        public int FallingNow { get; private set; }
        public int LastCollapse { get; private set; }
        readonly Dictionary<int, Material> palette = new Dictionary<int, Material>();
        Shader iceShader;

        public System.Action<Vector3> OnShard;      // a chunk turned into collectable ice
        public System.Action<int> OnCollapse;       // number of chunks that lost support

        Creature creature;
        Material mat;

        static readonly Color Light = new Color(0.42f, 0.78f, 1f);
        static readonly Color Deep = new Color(0.12f, 0.45f, 0.95f);
        static readonly Color Ember = new Color(1f, 0.45f, 0.08f);
        static readonly Color Shadow = new Color(0.08f, 0.14f, 0.38f);

        public Vector3 CellLocal(int i, int j, int k) =>
            new Vector3(i - (NX - 1) * 0.5f, j + 0.5f, k - (NZ - 1) * 0.5f);

        public void Build(Creature c, Material iceMat)
        {
            creature = c; mat = iceMat; iceShader = iceMat.shader;
            var rng = new System.Random(11);
            for (int i = 0; i < NX; i++)
                for (int j = 0; j < NY; j++)
                    for (int k = 0; k < NZ; k++)
                    {
                        var p = CellLocal(i, j, k);
                        float s = Mathf.Pow(Mathf.Abs(p.x) / RX, Shape) + Mathf.Pow(p.y / RY, Shape) + Mathf.Pow(Mathf.Abs(p.z) / RZ, Shape);
                        if (s > 1f) continue;
                        var world = transform.TransformPoint(p);
                        float d = creature.Distance(world);
                        if (d < 0.35f) { creatureCell[i, j, k] = true; continue; }

                        // Look in from the front (+z): is Kindle behind this chunk, and how deep?
                        float depth = 99f, closest = 99f;
                        for (float z = 0f; z < 9f; z += 0.5f)
                        {
                            float dz = creature.Distance(transform.TransformPoint(p + Vector3.forward * z));
                            if (dz < closest) closest = dz;
                            if (dz < 0f) { depth = z; break; }
                        }
                        float r = (float)rng.NextDouble();
                        var col = Color.Lerp(Deep, Light, Mathf.Clamp01(0.1f + r * 0.5f + p.y / RY * 0.4f));
                        if (rng.NextDouble() < 0.08) col = Color.Lerp(col, Color.white, 0.55f);
                        float glow = 0f;
                        if (depth < 99f) glow = Mathf.Clamp01(1f - depth / 6.5f) * 0.6f;
                        if (closest < 1.2f && depth >= 99f) col = Color.Lerp(col, Shadow, 0.35f);

                        var go = new GameObject($"Ice {i},{j},{k}");
                        go.transform.SetParent(transform, false);
                        go.transform.localPosition = p + new Vector3((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f) * 0.04f;
                        go.transform.localEulerAngles = new Vector3((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f) * 5f;
                        float scl = 0.965f + (float)rng.NextDouble() * 0.02f;
                        go.transform.localScale = Vector3.one * scl;
                        go.AddComponent<MeshFilter>().sharedMesh = Art.Cube;
                        var mr = go.AddComponent<MeshRenderer>();
                        mr.sharedMaterial = mat;
                        var bc = go.AddComponent<BoxCollider>();
                        bc.size = Vector3.one / scl;
                        var ch = go.AddComponent<IceChunk>();
                        ch.Init(this, new Vector3Int(i, j, k), ChunkHp, col, glow);
                        grid[i, j, k] = ch;
                        Live.Add(ch);
                        Total++;
                    }
        }

        /// Shared ice materials, quantized so the SRP batcher can batch the whole dome.
        public Material Palette(Color baseColor, float glow, int damage)
        {
            int r = Mathf.RoundToInt(baseColor.r * 15), g = Mathf.RoundToInt(baseColor.g * 15), b = Mathf.RoundToInt(baseColor.b * 15);
            int gl = Mathf.RoundToInt(glow * 6);
            damage = Mathf.Clamp(damage, 0, 3);
            int key = ((((r * 16 + g) * 16 + b) * 8 + gl) * 4) + damage;
            if (palette.TryGetValue(key, out var m)) return m;
            var c = new Color(r / 15f, g / 15f, b / 15f);
            float d = damage / 3f;
            c = Color.Lerp(c, new Color(0.93f, 0.97f, 1f), d * 0.6f);
            m = new Material(mat) { name = "cp_ice_" + key };
            c.a = Mathf.Lerp(0.42f, 0.7f, d);
            m.SetColor("_BaseColor", c);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.Lerp(new Color(0.03f, 0.1f, 0.22f), new Color(0.45f, 0.16f, 0.02f), gl / 6f) + Color.white * d * 0.1f);
            palette[key] = m;
            return m;
        }

        bool In(int i, int j, int k) => i >= 0 && j >= 0 && k >= 0 && i < NX && j < NY && k < NZ;

        public bool IsExposed(IceChunk c)
        {
            var v = c.Cell;
            return Empty(v.x - 1, v.y, v.z) || Empty(v.x + 1, v.y, v.z) || Empty(v.x, v.y + 1, v.z) ||
                   Empty(v.x, v.y, v.z - 1) || Empty(v.x, v.y, v.z + 1);
        }

        bool Empty(int i, int j, int k) => !In(i, j, k) || (grid[i, j, k] == null && !creatureCell[i, j, k]);

        /// A swing hit chunk `direct` at `point`. Damages every chunk within the tool's radius.
        public int Hit(IceChunk direct, Vector3 point, Vector3 normal, ToolDef tool)
        {
            hitList.Clear(); hitDist.Clear();
            hitList.Add(direct); hitDist.Add(0f);
            if (tool.Radius > 0.01f)
                foreach (var c in Live)
                {
                    if (c == direct) continue;
                    float d = Vector3.Distance(c.transform.position, point) / transform.lossyScale.x;
                    if (d <= tool.Radius) { hitList.Add(c); hitDist.Add(d); }
                }
            int broken = 0;
            for (int n = 0; n < hitList.Count; n++)
            {
                var c = hitList[n];
                float f = n == 0 ? 1f : Mathf.Lerp(1f, 0.45f, hitDist[n] / tool.Radius);
                c.Hp -= tool.Damage * f;
                if (c.Hp <= 0.001f) { Break(c, tool.Power); broken++; }
                else c.ShowDamage();
            }
            if (Fx.I != null) Fx.I.IceHit(point, normal, tool.Power);
            if (broken > 0) UpdateSupport();
            return broken;
        }

        void Break(IceChunk c, float power)
        {
            var v = c.Cell;
            grid[v.x, v.y, v.z] = null;
            Live.Remove(c);
            Removed++;
            if (Fx.I != null) Fx.I.IceBreak(c.transform.position, power);
            OnShard?.Invoke(c.transform.position);
            Destroy(c.gameObject);
        }

        /// Flood fill per layer, bottom to top. Unreached chunks fall.
        public int UpdateSupport()
        {
            var toFall = new List<IceChunk>();
            for (int j = 0; j < NY; j++)
            {
                queue.Clear();
                for (int i = 0; i < NX; i++)
                    for (int k = 0; k < NZ; k++)
                    {
                        dist[i, k] = int.MaxValue;
                        if (grid[i, j, k] == null) continue;
                        bool seed = j == 0 || grid[i, j - 1, k] != null || creatureCell[i, j - 1, k];
                        if (seed) { dist[i, k] = 0; queue.Enqueue(new Vector2Int(i, k)); }
                    }
                while (queue.Count > 0)
                {
                    var q = queue.Dequeue();
                    int nd = dist[q.x, q.y] + 1;
                    if (nd > MaxOverhang) continue;
                    Visit(q.x - 1, j, q.y, nd); Visit(q.x + 1, j, q.y, nd);
                    Visit(q.x, j, q.y - 1, nd); Visit(q.x, j, q.y + 1, nd);
                }
                for (int i = 0; i < NX; i++)
                    for (int k = 0; k < NZ; k++)
                        if (grid[i, j, k] != null && dist[i, k] == int.MaxValue)
                        {
                            toFall.Add(grid[i, j, k]);
                            grid[i, j, k] = null; // the layer above no longer rests on it
                        }
            }
            var center = transform.position;
            foreach (var c in toFall)
            {
                Live.Remove(c);
                Removed++;
                FallingNow++;
                var out_ = c.transform.position - center; out_.y = 0;
                var vel = out_.normalized * Random.Range(0.3f, 1.4f) + Vector3.down * 0.5f;
                c.BeginFall(vel, Random.insideUnitSphere * 2.5f, Random.Range(0.5f, 1.6f), true);
            }
            if (toFall.Count > 0) { LastCollapse = toFall.Count; OnCollapse?.Invoke(toFall.Count); }
            return toFall.Count;
        }

        void Visit(int i, int j, int k, int nd)
        {
            if (i < 0 || k < 0 || i >= NX || k >= NZ) return;
            if (grid[i, j, k] == null || dist[i, k] <= nd) return;
            dist[i, k] = nd;
            queue.Enqueue(new Vector2Int(i, k));
        }

        public void FallenShattered(IceChunk c, bool shards)
        {
            FallingNow = Mathf.Max(0, FallingNow - 1);
            if (shards) OnShard?.Invoke(c.transform.position);
        }

        /// Testing aid: break every chunk in a box of cells.
        public int BreakCells(Vector3Int min, Vector3Int max)
        {
            int n = 0;
            for (int i = min.x; i <= max.x; i++)
                for (int j = min.y; j <= max.y; j++)
                    for (int k = min.z; k <= max.z; k++)
                        if (In(i, j, k) && grid[i, j, k] != null) { Break(grid[i, j, k], 0.5f); n++; }
            UpdateSupport();
            return n;
        }

        /// Kindle breaks free: every remaining chunk bursts outward from `from`.
        public void Burst(Vector3 from)
        {
            StartCoroutine(BurstRoutine(from));
        }

        IEnumerator BurstRoutine(Vector3 from)
        {
            var list = new List<IceChunk>(Live);
            list.Sort((a, b) => Vector3.Distance(a.transform.position, from).CompareTo(Vector3.Distance(b.transform.position, from)));
            foreach (var c in list) { var v = c.Cell; grid[v.x, v.y, v.z] = null; }
            Live.Clear();
            int perFrame = Mathf.Max(6, list.Count / 30);
            for (int n = 0; n < list.Count; n++)
            {
                var c = list[n];
                if (c == null) continue;
                var dir = c.transform.position - from; dir.y = Mathf.Max(dir.y, 0.5f);
                dir.Normalize();
                c.BeginFall(dir * Random.Range(4f, 9f) + Vector3.up * Random.Range(2f, 5f), Random.insideUnitSphere * 6f, Random.Range(0.2f, 1.0f), false);
                if (n % perFrame == perFrame - 1) yield return null;
            }
        }
    }
}
