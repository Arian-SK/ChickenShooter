using UnityEngine;

namespace Cluck
{
    [CreateAssetMenu(menuName = "Cluck/Level")]
    public sealed class LevelDataSO : ScriptableObject
    {
        [Header("Level")]
        [Min(1)] public int stage = 1;
        public string sceneName, sector, subtitle;

        [Header("Chicken Difficulty")]
        [Tooltip("Apply these two values to every chicken wave in this level. Turn off to use the values in each wave.")]
        public bool useLevelDifficulty;
        [Min(1), InspectorName("Hits to Defeat")]
        [Tooltip("Health of each chicken. Each laser bolt deals one point with the default weapons.")]
        public int chickenHits = 2;
        [Min(.1f), InspectorName("Seconds Between Egg Drops")]
        [Tooltip("Average seconds between eggs fired by the flock. Smaller values fire more often.")]
        public float eggInterval = 1.8f;

        [Header("Presentation")]
        public AudioClip music;
        public Sprite backdrop;
        public Color accent = Color.cyan;
        [Header("Rounds")]
        public WaveDataSO[] rounds;

        internal int ChickenHealth(WaveDataSO wave) => Mathf.Max(1, useLevelDifficulty ? chickenHits : wave.health);
        internal float EggInterval(WaveDataSO wave) => Mathf.Max(.1f, useLevelDifficulty ? eggInterval : wave.eggInterval);
    }
}
