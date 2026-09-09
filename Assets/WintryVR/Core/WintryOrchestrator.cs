using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.AI;
using WintryVR.Audio;
using WintryVR.Capture;
using WintryVR.Networking;
using WintryVR.OCR;
using WintryVR.SceneUnderstanding;
using WintryVR.Settings;
using WintryVR.Spatial;
using WintryVR.UI;
using WintryVR.Voice;
using WintryVR.Diagnostics;

namespace WintryVR.Core
{
    /// <summary>
    /// The multimodal pipeline:
    ///   Voice → STT → Intent → visual context? → capture frame → scene/object analysis → spatial context →
    ///   AI reasoning (→ web search) → response → spatial UI → TTS → spatial audio.
    /// Fully asynchronous; the main thread is never blocked. Also owns the assistant state machine and the
    /// presence logic (Core ↔ character).
    /// </summary>
    public class WintryOrchestrator : MonoBehaviour
    {
        // ---- dependencies (set by the bootstrap)
        public IAssistantService Assistant;
        public IVisionService Vision;
        public IOCRService Ocr;
        public ITranslationService Translation;
        public ISearchService Search;
        public IMemoryService Memory;
        public ISceneUnderstandingService Scene;
        public ISpatialService Spatial;
        public ICharacterService Character;
        public VoiceService Voice;
        public ICameraCaptureService Camera;
        public IAssetGenerationService AssetGen;
        public ILocationService Location;
        public IInputService Input;
        public SpatialAudioService Audio;
        public SpatialPointerManager Pointers;
        public SmartHighlightManager Highlights;
        public InformationCardManager Cards;
        public TranslationOverlay Translations;
        public QuickActionsMenu QuickMenu;
        public ConversationHistoryPanel History;
        public SettingsMenu SettingsUI;
        public DebugPanel DebugUI;

        public AssistantState State { get; private set; } = AssistantState.Idle;
        public bool Busy => _turnCts != null;
        public int TurnsHandled { get; private set; }

        private SpatialQueryResolver _resolver;
        private CancellationTokenSource _turnCts;
        private ConceptResult _pendingLook;
        private float _lastActivity;
        private bool _online = true;
        private bool _initialized;
        public float IdleCollapseSeconds = 12f;

        public void Initialize()
        {
            _resolver = new SpatialQueryResolver(Spatial, Scene, Memory);
            _online = ConnectivityMonitor.IsOnline;
            _lastActivity = Time.time;

            if (Voice != null) Voice.OnUtterance += (text, lang) => _ = HandleUtteranceAsync(text, lang);
            WintryEvents.Subscribe<WakeWordDetectedEvent>(_ => OnWake());
            WintryEvents.Subscribe<UserMessageEvent>(e => _ = HandleUtteranceAsync(e.Text, "auto"));
            WintryEvents.Subscribe<QuickActionEvent>(e => HandleQuickAction(e.Intent));
            WintryEvents.Subscribe<ConnectivityChangedEvent>(e => { _online = e.Online; if (State == AssistantState.Idle || State == AssistantState.Offline) SetState(AssistantState.Idle); });
            WintryEvents.Subscribe<ClearSessionEvent>(_ => ClearSession());
            WintryEvents.Subscribe<ClearHistoryEvent>(_ => { Memory.ClearHistory(); History?.Clear(); });
            WintryEvents.Subscribe<ClearAllDataEvent>(_ => ClearAllData());
            WintryEvents.Subscribe<OpenSettingsEvent>(_ => SettingsUI?.Toggle());
            WintryEvents.Subscribe<ToggleHistoryEvent>(_ => History?.Toggle());
            if (Input != null) Input.OnGesture += OnGesture;
            _initialized = true;
            SetState(AssistantState.Idle);
        }

        // ------------------------------------------------------------------ state
        public void SetState(AssistantState s)
        {
            if (s == AssistantState.Idle && !_online) s = AssistantState.Offline;
            var prev = State;
            State = s;
            Character?.SetState(s);
            if (prev != s) WintryEvents.Publish(new StateChangedEvent { Previous = prev, Current = s });
        }

        private void Update()
        {
            if (!_initialized) return;
            // Presence: return to the discreet Core after the conversation naturally ends
            if (Character != null && Character.Form == PresenceForm.Character && !Busy && (State == AssistantState.Idle || State == AssistantState.Offline)
                && Voice != null && !Voice.ConversationOpen && !Voice.IsListening && !Voice.IsSpeaking && Time.time - _lastActivity > IdleCollapseSeconds && _pendingLook == null)
            {
                _ = Character.CollapseToCoreAsync();
            }
            if (Voice != null && Voice.IsListening && State == AssistantState.Idle) SetState(AssistantState.Listening);
            else if (Voice != null && !Voice.IsListening && State == AssistantState.Listening && !Busy) SetState(AssistantState.Idle);
        }

        private void OnWake()
        {
            _lastActivity = Time.time;
            Audio?.PlayCue("listen", 0.4f);
            if (Character != null && Character.Form != PresenceForm.Character) _ = Character.TransformToCharacterAsync();
            Voice?.OpenConversationWindow(10f);
            if (State == AssistantState.Idle) SetState(AssistantState.Listening);
        }

        private void OnGesture(GestureEvent g)
        {
            switch (g.Type)
            {
                case GestureType.PushToTalkStart: _lastActivity = Time.time; Voice?.StartListening(); SetState(AssistantState.Listening); if (Character != null && Character.Form == PresenceForm.Core) _ = Character.TransformToCharacterAsync(); break;
                case GestureType.PushToTalkEnd: Voice?.StopListening(); if (State == AssistantState.Listening) SetState(AssistantState.Thinking); break;
                case GestureType.PalmMenu: QuickMenu?.Toggle(g.Position); break;
                case GestureType.Dismiss: Dismiss(); break;
                case GestureType.DebugToggle: DebugUI?.Toggle(); break;
            }
        }

        // ------------------------------------------------------------------ entry points
        public void HandleQuickAction(Intent intent)
        {
            string lang = CurrentLanguage("auto");
            string text;
            switch (intent)
            {
                case Intent.IDENTIFY: text = lang == "it" ? "Cos'è questo?" : lang == "de" ? "Was ist das?" : lang == "fr" ? "Qu'est-ce que c'est ?" : lang == "es" ? "¿Qué es esto?" : "What is this?"; break;
                case Intent.OCR: text = lang == "it" ? "Leggimi questo." : lang == "de" ? "Lies das." : lang == "fr" ? "Lis ça." : lang == "es" ? "Lee esto." : "Read this."; break;
                case Intent.TRANSLATE: text = lang == "it" ? "Traduci questo." : lang == "de" ? "Übersetz das." : lang == "fr" ? "Traduis ça." : lang == "es" ? "Traduce esto." : "Translate this."; break;
                case Intent.SEARCH: text = lang == "it" ? "Quanto costa questo?" : lang == "de" ? "Was kostet das?" : lang == "fr" ? "Combien ça coûte ?" : lang == "es" ? "¿Cuánto cuesta esto?" : "How much does this cost?"; break;
                case Intent.EXPLAIN: text = lang == "it" ? "Spiegami questo." : lang == "de" ? "Erklär mir das." : lang == "fr" ? "Explique-moi ça." : lang == "es" ? "Explícame esto." : "Explain this to me."; break;
                case Intent.LOCATE:
                    _lastActivity = Time.time;
                    if (Character != null && Character.Form == PresenceForm.Core) _ = Character.TransformToCharacterAsync();
                    _ = SpeakAsync(lang == "it" ? "Cosa devo cercare?" : lang == "de" ? "Was soll ich suchen?" : lang == "fr" ? "Que dois-je chercher ?" : lang == "es" ? "¿Qué debo buscar?" : "What should I look for?", lang, CancellationToken.None);
                    Voice?.OpenConversationWindow(10f);
                    return;
                default: return;
            }
            _ = HandleUtteranceAsync(text, lang);
        }

        public async Task HandleUtteranceAsync(string text, string languageHint)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            _lastActivity = Time.time;
            // one turn at a time: a new utterance interrupts the previous one (barge-in)
            _turnCts?.Cancel();
            var cts = new CancellationTokenSource();
            _turnCts = cts;
            var ct = cts.Token;
            string lang = CurrentLanguage(languageHint, text);
            Localization.CurrentLanguage = lang;
            WintryEvents.Publish(new UserTurnEvent { Text = text, LanguageCode = lang });
            Voice?.Interrupt();
            try
            {
                if (Character != null && Character.Form != PresenceForm.Character) await Character.TransformToCharacterAsync();
                if (_pendingLook != null && IsConfirmation(text, out bool yes)) { await ResolvePendingLookAsync(yes, lang, ct); return; }

                var detected = IntentDetector.Detect(text, lang);
                Intent intent = detected.Intent;
                if (detected.Confidence < 0.5f && CloudUsable(Assistant.Provider.RequiresNetwork))
                {
                    SetState(AssistantState.Thinking);
                    var llmIntent = await Assistant.ClassifyIntentAsync(text, lang, ct);
                    if (llmIntent != Intent.GENERAL_CONVERSATION) intent = llmIntent;
                }
                WintryLog.I("Orchestrator", "Intent " + intent + " (" + detected.Confidence.ToString("0.00") + ") lang=" + lang + " :: " + text);

                switch (intent)
                {
                    case Intent.CLEAR_CONTEXT:
                        ClearSession();
                        await SpeakAsync(Localization.Get("ctx.cleared", lang), lang, ct);
                        break;
                    case Intent.DISMISS:
                        Dismiss();
                        break;
                    case Intent.CHANGE_LOOK:
                        await ProposeLookAsync(text, lang, ct);
                        break;
                    case Intent.LOCATE:
                    case Intent.COUNT:
                    case Intent.NAVIGATE:
                    case Intent.DESCRIBE:
                        {
                            var a = _resolver.Resolve(intent, text, lang);
                            if (a.Handled && !a.NeedsVision)
                            {
                                if (a.HasTarget) IndicateTarget(a.Target, a.TargetLabel);
                                var turn = new AssistantTurn { UserText = text, AssistantText = a.Speech, Intent = intent, LanguageCode = lang };
                                foreach (var o in a.Objects) turn.ReferencedObjectIds.Add(o.Id);
                                Memory.RememberTurn(turn);
                                WintryEvents.Publish(new AssistantReplyEvent { Turn = turn });
                                await SpeakAsync(a.Speech, lang, ct);
                                break;
                            }
                            // not answerable from memory: look with the camera, hinting what to find
                            string hint = intent == Intent.COUNT ? "Count every instance of: " + (string.IsNullOrEmpty(a.TargetLabel) ? text : a.TargetLabel) + ". List each as a separate object."
                                        : intent == Intent.LOCATE ? "Find this in the image if present: " + (string.IsNullOrEmpty(a.TargetLabel) ? text : a.TargetLabel)
                                        : "Describe what is visible; list the salient objects.";
                            await RunAssistantTurnAsync(text, intent, lang, hint, ct);
                            break;
                        }
                    default:
                        await RunAssistantTurnAsync(text, intent, lang, null, ct);
                        break;
                }
            }
            catch (OperationCanceledException) { WintryLog.V("Orchestrator", "Turn cancelled"); }
            catch (Exception ex)
            {
                WintryLog.E("Orchestrator", "Turn failed", ex);
                SetState(AssistantState.Error);
                try { await SpeakAsync(Localization.Get("err.generic", lang), lang, CancellationToken.None); } catch { }
            }
            finally
            {
                if (_turnCts == cts) { _turnCts = null; cts.Dispose(); SetState(AssistantState.Idle); }
                TurnsHandled++;
                _lastActivity = Time.time;
            }
        }

        // ------------------------------------------------------------------ the main pipeline
        private async Task RunAssistantTurnAsync(string text, Intent intent, string lang, string visionHint, CancellationToken ct)
        {
            SetState(AssistantState.Thinking);
            bool providerNeedsNet = Assistant.Provider.RequiresNetwork;
            bool online = CloudUsable(providerNeedsNet);
            var ctx = new AssistantContext { UserText = text, Intent = intent, LanguageCode = lang, Online = online };

            var refs = Memory.ResolveReference(text, lang, Spatial.HeadPosition, Spatial.GazeDirection);
            bool explicitMemoryRef = refs.Count > 0 && (intent == Intent.SEARCH || intent == Intent.COMPARE || intent == Intent.EXPLAIN || intent == Intent.GENERAL_CONVERSATION);
            bool needsVision = IntentDetector.NeedsVision(intent, text) || visionHint != null || (!explicitMemoryRef && (intent == Intent.SEARCH || intent == Intent.COMPARE || intent == Intent.EXPLAIN) && Memory.Focus == null);
            if (intent == Intent.IDENTIFY && refs.Count > 0 && LooksLikeExplicitName(text, refs[0])) needsVision = false;

            CapturedFrame frame = null;
            Vector3 focusPos = Vector3.zero; bool hasFocusPos = false;
            List<ObservedObject> seen = new List<ObservedObject>();

            if (needsVision && WintrySettings.Current.Privacy.CameraAllowed)
            {
                if (!online && providerNeedsNet)
                {
                    await SpeakAsync(Localization.Get("err.offline", lang), lang, ct);
                    return;
                }
                SetState(AssistantState.Vision);
                frame = await Camera.CaptureAsync(CaptureTrigger.VoiceCommand, ct);
                if (frame == null || !frame.IsValid)
                {
                    await SpeakAsync(Localization.Get("err.noCamera", lang), lang, ct);
                    return;
                }
                ctx.Frame = frame;
                PrivacyState.SetCloud(providerNeedsNet);

                if (intent == Intent.OCR || intent == Intent.TRANSLATE)
                {
                    ctx.Ocr = await Ocr.RecognizeAsync(frame, lang, ct);
                    if (!ctx.Ocr.Success)
                    {
                        PrivacyState.SetCloud(false);
                        await SpeakAsync(ctx.Ocr.UserMessage, lang, ct);
                        return;
                    }
                    // locate the text block in the world for overlays
                    Vector2 anchor = ctx.Ocr.Blocks.Count > 0 && ctx.Ocr.Blocks[0].NormalizedBounds.width > 0 ? ctx.Ocr.Blocks[0].NormalizedBounds.center : new Vector2(0.5f, 0.5f);
                    focusPos = ObjectLocalizer.Localize(frame, anchor, Scene, out _); hasFocusPos = true;

                    if (intent == Intent.TRANSLATE || (intent == Intent.OCR && WantsTranslation(text)))
                    {
                        SetState(AssistantState.Thinking);
                        string target = TargetLanguageFromText(text, lang);
                        var tr = await Translation.TranslateAsync(ctx.Ocr.FullText, ctx.Ocr.LanguageCode, target, ct);
                        PrivacyState.SetCloud(false);
                        if (!tr.Success) { await SpeakAsync(tr.UserMessage, lang, ct); return; }
                        WintryEvents.Publish(new TranslationReadyEvent { Original = ctx.Ocr.FullText, Translated = tr.Translated, SourceLanguage = tr.SourceLanguage, TargetLanguage = target, WorldPosition = focusPos });
                        Character?.LookAt(focusPos);
                        var trTurn = new AssistantTurn { UserText = text, AssistantText = tr.Translated, Intent = intent, LanguageCode = target, UsedVision = true };
                        Memory.RememberTurn(trTurn);
                        WintryEvents.Publish(new AssistantReplyEvent { Turn = trTurn });
                        await SpeakAsync(tr.Translated, target, ct);
                        return;
                    }
                    if (!WantsSummary(text))
                    {
                        PrivacyState.SetCloud(false);
                        Cards?.Show(new InformationCardData { Id = Guid.NewGuid().ToString("N"), Title = "Text", Subtitle = LanguageCodes.DisplayName(ctx.Ocr.LanguageCode), Lines = SplitLines(ctx.Ocr.FullText, 4), WorldPosition = focusPos, HasWorldPosition = true, ActionLabel = Localization.Get("card.more", lang), ActionQuery = SummaryQuery(lang) });
                        Character?.LookAt(focusPos);
                        var ocrTurn = new AssistantTurn { UserText = text, AssistantText = ctx.Ocr.FullText, Intent = intent, LanguageCode = ctx.Ocr.LanguageCode, UsedVision = true };
                        Memory.RememberTurn(ocrTurn);
                        WintryEvents.Publish(new AssistantReplyEvent { Turn = ocrTurn });
                        await SpeakAsync(ctx.Ocr.FullText, string.IsNullOrEmpty(ctx.Ocr.LanguageCode) ? lang : ctx.Ocr.LanguageCode, ct);
                        return;
                    }
                    // summary requested → continue to the assistant with the OCR text as context
                }
                else if (WintrySettings.Current.Vision.ObjectRecognition)
                {
                    ctx.Vision = await Vision.AnalyzeAsync(frame, visionHint, lang, ct);
                    if (ctx.Vision.Success)
                    {
                        float bestCentre = float.MaxValue;
                        foreach (var d in ctx.Vision.Detections)
                        {
                            Vector3 pos = ObjectLocalizer.Localize(frame, d, Scene, out bool hitGeom);
                            var obj = Memory.Remember(d, pos, true);
                            obj.Extents = EstimateExtents(d, frame, pos);
                            Scene.RegisterObject(obj);
                            seen.Add(obj);
                            float dc = d.HasBounds ? Vector2.Distance(d.NormalizedBounds.center, new Vector2(0.5f, 0.5f)) : 1f;
                            if (dc < bestCentre) { bestCentre = dc; focusPos = pos; hasFocusPos = true; }
                        }
                        if (seen.Count > 0) WintryEvents.Publish(new ObjectsObservedEvent { Objects = seen });
                    }
                }
            }

            // ---- search (explicit)
            if (intent == Intent.SEARCH && online && Search.IsAvailable)
            {
                SetState(AssistantState.Searching);
                string q = BuildSearchQuery(text, refs, seen, lang);
                ctx.Search = await Search.SearchAsync(q, lang, ct);
            }

            // ---- context
            var settings = WintrySettings.Current;
            if (settings.AI.ContextMemory) ctx.MemoryContext = Memory.BuildContext(12, 6);
            ctx.SpatialContext = BuildSpatialContext();
            if (intent == Intent.TRANSLATE) ctx.TargetLanguageForTranslation = TargetLanguageFromText(text, lang);

            if (!online && providerNeedsNet)
            {
                await SpeakAsync(Localization.Get("err.offline", lang), lang, ct);
                return;
            }

            // ---- reasoning
            SetState(AssistantState.Thinking);
            PrivacyState.SetCloud(providerNeedsNet);
            var answer = await Assistant.AskAsync(ctx, ct);
            if (answer.NeedsSearch && ctx.Search == null && online && Search.IsAvailable && !string.IsNullOrEmpty(answer.SearchQuery))
            {
                SetState(AssistantState.Searching);
                ctx.Search = await Search.SearchAsync(answer.SearchQuery, lang, ct);
                if (ctx.Search.Success)
                {
                    SetState(AssistantState.Thinking);
                    var refined = await Assistant.AskAsync(ctx, ct);
                    if (!string.IsNullOrEmpty(refined.Speech)) { refined.ObjectName = string.IsNullOrEmpty(refined.ObjectName) ? answer.ObjectName : refined.ObjectName; answer = refined; }
                }
            }
            PrivacyState.SetCloud(false);

            // ---- spatial UI
            ObservedObject focus = null;
            if (!string.IsNullOrEmpty(answer.ObjectName))
            {
                focus = FindOrCreateFocus(answer, seen, refs, frame, ref focusPos, ref hasFocusPos);
            }
            else if (refs.Count > 0) { focus = refs[0]; if (focus.HasPosition) { focusPos = focus.WorldPosition; hasFocusPos = true; } }
            else if (seen.Count > 0 && hasFocusPos) { focus = Nearest(seen, focusPos); }
            if (focus != null)
            {
                Memory.SetFocus(focus);
                if (focus.HasPosition && settings.Vision.SmartHighlight && focus.Confidence >= 0.3f) Highlights?.Highlight(focus);
                if (focus.HasPosition) Character?.LookAt(focus.WorldPosition);
                if (ctx.Search != null) Memory.RememberSearch(focus.Id, ctx.Search);
            }
            if (!string.IsNullOrEmpty(answer.CardTitle))
            {
                var card = new InformationCardData
                {
                    Id = Guid.NewGuid().ToString("N"), Title = answer.CardTitle, Subtitle = answer.CardSubtitle, Lines = answer.CardLines,
                    ObjectId = focus?.Id, ActionLabel = Localization.Get("card.more", lang), ActionQuery = MoreQuery(answer.CardTitle, lang),
                    WorldPosition = hasFocusPos ? focusPos : Vector3.zero, HasWorldPosition = hasFocusPos
                };
                if (ctx.Search != null && ctx.Search.Items.Count > 0) card.SourceUrl = ctx.Search.Items[0].Url;
                Cards?.Show(card);
            }
            if (!string.IsNullOrEmpty(answer.TranslatedText) && intent == Intent.TRANSLATE)
                WintryEvents.Publish(new TranslationReadyEvent { Original = answer.DetectedText, Translated = answer.TranslatedText, SourceLanguage = answer.SourceLanguage, TargetLanguage = lang, WorldPosition = hasFocusPos ? focusPos : Vector3.zero });

            // ---- speak
            string speech = string.IsNullOrEmpty(answer.Speech) ? Localization.Get("err.identify", lang) : answer.Speech;
            if (string.IsNullOrEmpty(answer.Speech) && needsVision) speech += " " + Localization.Get("err.identify.hint1", lang);
            var turn = new AssistantTurn { UserText = text, AssistantText = speech, Intent = intent, LanguageCode = answer.LanguageCode, Confidence = answer.Confidence, UsedVision = frame != null, UsedSearch = ctx.Search != null, Offline = !online };
            if (focus != null) turn.ReferencedObjectIds.Add(focus.Id);
            Memory.RememberTurn(turn);
            WintryEvents.Publish(new AssistantReplyEvent { Turn = turn });
            await SpeakAsync(speech, string.IsNullOrEmpty(answer.LanguageCode) ? lang : answer.LanguageCode, ct);
        }

        private ObservedObject FindOrCreateFocus(StructuredAnswer answer, List<ObservedObject> seen, List<ObservedObject> refs, CapturedFrame frame, ref Vector3 focusPos, ref bool hasFocusPos)
        {
            string name = answer.ObjectName.ToLowerInvariant();
            ObservedObject best = null;
            foreach (var o in seen) if (o.DisplayName.ToLowerInvariant().Contains(name) || name.Contains(o.Label.ToLowerInvariant())) { best = o; break; }
            if (best == null) foreach (var o in refs) if (o.DisplayName.ToLowerInvariant().Contains(name) || name.Contains((o.Label ?? "").ToLowerInvariant())) { best = o; break; }
            if (best == null && frame != null)
            {
                // the assistant identified something the detector did not list: place it at the focus point
                Vector2 p = answer.HasFocusPoint ? answer.FocusPoint : new Vector2(0.5f, 0.5f);
                Vector3 pos = ObjectLocalizer.Localize(frame, p, Scene, out _);
                var d = new Detection { Label = string.IsNullOrEmpty(answer.ObjectCategory) ? answer.ObjectName : answer.ObjectCategory, Identity = answer.ObjectName, Category = answer.ObjectCategory, Confidence = answer.Confidence < 0 ? 0.5f : answer.Confidence };
                best = Memory.Remember(d, pos, true);
                Scene.RegisterObject(best);
            }
            if (best == null && seen.Count > 0) best = hasFocusPos ? Nearest(seen, focusPos) : seen[0];
            if (best == null) best = Memory.Focus;
            if (best != null)
            {
                best.Identity = answer.ObjectName;
                if (!string.IsNullOrEmpty(answer.ObjectCategory)) best.Category = answer.ObjectCategory;
                if (answer.Confidence >= 0) best.Confidence = answer.Confidence;
                if (best.HasPosition) { focusPos = best.WorldPosition; hasFocusPos = true; }
                Scene.RegisterObject(best);
            }
            return best;
        }

        private static ObservedObject Nearest(List<ObservedObject> list, Vector3 pos)
        {
            ObservedObject best = null; float bd = float.MaxValue;
            foreach (var o in list) { float d = Vector3.Distance(o.WorldPosition, pos); if (d < bd) { bd = d; best = o; } }
            return best;
        }

        private Vector3 EstimateExtents(Detection d, CapturedFrame frame, Vector3 pos)
        {
            if (!d.HasBounds) return new Vector3(0.12f, 0.12f, 0.12f);
            float dist = Mathf.Max(0.3f, Vector3.Distance(frame.CameraPose.position, pos));
            float w = d.NormalizedBounds.width * 2f * dist * Mathf.Tan(frame.HorizontalFovDegrees * 0.5f * Mathf.Deg2Rad) * 0.5f;
            float h = d.NormalizedBounds.height * 2f * dist * Mathf.Tan(frame.VerticalFovDegrees * 0.5f * Mathf.Deg2Rad) * 0.5f;
            return new Vector3(Mathf.Clamp(w, 0.03f, 1.5f), Mathf.Clamp(h, 0.03f, 1.5f), Mathf.Clamp(w, 0.03f, 1.5f));
        }

        private string BuildSpatialContext()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("User is at ").Append(Spatial.HeadPosition.ToString("0.0")).Append(", looking toward ").Append(Spatial.GazeDirection.ToString("0.0")).AppendLine();
            if (Scene != null && WintrySettings.Current.Vision.SceneUnderstanding) sb.Append(Scene.Describe(Spatial.HeadPosition, Spatial.GazeDirection, 14));
            if (Location != null && Location.HasFix) sb.AppendLine(Location.DescribeForContext());
            return sb.ToString();
        }

        private string BuildSearchQuery(string text, List<ObservedObject> refs, List<ObservedObject> seen, string lang)
        {
            ObservedObject o = refs.Count > 0 ? refs[0] : Memory.Focus ?? (seen.Count > 0 ? seen[0] : null);
            string subject = o != null ? o.DisplayName : "";
            string t = text.ToLowerInvariant();
            string kind = (t.Contains("price") || t.Contains("cost") || t.Contains("costa") || t.Contains("prezzo") || t.Contains("kostet") || t.Contains("prix") || t.Contains("precio")) ? " price" :
                          (t.Contains("review") || t.Contains("recension") || t.Contains("bewert") || t.Contains("avis") || t.Contains("reseña")) ? " review" : "";
            return string.IsNullOrEmpty(subject) ? text : subject + kind;
        }

        private void IndicateTarget(Vector3 target, string label)
        {
            Pointers?.PointAt(target, label, 8f);
            Character?.PointAt(target);
            Audio?.PlayCueAt("pointer", target);
            WintryEvents.Publish(new PointAtEvent { Label = label });
        }

        // ------------------------------------------------------------------ looks
        private async Task ProposeLookAsync(string text, string lang, CancellationToken ct)
        {
            if (AssetGen == null) { await SpeakAsync(Localization.Get("err.generic", lang), lang, ct); return; }
            SetState(AssistantState.Thinking);
            var concept = await AssetGen.ProposeLookAsync(text, ct);
            if (!concept.Success) { await SpeakAsync(Localization.Get("err.generic", lang), lang, ct); return; }
            _pendingLook = concept;
            Cards?.Show(new InformationCardData
            {
                Id = "look", Title = concept.Look.Name, Subtitle = "New look", Lines = SplitLines(concept.Description ?? "", 3),
                ActionLabel = lang == "it" ? "Applica" : lang == "de" ? "Anwenden" : lang == "fr" ? "Appliquer" : lang == "es" ? "Aplicar" : "Apply", ActionQuery = "yes"
            });
            ConceptPreview.Show(concept, Spatial);
            Character?.SetExpression(FacialExpression.Excited);
            await SpeakAsync(Localization.Get("look.confirm", lang), lang, ct);
            Voice?.OpenConversationWindow(15f);
        }

        private async Task ResolvePendingLookAsync(bool yes, string lang, CancellationToken ct)
        {
            var concept = _pendingLook; _pendingLook = null;
            ConceptPreview.Hide();
            if (!yes) { await SpeakAsync(lang == "it" ? "Va bene, resto così." : lang == "de" ? "Okay, ich bleibe so." : lang == "fr" ? "D'accord, je reste comme ça." : lang == "es" ? "Vale, me quedo así." : "Okay, I'll stay as I am.", lang, ct); return; }
            SetState(AssistantState.Thinking);
            var asset = await AssetGen.BuildLookAsync(concept, ct);
            if (asset.Stage != AssetPipelineStage.Ready) { await SpeakAsync(Localization.Get("err.generic", lang), lang, ct); return; }
            Character?.ApplyCustomLook(asset.Look);
            Character?.SetExpression(FacialExpression.Happy);
            await SpeakAsync(Localization.Get("look.applied", lang), lang, ct);
        }

        private static bool IsConfirmation(string text, out bool yes)
        {
            string t = text.Trim().ToLowerInvariant().TrimEnd('.', '!');
            string[] yesW = { "yes", "yeah", "yep", "sure", "ok", "okay", "apply", "do it", "sì", "si", "va bene", "certo", "applica", "ja", "klar", "oui", "d'accord", "sí", "vale", "claro" };
            string[] noW = { "no", "nope", "cancel", "annulla", "nein", "non", "keep", "lascia", "discard" };
            foreach (var w in yesW) if (t == w || t.StartsWith(w + " ") || t.StartsWith(w + ",")) { yes = true; return true; }
            foreach (var w in noW) if (t == w || t.StartsWith(w + " ") || t.StartsWith(w + ",")) { yes = false; return true; }
            yes = false; return false;
        }

        // ------------------------------------------------------------------ helpers
        private async Task SpeakAsync(string text, string lang, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            SetState(AssistantState.Speaking);
            if (Voice != null) await Voice.SpeakAsync(text, lang, ct);
            else await Task.Delay(Mathf.Clamp(text.Length * 40, 500, 6000), ct);
            if (State == AssistantState.Speaking) SetState(AssistantState.Idle);
        }

        private bool CloudUsable(bool providerNeedsNet) => !providerNeedsNet || (_online && WintrySettings.Current.CloudAllowed);

        private string CurrentLanguage(string hint, string text = null)
        {
            var s = WintrySettings.Current;
            if (s.Voice.Language != WintryLanguage.Auto) return s.EffectiveLanguage();
            if (!string.IsNullOrEmpty(hint) && hint != "auto") return Localization.Normalize(hint);
            if (!string.IsNullOrEmpty(text)) return IntentDetector.DetectLanguage(text);
            return s.EffectiveLanguage();
        }

        private static bool LooksLikeExplicitName(string text, ObservedObject o)
        {
            string t = text.ToLowerInvariant();
            return !string.IsNullOrEmpty(o.Identity) && t.Contains(o.Identity.ToLowerInvariant());
        }

        private static bool WantsTranslation(string text) { string t = text.ToLowerInvariant(); return t.Contains("translat") || t.Contains("tradu") || t.Contains("übersetz"); }
        private static bool WantsSummary(string text) { string t = text.ToLowerInvariant(); return t.Contains("summar") || t.Contains("riassum") || t.Contains("zusammen") || t.Contains("résum") || t.Contains("resum") || t.Contains("mean") || t.Contains("significa") || t.Contains("bedeut") || t.Contains("veut dire") || t.Contains("explain") || t.Contains("spieg"); }

        private static string TargetLanguageFromText(string text, string fallback)
        {
            string t = text.ToLowerInvariant();
            if (t.Contains("italian") || t.Contains("italiano") || t.Contains("italienisch") || t.Contains("italien")) return "it";
            if (t.Contains("english") || t.Contains("inglese") || t.Contains("englisch") || t.Contains("anglais") || t.Contains("inglés")) return "en";
            if (t.Contains("german") || t.Contains("tedesco") || t.Contains("deutsch") || t.Contains("allemand") || t.Contains("alemán")) return "de";
            if (t.Contains("french") || t.Contains("francese") || t.Contains("französisch") || t.Contains("français") || t.Contains("francés")) return "fr";
            if (t.Contains("spanish") || t.Contains("spagnolo") || t.Contains("spanisch") || t.Contains("espagnol") || t.Contains("español")) return "es";
            return fallback;
        }

        private static List<string> SplitLines(string text, int max)
        {
            var list = new List<string>();
            foreach (var l in (text ?? "").Split('\n')) { if (l.Trim().Length == 0) continue; list.Add(l.Trim().Length > 60 ? l.Trim().Substring(0, 57) + "…" : l.Trim()); if (list.Count >= max) break; }
            return list;
        }

        private static string MoreQuery(string title, string lang) => lang == "it" ? "Dimmi di più su " + title : lang == "de" ? "Erzähl mir mehr über " + title : lang == "fr" ? "Dis-m'en plus sur " + title : lang == "es" ? "Cuéntame más sobre " + title : "Tell me more about " + title;
        private static string SummaryQuery(string lang) => lang == "it" ? "Riassumi questo testo" : lang == "de" ? "Fass diesen Text zusammen" : lang == "fr" ? "Résume ce texte" : lang == "es" ? "Resume este texto" : "Summarize this text";

        public void Dismiss()
        {
            _turnCts?.Cancel();
            Voice?.Interrupt();
            Voice?.CloseConversationWindow();
            QuickMenu?.Close();
            _pendingLook = null; ConceptPreview.Hide();
            if (Character != null && Character.Form == PresenceForm.Character) _ = Character.CollapseToCoreAsync();
            SetState(AssistantState.Idle);
        }

        public void ClearSession()
        {
            Memory.ClearContext();
            Highlights?.ClearAll(); Pointers?.ClearAll(); Cards?.ClearAll(); Translations?.ClearAll(); History?.Clear();
            foreach (var e in new List<SceneEntity>(Scene.Entities)) if (e.Source == "vision") Scene.RemoveObject(e.ObservedObjectId);
            WintryLog.I("Orchestrator", "Session cleared");
        }

        private void ClearAllData()
        {
            ClearSession();
            PlayerPrefs.DeleteAll(); PlayerPrefs.Save();
            SecretStore.ClearAll();
            try { var dir = System.IO.Path.Combine(Application.persistentDataPath, "WintryAssetCache"); if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true); } catch { }
            WintrySettings.Current.ResetToDefaults();
            WintryLog.I("Orchestrator", "All data cleared");
        }
    }

    /// <summary>Shows the concept image of a proposed look on a small floating quad.</summary>
    public static class ConceptPreview
    {
        private static GameObject _go;
        public static void Show(ConceptResult concept, ISpatialService spatial)
        {
            Hide();
            if (concept == null || concept.ConceptImage == null) return;
            _go = new GameObject("ConceptPreview");
            _go.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Quad(0.22f, 0.22f);
            var mr = _go.AddComponent<MeshRenderer>();
            var m = WintryMaterials.Unlit(Color.white);
            m = new Material(m); m.mainTexture = concept.ConceptImage;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", concept.ConceptImage);
            mr.sharedMaterial = m;
            _go.transform.position = WorldFirstLayout.SidePosition(spatial, 0.9f, false, 2f);
            _go.AddComponent<UI.BillboardToHead>().Head = spatial.Head;
        }
        public static void Hide() { if (_go != null) UnityEngine.Object.Destroy(_go); _go = null; }
    }
}
