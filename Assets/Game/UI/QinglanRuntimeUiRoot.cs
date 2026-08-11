using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Application;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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
        private readonly StringBuilder headerBuilder = new StringBuilder(512);
        private readonly StringBuilder objectiveBuilder = new StringBuilder(512);
        private readonly List<OptionCardView> optionCards = new List<OptionCardView>(24);
        private readonly List<HudIconView> hudIcons = new List<HudIconView>(12);
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
        private RectTransform optionContent;
        private ScrollRect optionScroll;
        private RectTransform buildPanelRect;
        private Image healthFill;
        private Image shieldFill;
        private Image experienceFill;
        private Image bossFill;
        private GameObject bossBarRoot;
        private RectTransform buildIconRoot;
        private TMP_Text vitalsText;
        private TMP_Text runStatusText;
        private TMP_Text bossText;
        private TMP_Text objectiveText;
        private TMP_FontAsset regularFont;
        private TMP_FontAsset boldFont;
        private TMP_FontAsset narrativeFont;
        private ILocalizationService localization;
        private Func<string, string> contentNameResolver;
        private IQinglanUiVisualCatalog visualCatalog;
        private ColorVisionMode lastColorVision = (ColorVisionMode)255;
        private float lastFontScale = -1f;
        private string renderedPageText = string.Empty;
        private string renderedHudText = string.Empty;

        public event Action<int> OptionInvoked;

        public Canvas SharedCanvas => canvas;
        public QinglanUiPageId CurrentPage { get; private set; }
        public int RenderedOptionCount { get; private set; }
        public int RenderedSelectedIndex { get; private set; }
        public int HudRefreshCount { get; private set; }
        public string RenderedPageText => renderedPageText;
        public string RenderedHudText => renderedHudText;
        public bool FormalBackgroundApplied { get; private set; }
        public int FormalVisualMissCount { get; private set; }
        public bool UsesFormalTmpFonts => regularFont != null && pageText != null && pageText.font != null;
        public int ActiveButtonCount { get; private set; }
        public int ClickableButtonCount { get; private set; }
        public int VisibleHudIconCount { get; private set; }
        public int FormalOptionIconCount { get; private set; }
        public float HealthBarFillAmount => healthFill == null ? 0f : healthFill.fillAmount;
        public bool BossBarVisible => bossBarRoot != null && bossBarRoot.activeSelf;
        /// <summary>True when the active Run HUD leaves the camera-rendered battlefield unobstructed.</summary>
        public bool GameplayWorldVisible => CurrentPage == QinglanUiPageId.RunHud &&
                                            pageBackground != null && !pageBackground.gameObject.activeSelf &&
                                            pagePanel != null && !pagePanel.gameObject.activeSelf;
        public bool HasAnyTextOverflow
        {
            get
            {
                if (pageText == null || hudText == null || dangerText == null) return false;
                pageText.ForceMeshUpdate();
                hudText.ForceMeshUpdate();
                dangerText.ForceMeshUpdate();
                if (IsOverflowing(pageText) || IsOverflowing(hudText) || IsOverflowing(dangerText) ||
                    IsOverflowing(vitalsText) || IsOverflowing(runStatusText) || IsOverflowing(bossText) ||
                    IsOverflowing(objectiveText)) return true;
                for (var index = 0; index < optionCards.Count; index++)
                    if (optionCards[index].HasOverflow) return true;
                return false;
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
            EnsureEventSystem();
            pageBackground = CreatePanel("Qinglan_FormalBackground", Vector2.zero, Vector2.one);
            pageBackground.color = new Color(0.035f, 0.055f, 0.06f, 1f);
            pagePanel = CreatePanel("Qinglan_PageLayer", new Vector2(0.045f, 0.07f), new Vector2(0.68f, 0.94f));
            pageText = CreateText(pagePanel.transform, "Qinglan_PageText", 28, TextAlignmentOptions.TopLeft,
                new Vector2(38f, 16f), new Vector2(-38f, -16f));
            pageText.font = boldFont;
            pageText.rectTransform.anchorMin = new Vector2(0f, 0.66f);
            pageText.rectTransform.anchorMax = Vector2.one;
            CreateOptionScrollView();
            hudPanel = CreatePanel("Qinglan_HudLayer", Vector2.zero, Vector2.one);
            hudPanel.color = Color.clear;
            hudPanel.raycastTarget = false;
            hudText = CreateText(hudPanel.transform, "Qinglan_HudText", 17, TextAlignmentOptions.TopLeft,
                new Vector2(24f, 20f), new Vector2(-24f, -20f));
            hudText.gameObject.SetActive(false);
            CreateHudWidgets();
            dangerPanel = CreatePanel("Qinglan_DangerLayer", new Vector2(0.62f, 0.018f), new Vector2(0.985f, 0.105f));
            dangerText = CreateText(dangerPanel.transform, "Qinglan_DangerText", 16, TextAlignmentOptions.MidlineLeft,
                new Vector2(18f, 8f), new Vector2(-18f, -8f));
            dangerText.font = boldFont;
            focusMarker = CreatePanel("Qinglan_FormalFocus", new Vector2(0.025f, 0.45f), new Vector2(0.055f, 0.55f));
            focusMarker.preserveAspect = true;
            focusMarker.gameObject.SetActive(false);
            ApplyChrome();
            RefreshDangerLegend();
        }

        public void ShowPage(QinglanPageViewModel page)
        {
            if (canvas == null) throw new InvalidOperationException("UI root is not initialized.");
            var pageChanged = CurrentPage != page.Page;
            CurrentPage = page.Page;
            var runHudVisible = page.Page == QinglanUiPageId.RunHud;
            var runMapOverlayVisible = runHudVisible &&
                                       (!string.IsNullOrEmpty(page.SubtitleKey) || page.OptionCount > 0);
            pageBackground.gameObject.SetActive(!runHudVisible);
            pagePanel.gameObject.SetActive(!runHudVisible || runMapOverlayVisible);
            focusMarker.gameObject.SetActive(false);
            hudPanel.gameObject.SetActive(runHudVisible);
            dangerPanel.gameObject.SetActive(runHudVisible);
            pagePanel.rectTransform.anchorMin = new Vector2(runHudVisible ? 0.035f : 0.045f, runHudVisible ? 0.12f : 0.07f);
            pagePanel.rectTransform.anchorMax = new Vector2(runHudVisible ? 0.48f : 0.68f, runHudVisible ? 0.90f : 0.94f);
            RenderedOptionCount = page.OptionCount;
            RenderedSelectedIndex = page.SelectedIndex;
            ActiveButtonCount = page.OptionCount;
            ClickableButtonCount = 0;
            FormalOptionIconCount = 0;
            pageText.font = page.Page == QinglanUiPageId.TitleProfile ||
                            page.Page == QinglanUiPageId.StoryOverlay ||
                            page.Page == QinglanUiPageId.RunResult
                ? narrativeFont
                : boldFont;
            if (!runHudVisible) ApplyPageBackground(page.Page);
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
            renderedPageText = pageBuilder.ToString();

            headerBuilder.Clear();
            AppendKey(headerBuilder, page.TitleKey);
            if (!string.IsNullOrEmpty(page.SubtitleKey))
            {
                headerBuilder.Append("\n");
                AppendKey(headerBuilder, page.SubtitleKey);
            }
            if (!string.IsNullOrEmpty(page.StatusKey))
            {
                headerBuilder.Append("\n◆ ");
                AppendKey(headerBuilder, page.StatusKey);
                if (!string.IsNullOrEmpty(page.StatusValue)) headerBuilder.Append(": ").Append(page.StatusValue);
            }
            pageText.text = headerBuilder.ToString();

            EnsureOptionCardCount(page.OptionCount);
            for (var index = 0; index < optionCards.Count; index++)
            {
                var active = index < page.OptionCount;
                optionCards[index].Root.SetActive(active);
                if (!active) continue;
                var option = page.GetOptionAt(index);
                optionCards[index].ApplyFontScale(lastFontScale > 0f ? lastFontScale : 1f);
                if (option.Enabled && option.Command != QinglanUiCommand.None) ClickableButtonCount++;
                optionCards[index].Apply(
                    Resolve(option.LabelKey),
                    string.IsNullOrEmpty(option.DescriptionKey) ? string.Empty : Resolve(option.DescriptionKey),
                    ResolveOptionValue(option.ValueText),
                    ResolveCardMetadata(option),
                    ResolveOptionIcon(option),
                    option.Enabled && option.Command != QinglanUiCommand.None,
                    index == page.SelectedIndex);
            }
            if (optionContent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(optionContent);
            if (pageChanged && optionScroll != null) optionScroll.verticalNormalizedPosition = 1f;
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
            renderedHudText = hudBuilder.ToString();
            hudText.text = renderedHudText;
            healthFill.fillAmount = Ratio(snapshot.Health, snapshot.MaximumHealth);
            shieldFill.fillAmount = Ratio(snapshot.Shield, snapshot.MaximumShield);
            experienceFill.fillAmount = Ratio(snapshot.Experience, snapshot.RequiredExperience);
            vitalsText.text = "♥ " + Round(snapshot.Health) + "/" + Round(snapshot.MaximumHealth) +
                              "    ◇ " + Round(snapshot.Shield) + "/" + Round(snapshot.MaximumShield) +
                              "    Lv." + snapshot.Level;
            runStatusText.text = FormatTime(snapshot.DurationSeconds) + "    御风 " +
                                 (snapshot.MechanicTier + 1) + "/4  ≫ " + Round(snapshot.MechanicValue);
            bossBarRoot.SetActive(snapshot.HasBoss);
            if (snapshot.HasBoss)
            {
                bossFill.fillAmount = Ratio(snapshot.BossHealth, snapshot.BossMaximumHealth);
                bossText.text = "▲ " + ResolveContent(snapshot.BossId) + "  " +
                                (snapshot.BossPhase + 1) + "/" + snapshot.BossPhaseCount;
            }

            EnsureHudIconCount(snapshot.BuildCount);
            if (buildPanelRect != null)
            {
                var width = Mathf.Clamp(0.035f + (snapshot.BuildCount * 0.047f), 0.10f, 0.37f);
                buildPanelRect.anchorMax = new Vector2(0.018f + width, 0.145f);
            }
            VisibleHudIconCount = snapshot.BuildCount;
            for (var index = 0; index < hudIcons.Count; index++)
            {
                var active = index < snapshot.BuildCount;
                hudIcons[index].Root.SetActive(active);
                if (!active) continue;
                var item = snapshot.GetBuildAt(index);
                hudIcons[index].Apply(
                    ResolveContentIcon(item.ContentId),
                    "Lv." + item.Level + "/" + item.MaximumLevel,
                    BuildGlyph(item.Kind));
            }

            objectiveBuilder.Clear();
            AppendKey(objectiveBuilder, "ui.qinglan.hud.map");
            var visibleObjectives = Math.Min(snapshot.MapCount, 5);
            for (var index = 0; index < visibleObjectives; index++)
            {
                var item = snapshot.GetMapAt(index);
                objectiveBuilder.Append("\n").Append(MapGlyph(item.Kind)).Append(' ')
                    .Append(ResolveContent(item.ContentId)).Append("  ")
                    .Append(Round(item.Progress * 100f)).Append('%');
            }
            objectiveText.text = objectiveBuilder.ToString();
        }

        public void ApplyAccessibility(AccessibilitySettings settings)
        {
            if (settings == null || canvas == null) return;
            if (Math.Abs(lastFontScale - settings.FontScale) > 0.001f)
            {
                lastFontScale = settings.FontScale;
                pageText.fontSize = Mathf.RoundToInt(28f * settings.FontScale);
                hudText.fontSize = Mathf.RoundToInt(17f * settings.FontScale);
                dangerText.fontSize = Mathf.RoundToInt(16f * settings.FontScale);
                vitalsText.fontSize = Mathf.RoundToInt(18f * settings.FontScale);
                runStatusText.fontSize = Mathf.RoundToInt(18f * settings.FontScale);
                bossText.fontSize = Mathf.RoundToInt(17f * settings.FontScale);
                objectiveText.fontSize = Mathf.RoundToInt(15f * settings.FontScale);
                for (var index = 0; index < optionCards.Count; index++)
                    optionCards[index].ApplyFontScale(settings.FontScale);
            }
            if (lastColorVision == settings.ColorVision) return;
            lastColorVision = settings.ColorVision;
            pagePanel.color = PanelColor(settings.ColorVision);
            dangerText.color = DangerColor(settings.ColorVision);
            RefreshDangerLegend();
        }

        public bool SupportsCharacter(char character)
        {
            return regularFont != null && regularFont.HasCharacter(character, true, false);
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

        private string ResolveOptionValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.StartsWith("ui.", StringComparison.Ordinal) ||
                value.StartsWith("content.", StringComparison.Ordinal) ||
                value.StartsWith("story.", StringComparison.Ordinal) ||
                value.StartsWith("collectible.", StringComparison.Ordinal) ||
                value.StartsWith("narrative.", StringComparison.Ordinal))
                return Resolve(value);
            return value;
        }

        private string ResolveCardMetadata(QinglanUiOption option)
        {
            var value = string.Empty;
            if (!string.IsNullOrEmpty(option.TagKey)) value = Resolve(option.TagKey);
            if (!string.IsNullOrEmpty(option.RelationKey))
                value = string.IsNullOrEmpty(value) ? Resolve(option.RelationKey) : value + " · " + Resolve(option.RelationKey);
            if (!string.IsNullOrEmpty(option.EligibilityKey))
                value = string.IsNullOrEmpty(value) ? Resolve(option.EligibilityKey) : value + " · " + Resolve(option.EligibilityKey);
            return value;
        }

        private Sprite ResolveOptionIcon(QinglanUiOption option)
        {
            var contentId = option.StableId;
            if (option.LabelKey.StartsWith("content.", StringComparison.Ordinal))
            {
                contentId = option.LabelKey.Substring("content.".Length);
                if (contentId.EndsWith(".name", StringComparison.Ordinal))
                    contentId = contentId.Substring(0, contentId.Length - ".name".Length);
            }

            if (TryResolveContentIcon(contentId, out var sprite))
            {
                FormalOptionIconCount++;
                return sprite;
            }

            var commandKey = string.Equals(option.StableId, "back", StringComparison.Ordinal) ||
                             string.Equals(option.StableId, "close", StringComparison.Ordinal)
                ? "qinglan/ui/input-glyph/keyboard/escape"
                : option.Command == QinglanUiCommand.None
                    ? string.Empty
                    : "qinglan/ui/input-glyph/keyboard/enter";
            if (!string.IsNullOrEmpty(commandKey) && visualCatalog != null &&
                visualCatalog.TryResolveSprite(commandKey, out sprite))
            {
                FormalOptionIconCount++;
                return sprite;
            }
            return null;
        }

        private Sprite ResolveContentIcon(string contentId)
        {
            return TryResolveContentIcon(contentId, out var sprite) ? sprite : null;
        }

        private bool TryResolveContentIcon(string contentId, out Sprite sprite)
        {
            sprite = null;
            if (visualCatalog == null || string.IsNullOrEmpty(contentId)) return false;
            if (visualCatalog.TryResolveSprite(contentId, out sprite)) return true;
            string key = null;
            if (contentId.StartsWith("qinglan.character.", StringComparison.Ordinal))
                key = "qinglan/character/" + Hyphenate(contentId.Substring("qinglan.character.".Length)) + "/portrait";
            else if (contentId.StartsWith("qinglan.skill.weapon.", StringComparison.Ordinal))
                key = "qinglan/skill/base/" + Hyphenate(contentId.Substring("qinglan.skill.weapon.".Length)) + "/vfx";
            else if (contentId.StartsWith("qinglan.skill.evolved.", StringComparison.Ordinal))
                key = "qinglan/skill/evolved/" + Hyphenate(contentId.Substring("qinglan.skill.evolved.".Length)) + "/vfx";
            else if (contentId.StartsWith("qinglan.skill.", StringComparison.Ordinal) &&
                     !contentId.StartsWith("qinglan.skill.enemy.", StringComparison.Ordinal) &&
                     !contentId.StartsWith("qinglan.skill.boss.", StringComparison.Ordinal))
            {
                var skillName = contentId.Substring("qinglan.skill.".Length);
                const string weaponPrefix = "weapon.";
                if (skillName.StartsWith(weaponPrefix, StringComparison.Ordinal))
                    skillName = skillName.Substring(weaponPrefix.Length);
                key = "qinglan/skill/base/" + Hyphenate(skillName) + "/vfx";
            }
            else if (contentId.StartsWith("qinglan.relic.", StringComparison.Ordinal))
                key = "qinglan/relic/" + Hyphenate(contentId.Substring("qinglan.relic.".Length)) + "/icon";
            else if (contentId.StartsWith("qinglan.pickup.", StringComparison.Ordinal))
                key = "qinglan/pickup/" + Hyphenate(contentId.Substring("qinglan.pickup.".Length)) + "/icon";
            else if (contentId.StartsWith("qinglan.collectible.", StringComparison.Ordinal))
                key = "qinglan/collectible/" + Hyphenate(contentId.Substring("qinglan.collectible.".Length)) + "/icon";
            else if (contentId.StartsWith("qinglan.story.", StringComparison.Ordinal))
            {
                var storyPath = contentId.Substring("qinglan.story.".Length);
                var separator = storyPath.IndexOf('.');
                key = separator < 0
                    ? "qinglan/story/" + Hyphenate(storyPath) + "/key-illustration"
                    : "qinglan/story/" + Hyphenate(storyPath.Substring(0, separator)) + "/" +
                      Hyphenate(storyPath.Substring(separator + 1)) + "/key-illustration";
            }
            else
            {
                var facility = contentId.IndexOf(".facility.", StringComparison.Ordinal);
                var insert = contentId.IndexOf(".insert.", StringComparison.Ordinal);
                var innate = contentId.IndexOf(".innate.", StringComparison.Ordinal);
                var mind = contentId.IndexOf(".mind.", StringComparison.Ordinal);
                var movement = contentId.IndexOf(".movement.", StringComparison.Ordinal);
                if (facility >= 0)
                    key = "qinglan/hub/facility/" + Hyphenate(contentId.Substring(facility + ".facility.".Length)) + "/icon";
                else if (insert >= 0)
                    key = "qinglan/hub/insert/" + Hyphenate(contentId.Substring(insert + ".insert.".Length)) + "/icon";
                else if (innate >= 0)
                    key = "qinglan/hub/meta-node/innate/" + contentId.Substring(innate + ".innate.".Length) + "/icon";
                else if (mind >= 0)
                    key = "qinglan/hub/meta-node/mind/" + contentId.Substring(mind + ".mind.".Length) + "/icon";
                else if (movement >= 0)
                    key = "qinglan/hub/meta-node/movement/" + contentId.Substring(movement + ".movement.".Length) + "/icon";
            }
            if (!string.IsNullOrEmpty(key) && visualCatalog.TryResolveSprite(key, out sprite)) return true;
            return visualCatalog.TryResolveSprite("ui.focus", out sprite);
        }

        private static string Hyphenate(string value) => value.Replace('_', '-').Replace('.', '-');

        private static float Ratio(float value, float maximum) =>
            maximum <= 0f ? 0f : Mathf.Clamp01(value / maximum);

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

        private void EnsureEventSystem()
        {
            if (!UnityEngine.Application.isPlaying || EventSystem.current != null) return;
            var eventObject = new GameObject("Qinglan_EventSystem");
            eventObject.transform.SetParent(transform, false);
            eventObject.AddComponent<EventSystem>();
            var inputModule = eventObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private void CreateOptionScrollView()
        {
            var viewportObject = new GameObject(
                "Qinglan_OptionViewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask),
                typeof(ScrollRect));
            viewportObject.transform.SetParent(pagePanel.transform, false);
            var viewport = (RectTransform)viewportObject.transform;
            viewport.anchorMin = new Vector2(0f, 0.035f);
            viewport.anchorMax = new Vector2(1f, 0.64f);
            viewport.offsetMin = new Vector2(28f, 8f);
            viewport.offsetMax = new Vector2(-28f, -8f);
            var viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0.01f, 0.02f, 0.025f, 0.14f);
            viewportImage.raycastTarget = true;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            var contentObject = new GameObject(
                "Qinglan_OptionCards",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            optionContent = (RectTransform)contentObject.transform;
            optionContent.anchorMin = new Vector2(0f, 1f);
            optionContent.anchorMax = Vector2.one;
            optionContent.pivot = new Vector2(0.5f, 1f);
            optionContent.offsetMin = Vector2.zero;
            optionContent.offsetMax = Vector2.zero;
            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            optionScroll = viewportObject.GetComponent<ScrollRect>();
            optionScroll.viewport = viewport;
            optionScroll.content = optionContent;
            optionScroll.horizontal = false;
            optionScroll.vertical = true;
            optionScroll.movementType = ScrollRect.MovementType.Clamped;
            optionScroll.scrollSensitivity = 36f;
        }

        private void EnsureOptionCardCount(int count)
        {
            while (optionCards.Count < count)
            {
                var index = optionCards.Count;
                var rootObject = new GameObject(
                    "Qinglan_OptionCard_" + index,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button),
                    typeof(LayoutElement));
                rootObject.transform.SetParent(optionContent, false);
                var background = rootObject.GetComponent<Image>();
                background.color = new Color(0.055f, 0.12f, 0.14f, 0.96f);
                if (visualCatalog != null && visualCatalog.TryResolveSprite("ui.panel", out var panelSprite))
                {
                    background.sprite = panelSprite;
                    background.type = panelSprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
                }
                var layout = rootObject.GetComponent<LayoutElement>();
                layout.preferredHeight = 112f;
                layout.minHeight = 86f;
                var button = rootObject.GetComponent<Button>();
                button.targetGraphic = background;
                button.transition = Selectable.Transition.ColorTint;
                button.colors = new ColorBlock
                {
                    normalColor = Color.white,
                    highlightedColor = new Color(0.72f, 1f, 0.94f, 1f),
                    pressedColor = new Color(0.42f, 0.86f, 0.82f, 1f),
                    selectedColor = new Color(0.72f, 1f, 0.94f, 1f),
                    disabledColor = new Color(0.38f, 0.42f, 0.42f, 0.68f),
                    colorMultiplier = 1f,
                    fadeDuration = 0.08f
                };
                button.onClick.AddListener(() => OptionInvoked?.Invoke(index));

                var icon = CreateChildImage(rootObject.transform, "Icon", new Vector2(0f, 0f), new Vector2(0f, 1f),
                    new Vector2(14f, 14f), new Vector2(102f, -14f));
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var label = CreateCardText(rootObject.transform, "Label", 20, boldFont,
                    new Vector2(0f, 0.42f), new Vector2(0.72f, 1f), new Vector2(112f, 2f), new Vector2(-8f, -8f));
                var description = CreateCardText(rootObject.transform, "Description", 14, regularFont,
                    new Vector2(0f, 0f), new Vector2(0.76f, 0.45f), new Vector2(112f, 8f), new Vector2(-8f, -2f));
                var value = CreateCardText(rootObject.transform, "Value", 15, boldFont,
                    new Vector2(0.73f, 0.48f), new Vector2(0.98f, 0.92f), Vector2.zero, Vector2.zero);
                value.alignment = TextAlignmentOptions.Center;
                var metadata = CreateCardText(rootObject.transform, "Metadata", 12, regularFont,
                    new Vector2(0.73f, 0.08f), new Vector2(0.98f, 0.46f), Vector2.zero, Vector2.zero);
                metadata.alignment = TextAlignmentOptions.Center;
                var focus = CreateChildImage(rootObject.transform, "Focus", Vector2.zero, Vector2.one,
                    new Vector2(3f, 3f), new Vector2(-3f, -3f));
                focus.color = new Color(0.42f, 1f, 0.9f, 0.72f);
                focus.raycastTarget = false;
                if (visualCatalog != null && visualCatalog.TryResolveSprite("ui.focus", out var focusSprite))
                {
                    focus.sprite = focusSprite;
                    focus.preserveAspect = false;
                }
                focus.gameObject.SetActive(false);
                var card = new OptionCardView(
                    rootObject, background, button, layout, icon, focus, label, description, value, metadata);
                card.ApplyFontScale(lastFontScale > 0f ? lastFontScale : 1f);
                optionCards.Add(card);
            }
        }

        private void CreateHudWidgets()
        {
            var vitalsPanel = CreatePanelUnder(hudPanel.transform, "Qinglan_HudVitals",
                new Vector2(0.018f, 0.84f), new Vector2(0.35f, 0.982f));
            vitalsPanel.color = new Color(0.02f, 0.055f, 0.065f, 0.9f);
            healthFill = CreateBar(vitalsPanel.transform, "Health", new Vector2(0.035f, 0.54f), new Vector2(0.965f, 0.81f),
                new Color(0.82f, 0.19f, 0.19f, 1f));
            shieldFill = CreateBar(vitalsPanel.transform, "Shield", new Vector2(0.035f, 0.30f), new Vector2(0.965f, 0.49f),
                new Color(0.16f, 0.66f, 0.9f, 1f));
            experienceFill = CreateBar(vitalsPanel.transform, "Experience", new Vector2(0.035f, 0.10f), new Vector2(0.965f, 0.24f),
                new Color(0.30f, 0.88f, 0.58f, 1f));
            vitalsText = CreateText(vitalsPanel.transform, "VitalsLabel", 18, TextAlignmentOptions.TopLeft,
                new Vector2(18f, -2f), new Vector2(-18f, -4f));
            vitalsText.font = boldFont;
            vitalsText.raycastTarget = false;

            var runPanel = CreatePanelUnder(hudPanel.transform, "Qinglan_HudRunStatus",
                new Vector2(0.37f, 0.91f), new Vector2(0.69f, 0.982f));
            runPanel.color = new Color(0.02f, 0.055f, 0.065f, 0.86f);
            runStatusText = CreateText(runPanel.transform, "RunStatusLabel", 18, TextAlignmentOptions.Center,
                new Vector2(12f, 4f), new Vector2(-12f, -4f));
            runStatusText.font = boldFont;

            bossBarRoot = CreatePanelUnder(hudPanel.transform, "Qinglan_HudBoss",
                new Vector2(0.28f, 0.815f), new Vector2(0.72f, 0.895f)).gameObject;
            bossFill = CreateBar(bossBarRoot.transform, "BossHealth", new Vector2(0.025f, 0.13f), new Vector2(0.975f, 0.46f),
                new Color(0.82f, 0.22f, 0.56f, 1f));
            bossText = CreateText(bossBarRoot.transform, "BossLabel", 17, TextAlignmentOptions.Center,
                new Vector2(12f, 2f), new Vector2(-12f, -2f));
            bossText.font = boldFont;
            bossBarRoot.SetActive(false);

            var buildPanel = CreatePanelUnder(hudPanel.transform, "Qinglan_HudBuild",
                new Vector2(0.018f, 0.018f), new Vector2(0.62f, 0.145f));
            buildPanelRect = buildPanel.rectTransform;
            buildPanel.color = new Color(0.02f, 0.055f, 0.065f, 0.88f);
            var buildObject = new GameObject("Qinglan_HudBuildIcons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buildObject.transform.SetParent(buildPanel.transform, false);
            buildIconRoot = (RectTransform)buildObject.transform;
            buildIconRoot.anchorMin = Vector2.zero;
            buildIconRoot.anchorMax = Vector2.one;
            buildIconRoot.offsetMin = new Vector2(12f, 8f);
            buildIconRoot.offsetMax = new Vector2(-12f, -8f);
            var buildLayout = buildObject.GetComponent<HorizontalLayoutGroup>();
            buildLayout.spacing = 8f;
            buildLayout.childAlignment = TextAnchor.MiddleLeft;
            buildLayout.childControlHeight = true;
            buildLayout.childControlWidth = false;
            buildLayout.childForceExpandHeight = true;
            buildLayout.childForceExpandWidth = false;

            var objectivePanel = CreatePanelUnder(hudPanel.transform, "Qinglan_HudObjectives",
                new Vector2(0.73f, 0.80f), new Vector2(0.982f, 0.982f));
            objectivePanel.color = new Color(0.02f, 0.055f, 0.065f, 0.82f);
            objectiveText = CreateText(objectivePanel.transform, "ObjectiveLabel", 15, TextAlignmentOptions.TopLeft,
                new Vector2(18f, 14f), new Vector2(-18f, -14f));
        }

        private void EnsureHudIconCount(int count)
        {
            while (hudIcons.Count < count)
            {
                var index = hudIcons.Count;
                var rootObject = new GameObject(
                    "Qinglan_HudBuildIcon_" + index,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(LayoutElement));
                rootObject.transform.SetParent(buildIconRoot, false);
                rootObject.GetComponent<LayoutElement>().preferredWidth = 78f;
                var background = rootObject.GetComponent<Image>();
                background.color = new Color(0.08f, 0.16f, 0.18f, 0.94f);
                var icon = CreateChildImage(rootObject.transform, "Icon", new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.94f),
                    Vector2.zero, Vector2.zero);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var level = CreateCardText(rootObject.transform, "Level", 11, boldFont,
                    new Vector2(0f, 0f), new Vector2(1f, 0.27f), Vector2.zero, Vector2.zero);
                level.alignment = TextAlignmentOptions.Center;
                hudIcons.Add(new HudIconView(rootObject, icon, level));
            }
        }

        private Image CreateBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var background = CreateChildImage(parent, name + "Background", anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            background.color = new Color(0f, 0f, 0f, 0.68f);
            background.raycastTarget = false;
            var fill = CreateChildImage(background.transform, name + "Fill", Vector2.zero, Vector2.one,
                new Vector2(3f, 3f), new Vector2(-3f, -3f));
            fill.color = color;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;
            return fill;
        }

        private Image CreateChildImage(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var value = new GameObject(name, typeof(RectTransform), typeof(Image));
            value.transform.SetParent(parent, false);
            var rect = (RectTransform)value.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return value.GetComponent<Image>();
        }

        private TMP_Text CreateCardText(
            Transform parent,
            string name,
            int fontSize,
            TMP_FontAsset font,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var text = CreateText(parent, name, fontSize, TextAlignmentOptions.MidlineLeft, offsetMin, offsetMax);
            text.rectTransform.anchorMin = anchorMin;
            text.rectTransform.anchorMax = anchorMax;
            text.font = font;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(8f, fontSize * 0.5f);
            text.fontSizeMax = fontSize;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private Image CreatePanelUnder(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = CreatePanel(name, anchorMin, anchorMax);
            panel.transform.SetParent(parent, false);
            var rect = panel.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            if (visualCatalog != null && visualCatalog.TryResolveSprite("ui.panel", out var panelSprite))
            {
                panel.sprite = panelSprite;
                panel.type = panelSprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            }
            return panel;
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
            if (text == null || !text.gameObject.activeInHierarchy) return false;
            if (text.enableAutoSizing) return text.isTextOverflowing;
            var height = text.rectTransform.rect.height;
            return text.isTextOverflowing || (height > 0f && text.preferredHeight > height + 0.5f);
        }

        private sealed class OptionCardView
        {
            private readonly Image background;
            private readonly Button button;
            private readonly LayoutElement layout;
            private readonly Image icon;
            private readonly Image focus;
            private readonly TMP_Text label;
            private readonly TMP_Text description;
            private readonly TMP_Text value;
            private readonly TMP_Text metadata;

            public OptionCardView(
                GameObject root,
                Image cardBackground,
                Button cardButton,
                LayoutElement cardLayout,
                Image cardIcon,
                Image focusImage,
                TMP_Text labelText,
                TMP_Text descriptionText,
                TMP_Text valueText,
                TMP_Text metadataText)
            {
                Root = root;
                background = cardBackground;
                button = cardButton;
                layout = cardLayout;
                icon = cardIcon;
                focus = focusImage;
                label = labelText;
                description = descriptionText;
                value = valueText;
                metadata = metadataText;
            }

            public GameObject Root { get; }

            public bool HasOverflow =>
                IsOverflowing(label) || IsOverflowing(description) || IsOverflowing(value) || IsOverflowing(metadata);

            public void Apply(
                string labelValue,
                string descriptionValue,
                string valueValue,
                string metadataValue,
                Sprite iconSprite,
                bool interactable,
                bool selected)
            {
                label.text = labelValue;
                description.text = descriptionValue;
                value.text = valueValue;
                metadata.text = metadataValue;
                description.gameObject.SetActive(!string.IsNullOrEmpty(descriptionValue));
                value.gameObject.SetActive(!string.IsNullOrEmpty(valueValue));
                metadata.gameObject.SetActive(!string.IsNullOrEmpty(metadataValue));
                icon.sprite = iconSprite;
                icon.gameObject.SetActive(iconSprite != null);
                button.interactable = interactable;
                focus.gameObject.SetActive(selected);
                background.color = !interactable
                    ? new Color(0.055f, 0.07f, 0.075f, 0.78f)
                    : selected
                        ? new Color(0.10f, 0.30f, 0.30f, 0.98f)
                        : new Color(0.055f, 0.12f, 0.14f, 0.96f);
            }

            public void ApplyFontScale(float scale)
            {
                var cardHeight = 112f * Mathf.Max(1f, scale);
                layout.minHeight = cardHeight;
                layout.preferredHeight = cardHeight;
                ((RectTransform)Root.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardHeight);
                label.fontSizeMax = 20f * scale;
                description.fontSizeMax = 14f * scale;
                value.fontSizeMax = 15f * scale;
                metadata.fontSizeMax = 12f * scale;
            }
        }

        private sealed class HudIconView
        {
            private readonly Image icon;
            private readonly TMP_Text level;

            public HudIconView(GameObject root, Image iconImage, TMP_Text levelText)
            {
                Root = root;
                icon = iconImage;
                level = levelText;
            }

            public GameObject Root { get; }

            public void Apply(Sprite sprite, string levelValue, string fallbackGlyph)
            {
                icon.sprite = sprite;
                icon.gameObject.SetActive(sprite != null);
                level.text = sprite == null ? fallbackGlyph + " " + levelValue : levelValue;
            }
        }

        private void OnDestroy()
        {
            if (UnityEngine.Application.isPlaying) Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
