using System.Linq;
using Game.Application;
using Game.Core;
using Game.Infrastructure;
using Game.Presentation;
using Game.Simulation;
using Game.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NumericsVector2 = System.Numerics.Vector2;

namespace Game.Tests.EditMode
{
    public sealed class QinglanG42VisualThemeTests
    {
        [Test]
        public void UiAndPresentationPalettesMatchTheStyleBible()
        {
            AssertToken(QinglanUiTheme.Ink950, QinglanPresentationTheme.Ink950, 16, 42, 45);
            AssertToken(QinglanUiTheme.Ink800, QinglanPresentationTheme.Ink800, 28, 66, 67);
            AssertToken(QinglanUiTheme.Jade500, QinglanPresentationTheme.Jade500, 66, 184, 173);
            AssertToken(QinglanUiTheme.Jade200, QinglanPresentationTheme.Jade200, 169, 229, 216);
            AssertToken(QinglanUiTheme.Rice100, QinglanPresentationTheme.Rice100, 242, 235, 216);
            AssertToken(QinglanUiTheme.Gold400, QinglanPresentationTheme.Gold400, 215, 181, 90);
            AssertToken(QinglanUiTheme.Cinnabar500, QinglanPresentationTheme.Cinnabar500, 228, 87, 61);
            AssertToken(QinglanUiTheme.Cinnabar300, QinglanPresentationTheme.Cinnabar300, 255, 139, 98);
            AssertToken(QinglanUiTheme.Void700, QinglanPresentationTheme.Void700, 86, 60, 118);
            Assert.That(QinglanUiTheme.MinimumBodyFontSize1080p, Is.GreaterThanOrEqualTo(18));
            Assert.That(QinglanUiTheme.MaximumFontScale, Is.EqualTo(2f));
            Assert.That(QinglanUiTheme.MinimumControlHeight, Is.GreaterThanOrEqualTo(52f));
            Assert.That(QinglanUiTheme.MotionInstantSeconds, Is.LessThan(QinglanUiTheme.MotionFastSeconds));
            Assert.That(QinglanUiTheme.MotionFastSeconds, Is.LessThan(QinglanUiTheme.MotionBaseSeconds));
            Assert.That(QinglanUiTheme.MotionBaseSeconds, Is.LessThan(QinglanUiTheme.MotionSlowSeconds));
            const float pixelsPerWorldUnitAt1080p = 1080f / (8.6f * 2f);
            Assert.That(QinglanPresentationTheme.PlayerActorScale * pixelsPerWorldUnitAt1080p,
                Is.InRange(96f, 120f));
            Assert.That(QinglanPresentationTheme.EnemyActorScale * pixelsPerWorldUnitAt1080p,
                Is.InRange(72f, 104f));
            Assert.That(QinglanPresentationTheme.BossActorScale * pixelsPerWorldUnitAt1080p,
                Is.InRange(180f, 280f));
            Assert.That(QinglanPresentationTheme.HeldWeaponAttackScale,
                Is.GreaterThan(QinglanPresentationTheme.HeldWeaponMoveScale));
            Assert.That(QinglanPresentationTheme.HeldWeaponMoveScale,
                Is.GreaterThan(QinglanPresentationTheme.HeldWeaponIdleScale));
            Assert.That(QinglanPresentationTheme.HeldWeaponAttackArcDegrees, Is.EqualTo(72f));
        }

        [Test]
        public void RuntimeUiUsesDistinctPageCompositionsAndSupportsTwoHundredPercentText()
        {
            var root = new GameObject("G42PageCompositions");
            try
            {
                var ui = root.AddComponent<QinglanRuntimeUiRoot>();
                ui.Initialize(new EchoLocalization(), id => "content." + id + ".name");
                var page = new QinglanPageViewModel(4);

                page.Reset(QinglanUiPageId.TitleProfile, "ui.qinglan.title.name");
                page.Add(new QinglanUiOption("start", "ui.qinglan.title.start", "", QinglanUiCommand.Start));
                ui.ShowPage(page);
                var pageLayer = (RectTransform)root.transform.Find("Qinglan_PageLayer");
                var viewport = (RectTransform)pageLayer.Find("Qinglan_OptionViewport");
                var hero = (RectTransform)pageLayer.Find("Qinglan_PageHero");
                Assert.That(pageLayer.anchorMax.x, Is.EqualTo(0.43f).Within(0.001f));
                Assert.That(hero.gameObject.activeSelf, Is.False);

                page.Reset(QinglanUiPageId.CharacterSelect, "ui.qinglan.character.title");
                page.Add(new QinglanUiOption(
                    "qinglan.character.yunli",
                    "content.qinglan.character.yunli.name",
                    "content.qinglan.character.yunli.description",
                    QinglanUiCommand.Continue));
                ui.ShowPage(page);
                Assert.That(pageLayer.anchorMax.x, Is.EqualTo(0.965f).Within(0.001f));
                Assert.That(hero.gameObject.activeSelf, Is.True);
                Assert.That(hero.anchorMin.x, Is.GreaterThan(0.5f));
                Assert.That(viewport.anchorMax.x, Is.LessThanOrEqualTo(0.51f));

                page.Reset(QinglanUiPageId.MapSelect, "ui.qinglan.map.title");
                page.Add(new QinglanUiOption(
                    "qinglan.map.qingyun",
                    "content.qinglan.map.qingyun.name",
                    "content.qinglan.map.qingyun.description",
                    QinglanUiCommand.Continue));
                ui.ShowPage(page);
                Assert.That(hero.gameObject.activeSelf, Is.True);
                Assert.That(hero.anchorMax.x, Is.LessThanOrEqualTo(0.47f));
                Assert.That(viewport.anchorMin.x, Is.GreaterThanOrEqualTo(0.50f));

                page.Reset(QinglanUiPageId.Settings, "ui.qinglan.settings.title");
                page.Add(new QinglanUiOption(
                    "font_scale",
                    "ui.qinglan.settings.font_scale",
                    "",
                    QinglanUiCommand.CycleSetting,
                    true,
                    "200%"));
                ui.ShowPage(page);
                var settings = new AccessibilitySettings();
                settings.SetFontScale(2f);
                ui.ApplyAccessibility(settings);
                Assert.That(settings.FontScale, Is.EqualTo(2f));
                Assert.That(hero.gameObject.activeSelf, Is.False);
                Assert.That(ui.SettingsPreviewVisible, Is.True);
                Assert.That(viewport.anchorMax.x, Is.EqualTo(0.62f).Within(0.001f));
                var firstCard = pageLayer.Find("Qinglan_OptionViewport/Qinglan_OptionCards/Qinglan_OptionCard_0");
                Assert.That(firstCard.GetComponent<LayoutElement>().preferredHeight, Is.GreaterThanOrEqualTo(440f));

                page.Reset(QinglanUiPageId.RunResult, "ui.qinglan.result.title");
                page.Add(new QinglanUiOption("hub", "ui.qinglan.result.hub", "", QinglanUiCommand.ContinueToHub));
                ui.ShowPage(page);
                Assert.That(pageLayer.anchorMin.x, Is.EqualTo(0.12f).Within(0.001f));
                Assert.That(pageLayer.anchorMax.x, Is.EqualTo(0.88f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
        [Test]
        public void RuntimeUiUsesThreePrimaryChoiceCardsAndKeepsDangerLegendOutOfCombat()
        {
            var root = new GameObject("G42UiLayout");
            try
            {
                var ui = root.AddComponent<QinglanRuntimeUiRoot>();
                ui.Initialize(new EchoLocalization(), id => "content." + id + ".name");
                var page = new QinglanPageViewModel(5);
                page.Reset(QinglanUiPageId.LevelUpChoice, "ui.qinglan.level_up.title");
                for (var index = 0; index < 3; index++)
                    page.Add(new QinglanUiOption(
                        "choice." + index,
                        "content.choice." + index + ".name",
                        "content.choice." + index + ".description",
                        QinglanUiCommand.SelectUpgrade,
                        true,
                        "150%",
                        "ui.qinglan.card.tag.skill"));
                page.Add(new QinglanUiOption("reroll", "ui.qinglan.level_up.reroll", "",
                    QinglanUiCommand.RerollUpgrade));
                page.Add(new QinglanUiOption("skip", "ui.qinglan.level_up.skip", "",
                    QinglanUiCommand.SkipUpgrade));
                ui.ShowPage(page);

                Assert.That(ui.UsesChoiceCardLayout, Is.True);
                Assert.That(ui.PermanentDangerLegendVisible, Is.False);
                for (var index = 0; index < 3; index++)
                {
                    var card = root.transform.Find(
                        "Qinglan_PageLayer/Qinglan_OptionViewport/Qinglan_OptionCards/Qinglan_OptionCard_" + index);
                    Assert.That(card, Is.Not.Null);
                    Assert.That(card.GetComponent<LayoutElement>().ignoreLayout, Is.True);
                    var rect = (RectTransform)card;
                    Assert.That(rect.anchorMin.y, Is.GreaterThanOrEqualTo(0.24f));
                    Assert.That(rect.anchorMax.y, Is.EqualTo(0.97f).Within(0.001f));
                }

                page.Reset(QinglanUiPageId.Settings, "ui.qinglan.settings.title");
                page.Add(new QinglanUiOption("font_scale", "ui.qinglan.settings.font_scale", "",
                    QinglanUiCommand.CycleSetting, true, "150%"));
                ui.ShowPage(page);
                var settings = new AccessibilitySettings();
                settings.SetFontScale(1.5f);
                settings.SetColorVision(ColorVisionMode.HighContrast);
                ui.ApplyAccessibility(settings);
                Assert.That(ui.UsesChoiceCardLayout, Is.False);
                Assert.That(ui.SettingsPreviewVisible, Is.True);
                Assert.That(ui.PermanentDangerLegendVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void WorldRegionsUseSoftBoundariesAndFormalStatefulLandmarks()
        {
            var root = new GameObject("G42CWorldRegions");
            var texture = new Texture2D(4, 4);
            var states = new Sprite[3];
            try
            {
                for (var index = 0; index < states.Length; index++)
                    states[index] = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, 4f, 4f),
                        Vector2.one * 0.5f,
                        4f);
                var markerId = ContentId.Create("test.map.objective.altar").Value;
                var canvas = new GameObject("Canvas").AddComponent<Canvas>();
                canvas.transform.SetParent(root.transform, false);
                var coordinator = root.AddComponent<PresentationCoordinator>();
                coordinator.Initialize(canvas, new AccessibilitySettings());
                coordinator.SetMap(new ProceduralMapConfiguration(
                    new Vector2(-48f, -36f),
                    new Vector2(48f, 36f),
                    16f,
                    null,
                    new[]
                    {
                        Vector2.zero,
                        new Vector2(-30f, 0f),
                        new Vector2(30f, 0f),
                        new Vector2(0f, 24f),
                        new Vector2(0f, -24f)
                    },
                    new[]
                    {
                        new ProceduralMapMarker(
                            markerId,
                            1,
                            new Vector2(8f, 6f),
                            new ProceduralMapMarkerSpriteSet(states))
                    }));

                Assert.That(coordinator.MapRegionTransitionDecalCount, Is.EqualTo(32));
                Assert.That(coordinator.MapRegionIdentityClusterCount, Is.EqualTo(5));
                Assert.That(coordinator.FormalMapMarkerCount, Is.EqualTo(1));
                Assert.That(coordinator.FormalMapMarkerStateSpriteCount, Is.EqualTo(3));

                var snapshot = new RunUiSnapshot();
                snapshot.Tick = 1;
                snapshot.AddMap(markerId.Value, 1, 4, 0.5f);
                coordinator.SyncRunState(snapshot);
                Assert.That(coordinator.VisibleFormalMapMarkerCount, Is.EqualTo(1));
                Assert.That(coordinator.CompletedFormalMapMarkerCount, Is.Zero);

                snapshot.Reset();
                snapshot.Tick = 2;
                snapshot.AddMap(markerId.Value, 1, 6, 1f);
                coordinator.SyncRunState(snapshot);
                Assert.That(coordinator.VisibleFormalMapMarkerCount, Is.EqualTo(1));
                Assert.That(coordinator.CompletedFormalMapMarkerCount, Is.EqualTo(1));
                var formalMarker = root.transform.Find("G2_7_ProceduralMap/FormalMapMarker_1_0");
                Assert.That(formalMarker, Is.Not.Null);
                Assert.That(formalMarker.position.y, Is.GreaterThan(1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                for (var index = 0; index < states.Length; index++)
                    if (states[index] != null) Object.DestroyImmediate(states[index]);
                Object.DestroyImmediate(texture);
            }
        }
        [Test]
        public void WorldSliceAddsNaturalArenaFalloffAndKeepsDensePickupSimulationViews()
        {
            var root = new GameObject("G42WorldReadability");
            try
            {
                var canvas = new GameObject("Canvas").AddComponent<Canvas>();
                canvas.transform.SetParent(root.transform, false);
                var coordinator = root.AddComponent<PresentationCoordinator>();
                coordinator.Initialize(canvas, new AccessibilitySettings());
                coordinator.SetMap(new ProceduralMapConfiguration(
                    new Vector2(-48f, -36f),
                    new Vector2(48f, 36f),
                    16f,
                    null,
                    null,
                    null));

                var world = new SimulationWorld();
                world.CreateArea(SimulationEntityState.Create(
                    new NumericsVector2(8f, 0f),
                    NumericsVector2.Zero));
                for (var index = 0; index < 40; index++)
                    world.CreatePickup(SimulationEntityState.Create(
                        new NumericsVector2(8f + (index % 8), 8f + (index / 8)),
                        NumericsVector2.Zero));
                new FixedTickRunner(world).Advance(SimulationClock.TickDurationSeconds);
                coordinator.Sync(world.RenderSnapshot, 1f);

                Assert.That(coordinator.MapCentralArenaTransitionCount, Is.EqualTo(14));
                Assert.That(coordinator.ActivePickupViewCount, Is.EqualTo(40),
                    "density grouping must not remove simulation-backed presentation views");
                Assert.That(coordinator.DensePickupPresentationActive, Is.True);
                Assert.That(coordinator.DensityGroupedPickupViewCount, Is.EqualTo(40));
                Assert.That(coordinator.DensityEmphasisPickupViewCount, Is.EqualTo(10));
                Assert.That(coordinator.ActiveAreaViewCount, Is.EqualTo(1));
                for (var index = 0; index < world.RenderSnapshot.Count; index++)
                {
                    var entity = world.RenderSnapshot.GetAt(index).Entity;
                    if (entity.Kind != EntityKind.Area) continue;
                    Assert.That(coordinator.TryGetView(entity, out var area), Is.True);
                    Assert.That(area.DangerFillVisible, Is.True);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CombatHudShowsOnlySixCoreBuildSlotsAndSummarizesTheRest()
        {
            var root = new GameObject("G42HudBuildDensity");
            try
            {
                var ui = root.AddComponent<QinglanRuntimeUiRoot>();
                ui.Initialize(new EchoLocalization(), id => "content." + id + ".name");
                var snapshot = new RunUiSnapshot();
                for (var index = 0; index < 11; index++)
                    snapshot.AddBuild("qinglan.test.build." + index, 1, 8, 1);
                for (var index = 0; index < 6; index++)
                    snapshot.AddMap("qinglan.test.objective." + index, (byte)((index % 3) + 1), 1, 0.5f);

                ui.ShowHud(snapshot);
                var accessibility = new AccessibilitySettings();
                accessibility.SetFontScale(2f);
                ui.ApplyAccessibility(accessibility);
                Canvas.ForceUpdateCanvases();

                Assert.That(ui.VisibleHudIconCount, Is.EqualTo(6));
                Assert.That(ui.HiddenHudBuildCount, Is.EqualTo(5));
                Assert.That(root.transform.Find(
                    "Qinglan_HudLayer/Qinglan_HudBuild/Qinglan_HudBuildIcon_6"), Is.Null);
                var overflow = root.transform.Find(
                    "Qinglan_HudLayer/Qinglan_HudBuild/M7_HudBuildOverflow");
                Assert.That(overflow, Is.Not.Null);
                Assert.That(overflow.gameObject.activeSelf, Is.True);
                Assert.That(overflow.GetComponent<TMP_Text>().text, Is.EqualTo("+5"));
                var objectives = (RectTransform)root.transform.Find("Qinglan_HudLayer/Qinglan_HudObjectives");
                Assert.That(objectives.anchorMin.y, Is.LessThanOrEqualTo(0.53f));
                Assert.That(objectives.anchorMin.x, Is.LessThanOrEqualTo(0.59f));
                var overflowDiagnostic = string.Join(" | ", ui.GetComponentsInChildren<TMP_Text>(true)
                    .Where(text => text.gameObject.activeInHierarchy)
                    .Select(text => text.name + ":rect=" + text.rectTransform.rect.height.ToString("0.0") +
                                    ",preferred=" + text.preferredHeight.ToString("0.0") +
                                    ",overflow=" + text.isTextOverflowing));
                Assert.That(ui.HasAnyTextOverflow, Is.False, overflowDiagnostic);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AcceptanceRouteAdvancesFromSimulationPositionAndSettlesAtFinalWaypoint()
        {
            var waypointIndex = 0;
            var movement = QinglanG40VisualAcceptanceRunner.ResolveWaypointMovement(
                new Vector2(10f, -10f),
                true,
                ref waypointIndex);

            Assert.That(waypointIndex, Is.EqualTo(1));
            Assert.That(movement.x, Is.GreaterThan(0.9f));
            Assert.That(movement.y, Is.LessThan(0f));

            waypointIndex = 4;
            movement = QinglanG40VisualAcceptanceRunner.ResolveWaypointMovement(
                new Vector2(34f, 10f),
                true,
                ref waypointIndex);

            Assert.That(waypointIndex, Is.EqualTo(4));
            Assert.That(movement, Is.EqualTo(Vector2.zero));

            movement = QinglanG40VisualAcceptanceRunner.ResolveWaypointMovement(
                new Vector2(34f, 10f),
                true,
                false,
                ref waypointIndex);
            Assert.That(waypointIndex, Is.EqualTo(0));
            Assert.That(movement.x, Is.LessThan(0f));
            Assert.That(movement.y, Is.LessThan(0f));
        }

        [Test]
        public void AcceptanceClockMapsWallTimeToWholeFixedTicks()
        {
            Assert.That(QinglanG40VisualAcceptanceRunner.CalculateTargetSimulationTickCount(0d), Is.Zero);
            Assert.That(
                QinglanG40VisualAcceptanceRunner.CalculateTargetSimulationTickCount(90d),
                Is.EqualTo(8775L));
            Assert.That(
                QinglanG40VisualAcceptanceRunner.CalculateTargetSimulationTickCount(720d, 1d),
                Is.EqualTo(21600L));
        }

        private static void AssertToken(
            Color32 ui,
            Color32 presentation,
            byte red,
            byte green,
            byte blue)
        {
            Assert.That(ui, Is.EqualTo(new Color32(red, green, blue, 255)));
            Assert.That(presentation, Is.EqualTo(ui));
        }

        private sealed class EchoLocalization : ILocalizationService
        {
            public string SelectedLocaleCode => "en";
            public string Resolve(string localizationKey) => "loc:" + localizationKey;
            public bool SelectLocale(string localeCode) => true;
            public bool SelectNextLocale() => true;
        }
    }
}
