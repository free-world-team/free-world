using UnityEngine;

namespace Game.Presentation
{
    /// <summary>G4.2 Qingci Sword Realm world/VFX tokens mirrored from the governed Style Bible.</summary>
    public static class QinglanPresentationTheme
    {
        public const float CentralArenaSampleDiameter = 24f;
        public const int PickupGroupingThreshold = 32;
        public const float PickupGroupingNearRadius = 4f;
        public const float PlayerOutlineScale = 1.18f;
        public const float EnemyOutlineScale = 1.11f;
        public const float DangerFillAlpha = 0.24f;

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
