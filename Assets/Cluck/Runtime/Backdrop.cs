using UnityEngine;

namespace Cluck
{
    internal sealed class Backdrop
    {
        readonly SpriteRenderer[] stars = new SpriteRenderer[72];
        readonly Vector2[] origins = new Vector2[72];
        readonly Color accent;

        public Backdrop(Transform parent, LevelDataSO level, Sprite speck, Material material, SpriteRenderer field)
        {
            accent = level.accent;
            if (field == null) CreateBackground(parent, level.backdrop, material);
            else field.sprite = level.backdrop;
            var random = new System.Random(level.stage * 717);
            for (int i = 0; i < stars.Length; i++)
            {
                var star = stars[i] = new GameObject("Drifting starlight").AddComponent<SpriteRenderer>();
                star.transform.SetParent(parent);
                star.sprite = speck;
                star.sharedMaterial = material;
                star.sortingOrder = -59;
                float scale = .13f + (float)random.NextDouble() * .45f;
                star.transform.localScale = new Vector3(scale, scale * (i % 3 == 0 ? 1.65f : .68f), 1);
                origins[i] = new Vector2((float)random.NextDouble() * 17.8f - 8.9f, (float)random.NextDouble() * 10);
            }
            Step(0);
        }

        internal static SpriteRenderer CreateBackground(Transform parent, Sprite sprite, Material material)
        {
            var field = new GameObject("Background").AddComponent<SpriteRenderer>();
            field.transform.SetParent(parent, false);
            field.sprite = sprite;
            field.sharedMaterial = material;
            field.sortingOrder = -60;
            field.transform.localScale = Vector3.one * 1.04f;
            return field;
        }

        public void Step(float time)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                float speed = .25f + .45f * (i % 3);
                var star = stars[i];
                var origin = origins[i];
                star.transform.localPosition = new Vector3(origin.x, 5 - Mathf.Repeat(origin.y + time * speed, 10));
                var tint = Color.Lerp(accent, Color.white, .5f);
                tint.a = .22f + .16f * Mathf.Sin(time * (1.3f + speed) + i * 1.7f);
                star.color = tint;
            }
        }
    }
}
