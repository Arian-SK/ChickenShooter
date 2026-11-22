using UnityEngine;

namespace Cluck
{
    internal sealed class BossFight
    {
        enum Phase { Arrival, Volley, Warning, Dive, Return, Enraged, Defeated }
        readonly Arena arena;
        readonly BossDataSO data;
        readonly PoolBody body;
        Phase phase;
        float elapsed, shotAt, total;
        int volleys;
        Vector2 from, destination;
        static readonly Vector2 Perch = new(0, 2.38f);

        internal BossFight(Arena arena, BossDataSO data, PoolBody body)
        {
            this.arena = arena;
            this.data = data;
            this.body = body;
            from = body.Position;
        }

        internal bool Alive => phase != Phase.Defeated;
        internal string Status => phase switch
        {
            Phase.Warning or Phase.Dive => "DIVE ATTACK",
            Phase.Enraged => "ENRAGED",
            Phase.Arrival => "INCOMING",
            _ => "EGG BARRAGE"
        };

        internal void Step(float dt, Vector2 player)
        {
            elapsed += dt;
            total += dt;
            switch (phase)
            {
                case Phase.Arrival:
                    body.Position = Vector2.Lerp(from, Perch, Mathf.SmoothStep(0, 1, elapsed / 1.35f));
                    if (elapsed >= 1.35f) Enter(Phase.Volley);
                    break;
                case Phase.Volley:
                    Hover();
                    if (elapsed < shotAt) break;
                    arena.BossSpread(body.Position + Vector2.down * .64f, player, data.volleySize, 13, data.projectileSpeed, false);
                    shotAt += data.volleyInterval;
                    if (++volleys >= 3) Enter(Phase.Warning);
                    break;
                case Phase.Warning:
                    body.Position = from + Vector2.right * (Mathf.Sin(elapsed * 70) * .14f);
                    body.Art.color = Color.Lerp(Color.white, data.accent, Mathf.PingPong(elapsed * 5, 1));
                    if (elapsed >= .72f)
                    {
                        destination = new Vector2(Mathf.Clamp(player.x, -6.8f, 6.8f), -3f);
                        Enter(Phase.Dive);
                        arena.BossSpread(body.Position, destination, 3, 21, data.projectileSpeed * 1.12f, true);
                    }
                    break;
                case Phase.Dive:
                    body.Position = Vector2.Lerp(from, destination, Mathf.SmoothStep(0, 1, elapsed / .74f));
                    if (elapsed >= .74f) Enter(Phase.Return);
                    break;
                case Phase.Return:
                    body.Position = Vector2.Lerp(from, Perch, Mathf.SmoothStep(0, 1, elapsed / .94f));
                    if (elapsed >= .94f) Enter(Phase.Volley);
                    break;
                case Phase.Enraged:
                    var hover = Perch + new Vector2(Mathf.Sin(total * 1.44f) * data.sweepWidth, 0);
                    body.Position = Vector2.MoveTowards(body.Position, hover, dt * 7f);
                    if (elapsed < shotAt) break;
                    arena.BossSpread(body.Position + Vector2.down * .65f, player, data.volleySize, 14, data.projectileSpeed * 1.12f, false);
                    shotAt += data.volleyInterval * .62f;
                    break;
            }
            if (body.Flash > 0)
            {
                body.Flash = Mathf.Max(0, body.Flash - dt);
                body.Art.color = body.Flash > 0 ? new Color(1.7f, 1.4f, 1.1f) : body.Tint;
            }
        }

        void Hover() => body.Position = Perch + new Vector2(Mathf.Sin(total * 1.1f) * data.sweepWidth, Mathf.Sin(total * 2f) * .14f);

        void Enter(Phase next)
        {
            phase = next;
            elapsed = 0;
            shotAt = .52f;
            volleys = 0;
            from = body.Position;
            body.Art.color = body.Tint;
            arena.ReportBoss(data, body.Health, Status);
        }

        internal void Hit(int damage)
        {
            if (!Alive) return;
            body.ApplyDamage(damage);
            if (body.Health == 0)
            {
                Enter(Phase.Defeated);
                arena.DefeatBoss(body);
                return;
            }
            if (phase != Phase.Enraged && body.Health < data.health * .30f)
            {
                body.Tint = Color.Lerp(Color.white, data.accent, .34f);
                Enter(Phase.Enraged);
                arena.SummonEscorts(data.escorts);
            }
            body.Flash = .1f;
            arena.ReportBoss(data, body.Health, Status);
        }
    }
}
