using UnityEngine;

namespace Cluck
{
    public enum Formation { Grid, Chevron, Diamond }
    public enum WaveKind { Flock, Asteroids }

    [CreateAssetMenu(menuName = "Cluck/Wave")]
    public sealed class WaveDataSO : ScriptableObject
    {
        public string title;
        public WaveKind kind;
        public PoolBody enemyPrefab;
        public Formation formation;
        [Min(1), InspectorName("Target Count"), Tooltip("Number of asteroids or chickens spawned in this round.")]
        public int count = 8;
        [Min(1)] public int columns = 5;
        [Min(1), Tooltip("Rock health, or chicken health when the level uses per-wave difficulty.")]
        public int health = 2;
        [Min(.1f)] public float travelSpeed = 6, eggSpeed = 4;
        [Min(.1f), Tooltip("Seconds between egg drops when the level uses per-wave difficulty.")]
        public float eggInterval = 1.5f;
        [Min(.1f)] public float diveSpeed = 8, diveInterval = 5;
        [Min(.1f)] public float spacing = 1.55f;
        [Min(0)] public float entryDelay = .13f;
        public Vector2 entryBend = new(7.7f, 3.5f);
        [Range(0, 1)] public float aim = .18f;
        [Min(1)] public int giftAfterKills = 7;
    }
}
