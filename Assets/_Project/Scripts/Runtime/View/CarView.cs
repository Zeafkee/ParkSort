using ParkSort.Core;
using UnityEngine;

namespace ParkSort.View
{
    public enum CarState
    {
        Pooled,
        Parked,
        Driving,
        AtGate,
        InWaiting,
        InGarage,
        Overflow,
        InTunnel,
    }

    public enum MissionKind
    {
        None,
        Garage,
        Waiting,
        Overflow,
    }

    public struct Mission
    {
        public MissionKind kind;
        public int lane;
        public int queueIndex;
        public int slot;
    }

    public sealed class CarView
    {
        public readonly GameObject Root;
        public readonly Transform Transform;
        public readonly Transform Body;
        public readonly MeshFilter Filter;
        public readonly MeshRenderer Renderer;
        public readonly Transform Question;
        public readonly Transform Shadow;
        public readonly CarPath Path = new CarPath();

        public int Index;
        public CarDef Def;
        public CarState State;
        public Mission Current;
        public Mission Next;
        public float Distance;
        public float Speed;
        public float ReverseUntil;
        public bool FinalLeg;
        public bool Hidden;
        public int WaitingSlot = -1;
        public float QuestionHeight;
        public int VisualLength = 2;

        public CarView(Transform parent, Material shadowMaterial, Mesh quad, Mesh questionMesh, Material neutral)
        {
            Root = new GameObject("Car");
            Transform = Root.transform;
            Transform.SetParent(parent, false);

            var body = new GameObject("Body");
            Body = body.transform;
            Body.SetParent(Transform, false);
            Filter = body.AddComponent<MeshFilter>();
            Renderer = body.AddComponent<MeshRenderer>();
            Configure(Renderer);

            var shadow = new GameObject("Shadow");
            Shadow = shadow.transform;
            Shadow.SetParent(Transform, false);
            Shadow.localPosition = new Vector3(0f, 0.02f, 0f);
            shadow.AddComponent<MeshFilter>().sharedMesh = quad;
            var shadowRenderer = shadow.AddComponent<MeshRenderer>();
            shadowRenderer.sharedMaterial = shadowMaterial;
            Configure(shadowRenderer);

            var question = new GameObject("Question");
            Question = question.transform;
            Question.SetParent(Transform, false);
            question.AddComponent<MeshFilter>().sharedMesh = questionMesh;
            var questionRenderer = question.AddComponent<MeshRenderer>();
            questionRenderer.sharedMaterial = neutral;
            Configure(questionRenderer);

            Root.SetActive(false);
            State = CarState.Pooled;
        }

        private static void Configure(MeshRenderer renderer)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        public float HalfLength => VisualLength * 0.5f;
    }
}
