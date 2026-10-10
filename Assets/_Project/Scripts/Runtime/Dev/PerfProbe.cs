#if UNITY_EDITOR || DEBUG
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace ParkSort.Dev
{
    public sealed class PerfProbe : MonoBehaviour
    {
        private ProfilerRecorder _gc;
        private ProfilerRecorder _drawCalls;
        private ProfilerRecorder _batches;
        private ProfilerRecorder _setPass;
        private ProfilerRecorder _triangles;
        private ProfilerRecorder _mainThread;
        private float _warmup;
        private float _remaining;
        private int _frames;
        private int _framesWithGc;
        private long _gcTotal;
        private long _gcWorst;
        private double _drawCallSum;
        private double _batchSum;
        private double _setPassSum;
        private double _triangleSum;
        private double _mainThreadSum;
        private long _maxDrawCalls;
        private double _worstFrame;

        public string Report { get; private set; } = "running";

        public static PerfProbe Begin(float seconds)
        {
            var existing = FindAnyObjectByType<PerfProbe>();
            if (existing != null) DestroyImmediate(existing.gameObject);

            var probe = new GameObject("PerfProbe").AddComponent<PerfProbe>();
            probe._remaining = seconds;
            probe._warmup = 1f;
            return probe;
        }

        private void OnEnable()
        {
            _gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
        }

        private void OnDisable()
        {
            _gc.Dispose();
            _drawCalls.Dispose();
            _batches.Dispose();
            _setPass.Dispose();
            _triangles.Dispose();
            _mainThread.Dispose();
        }

        private void Update()
        {
            if (_warmup > 0f)
            {
                _warmup -= Time.unscaledDeltaTime;
                return;
            }

            if (_remaining <= 0f) return;

            _frames++;
            long gc = _gc.LastValue;
            _gcTotal += gc;
            if (gc > 0) _framesWithGc++;
            if (gc > _gcWorst) _gcWorst = gc;
            _drawCallSum += _drawCalls.LastValue;
            _batchSum += _batches.LastValue;
            _setPassSum += _setPass.LastValue;
            _triangleSum += _triangles.LastValue;
            _mainThreadSum += _mainThread.LastValue * 1e-6;
            if (_drawCalls.LastValue > _maxDrawCalls) _maxDrawCalls = _drawCalls.LastValue;
            double frame = Time.unscaledDeltaTime * 1000.0;
            if (frame > _worstFrame) _worstFrame = frame;

            _remaining -= Time.unscaledDeltaTime;
            if (_remaining > 0f) return;

            var text = new StringBuilder();
            text.Append("frames=").Append(_frames);
            text.Append(" mainThread=").Append((_mainThreadSum / _frames).ToString("F2")).Append("ms");
            text.Append(" worstFrame=").Append(_worstFrame.ToString("F1")).Append("ms");
            text.Append(" drawCalls=").Append((_drawCallSum / _frames).ToString("F1")).Append(" (max ").Append(_maxDrawCalls).Append(")");
            text.Append(" batches=").Append((_batchSum / _frames).ToString("F1"));
            text.Append(" setPass=").Append((_setPassSum / _frames).ToString("F1"));
            text.Append(" triangles=").Append((_triangleSum / _frames).ToString("F0"));
            text.Append(" gcPerFrame=").Append((_gcTotal / (double)_frames).ToString("F0")).Append("B");
            text.Append(" framesWithGc=").Append(_framesWithGc).Append(" worstGc=").Append(_gcWorst).Append("B");
            Report = text.ToString();
        }
    }
}
#endif
