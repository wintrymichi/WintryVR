using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace WintryVR.Settings
{
    /// <summary>
    /// Runtime permission handling for Android/Quest. Every permission is requested lazily, right before the
    /// feature that needs it, with the reason visible in the UI. Nothing is requested silently at startup.
    /// </summary>
    public static class PermissionManager
    {
        public const string Microphone = "android.permission.RECORD_AUDIO";
        public const string Camera = "android.permission.CAMERA";
        public const string HeadsetCamera = "horizonos.permission.HEADSET_CAMERA"; // Passthrough Camera API (HorizonOS v74+)
        public const string SceneUse = "com.oculus.permission.USE_SCENE";
        public const string FineLocation = "android.permission.ACCESS_FINE_LOCATION";
        public const string CoarseLocation = "android.permission.ACCESS_COARSE_LOCATION";

        private static readonly Dictionary<string, bool> _cache = new Dictionary<string, bool>();

        public static bool Has(string permission)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Permission.HasUserAuthorizedPermission(permission);
#else
            return true;
#endif
        }

        /// <summary>Requests a permission and resolves with the user's decision.</summary>
        public static Task<bool> RequestAsync(string permission)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(permission)) return Task.FromResult(true);
            var tcs = new TaskCompletionSource<bool>();
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => { WintryLog.I("Permissions", permission + " granted"); tcs.TrySetResult(true); };
            callbacks.PermissionDenied += _ => { WintryLog.W("Permissions", permission + " denied"); tcs.TrySetResult(false); };
            callbacks.PermissionDeniedAndDontAskAgain += _ => { WintryLog.W("Permissions", permission + " denied permanently"); tcs.TrySetResult(false); };
            Permission.RequestUserPermission(permission, callbacks);
            return tcs.Task;
#else
            WintryLog.V("Permissions", "Editor: auto-granting " + permission);
            return Task.FromResult(true);
#endif
        }

        public static async Task<bool> EnsureMicrophoneAsync()
        {
            if (!WintrySettings.Current.Privacy.MicrophoneAllowed) return false;
            return await RequestAsync(Microphone);
        }

        public static async Task<bool> EnsureCameraAsync()
        {
            if (!WintrySettings.Current.Privacy.CameraAllowed) return false;
            bool cam = await RequestAsync(Camera);
            bool headset = await RequestAsync(HeadsetCamera);
            return cam && headset;
        }

        public static async Task<bool> EnsureLocationAsync()
        {
            if (!WintrySettings.Current.Privacy.LocationAllowed) return false;
            return await RequestAsync(CoarseLocation);
        }

        public static async Task<bool> EnsureSceneAsync()
        {
            return await RequestAsync(SceneUse);
        }
    }
}
