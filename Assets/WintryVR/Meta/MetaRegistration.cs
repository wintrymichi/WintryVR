using UnityEngine;
using WintryVR.Core;
using WintryVR.Interaction;
using WintryVR.MR;
using WintryVR.SceneUnderstanding;

namespace WintryVR.MetaPlatform
{
    /// <summary>
    /// Registers the Meta XR implementations with the provider registry when the Meta XR Core SDK (and MRUK)
    /// packages are present. The runtime never references these types directly, so the project still compiles
    /// and runs (with generic/mock fallbacks) if the packages are removed.
    /// Targets Meta XR SDK v74+ (OVRCameraRig, OVRManager, OVRPassthroughLayer, OVRHand, OVRInput,
    /// OVRSpatialAnchor, MRUK). See docs/META_SDK_NOTES.md for the exact API surface used.
    /// </summary>
    public static class MetaRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
#if WINTRY_META_XR
            ProviderRegistry.Register<ICapabilityProbe>("MetaXR", 10, () => new MetaCapabilityProbe());
            ProviderRegistry.Register<IMRRigProvider>("MetaXR", 10, () => new MetaMRRigProvider());
            ProviderRegistry.Register<IAnchorService>("MetaXR", 10, () => new MetaAnchorService());
            ProviderRegistry.Register<IXRInputSource>("MetaHands", 20, () => new MetaHandInputSource());
            ProviderRegistry.Register<IXRInputSource>("MetaControllers", 10, () => new MetaControllerInputSource());
#if WINTRY_MRUK
            ProviderRegistry.Register<ISceneProvider>("MRUK", 10, () => new MrukSceneProvider());
#endif
            WintryLog.I("Meta", "Meta XR providers registered");
#else
            WintryLog.I("Meta", "Meta XR SDK not present – generic providers will be used");
#endif
        }
    }
}
