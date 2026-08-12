using UnityEngine;

namespace Game.UI
{
    /// <summary>G4.2 Qingci Sword Realm UI tokens mirrored from the governed Style Bible.</summary>
    public static class QinglanUiTheme
    {
        public const int MinimumBodyFontSize1080p = 18;
        public const int KeyValueFontSize1080p = 22;
        public const int ChoiceTitleFontSize1080p = 24;
        public const float DefaultPanelAlpha = 0.88f;
        public const float HighContrastPanelAlpha = 0.98f;

        public static readonly Color32 Ink950 = new Color32(16, 42, 45, 255);
        public static readonly Color32 Ink800 = new Color32(28, 66, 67, 255);
        public static readonly Color32 Jade500 = new Color32(66, 184, 173, 255);
        public static readonly Color32 Jade200 = new Color32(169, 229, 216, 255);
        public static readonly Color32 Rice100 = new Color32(242, 235, 216, 255);
        public static readonly Color32 Gold400 = new Color32(215, 181, 90, 255);
        public static readonly Color32 Cinnabar500 = new Color32(228, 87, 61, 255);
        public static readonly Color32 Cinnabar300 = new Color32(255, 139, 98, 255);
        public static readonly Color32 Void700 = new Color32(86, 60, 118, 255);

        public static Color WithAlpha(Color32 value, float alpha) =>
            new Color(value.r / 255f, value.g / 255f, value.b / 255f, Mathf.Clamp01(alpha));
    }
}
