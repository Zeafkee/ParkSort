using System.Collections.Generic;
using UnityEngine;

namespace ParkSort.View
{
    public sealed class GarageLane
    {
        public enum Phase
        {
            Hidden,
            Open,
            Closing,
            Opening,
        }

        public struct Operation
        {
            public bool open;
            public int queueIndex;
            public int color;
        }

        public readonly Transform Transform;
        public readonly MeshRenderer Renderer;
        public readonly List<CarView> Parked = new List<CarView>(3);
        public readonly Queue<Operation> Operations = new Queue<Operation>(8);

        public int Lane;
        public int QueueIndex = -1;
        public int Color = -1;
        public int Arrived;
        public Phase State = Phase.Hidden;
        public float Timer;

        public GarageLane(Transform transform, MeshRenderer renderer, int lane)
        {
            Transform = transform;
            Renderer = renderer;
            Lane = lane;
        }

        public bool IsOpenFor(int queueIndex) => State == Phase.Open && QueueIndex == queueIndex;
        public bool Busy => Operations.Count > 0 || State == Phase.Closing || State == Phase.Opening;
    }
}
