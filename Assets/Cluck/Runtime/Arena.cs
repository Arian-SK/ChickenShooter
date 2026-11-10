using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Cluck
{
    public sealed class Arena : MonoBehaviour
    {
        public const float HalfWidth = 8.8f, HalfHeight = 4.95f;
        internal bool CombatActive => active && !Paused && state.Lives > 0;
        internal bool CanHitPlayer => !Paused && state.Lives > 0;
        internal bool Paused { get; set; }
        internal int Remaining => wave == null ? 0 : wave.count - resolved;
        internal bool Finished => active && resolved == wave.count;
        internal float Shake { get; private set; }
        internal event Action<int> TargetsChanged;
        internal event Action<bool> GiftCollected;
        internal enum PulseResult { NoTargets, Empty, Fired }

        BodyPool pool;
        GameManager state;
        CampaignDataSO data;
        PlayerShip ship;
        SpriteRenderer ring;
        WaveDataSO wave;
        LevelDataSO level;
        GameAudio sound;
        bool active, gifted;
        int spawned, resolved, kills;
        float spawnAt, eggAt, diveAt, clock, ringTime;
        int cursor, eggLane;
        const float RingDuration = .62f;

        internal void Setup(GameManager state, LevelDataSO level, GameAudio sound)
        {
            this.state = state;
            this.level = level;
            this.sound = sound;
            data = state.Campaign;
            pool = new BodyPool(transform);
            foreach (var tier in data.weaponTiers) pool.Warm(tier.projectile, 36);
            foreach (var config in level.rounds) pool.Warm(config.enemyPrefab, config.count);
            pool.Warm(data.egg, 24);
            pool.Warm(data.gift, 4);
            pool.Warm(data.spark, 80);

            var player = new GameObject("Player", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(PlayerShip));
            player.transform.SetParent(transform);
            player.transform.position = new Vector2(0, -3.7f);
            var rigid = player.GetComponent<Rigidbody2D>();
            rigid.gravityScale = 0;
            rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
            player.GetComponent<CircleCollider2D>().radius = .27f;
            player.GetComponent<CircleCollider2D>().isTrigger = true;
            ship = player.GetComponent<PlayerShip>();
            ship.Setup(this, state);

            ring = new GameObject("Pulse ring").AddComponent<SpriteRenderer>();
            ring.transform.SetParent(transform, false);
            ring.sprite = data.ring;
            ring.sharedMaterial = data.spriteMaterial;
            ring.sortingOrder = 8;
            ring.enabled = false;
        }

        internal void Begin(WaveDataSO config)
        {
            wave = config;
            clock = spawnAt = spawned = resolved = kills = cursor = eggLane = 0;
            eggAt = 2.8f;
            diveAt = wave.diveInterval + 1;
            gifted = false;
            active = true;
            TargetsChanged?.Invoke(Remaining);
        }

        internal void EndWave()
        {
            active = false;
            CancelBufferedFire();
            for (int i = pool.Live.Count - 1; i >= 0; i--)
            {
                var b = pool.Live[i];
                if (b.kind == BodyKind.Laser) pool.Release(b);
            }
        }

        internal void Lose()
        {
            EndWave();
            ship.KillVisual();
        }

        internal Vector2 ClampShip(Vector2 p) => new(
            Mathf.Clamp(p.x, -HalfWidth + .6f, HalfWidth - .6f),
            Mathf.Clamp(p.y, -HalfHeight + .64f, HalfHeight - 1.14f));

        internal void CancelBufferedFire() => ship.CancelBufferedFire();

        internal void Step(Vector2 axis, bool fireHeld, bool firePressed, float dt)
        {
            if (Paused) return;
            if (state.Lives > 0) ship.Step(axis, active && fireHeld, active && firePressed, dt);
            Shake = Mathf.MoveTowards(Shake, 0, dt * .80f);
            if (ringTime > 0)
            {
                ringTime = Mathf.Max(0, ringTime - dt);
                float progress = 1 - ringTime / RingDuration;
                ring.transform.localScale = Vector3.one * Mathf.Lerp(.22f, 19, 1 - (1 - progress) * (1 - progress));
                ring.color = new Color(.56f, .96f, 1, (1 - progress) * .9f);
                ring.enabled = ringTime > 0;
            }

            if (active)
            {
                clock += dt;
                if (spawned < wave.count && clock >= spawnAt)
                {
                    SpawnTarget(spawned++);
                    spawnAt = clock + wave.entryDelay;
                }
                if (wave.kind == WaveKind.Flock)
                {
                    if (clock >= eggAt)
                    {
                        TryEgg();
                        eggAt = clock + level.EggInterval(wave) * Random.Range(.78f, 1.22f);
                    }
                    if (clock >= diveAt)
                    {
                        TryDive();
                        diveAt = clock + wave.diveInterval * Random.Range(.83f, 1.16f);
                    }
                }
            }

            for (int i = pool.Live.Count - 1; i >= 0; i--)
            {
                var b = pool.Live[i];
                b.Age += dt;
                switch (b.kind)
                {
                    case BodyKind.Chicken: if (active) MoveChicken(b, dt); break;
                    case BodyKind.Rock: if (active) MoveRock(b, dt); break;
                    case BodyKind.Gift:
                        if (state.Lives > 0)
                        {
                            Vector2 delta = ship.Position - b.Position;
                            if (delta.sqrMagnitude < 4) b.Velocity = Vector2.Lerp(b.Velocity, delta.normalized * 5.2f, dt * 6);
                        }
                        b.Position += b.Velocity * dt;
                        if (state.Lives > 0 && (ship.Position - b.Position).sqrMagnitude < .39f) { TakeGift(b); continue; }
                        if (b.Position.y < -HalfHeight - .6f || b.Age > b.Life) { pool.Release(b); continue; }
                        break;
                    default:
                        b.Position += b.Velocity * dt;
                        if (b.Age > b.Life || (b.kind == BodyKind.Laser && b.Position.y > HalfHeight + .3f)
                            || (b.kind == BodyKind.Egg && b.Position.y < -HalfHeight - .5f))
                        {
                            pool.Release(b);
                            continue;
                        }
                        if (b.kind == BodyKind.Spark)
                        {
                            var tint = b.Tint;
                            tint.a *= 1 - b.Age / b.Life;
                            b.Art.color = tint;
                        }
                        break;
                }
                if (!b.Active) continue;
                if (b.Flash > 0)
                {
                    b.Flash -= dt;
                    b.Art.color = b.Flash > 0 ? HitTint : b.Tint;
                }
            }
        }

        void SpawnTarget(int i)
        {
            float side = i % 2 == 0 ? -1 : 1;
            if (wave.kind == WaveKind.Asteroids)
            {
                int lane = (i * 3 + i / 7) % 7;
                var rock = pool.Spawn(wave.enemyPrefab, this, new Vector2(-6.7f + lane * 2.23f, HalfHeight + .8f));
                rock.ResetHealth(Mathf.Max(1, wave.health));
                rock.VisualScale = Random.Range(.83f, 1.18f);
                rock.transform.localScale = Vector3.one * rock.VisualScale;
                rock.Velocity = new Vector2(side * Random.Range(.15f, .62f), -wave.travelSpeed * Random.Range(.86f, 1.16f));
                rock.Spin = Random.Range(45f, 105f) * side;
                return;
            }
            var end = FormationMath.Slot(wave, i);
            var start = new Vector2(side * wave.entryBend.x * .62f, HalfHeight + .68f);
            var bird = pool.Spawn(wave.enemyPrefab, this, start);
            bird.ResetHealth(level.ChickenHealth(wave));
            bird.Origin = start;
            bird.Slot = end;
            bird.PhaseOffset = i * .51f;
            bird.Target = new Vector2(side * wave.entryBend.x, wave.entryBend.y - (i / wave.columns) * .24f);
            bird.Travel = Mathf.Clamp((Vector2.Distance(start, bird.Target) + Vector2.Distance(bird.Target, end)) / wave.travelSpeed, .65f, 2.7f);
        }

        Vector2 Drift(PoolBody b) => b.Slot + new Vector2(Mathf.Sin(clock * (1.06f + state.Stage * .2f)) * .92f,
            Mathf.Sin(clock * 2.7f + b.PhaseOffset) * .12f);

        void MoveChicken(PoolBody b, float dt)
        {
            switch (b.Mode)
            {
                case 0: // Entry
                    float t = Mathf.Clamp01(b.Age / b.Travel);
                    b.Position = FormationMath.Entry(b.Origin, b.Target, Drift(b), t);
                    if (t >= 1) b.Mode = 1;
                    break;
                case 1: // Formation
                    b.Position = Drift(b);
                    break;
                case 2: // Dive warning
                    b.Position = Drift(b) + Vector2.right * Mathf.Sin(b.Age * 70) * .11f;
                    b.Art.color = new Color(1, .50f + Mathf.PingPong(b.Age * 9, .5f), .41f);
                    if (clock >= b.ReleaseTime)
                    {
                        var target = new Vector2(ship.Position.x, Mathf.Min(ship.Position.y, b.Position.y - 2.4f));
                        b.Velocity = (target - b.Position).normalized * wave.diveSpeed;
                        b.Mode = 3;
                        b.Art.color = b.Tint;
                    }
                    break;
                case 3: // Dive and exit
                    b.Position += b.Velocity * dt;
                    b.transform.rotation = Quaternion.Euler(0, 0, -b.Velocity.x * 3);
                    if (b.Position.y < -HalfHeight - .8f || Mathf.Abs(b.Position.x) > HalfWidth + 1)
                    {
                        b.Position = b.Origin;
                        b.Age = 0;
                        b.Mode = 0;
                        b.transform.rotation = Quaternion.identity;
                    }
                    break;
            }
        }

        void MoveRock(PoolBody b, float dt)
        {
            b.Position += b.Velocity * dt;
            b.transform.Rotate(0, 0, b.Spin * dt);
            if (b.Position.y < -HalfHeight - .9f) Resolve(b, false);
        }

        PoolBody FormationBird(ref int offset)
        {
            int total = pool.Live.Count;
            if (total == 0) return null;
            for (int i = 0; i < total; i++)
            {
                int slot = (offset + i) % total;
                var b = pool.Live[slot];
                if (b.kind != BodyKind.Chicken || b.Mode != 1) continue;
                offset = slot + Random.Range(1, 5);
                return b;
            }
            return null;
        }

        void TryEgg()
        {
            var b = FormationBird(ref eggLane);
            if (b == null) return;
            var from = b.Position + Vector2.down * .37f;
            var aimed = (ship.Position - from).normalized;
            if (aimed.y > -.28f) aimed = new Vector2(aimed.x, -.5f).normalized;
            var egg = pool.Spawn(data.egg, this, from, 4.7f);
            sound.Play(data.eggSound, .65f);
            egg.Velocity = Vector2.Lerp(Vector2.down, aimed, wave.aim).normalized * wave.eggSpeed;
            egg.transform.up = -egg.Velocity.normalized;
        }

        void TryDive()
        {
            var b = FormationBird(ref cursor);
            if (b == null) return;
            b.Mode = 2;
            b.ReleaseTime = clock + .53f;
        }

        internal void Fire(WeaponDataSO weapon, Vector2 muzzle)
        {
            sound.Play(data.laserSound, .56f, .075f);
            foreach (var lane in weapon.lanes)
            {
                var shot = pool.Spawn(weapon.projectile, this, muzzle + Vector2.right * lane.offset, .64f);
                var rotation = Quaternion.Euler(0, 0, lane.angle);
                shot.Damage = weapon.damage;
                shot.Velocity = rotation * Vector2.up * weapon.speed;
                shot.transform.rotation = rotation;
            }
        }

        internal void LaserHit(PoolBody shot, PoolBody target)
        {
            if (!CombatActive || !shot.Active || !target.Active ||
                target.kind is not (BodyKind.Chicken or BodyKind.Rock)) return;
            pool.Release(shot);
            target.ApplyDamage(shot.Damage);
            if (target.Health > 0)
            {
                sound.Play(data.impactSound, .65f, .06f);
                target.Flash = .085f;
                target.Art.color = HitTint;
                Burst(target.Position, new Color(.65f, .93f, 1), 3);
            }
            else Resolve(target, true);
        }

        internal void PlayerHit(PoolBody b)
        {
            sound.Play(data.damageSound);
            var at = ship.Position;
            if (b.kind == BodyKind.Egg) pool.Release(b);
            else Resolve(b, false);
            Shake = .30f;
            Burst(at, new Color(.40f, .89f, 1), 17);
            state.TakeHit();
        }

        internal void TakeGift(PoolBody body)
        {
            if (!body.Active) return;
            var position = body.Position;
            pool.Release(body);
            int previousPower = state.Power;
            state.CollectGift();
            sound.Play(data.giftSound, .78f);
            GiftCollected?.Invoke(state.Power > previousPower);
            Burst(position, new Color(.46f, 1, .72f), 15);
        }

        static bool PulseTarget(PoolBody body) =>
            body.kind == BodyKind.Chicken && body.Position.y < HalfHeight + .35f;

        internal PulseResult Bomb()
        {
            if (!CombatActive) return PulseResult.NoTargets;
            if (state.Bombs <= 0) return PulseResult.Empty;
            if (!pool.Live.Exists(PulseTarget)) return PulseResult.NoTargets;
            if (!state.SpendBomb()) return PulseResult.Empty;
            sound.Play(data.bombSound);
            ringTime = RingDuration;
            ring.transform.position = ship.Position;
            ring.enabled = true;
            Shake = .42f;
            for (int i = pool.Live.Count - 1; i >= 0; i--)
            {
                var b = pool.Live[i];
                if (!PulseTarget(b)) continue;
                Resolve(b, true);
            }
            return PulseResult.Fired;
        }

        static readonly Color HitTint = new(1f, .52f, .34f);

        void Resolve(PoolBody b, bool scored)
        {
            var position = b.Position;
            var kind = b.kind;
            pool.Release(b);
            resolved++;
            if (scored)
            {
                sound.Play(kind == BodyKind.Rock ? data.asteroidBreakSound : data.chickenDownSound, .83f, .075f);
                state.AddScore(kind == BodyKind.Rock ? 65 : 85 + state.Stage * 15);
                kills++;
                if (!gifted && kind == BodyKind.Chicken && kills >= wave.giftAfterKills)
                {
                    gifted = true;
                    var drop = pool.Spawn(data.gift, this, position, 7);
                    drop.Velocity = new Vector2(0, -3.8f);
                }
                Burst(position, kind == BodyKind.Rock ? new Color(1, .65f, .43f) : new Color(1, .78f, .35f), 8);
                Shake = Mathf.Max(Shake, .065f);
            }
            TargetsChanged?.Invoke(Remaining);
        }

        void Burst(Vector2 position, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var dir = Random.insideUnitCircle;
                var spark = pool.Spawn(data.spark, this, position, Random.Range(.17f, .39f));
                spark.Velocity = dir * Random.Range(1.4f, 6.2f);
                spark.Tint = spark.Art.color = color;
                spark.transform.rotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.up, dir));
                spark.transform.localScale = Vector3.one * Random.Range(.4f, 1.15f);
            }
        }
    }
}
