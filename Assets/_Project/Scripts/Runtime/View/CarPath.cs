using System.Collections.Generic;
using UnityEngine;

namespace ParkSort.View
{
    public sealed class CarPath
    {
        private const float CornerRadius = 0.5f;
        private const int CornerSteps = 5;

        private readonly List<Vector3> _raw = new List<Vector3>(16);
        private readonly List<Vector3> _points = new List<Vector3>(64);
        private readonly List<float> _lengths = new List<float>(64);
        private readonly Vector3[] _ordered = new Vector3[4];
        private readonly float[] _orderedKeys = new float[4];
        private int _orderedCount;
        private int _segment;

        public float Length { get; private set; }
        public Vector3 End => _points[_points.Count - 1];

        public void Clear()
        {
            _raw.Clear();
            _points.Clear();
            _lengths.Clear();
            _orderedCount = 0;
            _segment = 0;
            Length = 0f;
        }

        public void Add(Vector3 point)
        {
            if (_raw.Count > 0 && (_raw[_raw.Count - 1] - point).sqrMagnitude < 0.0004f) return;
            _raw.Add(point);
        }

        public void AddOrdered(Vector3 point, float key)
        {
            int i = _orderedCount++;
            while (i > 0 && _orderedKeys[i - 1] > key)
            {
                _ordered[i] = _ordered[i - 1];
                _orderedKeys[i] = _orderedKeys[i - 1];
                i--;
            }

            _ordered[i] = point;
            _orderedKeys[i] = key;
        }

        public void FlushOrdered()
        {
            for (int i = 0; i < _orderedCount; i++) Add(_ordered[i]);
            _orderedCount = 0;
        }

        public void Build()
        {
            _points.Clear();
            _lengths.Clear();
            _segment = 0;
            if (_raw.Count == 0) return;

            _points.Add(_raw[0]);
            for (int i = 1; i < _raw.Count - 1; i++)
            {
                Vector3 previous = _raw[i - 1], corner = _raw[i], next = _raw[i + 1];
                Vector3 incoming = corner - previous, outgoing = next - corner;
                float radius = Mathf.Min(CornerRadius, incoming.magnitude * 0.5f, outgoing.magnitude * 0.5f);
                if (radius < 0.05f || Vector3.Dot(incoming.normalized, outgoing.normalized) > 0.999f)
                {
                    _points.Add(corner);
                    continue;
                }

                Vector3 start = corner - incoming.normalized * radius;
                Vector3 end = corner + outgoing.normalized * radius;
                for (int s = 0; s <= CornerSteps; s++)
                {
                    float t = s / (float)CornerSteps;
                    float u = 1f - t;
                    _points.Add(u * u * start + 2f * u * t * corner + t * t * end);
                }
            }

            if (_raw.Count > 1) _points.Add(_raw[_raw.Count - 1]);

            float total = 0f;
            _lengths.Add(0f);
            for (int i = 1; i < _points.Count; i++)
            {
                total += Vector3.Distance(_points[i - 1], _points[i]);
                _lengths.Add(total);
            }

            Length = total;
        }

        public void Evaluate(float distance, out Vector3 position, out Vector3 forward)
        {
            if (_points.Count == 1)
            {
                position = _points[0];
                forward = Vector3.forward;
                return;
            }

            distance = Mathf.Clamp(distance, 0f, Length);
            while (_segment < _points.Count - 2 && _lengths[_segment + 1] < distance) _segment++;
            while (_segment > 0 && _lengths[_segment] > distance) _segment--;

            float span = _lengths[_segment + 1] - _lengths[_segment];
            float t = span > 0.0001f ? (distance - _lengths[_segment]) / span : 0f;
            Vector3 a = _points[_segment], b = _points[_segment + 1];
            position = Vector3.Lerp(a, b, t);
            forward = b - a;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.000001f) forward = Vector3.forward;
            else forward.Normalize();
        }
    }
}
