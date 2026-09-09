using UnityEngine;

namespace WintryVR.UI
{
    /// <summary>Keeps a quad facing the user (front side toward the head).</summary>
    public class BillboardToHead : MonoBehaviour
    {
        public Transform Head;
        private void LateUpdate()
        {
            if (Head == null) { var c = UnityEngine.Camera.main; if (c != null) Head = c.transform; else return; }
            Vector3 to = transform.position - Head.position;
            if (to.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }
    }
}
