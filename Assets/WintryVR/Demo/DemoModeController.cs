using System;
using System.Collections.Generic;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Voice.Providers;

namespace WintryVR.Demo
{
    /// <summary>
    /// Demo Mode: drives the whole product without a headset, camera, microphone or network. Simulates
    /// object recognition, AI answers, OCR, spatial markers, scene understanding and voice using the mock
    /// providers and a scripted scenario. Keys: N = next step, 1..8 = jump to step, R = restart, T = type.
    /// On device the scenario can be advanced from the quick menu ("Demo ▶") when demo mode is on.
    /// </summary>
    public class DemoModeController : MonoBehaviour
    {
        public Func<string, string, System.Threading.Tasks.Task> Utterance;  // (text, lang) → orchestrator
        public MockSttProvider Stt;
        public bool Enabled;
        public IReadOnlyList<DemoScenario.Step> Steps => DemoScenario.Steps;
        public int CurrentStep { get; private set; } = -1;
        private string _typed = "";
        private bool _typing;

        public void Next()
        {
            if (!Enabled || Steps.Count == 0) return;
            CurrentStep = (CurrentStep + 1) % Steps.Count;
            Run(Steps[CurrentStep]);
        }

        public void Jump(int index)
        {
            if (!Enabled || index < 0 || index >= Steps.Count) return;
            CurrentStep = index; Run(Steps[index]);
        }

        public void Restart() { CurrentStep = -1; }

        private void Run(DemoScenario.Step step)
        {
            WintryLog.I("Demo", "Step " + (CurrentStep + 1) + "/" + Steps.Count + ": " + step.Utterance);
            Utterance?.Invoke(step.Utterance, step.Language);
        }

        private void Update()
        {
            if (!Enabled) return;
            if (_typing)
            {
                foreach (char c in UnityEngine.Input.inputString)
                {
                    if (c == '\n' || c == '\r') { _typing = false; if (_typed.Length > 0) Utterance?.Invoke(_typed, "auto"); _typed = ""; }
                    else if (c == '\b') { if (_typed.Length > 0) _typed = _typed.Substring(0, _typed.Length - 1); }
                    else _typed += c;
                }
                return;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.N)) Next();
            if (UnityEngine.Input.GetKeyDown(KeyCode.R)) Restart();
            if (UnityEngine.Input.GetKeyDown(KeyCode.T)) { _typing = true; _typed = ""; }
            for (int i = 0; i < 9; i++) if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1 + i)) Jump(i);
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!Enabled) return;
            GUI.Label(new Rect(10, Screen.height - 70, 900, 60),
                "DEMO MODE  [N] next  [1-9] step  [R] restart  [T] type  [Space] push-to-talk  [M] menu  [F1] debug  [RMB] look  [WASD] move" +
                (_typing ? "\n> " + _typed + "_" : CurrentStep >= 0 ? "\nStep " + (CurrentStep + 1) + ": " + Steps[CurrentStep].Utterance : ""));
        }
#endif
    }

    public static class DemoScenario
    {
        public class Step { public string Utterance; public string Language = "en"; public string Note; }

        public static readonly List<Step> Steps = new List<Step>
        {
            new Step { Utterance = "Hey Wintry, what am I looking at?", Note = "wake word + IDENTIFY → Core morphs into the character, vision capture, info card" },
            new Step { Utterance = "How much does it cost?", Note = "SEARCH with context (\"it\" = focused object) → price card" },
            new Step { Utterance = "Read this.", Note = "OCR" },
            new Step { Utterance = "Translate it into Italian.", Note = "TRANSLATE → overlay near the text" },
            new Step { Utterance = "Where is my phone?", Note = "LOCATE from memory/scene → spatial pointer" },
            new Step { Utterance = "How many chairs are there?", Note = "COUNT from scene graph" },
            new Step { Utterance = "What's on the table?", Note = "DESCRIBE region from scene graph" },
            new Step { Utterance = "Which of the three is more expensive?", Note = "COMPARE with memory" },
            new Step { Utterance = "Wintry, create a new look for you: frosted white with soft blue glow.", Note = "CHANGE_LOOK → concept → confirm → apply" },
            new Step { Utterance = "Thanks Wintry, that's all.", Note = "DISMISS → character collapses to Core" }
        };
    }
}
