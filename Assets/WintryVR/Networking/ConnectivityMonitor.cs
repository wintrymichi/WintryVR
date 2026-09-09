using System.Collections;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Networking
{
    /// <summary>
    /// Tracks online/offline state and publishes ConnectivityChangedEvent. Offline Mode keeps MR,
    /// tracking, UI, anchors and local scene understanding alive while cloud features degrade gracefully.
    /// </summary>
    public sealed class ConnectivityMonitor : MonoBehaviour
    {
        public static ConnectivityMonitor Instance { get; private set; }
        public bool Online { get; private set; } = true;
        public bool ForceOffline;

        private void Awake()
        {
            Instance = this;
            ForceOffline = WintryConfig.Instance.ForceOffline;
        }

        private void Start() => StartCoroutine(Poll());

        private IEnumerator Poll()
        {
            var wait = new WaitForSeconds(3f);
            while (true)
            {
                bool now = !ForceOffline && Application.internetReachability != NetworkReachability.NotReachable;
                if (now != Online)
                {
                    Online = now;
                    WintryLog.I("Net", now ? "Online" : "Offline");
                    WintryEvents.Publish(new ConnectivityChangedEvent { Online = now });
                }
                yield return wait;
            }
        }

        public static bool IsOnline => Instance == null ? Application.internetReachability != NetworkReachability.NotReachable : Instance.Online;
    }
}
