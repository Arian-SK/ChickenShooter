using UnityEngine;

namespace Cluck
{
    public enum BodyKind { Laser, Egg, Chicken, Rock, Gift, Spark }

    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PoolBody : MonoBehaviour
    {
        public BodyKind kind;
        [SerializeField] Sprite mildlyHurt;
        [SerializeField] Sprite hurt;
        [SerializeField] Sprite badlyHurt;
        public SpriteRenderer Art { get; private set; }
        public Rigidbody2D Rigidbody { get; private set; }
        public int Health { get; private set; }
        public bool Active { get; internal set; }
        internal BodyPool Owner;
        internal Arena Arena;
        internal Vector2 Position { get => transform.position; set => transform.position = value; }
        internal Vector2 Velocity, Origin, Slot, Target;
        internal float Age, Life, Delay, Travel, Spin, Flash, PhaseOffset, VisualScale, ReleaseTime;
        internal int Damage, Mode;
        internal Color Tint;
        Sprite healthy;
        int maxHealth;

        void Awake()
        {
            Art = GetComponent<SpriteRenderer>();
            healthy = Art.sprite;
            Rigidbody = GetComponent<Rigidbody2D>();
        }

        internal void Activate(Arena arena, Vector2 position, float life)
        {
            Arena = arena;
            Damage = 1;
            ResetHealth(1);
            Mode = 0;
            Age = Delay = Travel = Spin = Flash = PhaseOffset = ReleaseTime = 0;
            Life = life;
            Velocity = Origin = Slot = Target = Vector2.zero;
            Tint = Color.white;
            VisualScale = 1;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            transform.localScale = Vector3.one;
            Active = true;
            gameObject.SetActive(true);
            Art.color = Tint;
        }

        internal void ResetHealth(int health)
        {
            Health = maxHealth = health;
            Art.sprite = healthy;
        }

        internal void ApplyDamage(int damage)
        {
            Health = Mathf.Max(0, Health - damage);
            if (Health == 0 || kind is not (BodyKind.Chicken or BodyKind.Rock)) return;
            int damageTaken = maxHealth - Health;
            long stage = kind == BodyKind.Rock
                ? Mathf.Min(damageTaken, 2)
                : (long)damageTaken * 4 / maxHealth;
            var damaged = stage switch
            {
                1 => mildlyHurt,
                2 => hurt,
                3 => badlyHurt,
                _ => healthy
            };
            Art.sprite = damaged != null ? damaged : healthy;
        }

        internal void SetDamageSprites(Sprite mild, Sprite moderate, Sprite severe)
        {
            mildlyHurt = mild;
            hurt = moderate;
            badlyHurt = severe;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Active && Arena != null && kind == BodyKind.Laser &&
                other.TryGetComponent<PoolBody>(out var target) &&
                target.kind is BodyKind.Chicken or BodyKind.Rock) Arena.LaserHit(this, target);
        }
    }
}
