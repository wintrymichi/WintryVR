using System.Collections;
using UnityEngine;
using WintryVR.Core;
using WintryVR.Settings;

namespace WintryVR.MR
{
    /// <summary>
    /// Optional, opt-in location awareness (coarse). Off by default; only started after the privacy toggle
    /// and the OS permission are granted. Used purely as text context ("near 45.46, 9.19") for the AI.
    /// </summary>
    public class LocationService : MonoBehaviour, ILocationService
    {
        public bool IsEnabled { get; private set; }
        public bool HasFix => IsEnabled && UnityEngine.Input.location.status == LocationServiceStatus.Running;
        public double Latitude => HasFix ? UnityEngine.Input.location.lastData.latitude : 0;
        public double Longitude => HasFix ? UnityEngine.Input.location.lastData.longitude : 0;

        public void SetEnabled(bool enabled)
        {
            if (enabled == IsEnabled) return;
            if (enabled) StartCoroutine(StartLocation()); else { UnityEngine.Input.location.Stop(); IsEnabled = false; }
        }

        private IEnumerator StartLocation()
        {
            var permTask = PermissionManager.EnsureLocationAsync();
            while (!permTask.IsCompleted) yield return null;
            if (!permTask.Result) { WintryLog.W("Location", "Permission denied or disabled"); yield break; }
            if (!UnityEngine.Input.location.isEnabledByUser) { WintryLog.W("Location", "Location disabled on device"); yield break; }
            UnityEngine.Input.location.Start(500f, 50f);
            int wait = 15;
            while (UnityEngine.Input.location.status == LocationServiceStatus.Initializing && wait-- > 0) yield return new WaitForSeconds(1f);
            IsEnabled = UnityEngine.Input.location.status == LocationServiceStatus.Running;
            WintryLog.I("Location", IsEnabled ? "Location running" : "Location failed: " + UnityEngine.Input.location.status);
        }

        public string DescribeForContext()
        {
            if (!HasFix) return "";
            return "Approximate user location: lat " + Latitude.ToString("0.000") + ", lon " + Longitude.ToString("0.000") + " (coarse, opt-in).";
        }
    }
}
