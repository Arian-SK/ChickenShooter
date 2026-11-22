using UnityEngine;

namespace Cluck
{
    [CreateAssetMenu(menuName = "Cluck/Boss")]
    public sealed class BossDataSO : ScriptableObject
    {
        public string bossName;
        public GameObject bossPrefab;
        public Sprite sprite;
        public Vector2 hitbox = new(2.1f, 1.72f);
        [Min(1)] public int health = 70;
        [Min(.2f)] public float volleyInterval = 1.3f;
        [Range(3, 9)] public int volleySize = 5;
        [Min(1)] public float projectileSpeed = 4.8f;
        [Min(1)] public float sweepWidth = 3f;
        [Min(0)] public int escorts = 2;
        public Color accent = Color.yellow;
    }
}
