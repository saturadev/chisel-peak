using UnityEngine;

namespace ChiselPeak
{
    /// One cube of the ice block. Static in the grid until it breaks or loses support and falls.
    public class IceChunk : MonoBehaviour
    {
        public Vector3Int Cell;
        public float Hp, MaxHp;
        public Color BaseColor;
        public float Glow;
        public bool Falling { get; private set; }
        public IceBlock Block;

        MeshRenderer mr;
        Vector3 baseScale;
        float punch;
        Rigidbody rb;
        float fallTime, shatterAt = -1, shatterDelay;
        bool countsForShards;

        public void Init(IceBlock block, Vector3Int cell, float hp, Color color, float glow)
        {
            Block = block; Cell = cell; Hp = MaxHp = hp; BaseColor = color; Glow = glow;
            mr = GetComponent<MeshRenderer>();
            baseScale = transform.localScale;
            Paint(0);
            enabled = false;
        }

        void Paint(float damage)
        {
            mr.sharedMaterial = Block.Palette(BaseColor, Glow, Mathf.CeilToInt(damage * 3f - 0.01f));
        }

        public void ShowDamage()
        {
            Paint(1f - Mathf.Clamp01(Hp / MaxHp));
            punch = 1f;
            enabled = true;
        }

        public void BeginFall(Vector3 velocity, Vector3 spin, float delay, bool shards)
        {
            if (Falling) return;
            Falling = true;
            enabled = true;
            countsForShards = shards;
            shatterDelay = delay;
            gameObject.layer = 2; // Ignore Raycast: the player's swing passes through falling ice
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 2f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.linearVelocity = velocity;
            rb.angularVelocity = spin;
            transform.localScale = baseScale;
        }

        void Update()
        {
            if (punch > 0)
            {
                punch = Mathf.Max(0, punch - Time.deltaTime * 9f);
                transform.localScale = baseScale * (1f - Mathf.Sin(punch * Mathf.PI) * 0.06f);
            }
            if (!Falling) { if (punch <= 0) enabled = false; return; }
            fallTime += Time.deltaTime;
            if (shatterAt < 0 && fallTime > 0.3f && (rb.linearVelocity.sqrMagnitude < 1.2f || transform.position.y < 0.9f))
                shatterAt = fallTime + shatterDelay;
            if ((shatterAt >= 0 && fallTime >= shatterAt) || fallTime > 5f || transform.position.y < -5f)
                Shatter();
        }

        void OnCollisionEnter(Collision c)
        {
            if (!Falling || Fx.I == null) return;
            if (c.relativeVelocity.sqrMagnitude > 25f && Random.value < 0.35f)
                Fx.I.IceHit(c.GetContact(0).point, Vector3.up, 0.3f);
        }

        void Shatter()
        {
            if (Fx.I != null) Fx.I.IceBreak(transform.position, 0.8f);
            Block.FallenShattered(this, countsForShards);
            Destroy(gameObject);
        }
    }
}
