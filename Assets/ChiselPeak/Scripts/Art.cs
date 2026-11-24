using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChiselPeak
{
    /// Runtime materials and meshes. Everything in the game is built from these.
    public static class Art
    {
        static Shader lit, particle;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        public static Mesh Cube, Sphere, Cylinder, Cone, Prism;
        public static Texture2D SoftDot;

        public static void Init(Shader litShader, Shader particleShader)
        {
            lit = litShader != null ? litShader : Shader.Find("Universal Render Pipeline/Lit");
            particle = particleShader != null ? particleShader : Shader.Find("Universal Render Pipeline/Particles/Unlit");
            cache.Clear();
            if (Cube == null) Cube = PrimMesh(PrimitiveType.Cube);
            if (Sphere == null) Sphere = PrimMesh(PrimitiveType.Sphere);
            if (Cylinder == null) Cylinder = PrimMesh(PrimitiveType.Cylinder);
            if (Cone == null) Cone = BuildCone(14);
            if (Prism == null) Prism = BuildPrism();
            if (SoftDot == null) SoftDot = BuildSoftDot(64);
        }

        static Mesh PrimMesh(PrimitiveType t)
        {
            var g = GameObject.CreatePrimitive(t);
            var m = g.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(g);
            return m;
        }

        public static Material Lit(Color c, float smooth = 0.15f, float emission = 0f)
        {
            string key = ColorUtility.ToHtmlStringRGBA(c) + smooth + "_" + emission;
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(lit) { name = "cp_" + key };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", 0f);
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            m.enableInstancing = true;
            cache[key] = m;
            return m;
        }

        /// Transparent lit material that still writes depth (ice).
        public static Material Glass(Color c, float smooth)
        {
            var m = new Material(lit) { name = "cp_glass" };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 1f);
            m.SetFloat("_EnvironmentReflections", 1f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
            return m;
        }

        public static Material ParticleMat(Color c, Texture tex, bool transparent)
        {
            var m = new Material(particle) { name = "cp_particle" };
            m.SetColor("_BaseColor", c);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            if (transparent)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            return m;
        }

        public static GameObject Part(Mesh mesh, Transform parent, Vector3 pos, Vector3 scale, Material mat,
            Vector3 euler = default, string name = "part", bool shadows = true)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localEulerAngles = euler;
            g.transform.localScale = scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return g;
        }

        public static void SetLayer(GameObject g, int layer)
        {
            g.layer = layer;
            foreach (Transform c in g.transform) SetLayer(c.gameObject, layer);
        }

        // Cone: base radius 0.5 at y=0, apex at y=1, flat shaded.
        static Mesh BuildCone(int seg)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            var apex = new Vector3(0, 1, 0);
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
                var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f);
                var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f);
                int b = v.Count; v.Add(p0); v.Add(apex); v.Add(p1);
                t.Add(b); t.Add(b + 1); t.Add(b + 2);
                b = v.Count; v.Add(p0); v.Add(p1); v.Add(Vector3.zero);
                t.Add(b); t.Add(b + 1); t.Add(b + 2);
            }
            var m = new Mesh { name = "cp_cone" };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        // Triangular prism for roofs: ridge along x at y=1, base at y=0 from z=-0.5 to 0.5.
        static Mesh BuildPrism()
        {
            Vector3 a = new Vector3(-0.5f, 0, -0.5f), b = new Vector3(-0.5f, 0, 0.5f), c = new Vector3(-0.5f, 1, 0);
            Vector3 d = new Vector3(0.5f, 0, -0.5f), e = new Vector3(0.5f, 0, 0.5f), f = new Vector3(0.5f, 1, 0);
            var v = new List<Vector3>(); var t = new List<int>();
            void Tri(Vector3 p, Vector3 q, Vector3 r) { int i = v.Count; v.Add(p); v.Add(q); v.Add(r); t.Add(i); t.Add(i + 1); t.Add(i + 2); }
            Tri(a, c, b); Tri(d, e, f);
            Tri(a, d, f); Tri(a, f, c);
            Tri(b, c, f); Tri(b, f, e);
            Tri(a, b, e); Tri(a, e, d);
            var m = new Mesh { name = "cp_prism" };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static Texture2D BuildSoftDot(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "cp_softdot" };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1 - r);
                    px[y * n + x] = new Color(1, 1, 1, a * a * (3 - 2 * a));
                }
            tex.SetPixels(px); tex.Apply();
            return tex;
        }
    }
}
