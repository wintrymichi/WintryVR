using UnityEngine;
using WintryVR.Core;

namespace WintryVR.Interaction
{
    /// <summary>
    /// A source of pointer + button/pinch state. Implemented for Meta hands/controllers (WintryVR.Meta),
    /// for generic XR controllers (UnityEngine.XR InputDevices) and for the editor (mouse/keyboard).
    /// </summary>
    public interface IXRInputSource
    {
        string Name { get; }
        InputModality Modality { get; }
        int Priority { get; }
        bool IsActive { get; }                       // tracked / connected this frame
        void Poll();                                 // called once per frame before queries
        bool TryGetPointer(out Ray ray, out bool isLeft);
        bool Select(bool left);                      // pinch (hands) / trigger (controllers)
        bool PushToTalk();                           // left pinch-hold / grip
        bool Menu();                                 // palm-up / menu button
        bool Secondary();                            // secondary button (dismiss)
        bool DebugCombo();
    }
}
