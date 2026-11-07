using UnityEngine;

namespace CountToZero
{
    [ExecuteAlways]
    public sealed class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var camera = Application.isPlaying && GameManager.Instance != null ? GameManager.Instance.View : Camera.main;
            if (camera != null) transform.rotation = camera.transform.rotation;
        }
    }
}
