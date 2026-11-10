using UnityEngine;

namespace Cluck
{
    public static class FormationMath
    {
        public static Vector2 Slot(WaveDataSO wave, int index)
        {
            int col = index % wave.columns, row = index / wave.columns;
            int inRow = Mathf.Min(wave.columns, wave.count - row * wave.columns);
            float x = (col - (inRow - 1) * .5f) * wave.spacing;
            float y = 2.75f - row * .83f;
            if (wave.formation == Formation.Chevron) y += Mathf.Abs(x) * .30f - .38f;
            if (wave.formation == Formation.Diamond)
            {
                x *= row % 2 == 0 ? .8f : 1;
                y -= Mathf.Abs(x) * .24f;
            }
            return new Vector2(x, y);
        }

        public static Vector2 Entry(Vector2 start, Vector2 bend, Vector2 end, float t)
        {
            var a = Vector2.Lerp(start, bend, t);
            var b = Vector2.Lerp(bend, end, t);
            return Vector2.Lerp(a, b, t);
        }
    }
}
