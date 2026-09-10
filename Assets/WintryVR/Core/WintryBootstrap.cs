using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.AI;
using WintryVR.AI.Providers;
using WintryVR.AssetGeneration;
using WintryVR.Audio;
using WintryVR.Capture;
using WintryVR.Capture.Providers;
using WintryVR.Character;
using WintryVR.Demo;
using WintryVR.Diagnostics;
using WintryVR.Interaction;
using WintryVR.MR;
using WintryVR.Networking;
using WintryVR.OCR;
using WintryVR.SceneUnderstanding;
using WintryVR.Settings;
using WintryVR.Spatial;
using WintryVR.UI;
using WintryVR.Voice;
using WintryVR.Voice.Providers;

namespace WintryVR.Core
{
    /// <summary>
    /// Single entry point of the app (the only component in the scene). Detects capabilities, builds the MR rig
    /// through the best available provider (Meta XR when present, generic XR otherwise), wires every layer
    /// through interfaces, and runs onboarding on first launch. Everything degrades gracefully: missing SDK,
    /// permission, network or hardware simply selects a fallback.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class WintryBootstrap : MonoBehaviour
    {
        [Header("Overrides (editor/testing)")]
        public bool ForceDemoMode;
        public bool SkipOnboarding;
        public bool ShowDebugPanelOnStart;

        public DeviceCapabilities Capabilities { get; private set; }
        public WintryOrchestrator Orchestrator { get; private set; }
        public MRRigHandles Rig { get; private set; }
        public bool DemoMode { get; private set; }
        public bool Ready { get; private set; }

        private CharacterService _character;
        private VoiceService _voice;
        private CameraCaptureService _camera;
        private SceneUnderstandingService _scene;
        private ISceneProvider _sceneProvider;
        private IAnchorService _anchors;
        private PerformanceMonitor _perf;

        private async void Start()
        {
            _ = MainThreadDispatcher.Instance;
            var cfg = WintryConfig.Instance;
            var settings = WintrySettings.Current;
            DemoMode = cfg.DemoMode || ForceDemoMode || (Application.isEditor && !UnityEngine.XR.XRSettings.isDeviceActive);
            Localization.CurrentLanguage = settings.EffectiveLanguage();
            WintryLog.I("Boot", "WintryVR starting. Demo=" + DemoMode + " lang=" + Localization.CurrentLanguage);

            // ---- capabilities
            Capabilities = DeviceCapabilities.ProbeGeneric();
            var probe = ProviderRegistry.Resolve<ICapabilityProbe>(null, () => null);
            probe?.Refine(Capabilities);

            // ---- MR layer
            var rigProvider = ProviderRegistry.Resolve<IMRRigProvider>(p => p.IsAvailable, () => new GenericMRRigProvider());
            Rig = rigProvider.Build(transform, Capabilities);
            Capabilities.Passthrough = Rig.Passthrough.IsSupported;
            Rig.Passthrough.SetEnabled(true);
            Rig.Passthrough.SetBrightness(settings.MR.PassthroughBrightness);
            WintryEvents.Subscribe<SettingsChangedEvent>(e => Rig.Passthrough.SetBrightness(WintrySettings.Current.MR.PassthroughBrightness));
            WintryEvents.Publish(new CapabilitiesDetectedEvent { Capabilities = Capabilities });
            WintryLog.I("Boot", Capabilities.Summary());

            gameObject.AddComponent<ConnectivityMonitor>();

            // ---- scene understanding
            _sceneProvider = settings.Vision.SceneUnderstanding ? ProviderRegistry.Resolve<ISceneProvider>(p => p.IsAvailable, () => null) : null;
            if (_sceneProvider == null && (DemoMode || Capabilities.IsEditor)) _sceneProvider = new DemoSceneProvider(transform);
            _scene = new SceneUnderstandingService(_sceneProvider);
            Capabilities.SceneUnderstanding = _sceneProvider != null && _sceneProvider.IsAvailable && _sceneProvider.Name != "demo-room";
            Capabilities.SceneMesh = _sceneProvider != null && _sceneProvider.HasSceneMesh;
            _sceneProvider?.RequestSceneCapture();

            // ---- spatial
            var spatial = new SpatialService(Rig.Head, _scene);
            _anchors = ProviderRegistry.Resolve<IAnchorService>(a => a.IsAvailable, () => new LocalAnchorService(Rig.TrackingSpace));
            Capabilities.SpatialAnchors = _anchors.IsAvailable; Capabilities.AnchorPersistence = _anchors.SupportsPersistence;

            // ---- AI
            var aiCfg = cfg;
            if (settings.AI.Provider != "auto") aiCfg.AiProvider = settings.AI.Provider;
            if (!string.IsNullOrEmpty(settings.AI.Model)) aiCfg.AiModel = settings.AI.Model;
            if (DemoMode && aiCfg.AiProvider == "auto") aiCfg.AiProvider = "mock";
            IAIProvider ai = AIProviderFactory.Create(aiCfg);
            if (!ai.IsConfigured) { WintryLog.W("Boot", ai.Name + " not configured → Mock provider"); ai = new MockAIProvider(); }
            var assistant = new AssistantService(ai);
            var vision = new VisionService(ai);
            var ocr = new OCRService(ai);
            var translation = new TranslationService(ai);
            var search = new SearchService(cfg);
            var memory = new ContextMemory();

            // ---- camera
            var camProviders = new List<ICameraCaptureProvider>();
            string camChoice = (cfg.CameraProvider ?? "auto").ToLowerInvariant();
            System.Func<Transform> head = () => Rig.Head;
            if (camChoice == "demo" || (camChoice == "auto" && DemoMode)) camProviders.Add(new DemoImageCaptureProvider(head));
            if (camChoice == "auto" || camChoice == "passthrough") camProviders.Add(new WebCamTextureCaptureProvider(head, cfg.CaptureMaxWidth, cfg.CaptureJpegQuality));
            if (camChoice == "auto" || camChoice == "render") camProviders.Add(new RenderCaptureProvider(() => Rig.CenterCamera, cfg.CaptureMaxWidth, cfg.CaptureJpegQuality));
            if (camChoice == "auto") camProviders.Add(new DemoImageCaptureProvider(head));
            _camera = new CameraCaptureService(camProviders);

            // ---- audio + character
            var characterGo = new GameObject("Wintry");
            characterGo.transform.SetParent(transform, false);
            var audio = characterGo.AddComponent<SpatialAudioService>();
            audio.Initialize(characterGo.transform);
            _character = characterGo.AddComponent<CharacterService>();
            _character.Initialize(spatial, audio);
            _character.Hide();

            // ---- voice
            _voice = gameObject.AddComponent<VoiceService>();
            var sttCfg = cfg; if (DemoMode && sttCfg.SttProvider == "auto") sttCfg.SttProvider = "mock";
            var stt = VoiceProviderFactory.CreateStt(sttCfg);
            var tts = VoiceProviderFactory.CreateTts(sttCfg);
            _voice.Configure(stt, tts, audio.VoiceSource);

            // ---- input
            var input = gameObject.AddComponent<InputService>();
            var sources = ProviderRegistry.ResolveAll<IXRInputSource>();   // Meta hands + controllers when present
            sources.Add(new GenericXRInputSource(Rig.TrackingSpace));
            sources.Add(new EditorInputSource(Rig.Head, Rig.RigRoot));
            input.Initialize(Rig.Head, sources);

            // ---- UI
            var uiRoot = new GameObject("WintryUI").transform;
            uiRoot.SetParent(transform, false);
            var interaction = uiRoot.gameObject.AddComponent<UIInteractionManager>(); interaction.Initialize(input, Rig.Head);
            var cards = New<InformationCardManager>(uiRoot, "Cards"); cards.Spatial = spatial;
            var highlights = New<SmartHighlightManager>(uiRoot, "Highlights"); highlights.Spatial = spatial; highlights.Enabled = settings.Vision.SmartHighlight;
            var pointers = New<SpatialPointerManager>(uiRoot, "Pointers"); pointers.Spatial = spatial; pointers.Origin = characterGo.transform;
            var hud = New<HudOverlay>(uiRoot, "HUD"); hud.Initialize(Rig.Head);
            var translations = New<TranslationOverlay>(uiRoot, "Translations"); translations.Spatial = spatial;
            var quick = New<QuickActionsMenu>(uiRoot, "QuickActions"); quick.Spatial = spatial;
            var history = New<ConversationHistoryPanel>(uiRoot, "History"); history.Spatial = spatial;
            var settingsUi = New<SettingsMenu>(uiRoot, "Settings"); settingsUi.Spatial = spatial;
            _perf = gameObject.AddComponent<PerformanceMonitor>();
            var debug = New<DebugPanel>(uiRoot, "Debug"); debug.Perf = _perf; debug.Spatial = spatial;
            WintryEvents.Subscribe<SettingsChangedEvent>(_ => highlights.Enabled = WintrySettings.Current.Vision.SmartHighlight);

            // ---- location (opt-in)
            var location = gameObject.AddComponent<WintryVR.MR.LocationService>();
            location.SetEnabled(settings.Privacy.LocationAllowed);
            WintryEvents.Subscribe<SettingsChangedEvent>(_ => location.SetEnabled(WintrySettings.Current.Privacy.LocationAllowed));

            // ---- asset generation
            var assetProvider = AssetGenerationService.CreateProvider(cfg);
            var assetGen = new AssetGenerationService(assetProvider, () => _character.CurrentLook);

            // ---- orchestrator
            Orchestrator = gameObject.AddComponent<WintryOrchestrator>();
            Orchestrator.Assistant = assistant; Orchestrator.Vision = vision; Orchestrator.Ocr = ocr; Orchestrator.Translation = translation; Orchestrator.Search = search;
            Orchestrator.Memory = memory; Orchestrator.Scene = _scene; Orchestrator.Spatial = spatial; Orchestrator.Character = _character; Orchestrator.Voice = _voice;
            Orchestrator.Camera = _camera; Orchestrator.AssetGen = assetGen; Orchestrator.Location = location; Orchestrator.Input = input; Orchestrator.Audio = audio;
            Orchestrator.Pointers = pointers; Orchestrator.Highlights = highlights; Orchestrator.Cards = cards; Orchestrator.Translations = translations;
            Orchestrator.QuickMenu = quick; Orchestrator.History = history; Orchestrator.SettingsUI = settingsUi; Orchestrator.DebugUI = debug;
            Orchestrator.Initialize();
            debug.ExtraInfo = DebugInfo;

            // ---- services
            WintryServices.Register<IAssistantService>(assistant);
            WintryServices.Register<IVisionService>(vision);
            WintryServices.Register<IOCRService>(ocr);
            WintryServices.Register<ITranslationService>(translation);
            WintryServices.Register<ISearchService>(search);
            WintryServices.Register<IMemoryService>(memory);
            WintryServices.Register<ISceneUnderstandingService>(_scene);
            WintryServices.Register<ISpatialService>(spatial);
            WintryServices.Register<ICharacterService>(_character);
            WintryServices.Register<IVoiceService>(_voice);
            WintryServices.Register<ICameraCaptureService>(_camera);
            WintryServices.Register<IAssetGenerationService>(assetGen);
            WintryServices.Register<IAnchorService>(_anchors);
            WintryServices.Register<IInputService>(input);
            WintryServices.Register<ILocationService>(location);
            WintryServices.Register<IPassthroughService>(Rig.Passthrough);
            WintryServices.Register<WintryOrchestrator>(Orchestrator);

            // ---- demo
            var demo = gameObject.AddComponent<DemoModeController>();
            demo.Enabled = DemoMode;
            demo.Stt = stt as MockSttProvider;
            demo.Utterance = (text, lang) => Orchestrator.HandleUtteranceAsync(text, lang);

            // ---- microphone (never in demo unless push-to-talk) and anchors restore
            _voice.AutoStartMicrophone = !DemoMode;
            if (!DemoMode) _voice.StartMicrophone();
            _ = _anchors.RestoreAsync();
            _ = _camera.InitializeAsync();

            // ---- onboarding / first frame
            if (!settings.OnboardingCompleted && !SkipOnboarding)
            {
                var onboarding = New<OnboardingFlow>(uiRoot, "Onboarding"); onboarding.Spatial = spatial;
                await onboarding.RunAsync(text => _voice.SpeakAsync(text, Localization.CurrentLanguage, System.Threading.CancellationToken.None), () => _character.ShowCore());
            }
            else _character.ShowCore();
            if (ShowDebugPanelOnStart || cfg.DebugPanel) debug.Toggle();
            Ready = true;
            WintryLog.I("Boot", "WintryVR ready. AI=" + ai.Name + "/" + ai.Model + " STT=" + stt.Name + " TTS=" + tts.Name + " Search=" + search.ProviderName + " Scene=" + _scene.ProviderName + " Anchors=" + _anchors.GetType().Name);
        }

        private static T New<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private string DebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("State ").Append(Orchestrator != null ? Orchestrator.State.ToString() : "-").Append("  form ").Append(_character != null ? _character.Form.ToString() : "-").Append("  turns ").Append(Orchestrator != null ? Orchestrator.TurnsHandled : 0).AppendLine();
            sb.Append("Caps ").Append(Capabilities.Summary()).AppendLine();
            sb.Append("Track ").Append(UnityEngine.XR.XRSettings.isDeviceActive ? UnityEngine.XR.XRSettings.loadedDeviceName : "none").Append("  PT ").Append(Rig != null && Rig.Passthrough.IsEnabled ? "on" : "off").Append("  rig ").Append(Rig != null ? Rig.ProviderName : "-").AppendLine();
            sb.Append("Mic ").Append(_voice != null && _voice.MicrophoneAvailable ? "on lvl " + _voice.CurrentInputLevel.ToString("0.000") : "off").Append("  cam ").Append(_camera != null && _camera.ActiveProvider != null ? _camera.ActiveProvider.Name + " x" + _camera.TotalCaptures : "none").AppendLine();
            sb.Append("Scene ").Append(_scene != null ? _scene.ProviderName + " " + _scene.Entities.Count + " ent" : "-").Append("  anchors ").Append(_anchors != null ? _anchors.Anchors.Count : 0).Append("  mem ").Append(Orchestrator != null ? Orchestrator.Memory.Objects.Count : 0).Append(" obj").AppendLine();
            sb.Append("Voice ").Append(_voice != null ? _voice.SttProvider.Name + "/" + _voice.TtsProvider.Name : "-").Append("  demo ").Append(DemoMode).AppendLine();
            return sb.ToString();
        }
    }
}
