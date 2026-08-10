using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Game.Application;
using Game.Platform.Null;
using Game.Presentation;
using Game.Simulation;
using Game.UI;
using TMPro;
using UnityEngine;

namespace Game.Infrastructure
{
    /// <summary>
    /// Opt-in Development Player smoke driver for the real Bootstrap scene. It uses the
    /// public UI/application commands and exists only to make the built Player gate repeatable.
    /// </summary>
    internal sealed class QinglanG28DevelopmentSmokeRunner : MonoBehaviour
    {
        private const string DevelopmentArgument = "-qinglanG28Smoke";
        private const string ReleaseArgument = "-qinglanG36ReleaseSmoke";
        private const string ScreenshotDirectoryVariable = "QINGLAN_G36_RELEASE_SCREENSHOT_DIR";

        internal static bool IsRequested()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length; index++)
                if (string.Equals(arguments[index], DevelopmentArgument, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arguments[index], ReleaseArgument, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool IsReleaseRequested()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length; index++)
                if (string.Equals(arguments[index], ReleaseArgument, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private IEnumerator Start()
        {
            yield return null;
            var host = GetComponent<QinglanDemoRuntimeHost>();
            var bootstrapper = GetComponent<GameBootstrapper>();
            var releaseRequested = IsReleaseRequested();
            var result = new QinglanG28PlayerSmokeResult
            {
                schemaVersion = 4,
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                releaseCandidateRequested = releaseRequested,
                debugBuild = Debug.isDebugBuild,
                nullPlatform = bootstrapper?.PlatformFacade is NullPlatformFacade,
                contentPackCount = bootstrapper?.ContentSummary.PackCount ?? 0,
                contentDefinitionCount = bootstrapper?.ContentSummary.DefinitionCount ?? 0,
                status = "FAIL"
            };
            if (host == null)
            {
                Finish(result, "The Qinglan runtime host is missing.");
                yield break;
            }

            result.titleVisited = host.Flow.Stage == DemoFlowStage.Title;
            result.formalVisualsLoaded = host.FormalVisualsLoaded;
            result.formalAudioLoaded = host.FormalAudioLoaded;
            result.formalFontsLoaded = host.FormalFontsLoaded;
            result.formalGlyphsReady = host.Ui.SupportsCharacter('剑') &&
                                       host.Ui.SupportsCharacter('Ｒ') &&
                                       host.Ui.SupportsCharacter('【');
            result.formalLocalizationResolved = VerifyFormalLocalization(
                host,
                out var localeCyclePassed,
                out var localizationDiagnostic);
            result.localeCyclePassed = localeCyclePassed;
            result.localizationDiagnostic = localizationDiagnostic;
            result.formalUiBackgroundApplied = host.Ui.FormalBackgroundApplied;
            result.formalAudioCueRouted = host.Presentation.RouteUiCue(PresentationAudioCue.Confirm);
            if (!result.titleVisited || !host.Flow.Execute(QinglanUiCommand.Start, "start", 0))
            {
                Finish(result, "Title to character selection failed.");
                yield break;
            }
            result.characterSelectVisited = host.Flow.Stage == DemoFlowStage.CharacterSelect;
            if (!host.Flow.Execute(QinglanUiCommand.Continue, "character", 0) ||
                !host.Flow.Execute(QinglanUiCommand.OpenLoadout, "map", 0))
            {
                Finish(result, "Character/map/loadout selection failed.");
                yield break;
            }
            result.mapAndLoadoutVisited = host.Flow.Stage == DemoFlowStage.MapSelect;
            if (!host.Flow.Execute(QinglanUiCommand.BeginRun, "begin", 0))
            {
                Finish(result, "Run preparation failed.");
                yield break;
            }
            host.TickRuntime(0d);
            host.TickRuntime(SimulationClock.TickDurationSeconds);
            result.activeRunVisited = host.Flow.Stage == DemoFlowStage.Active;
            result.activeViews = host.Presentation.ActiveViewCount;
            result.gameplayWorldVisible = host.Ui.GameplayWorldVisible;
            result.mapGroundTileCount = host.Presentation.MapGroundTileCount;
            result.formalMapGroundTileCount = host.Presentation.FormalMapGroundTileCount;
            result.formalMapPropCount = host.Presentation.FormalMapPropCount;
            yield return CaptureScreenshotIfRequested(result, "active-gameplay");
            if (!result.activeRunVisited || result.activeViews <= 0 || !host.Flow.TogglePause() ||
                host.Flow.Stage != DemoFlowStage.UserPaused || !host.Flow.TogglePause())
            {
                Finish(result, "Active run or pause/resume flow failed.");
                yield break;
            }
            result.pauseResumeVisited = host.Flow.Stage == DemoFlowStage.Active;

            host.Flow.Settings.SetColorVision(ColorVisionMode.HighContrast);
            host.Flow.Settings.SetFlashIntensity(0.25f);
            host.Flow.Settings.SetDamageNumbersEnabled(false);
            result.accessibilityApplied = host.Flow.Settings.ColorVision ==
                                          ColorVisionMode.HighContrast &&
                                          !host.Flow.Settings.DamageNumbersEnabled;
            for (var attempt = 0; attempt < 8 && host.Flow.Stage == DemoFlowStage.Active; attempt++)
            {
                host.Flow.DebugRequestLevelUp();
                host.TickRuntime(SimulationClock.TickDurationSeconds);
            }
            result.upgradeVisited = host.Flow.Stage == DemoFlowStage.UpgradePaused;
            if (!result.upgradeVisited ||
                !host.Flow.Execute(QinglanUiCommand.SelectUpgrade, "upgrade", 0))
            {
                Finish(result, "Upgrade choice flow failed.");
                yield break;
            }
            host.TickRuntime(0d);
            if (!host.Flow.DebugCompleteRun())
            {
                Finish(result, "Development completion command failed.");
                yield break;
            }
            host.TickRuntime(0d);
            result.resultVisited = host.Flow.Stage == DemoFlowStage.Result;
            for (var attempt = 0; attempt < 240 && !host.Flow.LastCommit.IsSuccess; attempt++)
            {
                host.TickRuntime(0.05d);
                yield return null;
            }
            result.saveCommitted = host.Flow.LastCommit.IsSuccess;
            var saveRoot = GameBootstrapper.ResolveSaveRoot();
            result.profileSavePresent = File.Exists(Path.Combine(saveRoot, SaveSlots.Profile));
            result.runRecoveryCleared = !File.Exists(Path.Combine(saveRoot, SaveSlots.RunRecovery));
            if (!result.resultVisited || !result.saveCommitted ||
                !host.Flow.Execute(QinglanUiCommand.ContinueToHub, "hub", 0))
            {
                Finish(result, "Result settlement or hub transition failed.");
                yield break;
            }
            host.TickRuntime(0d);
            result.hubVisited = host.Flow.Stage == DemoFlowStage.Hub;
            result.activeViewsAfterHub = host.Presentation.ActiveViewCount;
            result.vfxCreated = host.Presentation.CreatedVfxCount;
            result.audioSourcesCreated = host.Presentation.CreatedAudioSourceCount;
            result.audioSourceCapacity = host.Presentation.AudioSourceCapacity;
            result.audioStemCapacity = host.Presentation.AudioStemCapacity;
            result.audioReservedCriticalCapacity = host.Presentation.AudioReservedCriticalCapacity;
            if (!result.hubVisited || result.activeViewsAfterHub != 0 ||
                !host.Flow.Execute(QinglanUiCommand.StartAgain, "again", 0))
            {
                Finish(result, "Hub cleanup or restart failed.");
                yield break;
            }
            result.restartVisited = host.Flow.Stage == DemoFlowStage.CharacterSelect;
            host.Flow.Settings.SetFontScale(1.5f);
            host.Ui.ApplyAccessibility(host.Flow.Settings);
            Canvas.ForceUpdateCanvases();
            result.layoutScalePassed = !host.Ui.HasAnyTextOverflow;
            result.layoutDiagnostic = BuildLayoutDiagnostic(host.Ui);
            result.inputOwnerCount = FindObjectsByType<M7InputRouter>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            result.releaseContractPassed = !releaseRequested ||
                                           (!result.debugBuild && result.nullPlatform &&
                                            result.contentPackCount == 1 &&
                                            result.contentDefinitionCount == 193 &&
                                            result.formalMapGroundTileCount > 0 &&
                                            result.formalMapPropCount > 0 &&
                                            result.profileSavePresent && result.runRecoveryCleared);
            var passed = result.titleVisited && result.characterSelectVisited &&
                         result.formalVisualsLoaded && result.formalUiBackgroundApplied &&
                         result.formalAudioLoaded && result.formalAudioCueRouted &&
                         result.formalFontsLoaded && result.formalGlyphsReady &&
                         result.formalLocalizationResolved && result.localeCyclePassed &&
                         result.layoutScalePassed &&
                         result.mapAndLoadoutVisited && result.activeRunVisited &&
                         result.gameplayWorldVisible && result.mapGroundTileCount > 0 &&
                         result.screenshotsPassed &&
                         result.pauseResumeVisited && result.accessibilityApplied &&
                         result.upgradeVisited && result.resultVisited && result.saveCommitted &&
                         result.hubVisited && result.restartVisited &&
                         result.activeViewsAfterHub == 0 && result.inputOwnerCount == 1 &&
                         result.vfxCreated <= 200 && result.audioSourcesCreated <= 32 &&
                         result.audioSourceCapacity == 32 && result.audioStemCapacity == 8 &&
                         result.audioReservedCriticalCapacity == 8 && result.releaseContractPassed;
            result.status = passed ? "PASS" : "FAIL";
            result.error = passed ? string.Empty : "One or more Player smoke assertions failed.";
            WriteAndQuit(result, passed ? 0 : 2);
        }

        private static IEnumerator CaptureScreenshotIfRequested(
            QinglanG28PlayerSmokeResult result,
            string name)
        {
            var directory = Environment.GetEnvironmentVariable(ScreenshotDirectoryVariable);
            if (string.IsNullOrWhiteSpace(directory))
            {
                result.screenshotsPassed = true;
                yield break;
            }

            directory = Path.GetFullPath(directory);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, name + ".png");
            if (File.Exists(path)) File.Delete(path);
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            for (var frame = 0; frame < 120; frame++)
            {
                if (File.Exists(path) && new FileInfo(path).Length > 0)
                {
                    result.activeGameplayScreenshot = path;
                    result.screenshotsPassed = true;
                    yield break;
                }
                yield return null;
            }

            result.screenshotsPassed = false;
        }

        private static bool VerifyFormalLocalization(
            QinglanDemoRuntimeHost host,
            out bool localeCyclePassed,
            out string diagnostic)
        {
            localeCyclePassed = false;
            diagnostic = string.Empty;
            var localization = host.Localization;
            if (!localization.SelectLocale("en")) return false;
            var englishUi = localization.Resolve("ui.qinglan.title.subtitle");
            var englishContent = localization.Resolve("content.qinglan.skill.weapon.yufeng_sword.name");
            var englishNarrative = localization.Resolve("story.qinglan.story.lu_qingye.hearing_sword.01");
            var englishOk = englishUi ==
                            "The old court waits for the wind to return." &&
                            englishContent == "Yufeng Sword" &&
                            englishNarrative.StartsWith("When the wind crossed", StringComparison.Ordinal);
            if (!localization.SelectLocale("zh-Hans")) return false;
            var chineseContent = localization.Resolve("content.qinglan.skill.weapon.yufeng_sword.name");
            var chineseNarrative = localization.Resolve("story.qinglan.story.lu_qingye.hearing_sword.01");
            var chineseOk = chineseContent == "御风剑" &&
                            chineseNarrative.StartsWith("风过残碑时", StringComparison.Ordinal);
            if (!localization.SelectLocale("pseudo")) return false;
            var pseudo = localization.Resolve("ui.qinglan.title.subtitle");
            var pseudoOk = pseudo.StartsWith("【", StringComparison.Ordinal) &&
                           pseudo.IndexOf("Ｔ", StringComparison.Ordinal) >= 0;
            localeCyclePassed = localization.SelectNextLocale() && localization.SelectedLocaleCode == "en";
            diagnostic = "enUi=" + englishUi + " | enContent=" + englishContent +
                         " | enNarrative=" + englishNarrative + " | zhContent=" + chineseContent +
                         " | zhNarrative=" + chineseNarrative + " | pseudo=" + pseudo;
            return englishOk && chineseOk && pseudoOk;
        }

        private static string BuildLayoutDiagnostic(QinglanRuntimeUiRoot ui)
        {
            var texts = ui.GetComponentsInChildren<TMP_Text>(true);
            var output = new System.Text.StringBuilder(256);
            for (var index = 0; index < texts.Length; index++)
            {
                var text = texts[index];
                if (!text.name.StartsWith("Qinglan_", StringComparison.Ordinal)) continue;
                text.ForceMeshUpdate();
                if (output.Length > 0) output.Append(" | ");
                output.Append(text.name)
                    .Append(":rect=").Append(text.rectTransform.rect.height.ToString("0.0", CultureInfo.InvariantCulture))
                    .Append(",preferred=").Append(text.preferredHeight.ToString("0.0", CultureInfo.InvariantCulture))
                    .Append(",overflow=").Append(text.isTextOverflowing);
            }
            return output.ToString();
        }

        private static void Finish(QinglanG28PlayerSmokeResult result, string error)
        {
            result.status = "FAIL";
            result.error = error;
            WriteAndQuit(result, 2);
        }

        private static void WriteAndQuit(QinglanG28PlayerSmokeResult result, int exitCode)
        {
            try
            {
                var path = result.releaseCandidateRequested
                    ? Environment.GetEnvironmentVariable("QINGLAN_G36_RELEASE_PLAYER_RESULT")
                    : Environment.GetEnvironmentVariable("QINGLAN_G28_PLAYER_RESULT");
                if (string.IsNullOrWhiteSpace(path))
                    path = Path.Combine(
                        UnityEngine.Application.persistentDataPath,
                        result.releaseCandidateRequested
                            ? "QinglanG36ReleasePlayer.json"
                            : "QinglanG28PlayerSmoke.json");
                path = Path.GetFullPath(path);
                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory)) throw new InvalidOperationException("Invalid smoke result path.");
                Directory.CreateDirectory(directory);
                File.WriteAllText(path, JsonUtility.ToJson(result, true) + "\n");
                var marker = result.releaseCandidateRequested
                    ? "[Qinglan G3.6 Release Player]"
                    : "[Qinglan G2.8 Player Smoke]";
                if (exitCode == 0) Debug.Log(marker + " PASS: " + path);
                else Debug.LogError(marker + " FAIL: " + result.error);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                exitCode = 3;
            }
            UnityEngine.Application.Quit(exitCode);
        }

        [Serializable]
        private sealed class QinglanG28PlayerSmokeResult
        {
            public int schemaVersion;
            public string status;
            public string error;
            public string generatedAtUtc;
            public bool releaseCandidateRequested;
            public bool releaseContractPassed;
            public bool debugBuild;
            public bool nullPlatform;
            public int contentPackCount;
            public int contentDefinitionCount;
            public bool titleVisited;
            public bool formalVisualsLoaded;
            public bool formalAudioLoaded;
            public bool formalFontsLoaded;
            public bool formalLocalizationResolved;
            public bool localeCyclePassed;
            public bool formalGlyphsReady;
            public bool layoutScalePassed;
            public string localizationDiagnostic;
            public string layoutDiagnostic;
            public bool formalAudioCueRouted;
            public bool formalUiBackgroundApplied;
            public bool characterSelectVisited;
            public bool mapAndLoadoutVisited;
            public bool activeRunVisited;
            public bool gameplayWorldVisible;
            public bool screenshotsPassed;
            public string activeGameplayScreenshot;
            public int mapGroundTileCount;
            public int formalMapGroundTileCount;
            public int formalMapPropCount;
            public bool pauseResumeVisited;
            public bool accessibilityApplied;
            public bool upgradeVisited;
            public bool resultVisited;
            public bool saveCommitted;
            public bool profileSavePresent;
            public bool runRecoveryCleared;
            public bool hubVisited;
            public bool restartVisited;
            public int activeViews;
            public int activeViewsAfterHub;
            public int inputOwnerCount;
            public int vfxCreated;
            public int audioSourcesCreated;
            public int audioSourceCapacity;
            public int audioStemCapacity;
            public int audioReservedCriticalCapacity;
        }
    }
}
