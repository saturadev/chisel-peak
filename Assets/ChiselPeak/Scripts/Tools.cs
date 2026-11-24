using UnityEngine;

namespace ChiselPeak
{
    public class ToolDef
    {
        public string Name;
        public float Damage, Radius, Reach, SwingTime, Shake, Power;
        public int Cost;
        public bool Thrust;
    }

    public static class Tools
    {
        public static readonly ToolDef[] All =
        {
            new ToolDef { Name = "Stone Hammer",  Damage = 1.4f, Radius = 0f,   Reach = 3.4f, SwingTime = 0.36f, Shake = 0.05f, Power = 0.35f, Cost = 0 },
            new ToolDef { Name = "Pickaxe",       Damage = 3f,   Radius = 1.35f, Reach = 3.5f, SwingTime = 0.38f, Shake = 0.09f, Power = 0.6f,  Cost = 2 },
            new ToolDef { Name = "Heavy Mallet",  Damage = 5f,   Radius = 1.9f, Reach = 3.7f, SwingTime = 0.5f, Shake = 0.2f,  Power = 1f,    Cost = 6 },
            new ToolDef { Name = "Battering Ram", Damage = 8f, Radius = 2.4f, Reach = 4.3f, SwingTime = 0.70f, Shake = 0.38f, Power = 1.6f,  Cost = 14, Thrust = true },
        };

        static readonly Color Wood = new Color(0.58f, 0.36f, 0.18f);
        static readonly Color WoodLight = new Color(0.8f, 0.6f, 0.36f);
        static readonly Color Stone = new Color(0.56f, 0.57f, 0.62f);
        static readonly Color Flint = new Color(0.36f, 0.4f, 0.5f);
        static readonly Color Rope = new Color(0.86f, 0.72f, 0.46f);
        static readonly Color Metal = new Color(0.42f, 0.44f, 0.5f);
        static readonly Color Mitten = new Color(0.47f, 0.27f, 0.15f);
        static readonly Color Fur = new Color(0.93f, 0.86f, 0.74f);

        /// First-person model. Grip at the origin, handle along +y (ram: log along +z).
        public static GameObject BuildView(int index, Transform parent)
        {
            var root = new GameObject(All[index].Name);
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * (index == 3 ? 0.42f : 0.62f);
            var t = root.transform;
            var wood = Art.Lit(Wood, 0.2f); var woodL = Art.Lit(WoodLight, 0.2f);
            var rope = Art.Lit(Rope, 0.1f); var metal = Art.Lit(Metal, 0.6f);
            switch (index)
            {
                case 0:
                    P(Art.Cylinder, t, new Vector3(0, 0.3f, 0), new Vector3(0.065f, 0.42f, 0.065f), wood);
                    P(Art.Cube, t, new Vector3(0, 0.72f, 0.04f), new Vector3(0.2f, 0.2f, 0.38f), Art.Lit(Stone, 0.25f), new Vector3(4, 0, 0));
                    P(Art.Cube, t, new Vector3(0, 0.72f, 0.04f), new Vector3(0.215f, 0.215f, 0.1f), rope);
                    P(Art.Cube, t, new Vector3(0, 0.6f, 0f), new Vector3(0.09f, 0.08f, 0.09f), rope);
                    break;
                case 1:
                    P(Art.Cylinder, t, new Vector3(0, 0.35f, 0), new Vector3(0.06f, 0.5f, 0.06f), wood);
                    P(Art.Cube, t, new Vector3(0, 0.84f, 0.2f), new Vector3(0.08f, 0.09f, 0.42f), Art.Lit(Flint, 0.5f), new Vector3(14, 0, 0));
                    P(Art.Cone, t, new Vector3(0, 0.79f, 0.4f), new Vector3(0.09f, 0.18f, 0.09f), Art.Lit(Flint, 0.5f), new Vector3(100, 0, 0));
                    P(Art.Cube, t, new Vector3(0, 0.84f, -0.12f), new Vector3(0.08f, 0.09f, 0.24f), Art.Lit(Flint, 0.5f), new Vector3(-14, 0, 0));
                    P(Art.Cube, t, new Vector3(0, 0.84f, 0f), new Vector3(0.11f, 0.13f, 0.12f), rope);
                    break;
                case 2:
                    P(Art.Cylinder, t, new Vector3(0, 0.36f, 0), new Vector3(0.075f, 0.52f, 0.075f), wood);
                    P(Art.Cylinder, t, new Vector3(0, 0.9f, 0.02f), new Vector3(0.38f, 0.26f, 0.38f), woodL, new Vector3(90, 0, 0));
                    P(Art.Cylinder, t, new Vector3(0, 0.9f, 0.24f), new Vector3(0.41f, 0.035f, 0.41f), metal, new Vector3(90, 0, 0));
                    P(Art.Cylinder, t, new Vector3(0, 0.9f, -0.2f), new Vector3(0.41f, 0.035f, 0.41f), metal, new Vector3(90, 0, 0));
                    break;
                default:
                    P(Art.Cylinder, t, new Vector3(-0.05f, 0.05f, 0.7f), new Vector3(0.34f, 0.85f, 0.34f), woodL, new Vector3(90, 0, 0));
                    P(Art.Cone, t, new Vector3(-0.05f, 0.05f, 1.55f), new Vector3(0.36f, 0.32f, 0.36f), metal, new Vector3(90, 0, 0));
                    P(Art.Cylinder, t, new Vector3(-0.05f, 0.05f, 1.4f), new Vector3(0.38f, 0.04f, 0.38f), metal, new Vector3(90, 0, 0));
                    P(Art.Cylinder, t, new Vector3(-0.05f, 0.05f, 0.15f), new Vector3(0.38f, 0.04f, 0.38f), metal, new Vector3(90, 0, 0));
                    P(Art.Cylinder, t, new Vector3(-0.05f, 0.05f, 0.65f), new Vector3(0.36f, 0.06f, 0.36f), rope, new Vector3(90, 0, 0));
                    // grips
                    P(Art.Cylinder, t, new Vector3(-0.05f, -0.2f, 0.3f), new Vector3(0.05f, 0.12f, 0.05f), wood);
                    P(Art.Cylinder, t, new Vector3(-0.05f, -0.2f, 1.0f), new Vector3(0.05f, 0.12f, 0.05f), wood);
                    Hand(t, new Vector3(-0.05f, -0.32f, 1.0f));
                    break;
            }
            Hand(t, index == 3 ? new Vector3(-0.05f, -0.32f, 0.3f) : Vector3.zero);
            Art.SetLayer(root, PlayerRig.ViewLayer);
            return root;
        }

        static void Hand(Transform t, Vector3 at)
        {
            P(Art.Sphere, t, at + new Vector3(0, 0.02f, 0), new Vector3(0.19f, 0.2f, 0.19f), Art.Lit(Mitten, 0.2f));
            P(Art.Cylinder, t, at + new Vector3(0.02f, -0.12f, -0.02f), new Vector3(0.17f, 0.06f, 0.17f), Art.Lit(Fur, 0.05f));
        }

        static void P(Mesh m, Transform t, Vector3 p, Vector3 s, Material mat, Vector3 e = default)
            => Art.Part(m, t, p, s, mat, e, "p", false);
    }
}
