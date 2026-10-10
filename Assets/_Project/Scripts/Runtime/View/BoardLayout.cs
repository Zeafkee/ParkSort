using ParkSort.Core;
using UnityEngine;

namespace ParkSort.View
{
    public sealed class BoardLayout
    {
        public const float RoadWidth = 1.3f;
        public const float GarageDepth = 3.2f;
        public const float GarageLaneWidth = 1.03f;
        public const float WaitingSpacing = 1.0f;
        public const float WaitingDepth = 2.95f;

        private int _width;
        private int _height;
        private int _baseWaitingSlots;

        public Vector2 LotMin { get; private set; }
        public Vector2 LotMax { get; private set; }
        public Vector2 RingMin { get; private set; }
        public Vector2 RingMax { get; private set; }
        public float GarageZ { get; private set; }
        public float WaitingZ { get; private set; }
        public Bounds Bounds { get; private set; }

        public void Configure(LevelDef level)
        {
            _width = level.width;
            _height = level.height;
            _baseWaitingSlots = level.waitingSlots;

            LotMin = new Vector2(-level.width * 0.5f, 0f);
            LotMax = new Vector2(level.width * 0.5f, level.height);
            float half = RoadWidth * 0.5f;
            RingMin = LotMin - new Vector2(half, half);
            RingMax = LotMax + new Vector2(half, half);
            GarageZ = LotMax.y + RoadWidth + 0.55f + GarageDepth * 0.5f;
            WaitingZ = LotMin.y - RoadWidth - 0.45f - WaitingDepth * 0.5f;

            float halfWidth = Mathf.Max(level.width * 0.5f + RoadWidth + 0.5f, 3.9f);
            float minZ = WaitingZ - WaitingDepth * 0.5f - 0.3f;
            float maxZ = GarageZ + GarageDepth * 0.5f + 0.6f;
            Bounds = new Bounds(new Vector3(0f, 0f, (minZ + maxZ) * 0.5f), new Vector3(halfWidth * 2f, 0f, maxZ - minZ));
        }

        public Vector3 CellCenter(float x, float y) => new Vector3(LotMin.x + x + 0.5f, 0f, LotMin.y + y + 0.5f);

        public Vector3 CarCenter(CarDef car)
        {
            float offset = (car.length - 1) * 0.5f;
            return CellCenter(car.x + Directions.Dx(car.direction) * offset, car.y + Directions.Dy(car.direction) * offset);
        }

        public static Vector3 Forward(Direction direction) => new Vector3(Directions.Dx(direction), 0f, Directions.Dy(direction));

        public static Quaternion Rotation(Direction direction) => Quaternion.LookRotation(Forward(direction), Vector3.up);

        public Vector3 GarageCenter(int lane) => new Vector3(lane == 0 ? -1.95f : 1.95f, 0f, GarageZ);

        public Vector3 GarageSlot(int lane, int slot, int carLength)
        {
            Vector3 center = GarageCenter(lane);
            float depthOffset = (3 - carLength) * 0.5f * 0.55f;
            return center + new Vector3((slot - 1) * GarageLaneWidth, 0.1f, depthOffset - 0.05f);
        }

        public Vector3 GarageGate(int lane, int slot) => new Vector3(GarageCenter(lane).x + (slot - 1) * GarageLaneWidth, 0f, RingMax.y);

        public float WaitingX(int slot) => (slot - (_baseWaitingSlots - 1) * 0.5f) * WaitingSpacing;

        public Vector3 WaitingSlot(int slot, int carLength)
        {
            float depthOffset = (3 - carLength) * 0.5f * 0.6f;
            return new Vector3(WaitingX(slot), 0.03f, WaitingZ + depthOffset);
        }

        public Vector3 WaitingSlotCenter(int slot) => new Vector3(WaitingX(slot), 0f, WaitingZ);

        public Vector3 WaitingGate(int slot) => new Vector3(WaitingX(slot), 0f, RingMin.y);

        public Vector3 ExitPoint(CarDef car)
        {
            Vector3 center = CarCenter(car);
            switch (car.direction)
            {
                case Direction.Up: return new Vector3(center.x, 0f, RingMax.y);
                case Direction.Down: return new Vector3(center.x, 0f, RingMin.y);
                case Direction.Right: return new Vector3(RingMax.x, 0f, center.z);
                default: return new Vector3(RingMin.x, 0f, center.z);
            }
        }

        public bool PointToCell(Vector3 world, out int x, out int y)
        {
            x = Mathf.FloorToInt(world.x - LotMin.x);
            y = Mathf.FloorToInt(world.z - LotMin.y);
            return x >= 0 && y >= 0 && x < _width && y < _height;
        }

        private float Perimeter => 2f * ((RingMax.x - RingMin.x) + (RingMax.y - RingMin.y));

        private float RingParameter(Vector3 p)
        {
            float w = RingMax.x - RingMin.x, h = RingMax.y - RingMin.y;
            const float epsilon = 0.01f;
            if (Mathf.Abs(p.z - RingMin.y) < epsilon) return Mathf.Clamp(p.x - RingMin.x, 0f, w);
            if (Mathf.Abs(p.x - RingMax.x) < epsilon) return w + Mathf.Clamp(p.z - RingMin.y, 0f, h);
            if (Mathf.Abs(p.z - RingMax.y) < epsilon) return w + h + Mathf.Clamp(RingMax.x - p.x, 0f, w);
            return 2f * w + h + Mathf.Clamp(RingMax.y - p.z, 0f, h);
        }

        private Vector3 Corner(int index)
        {
            switch (((index % 4) + 4) % 4)
            {
                case 0: return new Vector3(RingMin.x, 0f, RingMin.y);
                case 1: return new Vector3(RingMax.x, 0f, RingMin.y);
                case 2: return new Vector3(RingMax.x, 0f, RingMax.y);
                default: return new Vector3(RingMin.x, 0f, RingMax.y);
            }
        }

        private float CornerParameter(int index)
        {
            float w = RingMax.x - RingMin.x, h = RingMax.y - RingMin.y;
            switch (((index % 4) + 4) % 4)
            {
                case 0: return 0f;
                case 1: return w;
                case 2: return w + h;
                default: return 2f * w + h;
            }
        }

        public void AppendRingRoute(CarPath path, Vector3 from, Vector3 to)
        {
            float total = Perimeter;
            float a = RingParameter(from), b = RingParameter(to);
            float forward = (b - a + total) % total;

            if (forward <= total * 0.5f)
            {
                for (int i = 0; i < 4; i++)
                {
                    float corner = (CornerParameter(i) - a + total) % total;
                    if (corner > 0.001f && corner < forward - 0.001f) path.AddOrdered(Corner(i), corner);
                }
            }
            else
            {
                float backward = total - forward;
                for (int i = 0; i < 4; i++)
                {
                    float corner = (a - CornerParameter(i) + total) % total;
                    if (corner > 0.001f && corner < backward - 0.001f) path.AddOrdered(Corner(i), corner);
                }
            }

            path.FlushOrdered();
            path.Add(to);
        }
    }
}
