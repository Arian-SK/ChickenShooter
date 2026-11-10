using UnityEngine;

namespace Cluck
{
    [RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public sealed class PlayerShip : MonoBehaviour
    {
        const float Speed = 7.8f, Immunity = 1.65f;
        Arena arena;
        GameManager state;
        SpriteRenderer hull, shield, exhaust;
        float immune, cooldown, recoil, bank, fireBuffer;
        internal Vector2 Position => transform.position;

        internal void Setup(Arena arena, GameManager state)
        {
            this.arena = arena;
            this.state = state;
            var data = state.Campaign;
            hull = GetComponent<SpriteRenderer>();
            hull.sprite = data.ship;
            hull.sharedMaterial = data.spriteMaterial;
            hull.sortingOrder = 12;
            shield = Visual("Shield", data.shield, 13);
            shield.enabled = false;
            exhaust = Visual("Exhaust", data.flare, 11);
            exhaust.transform.localPosition = new Vector3(0, -.52f);
            exhaust.color = new Color(.32f, .88f, 1, .9f);
            state.Damaged += OnDamage;
        }

        SpriteRenderer Visual(string label, Sprite sprite, int order)
        {
            var view = new GameObject(label).AddComponent<SpriteRenderer>();
            view.transform.SetParent(transform, false);
            view.sprite = sprite;
            view.sharedMaterial = state.Campaign.spriteMaterial;
            view.sortingOrder = order;
            return view;
        }

        internal void Step(Vector2 axis, bool fireHeld, bool firePressed, float dt)
        {
            immune = Mathf.Max(0, immune - dt);
            cooldown = Mathf.Max(0, cooldown - dt);
            fireBuffer = firePressed ? .17f : Mathf.Max(0, fireBuffer - dt);
            recoil = Mathf.MoveTowards(recoil, 0, dt * 6);
            bank = Mathf.Lerp(bank, -axis.x * 13, dt * 11);
            var next = Position + Vector2.ClampMagnitude(axis, 1) * (Speed * dt);
            transform.SetPositionAndRotation(arena.ClampShip(next), Quaternion.Euler(0, 0, bank));
            exhaust.transform.localScale = new Vector3(.55f, .70f + Mathf.Abs(axis.y) * .3f + Mathf.Sin(Time.time * 45) * .15f + recoil * .3f, 1);
            bool blinking = immune > 0 && Mathf.FloorToInt(immune * 10) % 2 == 0;
            hull.color = blinking ? new Color(.65f, .95f, 1, .64f) : Color.white;
            shield.enabled = immune > 0;
            shield.color = new Color(.28f, .93f, 1, blinking ? .68f : .25f);
            if ((fireHeld || fireBuffer > 0) && cooldown <= 0)
            {
                var weapon = state.Campaign.weaponTiers[state.Power - 1];
                cooldown = 1 / weapon.shotsPerSecond;
                fireBuffer = 0;
                recoil = 1;
                arena.Fire(weapon, Position + Vector2.up * .40f);
            }
        }

        internal void KillVisual() => gameObject.SetActive(false);
        internal void CancelBufferedFire() => fireBuffer = 0;

        void OnDamage() => immune = Immunity;

        void OnTriggerEnter2D(Collider2D other) => CheckContact(other);
        void OnTriggerStay2D(Collider2D other) => CheckContact(other);

        void CheckContact(Collider2D other)
        {
            if (arena == null || !arena.CanHitPlayer || !other.TryGetComponent<PoolBody>(out var body) || !body.Active) return;
            if (body.kind != BodyKind.Egg && !arena.CombatActive) return;
            if (body.kind == BodyKind.Gift) { arena.TakeGift(body); return; }
            if (immune > 0 || (body.kind != BodyKind.Chicken && body.kind != BodyKind.Rock && body.kind != BodyKind.Egg)) return;
            arena.PlayerHit(body);
        }

        void OnDestroy() { if (state != null) state.Damaged -= OnDamage; }
    }
}
