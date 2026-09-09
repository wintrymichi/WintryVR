using System;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Settings;
using WintryVR.Spatial;

namespace WintryVR.UI
{
    /// <summary>
    /// First launch (< 60 s): WINTRYVR / tagline → "Look around." → the Core appears → "Hi. I'm Wintry." →
    /// "Ask me anything about what you see." → hint about the wake word / pinch. Skippable by pinch.
    /// </summary>
    public class OnboardingFlow : MonoBehaviour
    {
        public ISpatialService Spatial;
        public bool Skip;

        public async Task RunAsync(Func<string, Task> speak, Action showCore)
        {
            string lang = WintrySettings.Current.EffectiveLanguage();
            var panel = GlassPanel.Create("Onboarding", 0.34f, 0.12f, Spatial?.Head, new Color(0.05f, 0.08f, 0.14f));
            panel.transform.SetParent(transform, false);
            panel.Closable = false; panel.Resizable = false; panel.Movable = false;
            panel.transform.position = Spatial.ComfortablePosition(1.1f, 0f, -4f);
            panel.SetTitle("WINTRYVR");
            panel.SetBody(Localization.Get("app.tagline", lang));
            panel.AddButton("Skip", () => Skip = true, 0.08f);
            await Wait(2.4f);
            if (!Skip)
            {
                panel.SetBody(Localization.Get("onboard.look", lang));
                await Wait(2.2f);
            }
            showCore?.Invoke();
            if (!Skip)
            {
                panel.SetBody(Localization.Get("onboard.hi", lang));
                await speak(Localization.Get("onboard.hi", lang));
                await Wait(0.4f);
            }
            if (!Skip)
            {
                panel.SetBody(Localization.Get("onboard.ask", lang));
                await speak(Localization.Get("onboard.ask", lang));
                await Wait(0.6f);
            }
            if (!Skip)
            {
                panel.SetBody(Localization.Get("onboard.wake", lang));
                await Wait(3f);
            }
            panel.Close();
            WintrySettings.Current.OnboardingCompleted = true;
            WintrySettings.Current.Save("onboarding");
        }

        private async Task Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end && !Skip) await Task.Yield();
        }
    }
}
