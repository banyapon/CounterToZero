using UnityEngine;

namespace CountToZero
{
    /// HP colour: full = green, middle = yellow, getting low = orange, near death = red.
    public static class HealthTint
    {
        public static readonly Color Full = new Color(.18f, .95f, .35f);
        public static readonly Color Middle = new Color(1f, .9f, .15f);
        public static readonly Color Low = new Color(1f, .55f, .1f);
        public static readonly Color Critical = new Color(1f, .16f, .2f);
        public static Color For(float fraction) =>
            fraction > .7f ? Full : fraction > .5f ? Middle : fraction > .3f ? Low : Critical;
    }
}
