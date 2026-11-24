using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ChiselPeak
{
    /// Bright cartoon snowfield: sky, sun, snow, cliffs, pines, a little camp, crates and pebbles.
    public static class World
    {
        public static Transform Build(Transform root, Shader skyShader)
        {
            var rng = new System.Random(3);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            // sky and light
            var sky = new Material(skyShader != null ? skyShader : Shader.Find("Skybox/Procedural"));
            sky.SetColor("_SkyTint", new Color(0.36f, 0.58f, 1f));
            sky.SetColor("_GroundColor", new Color(0.85f, 0.88f, 0.95f));
            sky.SetFloat("_AtmosphereThickness", 0.8f);
            sky.SetFloat("_Exposure", 2.1f);
            sky.SetFloat("_SunSize", 0.05f);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.82f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.7f, 0.74f, 0.86f);
            RenderSettings.ambientGroundColor = new Color(0.55f, 0.55f, 0.66f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.8f, 0.88f, 1f);
            RenderSettings.fogStartDistance = 45f; RenderSettings.fogEndDistance = 190f;

            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(root, false);
            sunGo.transform.rotation = Quaternion.Euler(38f, 28f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1f, 0.95f, 0.86f); sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.6f;
            RenderSettings.sun = sun;

            // post
            var volGo = new GameObject("Post"); volGo.transform.SetParent(root, false);
            var vol = volGo.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 10;
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = prof.Add<Bloom>(true); bloom.intensity.Override(0.45f); bloom.threshold.Override(1.05f);
            var ca = prof.Add<ColorAdjustments>(true); ca.saturation.Override(18f); ca.contrast.Override(6f); ca.postExposure.Override(0.1f);
            var vig = prof.Add<Vignette>(true); vig.intensity.Override(0.18f);
            var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.Neutral);
            vol.sharedProfile = prof;

            // ground
            var snow = Art.Lit(new Color(0.93f, 0.93f, 1f), 0.25f);
            var groundGo = Art.Part(Art.Cube, root, new Vector3(0, -0.5f, 0), new Vector3(500, 1, 500), snow, default, "Ground");
            groundGo.AddComponent<BoxCollider>();
            var groundPlane = new GameObject("GroundPlane").transform; groundPlane.SetParent(root, false);

            var mounds = new GameObject("Mounds").transform; mounds.SetParent(root, false);
            var snowShade = Art.Lit(new Color(0.88f, 0.89f, 1f), 0.25f);
            for (int n = 0; n < 40; n++)
            {
                float a = R(0, Mathf.PI * 2), d = R(11, 70);
                var p = new Vector3(Mathf.Cos(a) * d, -0.2f, Mathf.Sin(a) * d);
                if (p.z < -6 && Mathf.Abs(p.x) < 6) continue;
                float s = R(3, 9);
                Art.Part(Art.Sphere, mounds, p, new Vector3(s, R(0.8f, 1.6f), s * R(0.7f, 1.3f)), n % 2 == 0 ? snow : snowShade, default, "Mound");
            }

            // cliffs
            var rock = Art.Lit(new Color(0.66f, 0.47f, 0.37f), 0.1f);
            var rockDark = Art.Lit(new Color(0.52f, 0.36f, 0.3f), 0.1f);
            var cliffs = new GameObject("Cliffs").transform; cliffs.SetParent(root, false);
            for (int n = 0; n < 34; n++)
            {
                float a = n / 34f * Mathf.PI * 2 + R(-0.05f, 0.05f), d = R(60, 85);
                var p = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                var c = new GameObject("Cliff").transform; c.SetParent(cliffs, false); c.localPosition = p;
                c.localEulerAngles = new Vector3(0, R(0, 360), 0);
                int stack = rng.Next(2, 4); float y = 0, w = R(12, 20);
                for (int s = 0; s < stack; s++)
                {
                    float h = R(6, 12);
                    Art.Part(Art.Cube, c, new Vector3(R(-2, 2), y + h * 0.5f, R(-2, 2)), new Vector3(w, h, w * R(0.6f, 1f)), s % 2 == 0 ? rock : rockDark,
                        new Vector3(R(-4, 4), R(0, 30), R(-4, 4)), "Rock");
                    y += h * 0.9f; w *= R(0.6f, 0.85f);
                }
                Art.Part(Art.Sphere, c, new Vector3(0, y + 0.4f, 0), new Vector3(w * 1.25f, 2.4f, w * 1.05f), snow, default, "Cap");
            }

            // pines
            var pine = Art.Lit(new Color(0.12f, 0.45f, 0.38f), 0.1f);
            var trunk = Art.Lit(new Color(0.45f, 0.28f, 0.16f), 0.1f);
            var trees = new GameObject("Pines").transform; trees.SetParent(root, false);
            for (int n = 0; n < 60; n++)
            {
                float a = R(0, Mathf.PI * 2), d = R(24, 58);
                var p = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                if (p.z < -10 && Mathf.Abs(p.x) < 14) continue; // keep the view behind the player clear
                float s = R(0.8f, 1.6f);
                var t = new GameObject("Pine").transform; t.SetParent(trees, false); t.localPosition = p; t.localScale = Vector3.one * s;
                Art.Part(Art.Cylinder, t, new Vector3(0, 0.8f, 0), new Vector3(0.5f, 0.8f, 0.5f), trunk, default, "Trunk");
                for (int k = 0; k < 3; k++)
                {
                    float w = 3.4f - k * 0.9f;
                    Art.Part(Art.Cone, t, new Vector3(0, 1.4f + k * 1.5f, 0), new Vector3(w, 2.6f, w), pine, default, "Leaves");
                    Art.Part(Art.Cone, t, new Vector3(0, 2.6f + k * 1.5f, 0), new Vector3(w * 0.45f, 1.3f, w * 0.45f), snow, default, "Snow");
                }
            }

            // clouds
            var cloud = Art.Lit(new Color(1f, 1f, 1f), 0f, 0.35f);
            var clouds = new GameObject("Clouds").transform; clouds.SetParent(root, false);
            for (int n = 0; n < 14; n++)
            {
                float a = R(0, Mathf.PI * 2), d = R(120, 170);
                var c = new GameObject("Cloud").transform; c.SetParent(clouds, false);
                c.localPosition = new Vector3(Mathf.Cos(a) * d, R(35, 70), Mathf.Sin(a) * d);
                c.LookAt(new Vector3(0, c.localPosition.y, 0));
                for (int k = 0; k < 5; k++)
                {
                    float s = R(10, 20);
                    Art.Part(Art.Sphere, c, new Vector3(R(-18, 18), R(-3, 3), R(-3, 3)), new Vector3(s * 1.4f, s * 0.8f, s), cloud, default, "Puff", false);
                }
            }

            // camp to the left: hut, banner, fire
            var camp = new GameObject("Camp").transform; camp.SetParent(root, false);
            camp.localPosition = new Vector3(-15f, 0, -3f); camp.localEulerAngles = new Vector3(0, 70, 0);
            var plank = Art.Lit(new Color(0.6f, 0.38f, 0.22f), 0.15f);
            var plankDark = Art.Lit(new Color(0.42f, 0.25f, 0.15f), 0.15f);
            Art.Part(Art.Cube, camp, new Vector3(0, 1.6f, 0), new Vector3(5.5f, 3.2f, 4.2f), plank, default, "Hut");
            Art.Part(Art.Cube, camp, new Vector3(0, 1.1f, -2.12f), new Vector3(1.3f, 2.2f, 0.1f), plankDark, default, "Door");
            Art.Part(Art.Prism, camp, new Vector3(0, 3.2f, 0), new Vector3(6.4f, 2.2f, 5.2f), plankDark, default, "Roof");
            Art.Part(Art.Prism, camp, new Vector3(0, 3.55f, 0), new Vector3(6.6f, 2.0f, 5.0f), snow, default, "RoofSnow");
            Art.Part(Art.Cylinder, camp, new Vector3(3.6f, 2.2f, -2.4f), new Vector3(0.15f, 2.2f, 0.15f), plankDark, default, "Pole");
            Art.Part(Art.Cube, camp, new Vector3(3.6f, 3.0f, -1.85f), new Vector3(0.05f, 1.6f, 1.0f), Art.Lit(new Color(1f, 0.85f, 0.55f), 0.1f), default, "Flag");
            Art.Part(Art.Sphere, camp, new Vector3(3.57f, 3.05f, -1.85f), new Vector3(0.05f, 0.7f, 0.5f), Art.Lit(new Color(0.95f, 0.45f, 0.12f), 0.1f), default, "Emblem");
            for (int k = 0; k < 4; k++)
                Art.Part(Art.Cylinder, camp, new Vector3(0.5f, 0.15f, -4.2f), new Vector3(0.22f, 0.7f, 0.22f), plankDark, new Vector3(90, k * 45, 0), "Log");
            Art.Part(Art.Sphere, camp, new Vector3(0.5f, 0.3f, -4.2f), new Vector3(0.6f, 0.4f, 0.6f), Art.Lit(new Color(1f, 0.55f, 0.15f), 0.1f, 2.5f), default, "Embers");
            Fx.Fire(camp, new Vector3(0.5f, 0.35f, -4.2f));
            var fireLight = new GameObject("FireLight").AddComponent<Light>();
            fireLight.transform.SetParent(camp, false); fireLight.transform.localPosition = new Vector3(0.5f, 1.2f, -4.2f);
            fireLight.type = LightType.Point; fireLight.color = new Color(1f, 0.6f, 0.25f); fireLight.range = 6f; fireLight.intensity = 3f;

            // crates and pebbles near the ice
            var crateMat = Art.Lit(new Color(0.55f, 0.32f, 0.2f), 0.1f);
            Crate(root, new Vector3(-3.2f, 0, -7.2f), 20, crateMat, plankDark);
            Crate(root, new Vector3(4.8f, 0, -6.4f), -15, crateMat, plankDark);
            var pebble = Art.Lit(new Color(0.48f, 0.38f, 0.34f), 0.1f);
            for (int n = 0; n < 26; n++)
            {
                float a = R(0, Mathf.PI * 2), d = R(7, 14);
                var p = new Vector3(Mathf.Cos(a) * d, 0.05f, Mathf.Sin(a) * d * 0.8f);
                float s = R(0.2f, 0.45f);
                Art.Part(Art.Sphere, root, p, new Vector3(s, s * 0.6f, s), pebble, new Vector3(0, R(0, 180), 0), "Pebble");
            }
            return groundPlane;
        }

        static void Crate(Transform root, Vector3 p, float yaw, Material m, Material dark)
        {
            var c = new GameObject("Crate").transform; c.SetParent(root, false); c.localPosition = p; c.localEulerAngles = new Vector3(0, yaw, 0);
            Art.Part(Art.Cube, c, new Vector3(0, 0.4f, 0), new Vector3(1.1f, 0.8f, 0.8f), m, default, "Box").AddComponent<BoxCollider>();
            Art.Part(Art.Cube, c, new Vector3(0, 0.4f, -0.41f), new Vector3(1.12f, 0.12f, 0.03f), dark, default, "Band");
            Art.Part(Art.Cube, c, new Vector3(0, 0.8f, 0), new Vector3(1.0f, 0.04f, 0.7f), Art.Lit(new Color(0.93f, 0.93f, 1f), 0.25f), default, "SnowTop");
        }
    }
}
