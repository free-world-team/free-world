using System;
using Game.Application;
using Game.Content.Runtime;
using Game.Core;
using Game.Presentation;
using Game.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Infrastructure
{
    /// <summary>Single Unity owner for the G2.6 Demo UI, input, View, and run lifecycle.</summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class QinglanDemoRuntimeHost : MonoBehaviour
    {
        private const double UiRefreshIntervalSeconds = 0.1d;
        private QinglanDemoPresenter presenter;
        private PresentationCameraRig cameraRig;
        private RunSession lastSession;
        private DemoFlowStage lastStage;
        private double uiRefreshAccumulator;
        private string lastLocaleCode = string.Empty;
        private string pendingStartupLocaleCode = string.Empty;
        private bool initialized;
        private QinglanFormalVisualLoader formalVisualLoader;
        private QinglanFormalAudioLoader formalAudioLoader;
        private QinglanFormalFontLoader formalFontLoader;
        private Light presentationLight;
        private bool visualAcceptanceMovementEnabled;
        private Vector2 visualAcceptanceMovement;

        public QinglanDemoFlowController Flow { get; private set; }
        public M7InputRouter Input { get; private set; }
        public QinglanRuntimeUiRoot Ui { get; private set; }
        public PresentationCoordinator Presentation { get; private set; }
        public ILocalizationService Localization { get; private set; }
        public QinglanPageViewModel CurrentPage => presenter?.CurrentPage;
        public bool FormalVisualsLoaded => formalVisualLoader?.IsLoaded == true;
        internal FormalVisualCatalog FormalVisualCatalog => formalVisualLoader?.Catalog;
        public bool FormalAudioLoaded => formalAudioLoader?.IsLoaded == true;
        public bool FormalFontsLoaded => formalFontLoader?.IsLoaded == true;

        public void Initialize(
            GameApplication application,
            Camera presentationCamera,
            InputActionAsset inputActions,
            M8RuntimeServices runtimeServices)
        {
            if (initialized) throw new InvalidOperationException("QinglanDemoRuntimeHost is already initialized.");
            if (application == null) throw new ArgumentNullException(nameof(application));
            if (runtimeServices == null) throw new ArgumentNullException(nameof(runtimeServices));
            bootstrapApplication = application;
            Localization = new UnityLocalizationService();
            pendingStartupLocaleCode = runtimeServices.Settings.LocaleCode;

            Input = gameObject.AddComponent<M7InputRouter>();
            Input.Initialize(inputActions);
            Input.ApplyBindingOverrides(runtimeServices.Settings.BindingOverrides);
            Flow = new QinglanDemoFlowController(application, runtimeServices, Input, Localization);
            formalVisualLoader = new QinglanFormalVisualLoader();
            if (!formalVisualLoader.LoadForStartup())
                Debug.LogWarning("[Qinglan Formal Visuals] Development fallback: " + formalVisualLoader.LastError);
            formalAudioLoader = new QinglanFormalAudioLoader();
            if (!formalAudioLoader.LoadForStartup())
                Debug.LogWarning("[Qinglan Formal Audio] Development fallback: " + formalAudioLoader.LastError);
            formalFontLoader = new QinglanFormalFontLoader();
            if (!formalFontLoader.LoadForStartup())
                Debug.LogWarning("[Qinglan Formal Fonts] Default TMP fallback: " + formalFontLoader.LastError);

            var uiObject = new GameObject("Qinglan_Demo_UI");
            uiObject.transform.SetParent(transform, false);
            Ui = uiObject.AddComponent<QinglanRuntimeUiRoot>();
            Ui.Initialize(Localization, ResolveContentNameKey, formalVisualLoader, formalFontLoader);
            Ui.OptionInvoked += OnOptionInvoked;

            var presentationObject = new GameObject("Qinglan_Demo_Presentation");
            presentationObject.transform.SetParent(transform, false);
            Presentation = presentationObject.AddComponent<PresentationCoordinator>();
            Presentation.Initialize(
                Ui.SharedCanvas,
                Flow.Settings,
                formalVisualLoader.Catalog == null ? null : formalVisualLoader.Catalog.CreateEntityCatalog(),
                QinglanProceduralPresentationFactory.Build(application.ContentRegistry),
                formalVisualLoader.Catalog,
                formalAudioLoader.Catalog,
                formalVisualLoader.DirectionalSprites);
            presenter = new QinglanDemoPresenter(Flow, Ui);

            if (presentationCamera == null)
            {
                var cameraObject = new GameObject("Qinglan_ProgrammaticCamera");
                cameraObject.transform.SetParent(transform, false);
                presentationCamera = cameraObject.AddComponent<Camera>();
            }
            cameraRig = presentationCamera.GetComponent<PresentationCameraRig>();
            if (cameraRig == null) cameraRig = presentationCamera.gameObject.AddComponent<PresentationCameraRig>();
            cameraRig.ConfigureTiltedOrthographic(presentationCamera);
            CreatePresentationLight();

            Input.Navigate += OnNavigate;
            Input.Submit += OnSubmit;
            Input.Cancel += OnCancel;
            Input.Pause += OnPause;
            Input.Map += OnMap;
            Input.Tab += OnTab;
            Input.Page += OnPage;
            Input.FocusRestoreRequested += OnFocusRestore;
            Input.GamepadDisconnected += OnGamepadDisconnected;
            Input.DebugLevelUp += OnDebugLevelUp;
            Input.DebugCompleteRun += OnDebugCompleteRun;
            lastStage = Flow.Stage;
            lastLocaleCode = Localization.SelectedLocaleCode;
            ApplyInputMode();
            initialized = true;
        }

        private void Update() => TickRuntime(Time.unscaledDeltaTime);

        public void TickRuntime(double elapsedSeconds)
        {
            if (!initialized) return;
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            ApplyPendingStartupLocale();
            Input.SetStickDeadzone(Flow.Settings.StickDeadzone);
            var move = visualAcceptanceMovementEnabled ? visualAcceptanceMovement : Input.Move;
            Flow.SetMovement(new System.Numerics.Vector2(move.x, move.y));
            Flow.SetInteractHeld(Input.InteractHeld);
            var executedTicks = Flow.Tick(elapsedSeconds);
            var session = Flow.Session;
            if (lastSession != session)
            {
                Presentation.Clear();
                var mapConfiguration = session == null ? null :
                    QinglanProceduralMapFactory.Build(
                        bootstrapApplication.ContentRegistry,
                        session.Descriptor.MapId,
                        formalVisualLoader.MapTiles,
                        formalVisualLoader.MapProps,
                        formalVisualLoader.MapStateSprites);
                Presentation.SetMap(mapConfiguration);
                if (mapConfiguration != null)
                    cameraRig.SetBounds(new Rect(
                        mapConfiguration.Minimum.x,
                        mapConfiguration.Minimum.y,
                        mapConfiguration.Maximum.x - mapConfiguration.Minimum.x,
                        mapConfiguration.Maximum.y - mapConfiguration.Minimum.y));
                cameraRig.SetTarget(null);
                lastSession = session;
            }

            if (session != null)
            {
                cameraRig.EffectsEnabled = Flow.Settings.ScreenShakeEnabled;
                if (executedTicks > 0)
                {
                    Presentation.ConsumeLatestEvents(
                        session.RenderSnapshot.Tick,
                        session.SimulationEvents,
                        session.CombatEvents);
                    if (Presentation.LastDeathRequestCount > 0 && Flow.Settings.ScreenShakeEnabled)
                        cameraRig.RequestShake(0.18f * Flow.Settings.FlashIntensity, 0.2f);
                }
                Presentation.Sync(session.RenderSnapshot, session.InterpolationAlpha, session);
                if (Presentation.TryGetView(session.Player, out var playerView)) cameraRig.SetTarget(playerView.transform);
                Presentation.SyncRunState(presenter.CurrentHud);
            }
            Presentation.SetMixState(ResolveMixState());
            Presentation.TickEffects((float)elapsedSeconds);

            var stageChanged = lastStage != Flow.Stage;
            var localeChanged = !string.Equals(lastLocaleCode, Localization.SelectedLocaleCode, StringComparison.Ordinal);
            if (localeChanged) lastLocaleCode = Localization.SelectedLocaleCode;
            if (stageChanged || localeChanged)
            {
                lastStage = Flow.Stage;
                presenter.Refresh(true);
                ApplyInputMode();
            }
            uiRefreshAccumulator += elapsedSeconds;
            if (uiRefreshAccumulator >= UiRefreshIntervalSeconds)
            {
                uiRefreshAccumulator -= UiRefreshIntervalSeconds;
                presenter.Refresh(false);
                ApplyInputMode();
            }
        }

        private void ApplyPendingStartupLocale()
        {
            if (string.IsNullOrEmpty(pendingStartupLocaleCode)) return;
            var requestedLocale = pendingStartupLocaleCode;
            pendingStartupLocaleCode = string.Empty;
            if (!Localization.SelectLocale(requestedLocale))
                Debug.LogWarning("[Qinglan Localization] Saved locale is unavailable: " + requestedLocale);
        }

        internal void SetVisualAcceptanceMovement(Vector2 movement)
        {
            visualAcceptanceMovement = Vector2.ClampMagnitude(movement, 1f);
            visualAcceptanceMovementEnabled = true;
        }

        internal void ClearVisualAcceptanceMovement()
        {
            visualAcceptanceMovement = Vector2.zero;
            visualAcceptanceMovementEnabled = false;
        }

        private string ResolveContentNameKey(string value)
        {
            var id = ContentId.Create(value);
            return id.IsSuccess && bootstrapApplication.ContentRegistry.TryGet(id.Value, out ContentRegistryEntry entry)
                ? entry.Definition.LocalizedNameKey
                : string.Empty;
        }

        private GameApplication bootstrapApplication;

        private PresentationMixState ResolveMixState()
        {
            if (presenter?.CurrentPage?.Page == QinglanUiPageId.StoryOverlay)
                return PresentationMixState.Story;
            if (Flow.Stage == DemoFlowStage.UserPaused || Flow.Stage == DemoFlowStage.UpgradePaused ||
                Flow.Stage == DemoFlowStage.RewardPaused)
                return PresentationMixState.Paused;
            if (presenter?.CurrentHud?.HasBoss == true) return PresentationMixState.Boss;
            return PresentationMixState.Gameplay;
        }

        private void ApplyInputMode() => Input.SetGameplayMode(Flow.IsGameplayInputEnabled);

        private void CreatePresentationLight()
        {
            var lightObject = new GameObject("Qinglan_2_5D_KeyLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            presentationLight = lightObject.AddComponent<Light>();
            presentationLight.type = LightType.Directional;
            presentationLight.color = new Color(0.92f, 0.96f, 1f, 1f);
            presentationLight.intensity = 1.15f;
            presentationLight.shadows = LightShadows.Soft;
            presentationLight.shadowStrength = 0.58f;
        }

        private void OnPause()
        {
            if (Flow.TogglePause())
            {
                Presentation.RouteUiCue(PresentationAudioCue.UiPauseToggle);
                presenter.Refresh(true);
                ApplyInputMode();
            }
        }

        private void OnSubmit()
        {
            Presentation.RouteUiCue(PresentationAudioCue.Confirm);
            presenter.Submit();
            ApplyInputMode();
        }

        private void OnOptionInvoked(int optionIndex)
        {
            Presentation.RouteUiCue(PresentationAudioCue.Confirm);
            presenter.SelectAndSubmit(optionIndex);
            ApplyInputMode();
        }

        private void OnCancel()
        {
            Presentation.RouteUiCue(PresentationAudioCue.UiCancel);
            presenter.Cancel();
            ApplyInputMode();
        }

        private void OnMap()
        {
            if (Flow.ToggleRunMap())
            {
                Presentation.RouteUiCue(PresentationAudioCue.UiPageOpen);
                presenter.Refresh(true);
                ApplyInputMode();
            }
        }

        private void OnNavigate(float value)
        {
            Presentation.RouteUiCue(PresentationAudioCue.UiNavigate);
            presenter.Navigate(value);
        }

        private void OnFocusRestore()
        {
            presenter.Refresh(true);
            presenter.RestoreFocus();
            ApplyInputMode();
        }

        private void OnGamepadDisconnected()
        {
            if (Flow.Stage == DemoFlowStage.Active) Flow.TogglePause();
        }

        private void OnTab(float value)
        {
            Presentation.RouteUiCue(PresentationAudioCue.UiTabChange);
            presenter.Tab(value > 0f ? 1 : -1);
        }

        private void OnPage(float value)
        {
            Presentation.RouteUiCue(PresentationAudioCue.UiPageOpen);
            presenter.Page(value > 0f ? 1 : -1);
        }

        private void OnDebugLevelUp()
        {
            if (Input.DebugEnabled && Flow.DebugRequestLevelUp()) presenter.Refresh(true);
        }

        private void OnDebugCompleteRun()
        {
            if (Input.DebugEnabled && Flow.DebugCompleteRun()) presenter.Refresh(true);
        }

        private void OnDestroy()
        {
            if (!initialized || Input == null) return;
            Input.Navigate -= OnNavigate;
            Input.Submit -= OnSubmit;
            Input.Cancel -= OnCancel;
            Input.Pause -= OnPause;
            Input.Map -= OnMap;
            Input.Tab -= OnTab;
            Input.Page -= OnPage;
            Input.FocusRestoreRequested -= OnFocusRestore;
            Input.GamepadDisconnected -= OnGamepadDisconnected;
            Input.DebugLevelUp -= OnDebugLevelUp;
            Input.DebugCompleteRun -= OnDebugCompleteRun;
            Ui.OptionInvoked -= OnOptionInvoked;
            Presentation?.Shutdown();
            Flow.Dispose();
            formalAudioLoader?.Dispose();
            formalAudioLoader = null;
            formalFontLoader?.Dispose();
            formalFontLoader = null;
            formalVisualLoader?.Dispose();
            formalVisualLoader = null;
            initialized = false;
        }
    }
}
