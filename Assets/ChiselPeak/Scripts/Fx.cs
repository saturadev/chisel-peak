using UnityEngine;

namespace ChiselPeak
{
    /// Pooled particle systems: ice shards, snow puffs, falling snow, steam.
    public class Fx : MonoBehaviour
    {
        public static Fx I;
        ParticleSystem shards, puff, sparkle, steam;

        public void Build(Transform ground)
        {
            I = this;
            var shardMat = Art.ParticleMat(new Color(0.82f, 0.94f, 1f), null, false);
            var puffMat = Art.ParticleMat(new Color(1f, 1f, 1f, 0.9f), Art.SoftDot, true);
            var sparkMat = Art.ParticleMat(new Color(1f, 1f, 1f, 1f), Art.SoftDot, true);

            shards = Make("IceShards", shardMat, 700, true);
            var m = shards.main; m.gravityModifier = 1.7f;
            var col = shards.collision; col.enabled = true; col.type = ParticleSystemCollisionType.Planes;
            col.SetPlane(0, ground); col.dampen = 0.45f; col.bounce = 0.25f; col.lifetimeLoss = 0f;
            var sol = shards.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.75f, 1), new Keyframe(1, 0)));

            puff = Make("SnowPuff", puffMat, 300, false);
            var pm = puff.main; pm.gravityModifier = -0.04f;
            var lv = puff.limitVelocityOverLifetime; lv.enabled = true; lv.drag = 2.5f; lv.limit = 100f;
            var ps = puff.sizeOverLifetime; ps.enabled = true;
            ps.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 1.6f)));
            var pc = puff.colorOverLifetime; pc.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(0.85f, 0.92f, 1f), 1) },
                      new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0, 1) });
            pc.color = g;

            sparkle = Make("Sparkle", sparkMat, 200, false);
            var sc = sparkle.colorOverLifetime; sc.enabled = true;
            var g2 = new Gradient();
            g2.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(0.7f, 0.9f, 1f), 1) },
                       new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            sc.color = g2;
            var sr = sparkle.GetComponent<ParticleSystemRenderer>(); sr.renderMode = ParticleSystemRenderMode.Stretch; sr.velocityScale = 0.06f; sr.lengthScale = 1f;

            steam = Make("Steam", puffMat, 200, false);
            var stm = steam.main; stm.gravityModifier = -0.15f;
            var ss = steam.sizeOverLifetime; ss.enabled = true;
            ss.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.4f), new Keyframe(1, 2f)));
            var stc = steam.colorOverLifetime; stc.enabled = true; stc.color = g;
        }

        ParticleSystem Make(string name, Material mat, int max, bool meshShards)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var p = go.AddComponent<ParticleSystem>();
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = p.main;
            main.playOnAwake = false; main.loop = false; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0; main.startLifetime = 1;
            if (meshShards) { main.startRotation3D = true; main.startSize3D = false; }
            var em = p.emission; em.enabled = false;
            var sh = p.shape; sh.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            if (meshShards)
            {
                r.renderMode = ParticleSystemRenderMode.Mesh;
                r.mesh = Art.Cube;
                r.alignment = ParticleSystemRenderSpace.Local;
            }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            p.Play();
            return p;
        }

        public void IceHit(Vector3 p, Vector3 n, float power)
        {
            int count = Mathf.RoundToInt(5 + power * 9);
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                ep.position = p + Random.insideUnitSphere * 0.15f;
                ep.velocity = n * Random.Range(1.5f, 4.5f) + Random.insideUnitSphere * 2.5f + Vector3.up * 1.5f;
                ep.startSize = Random.Range(0.05f, 0.14f) * (0.8f + power * 0.4f);
                ep.rotation3D = new Vector3(Random.value * 360, Random.value * 360, Random.value * 360);
                ep.angularVelocity3D = Random.insideUnitSphere * 400;
                ep.startLifetime = Random.Range(0.7f, 1.3f);
                ep.startColor = Color.Lerp(Color.white, new Color(0.7f, 0.88f, 1f), Random.value);
                shards.Emit(ep, 1);
            }
            var pp = new ParticleSystem.EmitParams();
            for (int i = 0; i < 2 + power * 2; i++)
            {
                pp.position = p + n * 0.1f + Random.insideUnitSphere * 0.2f;
                pp.velocity = n * Random.Range(0.6f, 1.8f) + Random.insideUnitSphere * 0.7f;
                pp.startSize = Random.Range(0.35f, 0.7f) * (0.8f + power * 0.5f);
                pp.startLifetime = Random.Range(0.5f, 0.9f);
                puff.Emit(pp, 1);
            }
            var sp = new ParticleSystem.EmitParams();
            for (int i = 0; i < 6; i++)
            {
                sp.position = p + n * 0.05f;
                sp.velocity = (n + Random.insideUnitSphere * 1.2f).normalized * Random.Range(4f, 8f);
                sp.startSize = Random.Range(0.04f, 0.08f);
                sp.startLifetime = Random.Range(0.12f, 0.25f);
                sparkle.Emit(sp, 1);
            }
        }

        public void IceBreak(Vector3 p, float power)
        {
            int count = Mathf.RoundToInt(10 + power * 8);
            var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                ep.position = p + Random.insideUnitSphere * 0.45f;
                ep.velocity = Random.insideUnitSphere * 3.5f * (0.7f + power * 0.3f) + Vector3.up * 2.5f;
                ep.startSize = Random.Range(0.1f, 0.3f);
                ep.rotation3D = new Vector3(Random.value * 360, Random.value * 360, Random.value * 360);
                ep.angularVelocity3D = Random.insideUnitSphere * 300;
                ep.startLifetime = Random.Range(1.0f, 1.8f);
                ep.startColor = Color.Lerp(new Color(0.85f, 0.95f, 1f), new Color(0.45f, 0.75f, 1f), Random.value);
                shards.Emit(ep, 1);
            }
            var pp = new ParticleSystem.EmitParams();
            for (int i = 0; i < 3; i++)
            {
                pp.position = p + Random.insideUnitSphere * 0.4f;
                pp.velocity = Random.insideUnitSphere * 1.2f;
                pp.startSize = Random.Range(0.8f, 1.4f);
                pp.startLifetime = Random.Range(0.7f, 1.1f);
                puff.Emit(pp, 1);
            }
        }

        public void Steam(Vector3 p, Vector3 dir, int count)
        {
            var pp = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                pp.position = p + Random.insideUnitSphere * 0.2f;
                pp.velocity = dir * Random.Range(1.5f, 3f) + Random.insideUnitSphere * 0.5f;
                pp.startSize = Random.Range(0.5f, 0.9f);
                pp.startLifetime = Random.Range(0.8f, 1.4f);
                steam.Emit(pp, 1);
            }
        }

        public static ParticleSystem Snowfall(Transform follow)
        {
            var go = new GameObject("Snowfall");
            go.transform.SetParent(follow, false);
            go.transform.localPosition = new Vector3(0, 10, 6);
            var p = go.AddComponent<ParticleSystem>();
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = p.main;
            main.loop = true; main.playOnAwake = true; main.maxParticles = 900;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 9f; main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
            main.gravityModifier = 0.03f;
            main.prewarm = true;
            var em = p.emission; em.rateOverTime = 90;
            var sh = p.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(30, 1, 30);
            var noise = p.noise; noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.3f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Art.ParticleMat(new Color(1, 1, 1, 0.95f), Art.SoftDot, true);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            p.Play();
            return p;
        }

        public static ParticleSystem Fire(Transform parent, Vector3 pos)
        {
            var go = new GameObject("Campfire");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var p = go.AddComponent<ParticleSystem>();
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = p.main; main.loop = true; main.maxParticles = 120;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.1f), new Color(1f, 0.85f, 0.25f));
            var em = p.emission; em.rateOverTime = 40;
            var sh = p.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12; sh.radius = 0.35f;
            sh.rotation = new Vector3(-90, 0, 0);
            var sol = p.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Art.ParticleMat(Color.white, Art.SoftDot, true);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            p.Play();
            return p;
        }
    }
}
