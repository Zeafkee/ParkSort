using UnityEngine;

namespace ParkSort.View
{
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private float pitch = 58f;
        [SerializeField, Range(0f, 0.4f)] private float topPadding = 0.17f;
        [SerializeField, Range(0f, 0.4f)] private float bottomPadding = 0.2f;
        [SerializeField] private float sidePadding = 0.25f;
        [SerializeField] private float objectHeight = 1.3f;
        [SerializeField] private float distance = 40f;

        private Bounds _bounds = new Bounds(new Vector3(0f, 0f, 4f), new Vector3(9f, 0f, 18f));
        private float _lastAspect = -1f;

        public Camera Camera => cam;

        public void Fit(Bounds bounds)
        {
            _bounds = bounds;
            Apply();
        }

        private void LateUpdate()
        {
            if (!Mathf.Approximately(cam.aspect, _lastAspect)) Apply();
        }

        public void Apply()
        {
            _lastAspect = cam.aspect;
            float radians = pitch * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians), cos = Mathf.Cos(radians);

            float contentHeight = _bounds.size.z * sin + objectHeight * cos;
            float usable = Mathf.Max(0.2f, 1f - topPadding - bottomPadding);
            float sizeForHeight = contentHeight * 0.5f / usable;
            float sizeForWidth = (_bounds.size.x * 0.5f + sidePadding) / cam.aspect;
            float size = Mathf.Max(sizeForHeight, sizeForWidth);

            cam.orthographic = true;
            cam.orthographicSize = size;

            Quaternion rotation = Quaternion.Euler(pitch, 0f, 0f);
            Vector3 focus = _bounds.center;
            focus.z += (topPadding - bottomPadding) * size / sin + objectHeight * 0.5f * cos / sin;
            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
        }
    }
}
