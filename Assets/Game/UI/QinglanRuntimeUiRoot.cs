using System;
using System.Globalization;
using System.Text;
using Game.Application;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public interface IQinglanUiVisualCatalog
    {
        bool TryResolveSprite(string stableKey, out Sprite sprite);
    }

    public interface IQinglanUiFontCatalog
    {
        TMP_FontAsset Regular { get; }
        TMP_FontAsset Bold { get; }
        TMP_FontAsset Narrative { get; }
    }

    /// <summary>Single procedural placeholder Canvas with separated page and HUD layers.</summary>
    public sealed class QinglanRuntimeUiRoot : MonoBehaviour, IQinglanDemoView
    {
        private readonly StringBuilder pageBuilder = new StringBuilder(8192);
        private readonly StringBuilder hudBuilder = new StringBuilder(4096);
        private Canvas canvas;
        private CanvasScaler scaler;
        private Image pagePanel;
        private Image pageBackground;
        private Image focusMarker;
        private Image hudPanel;
        private Image dangerPanel;
        private TMP_Text pageText;
        private TMP_Text hudText;
        private TMP_Text dangerText;
        private TMP_FontAsset regularFont;
        private TMP_FontAsset boldFont;
        private TMP_FontAsset narrativeFont;
        private ILocalizationService localization;
        private Func<string, string> contentNameResolver;
        private IQinglanUiVisualCatalog visualCatalog;
        private ColorVisionMode lastColorVision = (ColorVisionMode)255;
        private float lastFontScale = -1f;

        public Canvas SharedCanvas => canvas;
        public QinglanUiPageId CurrentPage { get; private set; }
        public int RenderedOptionCount { get; private set; }
        public int RenderedSelectedIndex { get; private set; }
        public int HudRefreshCount { get; private set; }
        public string RenderedPageText => pageText == null ? string.Empty : pageText.text;
        public string RenderedHudText => hudText == null ? string.Empty : hudText.text;
        public bool FormalBackgroundApplied { get; private set; }
        public int FormalVisualMissCount { get; private set; }
        public bool UsesFormalTmpFonts => regularFont != null && pageText != null && pageText.font != null;
        public bool HasAnyTextOverflow
        {
            get
            {
                if (pageText == null || hudText == null || dangerText == null) return false;
                pageText.ForceMeshUpdate();
                hudText.ForceMeshUpdate();
                dangerText.ForceMeshUpdate();
                return IsOverflowing(pageText) || IsOverflowing(hudText) || IsOverflowing(dangerText);
            }
        }

        public void Initialize(
            ILocalizationService localizationService,
            Func<string, string> resolveContentNameKey,
            IQinglanUiVisualCatalog formalVisualCatalog = null,
            IQinglanUiFontCatalog formalFontCatalog = null)
        {
            if (canvas != null) return;
            localization = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            contentNameResolver = resolveContentNameKey ?? throw new ArgumentNullException(nameof(resolveContentNameKey));
            visualCatalog = formalVisualCatalog;
            regularFont = formalFontCatalog?.Regular ?? TMP_Settings.defaultFontAsset;
            if (regularFont == null)
                throw new InvalidOperationException("The governed TMP default font asset is unavailable.");
            boldFont = formalFontCatalog?.Bold ?? regularFont;
            narrativeFont = formalFontCatalog?.Narrative ?? regularFont;
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            pageBackground = CreatePanel("Qinglan_FormalBackground", Vector2.zero, Vector2.one);
            pageBackground.color = new Color(0.035f, 0.055f, 0.06f, 1f);
            pagePanel = CreatePanel("Qinglan_PageLayer", new Vector2(0.025f, 0.04f), new Vector2(0.66f, 0.96f));
            pageText = CreateText(pagePanel.transform, "Qinglan_PageText", 20, TextAlignmentOptions.TopLeft,
                new Vector2(34f, 24f), new Vector2(-34f, -24f));
            hudPanel = CreatePanel("Qinglan_HudLayer", new Vector2(0.68f, 0.14f), new Vector2(0.98f, 0.96f));
            hudText = CreateText(hudPanel.transform, "Qinglan_HudText", 17, TextAlignmentOptions.TopLeft,
                new Vector2(24f, 20f), new Vector2(-24f, -20f));
            dangerPanel = CreatePanel("Qinglan_DangerLayer", new Vector2(0.68f, 0.04f), new Vector2(0.98f, 0.12f));
            dangerText = CreateText(dangerPanel.transform, "Qinglan_DangerText", 18, TextAlignmentOptions.TopLeft,
                new Vector2(24f, 20f), new Vector2(-24f, -20f));
            dangerText.font = boldFont;
            focusMarker = CreatePanel("Qinglan_FormalFocus", new Vector2(0.025f, 0.45f), new Vector2(0.055f, 0.55f));
            focusMarker.preserveAspect = true;
            ApplyChrome();
            RefreshDangerLegend();
        }

        public void ShowPage(QinglanPageViewModel page)
        {
            if (canvas == null) throw new InvalidOperationException("UI root is not initialized.");
            CurrentPage = page.Page;
            var runHudVisible = page.Page == QinglanUiPageId.RunHud;
            hudPanel.gameObject.SetActive(runHudVisible);
            dangerPanel.gameObject.SetActive(runHudVisible);
            pagePanel.rectTransform.anchorMax = new Vector2(runHudVisible ? 0.66f : 0.98f, 0.96f);
            RenderedOptionCount = page.OptionCount;
            RenderedSelectedIndex = page.SelectedIndex;
            pageText.font = page.Page == QinglanUiPageId.TitleProfile ||
                            page.Page == QinglanUiPageId.StoryOverlay ||
                            page.Page == QinglanUiPageId.RunResult
                ? narrativeFont
                : regularFont;
            ApplyPageBackground(page.Page);
            pageBuilder.Clear();
            AppendKey(pageBuilder, page.TitleKey);
            if (!string.IsNullOrEmpty(page.SubtitleKey))
            {
                pageBuilder.Append("\n\n");
                AppendKey(pageBuilder, page.SubtitleKey);
            }
            if (!string.IsNullOrEmpty(page.StatusKey))
            {
                pageBuilder.Append("\n\n◆ ");
                AppendKey(pageBuilder, page.StatusKey);
                if (!string.IsNullOrEmpty(page.StatusValue)) pageBuilder.Append(": ").Append(page.StatusValue);
            }
            for (var index = 0; index < page.OptionCount; index++)
            {
                var option = page.GetOptionAt(index);
                pageBuilder.Append("\n\n");
                pageBuilder.Append(index == page.SelectedIndex ? "◆ " : option.Enabled ? "◇ " : "× ");
                AppendKey(pageBuilder, option.LabelKey);
                if (!string.IsNullOrEmpty(option.ValueText))
                {
                    pageBuilder.Append("  [");
                    AppendValue(pageBuilder, option.ValueText);
                    pageBuilder.Append(']');
                }
                if (!string.IsNullOrEmpty(option.DescriptionKey))
                {
                    pageBuilder.Append("\n   ");
                    AppendKey(pageBuilder, option.DescriptionKey);
                }
                AppendCardMetadata(option);
            }
            pageText.text = pageBuilder.ToString();
        }

        public void ShowHud(RunUiSnapshot snapshot)
        {
            if (canvas == null || snapshot == null) return;
            HudRefreshCount++;
            hudBuilder.Clear();
            AppendKey(hudBuilder, "ui.qinglan.hud.vitals");
            hudBuilder.Append("\n♥ ").Append(Round(snapshot.Health)).Append('/').Append(Round(snapshot.MaximumHealth));
            hudBuilder.Append("   ◇ ").Append(Round(snapshot.Shield)).Append('/').Append(Round(snapshot.MaximumShield));
            hudBuilder.Append("\nLv.").Append(snapshot.Level).Append("  XP ").Append(Round(snapshot.Experience)).Append('/').Append(Round(snapshot.RequiredExperience));
            hudBuilder.Append("\n\n");
            AppendKey(hudBuilder, "ui.qinglan.hud.run");
            hudBuilder.Append("\n时 ").Append(FormatTime(snapshot.DurationSeconds));
            hudBuilder.Append("   ");
            AppendKey(hudBuilder, "ui.qinglan.hud.windride");
            hudBuilder.Append(' ').Append(snapshot.MechanicTier + 1).Append("/4  ≫ ").Append(Round(snapshot.MechanicValue));
            if (snapshot.HasBoss)
            {
                hudBuilder.Append("\n\n▲ ").Append(ResolveContent(snapshot.BossId));
                hudBuilder.Append("  ").Append(snapshot.BossPhase + 1).Append('/').Append(snapshot.BossPhaseCount);
                hudBuilder.Append("\n").Append(Round(snapshot.BossHealth)).Append('/').Append(Round(snapshot.BossMaximumHealth));
            }
            hudBuilder.Append("\n\n");
            AppendKey(hudBuilder, "ui.qinglan.hud.build");
            for (var index = 0; index < snapshot.BuildCount; index++)
            {
                var item = snapshot.GetBuildAt(index);
                hudBuilder.Append("\n").Append(BuildGlyph(item.Kind)).Append(' ').Append(ResolveContent(item.ContentId));
                hudBuilder.Append("  Lv.").Append(item.Level).Append('/').Append(item.MaximumLevel);
            }
            hudBuilder.Append("\n\n");
            AppendKey(hudBuilder, "ui.qinglan.hud.map");
            for (var index = 0; index < snapshot.MapCount; index++)
            {
                var item = snapshot.GetMapAt(index);
                hudBuilder.Append("\n").Append(MapGlyph(item.Kind)).Append(' ').Append(ResolveContent(item.ContentId));
                hudBuilder.Append("  ").Append(Round(item.Progress * 100f)).Append('%');
            }
            hudText.text = hudBuilder.ToString();
        }

        public void ApplyAccessibility(AccessibilitySettings settings)
        {
            if (settings == null || canvas == null) return;
            if (Math.Abs(lastFontScale - settings.FontScale) > 0.001f)
            {
                lastFontScale = settings.FontScale;
                pageText.fontSize = Mathf.RoundToInt(20f * settings.FontScale);
                hudText.fontSize = Mathf.RoundToInt(17f * settings.FontScale);
                dangerText.fontSize = Mathf.RoundToInt(18f * settings.FontScale);
            }
            if (lastColorVision == settings.ColorVision) return;
            lastColorVision = settings.ColorVision;
            pagePanel.color = PanelColor(settings.ColorVision);
            dangerText.color = DangerColor(settings.ColorVision);
            RefreshDangerLegend();
        }

        public bool SupportsCharacter(char character)
        {
            return regularFont != null && regularFont.HasCharacter(character, true, true);
        }

        private void AppendCardMetadata(QinglanUiOption option)
        {
            if (string.IsNullOrEmpty(option.TagKey) && string.IsNullOrEmpty(option.RelationKey) &&
                string.IsNullOrEmpty(option.EligibilityKey)) return;
            pageBuilder.Append("\n   ");
            if (!string.IsNullOrEmpty(option.TagKey))
            {
                pageBuilder.Append('[');
                AppendKey(pageBuilder, option.TagKey);
                pageBuilder.Append("] ");
            }
            if (!string.IsNullOrEmpty(option.RelationKey)) AppendKey(pageBuilder, option.RelationKey);
            if (!string.IsNullOrEmpty(option.EligibilityKey))
            {
                pageBuilder.Append(" · ");
                AppendKey(pageBuilder, option.EligibilityKey);
            }
        }

        private void RefreshDangerLegend()
        {
            // Shape and direction remain readable when hue cannot be distinguished.
            dangerText.text = "▲  ▶  ◆  " + Resolve("ui.qinglan.accessibility.danger_legend");
        }

        private void ApplyChrome()
        {
            if (visualCatalog == null) return;
            if (visualCatalog.TryResolveSprite("ui.focus", out var focus))
            {
                focusMarker.sprite = focus;
                focusMarker.color = Color.white;
            }
            if (visualCatalog.TryResolveSprite("ui.panel", out var panel))
            {
                pagePanel.sprite = panel;
                pagePanel.type = panel.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            }
            if (visualCatalog.TryResolveSprite("ui.cursor.pointer", out var cursor) &&
                cursor.texture != null && UnityEngine.Application.isPlaying)
                Cursor.SetCursor(cursor.texture, Vector2.zero, CursorMode.Auto);
        }

        private void ApplyPageBackground(QinglanUiPageId page)
        {
            var key = BackgroundKey(page);
            if (visualCatalog != null && visualCatalog.TryResolveSprite(key, out var sprite))
            {
                pageBackground.sprite = sprite;
                pageBackground.color = Color.white;
                FormalBackgroundApplied = true;
                return;
            }

            pageBackground.sprite = null;
            pageBackground.color = new Color(0.035f, 0.055f, 0.06f, 1f);
            FormalBackgroundApplied = false;
            if (visualCatalog != null) FormalVisualMissCount++;
        }

        private static string BackgroundKey(QinglanUiPageId page)
        {
            switch (page)
            {
                case QinglanUiPageId.TitleProfile: return "ui.page.title";
                case QinglanUiPageId.CharacterSelect: return "ui.page.character_select";
                case QinglanUiPageId.MapSelect: return "ui.page.map_select";
                case QinglanUiPageId.Loadout:
                case QinglanUiPageId.LoadoutConfirmation: return "ui.page.loadout";
                case QinglanUiPageId.LevelUpChoice:
                case QinglanUiPageId.RewardChoice: return "ui.page.choice";
                case QinglanUiPageId.Hub:
                case QinglanUiPageId.HubFacility:
                case QinglanUiPageId.Collection: return "ui.page.hub";
                case QinglanUiPageId.StoryOverlay:
                case QinglanUiPageId.RunResult: return "ui.page.story_result";
                default: return "ui.page.title_safe";
            }
        }

        private Image CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(transform, false);
            var rect = (RectTransform)panelObject.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = panelObject.GetComponent<Image>();
            image.color = new Color(0.035f, 0.075f, 0.085f, 0.92f);
            return image;
        }

        private TMP_Text CreateText(
            Transform parent,
            string name,
            int fontSize,
            TextAlignmentOptions alignment,
            Vector2 minimumOffset,
            Vector2 maximumOffset)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var rect = (RectTransform)textObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = minimumOffset;
            rect.offsetMax = maximumOffset;
            var text = textObject.GetComponent<TMP_Text>();
            text.font = regularFont;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.95f, 0.94f, 0.84f, 1f);
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = false;
            text.lineSpacing = -3f;
            return text;
        }

        private void AppendKey(StringBuilder builder, string key)
        {
            if (!string.IsNullOrEmpty(key)) builder.Append(Resolve(key));
        }

        private void AppendValue(StringBuilder builder, string value)
        {
            if (value.StartsWith("ui.", StringComparison.Ordinal) ||
                value.StartsWith("content.", StringComparison.Ordinal) ||
                value.StartsWith("story.", StringComparison.Ordinal) ||
                value.StartsWith("collectible.", StringComparison.Ordinal) ||
                value.StartsWith("narrative.", StringComparison.Ordinal))
                builder.Append(Resolve(value));
            else builder.Append(value);
        }

        private string Resolve(string key) => localization.Resolve(key);

        private string ResolveContent(string id)
        {
            var key = contentNameResolver(id);
            return string.IsNullOrEmpty(key) ? Resolve("ui.qinglan.content.unknown") : Resolve(key);
        }

        private static string Round(float value) =>
            Math.Round(value, 1).ToString(CultureInfo.InvariantCulture);

        private static string FormatTime(double seconds)
        {
            var total = Math.Max(0, (int)seconds);
            return (total / 60).ToString("00", CultureInfo.InvariantCulture) + ":" +
                   (total % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        private static string BuildGlyph(byte kind) => kind == 1 ? "剑" : kind == 2 ? "◆" : kind == 3 ? "◎" : "◇";
        private static string MapGlyph(byte kind) => kind == 1 ? "◎" : kind == 2 ? "△" : "◇";

        private static Color PanelColor(ColorVisionMode mode)
        {
            switch (mode)
            {
                case ColorVisionMode.Protanopia: return new Color(0.035f, 0.07f, 0.12f, 0.94f);
                case ColorVisionMode.Deuteranopia: return new Color(0.06f, 0.055f, 0.12f, 0.94f);
                case ColorVisionMode.Tritanopia: return new Color(0.09f, 0.05f, 0.07f, 0.94f);
                case ColorVisionMode.HighContrast: return new Color(0f, 0f, 0f, 0.98f);
                default: return new Color(0.035f, 0.075f, 0.085f, 0.92f);
            }
        }

        private static Color DangerColor(ColorVisionMode mode) =>
            mode == ColorVisionMode.HighContrast ? Color.white : new Color(1f, 0.72f, 0.32f, 1f);

        private static bool IsOverflowing(TMP_Text text)
        {
            if (!text.gameObject.activeInHierarchy) return false;
            var height = text.rectTransform.rect.height;
            return text.isTextOverflowing || (height > 0f && text.preferredHeight > height + 0.5f);
        }

        private void OnDestroy()
        {
            if (UnityEngine.Application.isPlaying) Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
