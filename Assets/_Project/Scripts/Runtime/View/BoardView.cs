using System;
using System.Collections.Generic;
using DG.Tweening;
using ParkSort.Audio;
using ParkSort.Core;
using ParkSort.Data;
using TMPro;
using UnityEngine;

namespace ParkSort.View
{
    public sealed class BoardView : MonoBehaviour
    {
        private const int WaitingSlotPool = 7;
        private const int ObstaclePool = 10;
        private const int TunnelPool = 4;
        private const float CloseTime = 0.38f;
        private const float OpenTime = 0.32f;

        [SerializeField] private GameTheme theme;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private MeshRenderer ground;
        [SerializeField] private ParticleSystem dust;
        [SerializeField] private ParticleSystem confetti;
        [SerializeField] private Transform[] decor = new Transform[0];

        [Header("Motion")]
        [SerializeField] private float driveSpeed = 12f;
        [SerializeField] private float acceleration = 40f;
        [SerializeField] private float turnSharpness = 16f;

        private static readonly int LotRectId = Shader.PropertyToID("_LotRect");
        private static readonly int RoadWidthId = Shader.PropertyToID("_RoadWidth");
        private static readonly int LotColorId = Shader.PropertyToID("_LotColor");
        private static readonly int RoadColorId = Shader.PropertyToID("_RoadColor");
        private static readonly int LineColorId = Shader.PropertyToID("_LineColor");
        private static readonly int GrassColorId = Shader.PropertyToID("_GrassColor");
        private static readonly int GrassColor2Id = Shader.PropertyToID("_GrassColor2");
        private static readonly int CurbColorId = Shader.PropertyToID("_CurbColor");
        private static readonly int LightColorId = Shader.PropertyToID("_PS_LightColor");
        private static readonly int AmbientId = Shader.PropertyToID("_PS_Ambient");

        private readonly BoardLayout _layout = new BoardLayout();
        private readonly List<CarView> _pool = new List<CarView>(LevelDef.MaxCars);
        private readonly List<CarView> _active = new List<CarView>(LevelDef.MaxCars);
        private readonly GarageLane[] _lanes = new GarageLane[2];
        private readonly Transform[] _waitingSlots = new Transform[WaitingSlotPool];
        private readonly Transform[] _obstacles = new Transform[ObstaclePool];
        private readonly Transform[] _tunnels = new Transform[TunnelPool];
        private readonly Transform[] _tunnelBodies = new Transform[TunnelPool];
        private readonly MeshRenderer[] _tunnelLights = new MeshRenderer[TunnelPool];
        private readonly TextMeshPro[] _tunnelLabels = new TextMeshPro[TunnelPool];
        private readonly int[] _tunnelShown = new int[TunnelPool];
        private readonly Vector3[] _decorSpots = new Vector3[6];
        private CarView[] _byIndex = new CarView[0];
        private Material _groundMaterial;
        private LevelDef _level;
        private ParkingGame _game;
        private int _waitingVisible;
        private int _hintCar = -1;
        private Transform _hintHandle;
        private bool _completedRaised;
        private bool _wonPending;

        public BoardLayout Layout => _layout;
        public event Action Completed;
        public event Action Failed;
        public event Action<int> GarageFinished;

        public bool IsBusy
        {
            get
            {
                for (int i = 0; i < _active.Count; i++)
                {
                    CarState state = _active[i].State;
                    if (state == CarState.Driving || state == CarState.AtGate) return true;
                    if (state == CarState.InWaiting && _active[i].Next.kind != MissionKind.None) return true;
                }

                return _lanes[0].Busy || _lanes[1].Busy;
            }
        }

        private void Awake()
        {
            _groundMaterial = ground.material;

            for (int i = 0; i < LevelDef.MaxCars; i++)
                _pool.Add(new CarView(transform, theme.shadowMaterial, theme.quad, theme.question, theme.neutralMaterial));

            for (int lane = 0; lane < _lanes.Length; lane++)
            {
                MeshRenderer renderer = CreateRenderer("Garage" + lane, theme.garage, theme.neutralMaterial);
                _lanes[lane] = new GarageLane(renderer.transform, renderer, lane);
                renderer.gameObject.SetActive(false);
            }

            for (int i = 0; i < _waitingSlots.Length; i++)
            {
                _waitingSlots[i] = CreateRenderer("WaitingSlot" + i, theme.waitingSlot, theme.neutralMaterial).transform;
                _waitingSlots[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < _obstacles.Length; i++)
            {
                Mesh mesh = theme.obstacles[i % theme.obstacles.Length];
                _obstacles[i] = CreateRenderer("Obstacle" + i, mesh, theme.neutralMaterial).transform;
                _obstacles[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < _tunnels.Length; i++)
            {
                var root = new GameObject("Tunnel" + i);
                root.transform.SetParent(transform, false);
                _tunnels[i] = root.transform;

                var body = new GameObject("Body");
                body.transform.SetParent(root.transform, false);
                _tunnelBodies[i] = body.transform;

                MeshRenderer baseRenderer = CreateRenderer("Base", theme.tunnelBase, theme.neutralMaterial);
                baseRenderer.transform.SetParent(body.transform, false);
                _tunnelLights[i] = CreateRenderer("Light", theme.tunnelLight, theme.hiddenMaterial);
                _tunnelLights[i].transform.SetParent(body.transform, false);

                var labelObject = new GameObject("Count");
                labelObject.transform.SetParent(root.transform, false);
                var label = labelObject.AddComponent<TextMeshPro>();
                label.font = theme.font;
                if (theme.fontMaterial != null) label.fontSharedMaterial = theme.fontMaterial;
                label.fontSize = 7f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.richText = false;
                label.rectTransform.sizeDelta = new Vector2(2f, 1f);
                _tunnelLabels[i] = label;

                root.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_groundMaterial != null) Destroy(_groundMaterial);
        }

        private MeshRenderer CreateRenderer(string objectName, Mesh mesh, Material material)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return renderer;
        }

        public void ApplySkin(Skin skin)
        {
            _groundMaterial.SetColor(LotColorId, skin.lot);
            _groundMaterial.SetColor(RoadColorId, skin.road);
            _groundMaterial.SetColor(LineColorId, skin.line);
            _groundMaterial.SetColor(GrassColorId, skin.grass);
            _groundMaterial.SetColor(GrassColor2Id, skin.grassAlternate);
            _groundMaterial.SetColor(CurbColorId, skin.curb);

            Color light = skin.light.linear;
            Color ambient = skin.ambient.linear;
            Shader.SetGlobalVector(LightColorId, new Vector4(light.r, light.g, light.b, 1f));
            Shader.SetGlobalVector(AmbientId, new Vector4(ambient.r, ambient.g, ambient.b, 1f));
            if (cameraRig != null) cameraRig.Camera.backgroundColor = skin.grass;
        }

        public void Build(ParkingGame game)
        {
            _game = game;
            _level = game.Level;
            _layout.Configure(_level);
            _completedRaised = false;
            _wonPending = false;
            ClearHint();
            _hintCar = -1;
            _hintHandle = null;

            _groundMaterial.SetVector(LotRectId, new Vector4(_layout.LotMin.x, _layout.LotMin.y, _layout.LotMax.x, _layout.LotMax.y));
            _groundMaterial.SetFloat(RoadWidthId, BoardLayout.RoadWidth);

            for (int i = 0; i < _active.Count; i++) Release(_active[i]);
            _active.Clear();

            if (_byIndex.Length != _level.cars.Length) _byIndex = new CarView[_level.cars.Length];
            for (int i = 0; i < _level.cars.Length; i++)
            {
                CarView car = _pool[i];
                CarDef def = _level.cars[i];
                car.Index = i;
                car.Def = def;
                car.VisualLength = def.InTunnel ? 2 : def.length;
                car.Current = default;
                car.Next = default;
                car.WaitingSlot = -1;
                car.Speed = 0f;
                car.Filter.sharedMesh = car.VisualLength >= 3 ? theme.van : theme.car;
                car.Shadow.localScale = new Vector3(1.05f, 1f, car.VisualLength * 0.98f + 0.1f);
                car.Transform.DOKill();
                car.Body.DOKill();
                car.Transform.SetPositionAndRotation(_layout.CarCenter(def), BoardLayout.Rotation(def.direction));
                car.Transform.localScale = Vector3.one;
                car.Body.localPosition = Vector3.zero;
                car.Body.localScale = Vector3.one;
                car.Question.localScale = Vector3.one * 1.45f;
                car.Question.rotation = Quaternion.Euler(58f, 0f, 0f);
                car.QuestionHeight = def.length >= 3 ? 0.98f : 0.88f;
                car.Question.position = car.Transform.position + Vector3.up * car.QuestionHeight;

                bool inLot = game.IsPresent(i) && !def.InTunnel;
                SetHidden(car, inLot && !game.IsRevealed(i));
                car.State = inLot ? CarState.Parked : game.IsPresent(i) ? CarState.InTunnel : CarState.Pooled;
                car.Root.SetActive(inLot);
                _active.Add(car);
                _byIndex[i] = car;
            }

            for (int lane = 0; lane < _lanes.Length; lane++)
            {
                GarageLane garage = _lanes[lane];
                garage.Operations.Clear();
                garage.Parked.Clear();
                garage.Arrived = 0;
                garage.Transform.DOKill();
                garage.Transform.position = _layout.GarageCenter(lane);
                garage.Transform.localScale = Vector3.one;

                int queueIndex = lane < game.LaneCount ? game.LaneQueueIndex(lane) : -1;
                if (queueIndex >= 0)
                {
                    garage.QueueIndex = queueIndex;
                    garage.Color = _level.garageQueue[queueIndex];
                    garage.Renderer.sharedMaterial = theme.paintMaterials[garage.Color];
                    garage.State = GarageLane.Phase.Open;
                    garage.Transform.gameObject.SetActive(true);

                    for (int slot = 0; slot < game.LaneFill(lane); slot++)
                    {
                        CarView parked = _byIndex[game.LaneCar(lane, slot)];
                        Stand(parked, _layout.GarageSlot(lane, slot, parked.VisualLength), Vector3.forward, CarState.InGarage);
                        garage.Parked.Add(parked);
                        garage.Arrived++;
                    }
                }
                else
                {
                    garage.QueueIndex = -1;
                    garage.State = GarageLane.Phase.Hidden;
                    garage.Transform.gameObject.SetActive(false);
                }
            }

            _waitingVisible = game.WaitingCapacity;
            for (int i = 0; i < _waitingSlots.Length; i++)
            {
                _waitingSlots[i].DOKill();
                _waitingSlots[i].localScale = Vector3.one;
                _waitingSlots[i].position = _layout.WaitingSlotCenter(i);
                _waitingSlots[i].gameObject.SetActive(i < _waitingVisible);

                int waitingCar = i < _waitingVisible ? game.WaitingCar(i) : ParkingGame.NoCar;
                if (waitingCar == ParkingGame.NoCar) continue;
                CarView waiting = _byIndex[waitingCar];
                Stand(waiting, _layout.WaitingSlot(i, waiting.VisualLength), Vector3.back, CarState.InWaiting);
                waiting.WaitingSlot = i;
            }

            if (game.OverflowCar != ParkingGame.NoCar)
                Stand(_byIndex[game.OverflowCar], _layout.WaitingGate(game.WaitingCapacity), Vector3.back, CarState.Overflow);

            for (int i = 0; i < _tunnels.Length; i++)
            {
                bool used = i < _level.tunnels.Length;
                _tunnels[i].gameObject.SetActive(used);
                if (!used) continue;

                TunnelDef tunnel = _level.tunnels[i];
                Vector3 position = _layout.CellCenter(tunnel.x, tunnel.y);
                _tunnels[i].position = position;
                _tunnelBodies[i].DOKill();
                _tunnelBodies[i].localPosition = Vector3.zero;
                _tunnelBodies[i].localScale = Vector3.one;
                _tunnelBodies[i].localRotation = BoardLayout.Rotation(tunnel.direction);
                _tunnelLabels[i].transform.SetPositionAndRotation(position + new Vector3(0f, 1.25f, -0.15f), Quaternion.Euler(58f, 0f, 0f));
                _tunnelShown[i] = -1;
            }

            RefreshTunnels();

            for (int i = 0; i < _obstacles.Length; i++)
            {
                bool used = i < _level.blockedCells.Length;
                _obstacles[i].gameObject.SetActive(used);
                if (!used) continue;
                int cell = _level.blockedCells[i];
                _obstacles[i].position = _layout.CellCenter(cell % _level.width, cell / _level.width);
                _obstacles[i].rotation = Quaternion.Euler(0f, (cell * 37) % 4 * 90f, 0f);
                _obstacles[i].localScale = Vector3.one * (i % 2 == 1 ? 1.55f : 1f);
            }

            PlaceDecor();
            if (cameraRig != null) cameraRig.Fit(_layout.Bounds);
        }

        private void Stand(CarView car, Vector3 position, Vector3 forward, CarState state)
        {
            car.Transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
            car.State = state;
            SetHidden(car, false);
            car.Root.SetActive(true);
        }

        private void RefreshTunnels()
        {
            for (int i = 0; i < _level.tunnels.Length && i < _tunnels.Length; i++)
            {
                int front = _game.TunnelFront(i);
                int count = _game.TunnelCount(i);
                _tunnelLights[i].sharedMaterial = front == ParkingGame.NoCar ? theme.hiddenMaterial : theme.paintMaterials[_level.cars[front].color];
                if (_tunnelShown[i] == count) continue;

                _tunnelShown[i] = count;
                _tunnelLabels[i].gameObject.SetActive(count > 0);
                if (count > 0) _tunnelLabels[i].SetText("{0}", (float)count);
            }
        }

        private Transform Handle(int car)
        {
            CarView view = _byIndex[car];
            return view.State == CarState.InTunnel ? _tunnelBodies[view.Def.tunnel - 1] : view.Body;
        }

        private void PlaceDecor()
        {
            float half = _layout.Bounds.extents.x;
            float left = -half + 0.45f, right = half - 0.45f;
            float waiting = _layout.WaitingZ;
            float gap = _layout.RingMax.y + 0.95f;
            _decorSpots[0] = new Vector3(left, 0f, waiting + 0.9f);
            _decorSpots[1] = new Vector3(right, 0f, waiting + 0.9f);
            _decorSpots[2] = new Vector3(left + 0.1f, 0f, waiting - 0.7f);
            _decorSpots[3] = new Vector3(right - 0.1f, 0f, waiting - 0.7f);
            _decorSpots[4] = new Vector3(left - 0.15f, 0f, gap);
            _decorSpots[5] = new Vector3(right + 0.15f, 0f, gap);

            for (int i = 0; i < decor.Length; i++)
            {
                bool used = i < _decorSpots.Length;
                decor[i].gameObject.SetActive(used);
                if (used) decor[i].position = _decorSpots[i];
            }
        }

        private void Release(CarView car)
        {
            car.Transform.DOKill();
            car.Body.DOKill();
            car.Root.SetActive(false);
            car.State = CarState.Pooled;
        }

        private void SetHidden(CarView car, bool hidden)
        {
            car.Hidden = hidden;
            car.Renderer.sharedMaterial = hidden ? theme.hiddenMaterial : theme.paintMaterials[car.Def.color];
            car.Question.gameObject.SetActive(hidden);
        }

        public int PickCar(Ray ray)
        {
            if (_game == null) return ParkingGame.NoCar;

            int best = ParkingGame.NoCar;
            for (float height = 0.65f; height >= 0.05f && best == ParkingGame.NoCar; height -= 0.3f)
            {
                if (Mathf.Abs(ray.direction.y) < 0.0001f) break;
                float t = (height - ray.origin.y) / ray.direction.y;
                if (t <= 0f) continue;
                if (_layout.PointToCell(ray.origin + ray.direction * t, out int x, out int y)) best = _game.CarAt(x, y);
            }

            return best;
        }

        public Vector3 CarPosition(int car)
        {
            CarView view = _byIndex[car];
            return view.State == CarState.InTunnel ? _tunnels[view.Def.tunnel - 1].position : view.Transform.position;
        }

        public void ShowHint(int car)
        {
            ClearHint();
            if (car < 0 || car >= _byIndex.Length) return;
            _hintCar = car;
            _hintHandle = Handle(car);
            _hintHandle.DOKill();
            _hintHandle.localScale = Vector3.one;
            _hintHandle.DOScale(1.14f, 0.36f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            _hintHandle.DOLocalMoveY(0.16f, 0.36f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        public void ClearHint()
        {
            if (_hintCar < 0 || _hintHandle == null) return;
            _hintHandle.DOKill();
            _hintHandle.localScale = Vector3.one;
            _hintHandle.localPosition = Vector3.zero;
            _hintHandle = null;
            _hintCar = -1;
        }

        public void Bump(int car)
        {
            CarView view = _byIndex[car];
            if (car == _hintCar) ClearHint();

            if (view.State == CarState.InTunnel)
            {
                Transform tunnel = _tunnelBodies[view.Def.tunnel - 1];
                tunnel.DOKill(true);
                tunnel.DOPunchScale(new Vector3(0.12f, -0.1f, 0.12f), 0.25f, 5, 0.6f);
                AudioManager.Play(Sfx.Bump);
                return;
            }

            if (view.State != CarState.Parked) return;

            float free = Mathf.Min(_game.FreeCellsAhead(car), 1.5f);
            Vector3 push = Vector3.forward * (free + 0.22f);
            view.Body.DOKill();
            view.Body.localPosition = Vector3.zero;
            view.Body.localScale = Vector3.one;
            DOTween.Sequence().SetTarget(view.Body)
                .Append(view.Body.DOLocalMove(push, 0.08f + free * 0.04f).SetEase(Ease.OutQuad))
                .Append(view.Body.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutBack));

            int blocker = _game.BlockingCar(car);
            if (blocker != ParkingGame.NoCar && _byIndex[blocker].State == CarState.Parked)
            {
                Transform other = _byIndex[blocker].Body;
                other.DOKill();
                other.localPosition = Vector3.zero;
                other.localScale = Vector3.one;
                other.DOShakeRotation(0.3f, new Vector3(0f, 7f, 5f), 14, 60f).SetDelay(0.08f + free * 0.04f)
                    .OnKill(() => other.localRotation = Quaternion.identity);
            }

            AudioManager.Play(Sfx.Bump);
        }

        public void Play(MoveResult result)
        {
            for (int i = 0; i < result.Steps.Count; i++)
            {
                Step step = result.Steps[i];
                switch (step.kind)
                {
                    case StepKind.CarToGarage:
                        BeginExit(_byIndex[step.car], new Mission { kind = MissionKind.Garage, lane = step.lane, queueIndex = step.queueIndex, slot = step.slot });
                        break;

                    case StepKind.CarToWaiting:
                        CarView waiting = _byIndex[step.car];
                        var mission = new Mission { kind = MissionKind.Waiting, slot = step.slot };
                        if (waiting.State == CarState.Overflow) BeginFromOverflow(waiting, mission);
                        else BeginExit(waiting, mission);
                        break;

                    case StepKind.CarOverflow:
                        BeginExit(_byIndex[step.car], new Mission { kind = MissionKind.Overflow, slot = step.slot });
                        break;

                    case StepKind.GarageClosed:
                        _lanes[step.lane].Operations.Enqueue(new GarageLane.Operation { open = false, queueIndex = step.queueIndex, color = step.color });
                        break;

                    case StepKind.GarageOpened:
                        _lanes[step.lane].Operations.Enqueue(new GarageLane.Operation { open = true, queueIndex = step.queueIndex, color = step.color });
                        break;

                    case StepKind.WaitingToGarage:
                        _byIndex[step.car].Next = new Mission { kind = MissionKind.Garage, lane = step.lane, queueIndex = step.queueIndex, slot = step.slot };
                        break;

                    case StepKind.CarRevealed:
                        Reveal(_byIndex[step.car]);
                        break;
                }
            }

            RefreshTunnels();
            if (result.Status == GameStatus.Won) _wonPending = true;
        }

        public void ShowExtraWaitingSlot()
        {
            if (_waitingVisible >= _waitingSlots.Length) return;
            Transform slot = _waitingSlots[_waitingVisible++];
            slot.gameObject.SetActive(true);
            slot.localScale = Vector3.zero;
            slot.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
            Emit(confetti, slot.position + Vector3.up * 0.3f, Color.white, 14);
        }

        private void Reveal(CarView car)
        {
            if (!car.Hidden) return;
            SetHidden(car, false);
            car.Body.DOKill();
            car.Body.localScale = Vector3.one;
            car.Body.DOPunchScale(Vector3.one * 0.22f, 0.3f, 5, 0.6f);
            Emit(confetti, car.Transform.position + Vector3.up * 0.7f, theme.colors[car.Def.color], 10);
            AudioManager.Play(Sfx.Reveal);
        }

        private void BeginExit(CarView car, Mission mission)
        {
            if (car.Index == _hintCar) ClearHint();
            if (car.Hidden) SetHidden(car, false);

            car.Transform.DOKill();
            if (car.State == CarState.InTunnel)
            {
                TunnelDef tunnel = _level.tunnels[car.Def.tunnel - 1];
                car.Transform.SetPositionAndRotation(_layout.CellCenter(tunnel.x, tunnel.y), BoardLayout.Rotation(tunnel.direction));
                car.Transform.localScale = Vector3.one * 0.45f;
                car.Transform.DOScale(1f, 0.28f).SetEase(Ease.OutBack);
                car.Renderer.sharedMaterial = theme.paintMaterials[car.Def.color];
                car.Root.SetActive(true);

                Transform body = _tunnelBodies[car.Def.tunnel - 1];
                body.DOKill(true);
                body.DOPunchScale(new Vector3(0.14f, 0.1f, 0.14f), 0.25f, 5, 0.6f);
            }

            car.Body.DOKill();
            car.Body.localPosition = Vector3.zero;
            car.Body.localRotation = Quaternion.identity;
            car.Body.localScale = Vector3.one;
            car.Body.DOPunchScale(new Vector3(-0.08f, 0.06f, 0.14f), 0.25f, 4, 0.5f);

            car.Current = mission;
            car.Path.Clear();
            car.Path.Add(car.Transform.position);
            Vector3 exit = _layout.ExitPoint(car.Def);
            car.Path.Add(exit);

            if (mission.kind == MissionKind.Garage)
            {
                _layout.AppendRingRoute(car.Path, exit, _layout.GarageGate(mission.lane, mission.slot));
            }
            else
            {
                _layout.AppendRingRoute(car.Path, exit, _layout.WaitingGate(mission.slot));
                if (mission.kind == MissionKind.Waiting) car.Path.Add(_layout.WaitingSlot(mission.slot, car.VisualLength));
            }

            StartDriving(car, 0f, false);
            Emit(dust, car.Transform.position - car.Transform.forward * car.HalfLength + Vector3.up * 0.12f, new Color(1f, 1f, 1f, 0.8f), 5);
            AudioManager.Play(Sfx.Drive);
        }

        private void BeginFromOverflow(CarView car, Mission mission)
        {
            car.Current = mission;
            car.Body.DOKill();
            car.Body.localRotation = Quaternion.identity;
            car.Path.Clear();
            car.Path.Add(car.Transform.position);
            car.Path.Add(_layout.WaitingGate(mission.slot));
            car.Path.Add(_layout.WaitingSlot(mission.slot, car.VisualLength));
            StartDriving(car, 0f, false);
        }

        private void BeginGarageLeg(CarView car)
        {
            Mission mission = car.Current;
            car.Path.Clear();
            car.Path.Add(car.Transform.position);
            car.Path.Add(_layout.GarageSlot(mission.lane, mission.slot, car.VisualLength));
            StartDriving(car, car.Speed, true);
        }

        private void BeginFromWaiting(CarView car)
        {
            car.Current = car.Next;
            car.Next = default;
            car.WaitingSlot = -1;

            var gate = new Vector3(car.Transform.position.x, 0f, _layout.RingMin.y);
            car.Path.Clear();
            car.Path.Add(car.Transform.position);
            car.Path.Add(gate);
            float reverse = Vector3.Distance(car.Transform.position, gate);
            _layout.AppendRingRoute(car.Path, gate, _layout.GarageGate(car.Current.lane, car.Current.slot));
            StartDriving(car, 0f, false);
            car.ReverseUntil = reverse;
            AudioManager.Play(Sfx.Drive);
        }

        private void StartDriving(CarView car, float speed, bool finalLeg)
        {
            car.Path.Build();
            car.Distance = 0f;
            car.Speed = speed;
            car.ReverseUntil = 0f;
            car.FinalLeg = finalLeg;
            car.State = CarState.Driving;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _active.Count; i++)
            {
                CarView car = _active[i];
                switch (car.State)
                {
                    case CarState.Driving:
                        Drive(car, dt);
                        break;

                    case CarState.AtGate:
                        if (_lanes[car.Current.lane].IsOpenFor(car.Current.queueIndex)) BeginGarageLeg(car);
                        break;

                    case CarState.InWaiting:
                        if (car.Next.kind == MissionKind.Garage && _lanes[car.Next.lane].IsOpenFor(car.Next.queueIndex)) BeginFromWaiting(car);
                        break;
                }

                if (car.Hidden)
                {
                    float bob = Mathf.Sin(Time.time * 3.2f + car.Index) * 0.07f;
                    car.Question.position = car.Transform.position + Vector3.up * (car.QuestionHeight + bob);
                }
            }

            for (int lane = 0; lane < _lanes.Length; lane++) UpdateLane(_lanes[lane], dt);

            if (_wonPending && !_completedRaised && !IsBusy)
            {
                _completedRaised = true;
                Completed?.Invoke();
            }
        }

        private void Drive(CarView car, float dt)
        {
            car.Speed = Mathf.MoveTowards(car.Speed, driveSpeed, acceleration * dt);
            car.Distance += car.Speed * dt;
            car.Path.Evaluate(car.Distance, out Vector3 position, out Vector3 forward);
            if (car.Distance < car.ReverseUntil) forward = -forward;

            Quaternion target = Quaternion.LookRotation(forward, Vector3.up);
            Quaternion rotation = Quaternion.Slerp(car.Transform.rotation, target, 1f - Mathf.Exp(-turnSharpness * dt));
            car.Transform.SetPositionAndRotation(position, rotation);

            if (car.Distance < car.Path.Length) return;

            car.Transform.rotation = target;
            Arrive(car);
        }

        private void Arrive(CarView car)
        {
            switch (car.Current.kind)
            {
                case MissionKind.Garage:
                    GarageLane lane = _lanes[car.Current.lane];
                    if (!car.FinalLeg)
                    {
                        if (lane.IsOpenFor(car.Current.queueIndex)) BeginGarageLeg(car);
                        else
                        {
                            car.State = CarState.AtGate;
                            car.Speed = 0f;
                        }

                        return;
                    }

                    car.State = CarState.InGarage;
                    car.Speed = 0f;
                    lane.Parked.Add(car);
                    lane.Arrived++;
                    Punch(car);
                    AudioManager.Play(Sfx.Park);
                    break;

                case MissionKind.Waiting:
                    car.State = CarState.InWaiting;
                    car.Speed = 0f;
                    car.WaitingSlot = car.Current.slot;
                    Punch(car);
                    AudioManager.Play(Sfx.Wait);
                    break;

                case MissionKind.Overflow:
                    car.State = CarState.Overflow;
                    car.Speed = 0f;
                    car.Body.DOKill();
                    car.Body.DOShakeRotation(0.5f, new Vector3(0f, 10f, 6f), 16, 70f);
                    Failed?.Invoke();
                    break;
            }
        }

        private static void Punch(CarView car)
        {
            car.Body.DOKill();
            car.Body.localScale = Vector3.one;
            car.Body.localPosition = Vector3.zero;
            car.Body.DOPunchScale(new Vector3(0.1f, 0.14f, -0.08f), 0.24f, 4, 0.5f);
        }

        private void UpdateLane(GarageLane lane, float dt)
        {
            switch (lane.State)
            {
                case GarageLane.Phase.Open:
                case GarageLane.Phase.Hidden:
                    if (lane.Operations.Count == 0) return;
                    GarageLane.Operation operation = lane.Operations.Peek();
                    if (!operation.open)
                    {
                        if (lane.State != GarageLane.Phase.Open || lane.Arrived < _level.garageCapacity) return;
                        lane.Operations.Dequeue();
                        lane.State = GarageLane.Phase.Closing;
                        lane.Timer = 0f;
                        Emit(confetti, lane.Transform.position + Vector3.up * 0.9f, theme.colors[lane.Color], 36);
                        AudioManager.Play(Sfx.GarageDone);
                        GarageFinished?.Invoke(lane.Color);
                    }
                    else
                    {
                        lane.Operations.Dequeue();
                        lane.QueueIndex = operation.queueIndex;
                        lane.Color = operation.color;
                        lane.Arrived = 0;
                        lane.Renderer.sharedMaterial = theme.paintMaterials[operation.color];
                        lane.Transform.localScale = Vector3.zero;
                        lane.Transform.gameObject.SetActive(true);
                        lane.State = GarageLane.Phase.Opening;
                        lane.Timer = 0f;
                        AudioManager.Play(Sfx.GarageOpen);
                    }

                    break;

                case GarageLane.Phase.Closing:
                    lane.Timer += dt;
                    float closing = Mathf.Clamp01(lane.Timer / CloseTime);
                    float shrink = 1f - EaseInBack(closing);
                    lane.Transform.localScale = Vector3.one * Mathf.Max(0f, shrink);
                    for (int i = 0; i < lane.Parked.Count; i++)
                    {
                        Transform car = lane.Parked[i].Transform;
                        car.localScale = Vector3.one * Mathf.Max(0f, shrink);
                        car.position += Vector3.up * (dt * 2.2f * closing);
                    }

                    if (closing < 1f) return;

                    for (int i = 0; i < lane.Parked.Count; i++) Release(lane.Parked[i]);
                    lane.Parked.Clear();
                    lane.Transform.gameObject.SetActive(false);
                    lane.QueueIndex = -1;
                    lane.State = GarageLane.Phase.Hidden;
                    break;

                case GarageLane.Phase.Opening:
                    lane.Timer += dt;
                    float opening = Mathf.Clamp01(lane.Timer / OpenTime);
                    lane.Transform.localScale = Vector3.one * EaseOutBack(opening);
                    if (opening >= 1f) lane.State = GarageLane.Phase.Open;
                    break;
            }
        }

        private static float EaseInBack(float t)
        {
            const float s = 1.70158f;
            return t * t * ((s + 1f) * t - s);
        }

        private static float EaseOutBack(float t)
        {
            const float s = 1.70158f;
            t -= 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }

        public void Celebrate()
        {
            Vector3 center = _layout.Bounds.center;
            for (int i = 0; i < theme.colors.Length; i++)
            {
                Vector3 offset = new Vector3(Mathf.Sin(i * 2.4f) * 2.6f, 1.2f, Mathf.Cos(i * 1.7f) * 3.4f);
                Emit(confetti, center + offset, theme.colors[i], 16);
            }
        }

        private static void Emit(ParticleSystem system, Vector3 position, Color color, int count)
        {
            if (system == null) return;
            var parameters = new ParticleSystem.EmitParams
            {
                position = position,
                startColor = color,
                applyShapeToPosition = true,
            };
            system.Emit(parameters, count);
        }
    }
}
