using System;
using System.Collections.Generic;

namespace WintryVR.Core
{
    /// <summary>
    /// Lightweight typed event bus. Layers communicate through events rather than direct references
    /// so that MR, AI, Voice, Spatial, Character and UI stay decoupled.
    /// </summary>
    public static class WintryEvents
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        public static void Subscribe<T>(Action<T> handler)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) { list = new List<Delegate>(); _handlers[typeof(T)] = list; }
            list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (_handlers.TryGetValue(typeof(T), out var list)) list.Remove(handler);
        }

        /// <summary>Publishes on the calling thread. Use PublishOnMain from background threads.</summary>
        public static void Publish<T>(T evt)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;
            var snapshot = list.ToArray();
            foreach (var d in snapshot)
            {
                try { ((Action<T>)d)(evt); }
                catch (Exception ex) { WintryLog.E("Events", "Handler for " + typeof(T).Name + " failed", ex); }
            }
        }

        public static void PublishOnMain<T>(T evt)
        {
            if (MainThreadDispatcher.IsMainThread) Publish(evt);
            else MainThreadDispatcher.Enqueue(() => Publish(evt));
        }

        public static void Clear() => _handlers.Clear();
    }

    // ---- Event payloads -------------------------------------------------------

    public struct StateChangedEvent { public AssistantState Previous; public AssistantState Current; }
    public struct PresenceChangedEvent { public PresenceForm Previous; public PresenceForm Current; }
    public struct WakeWordDetectedEvent { public string Transcript; }
    public struct TranscriptEvent { public string Text; public bool IsFinal; public string LanguageCode; }
    public struct AssistantReplyEvent { public AssistantTurn Turn; }
    public struct ObjectsObservedEvent { public List<ObservedObject> Objects; }
    public struct PrivacyIndicatorEvent { public bool CameraActive; public bool MicrophoneActive; public bool CloudActive; }
    public struct ConnectivityChangedEvent { public bool Online; }
    public struct SettingsChangedEvent { public string Section; }
    public struct SpeechAmplitudeEvent { public float Amplitude; }
    public struct UserMessageEvent { public string Text; }          // a request to handle (cards, demo, typed)
    public struct UserTurnEvent { public string Text; public string LanguageCode; }   // what the user said, for display
    public struct PointAtEvent { public ObservedObject Target; public string Label; }
    public struct TranslationReadyEvent { public string Original; public string Translated; public string SourceLanguage; public string TargetLanguage; public UnityEngine.Vector3 WorldPosition; }
    public struct InformationCardRequestEvent { public InformationCardData Card; }
    public struct CapabilitiesDetectedEvent { public DeviceCapabilities Capabilities; }
}
