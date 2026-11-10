using UnityEngine;
using UnityEngine.InputSystem;

namespace Cluck
{
    [CreateAssetMenu(menuName = "Cluck/Campaign")]
    public sealed class CampaignDataSO : ScriptableObject
    {
        public string menuScene = "MainMenu";
        public LevelDataSO[] levels;
        public WeaponDataSO[] weaponTiers;
        public InputActionAsset controls;
        public Sprite ship, shield, flare, ring;
        public Material spriteMaterial;
        public PoolBody egg, gift, spark;

        [Header("Audio")]
        public AudioClip menuMusic;
        public AudioClip laserSound, eggSound, impactSound;
        public AudioClip asteroidBreakSound, chickenDownSound, damageSound;
        public AudioClip giftSound, bombSound, roundClearSound;
    }
}
