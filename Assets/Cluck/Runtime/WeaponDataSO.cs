using UnityEngine;

namespace Cluck
{
    [System.Serializable]
    public struct ShotLane
    {
        public float offset, angle;
        public ShotLane(float offset, float angle) { this.offset = offset; this.angle = angle; }
    }

    [CreateAssetMenu(menuName = "Cluck/Weapon")]
    public sealed class WeaponDataSO : ScriptableObject
    {
        public string weaponName;
        public Sprite icon;
        public PoolBody projectile;
        [Min(1)] public int damage = 1;
        [Min(.1f)] public float speed = 18, shotsPerSecond = 6;
        public ShotLane[] lanes;
    }
}
