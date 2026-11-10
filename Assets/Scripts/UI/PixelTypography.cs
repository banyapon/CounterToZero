using TMPro;
namespace CountToZero
{
    public static class PixelTypography
    {
        // Match the old body's cap height: (31 / 90) / (7 / 8).
        public const float BodyScale = 0.3936508f;
        public const float TitleScale = 1.1428571f;
        public static float TitleSize(TMP_Text text, float size) =>
            text.font != null && text.font.name == "Pixel UI Sci-Fi TMP" ? size * TitleScale : size;
        public static float Size(TMP_Text text, float size) =>
            text.font != null && text.font.name == "Pixel UI Sci-Fi TMP" ? size * BodyScale : size;
    }
}
