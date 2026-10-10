using System.Collections.Generic;
using ParkSort.Audio;
using ParkSort.Data;
using ParkSort.Game;
using ParkSort.UI;
using ParkSort.View;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ParkSort.EditorTools
{
    public static partial class SceneBuilder
    {
        private const string ScenePath = Root + "/Scenes/Main.unity";
        private const string ModelFolder = Root + "/Art/Models";

        private static readonly Color32[] Palette =
        {
            new Color32(240, 78, 82, 255),
            new Color32(62, 136, 246, 255),
            new Color32(255, 198, 44, 255),
            new Color32(66, 196, 110, 255),
            new Color32(158, 104, 236, 255),
            new Color32(255, 138, 54, 255),
            new Color32(255, 118, 182, 255),
            new Color32(46, 198, 204, 255),
        };

        private static readonly Color Ink = new Color32(44, 52, 86, 255);
        private static readonly Color Panel = new Color32(250, 250, 255, 255);
        private static readonly Color Accent = new Color32(62, 196, 110, 255);
        private static readonly Color Blue = new Color32(62, 136, 246, 255);
        private static readonly Color Warm = new Color32(255, 158, 44, 255);
        private static readonly Color Red = new Color32(240, 88, 92, 255);

        [MenuItem("ParkSort/Setup/Build Everything")]
        public static void BuildEverything()
        {
            ApplyProjectSettings();
            BuildMaterials();
            GameTheme theme = BuildTheme();
            BuildShopCatalog();
            LevelCatalog catalog = LoadOrCreate<LevelCatalog>(DataFolder + "/LevelCatalog.asset");
            Debug.Log(LevelBaker.Bake(catalog));
            AssetDatabase.SaveAssets();
            BuildScene();
            AssetDatabase.SaveAssets();
            Debug.Log("ParkSort: setup finished.");
        }

        private static Mesh Model(string name)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelFolder}/{name}.lpmesh");
            if (mesh == null) Debug.LogError($"Model missing: {name} (run Tools/blender/build_all.py)");
            return mesh;
        }

        private static Mesh QuadMesh()
        {
            EnsureFolder(Root + "/Art/Meshes");
            string path = Root + "/Art/Meshes/Quad.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = "Quad" };
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.Clear();
            mesh.SetVertices(new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f),
            });
            mesh.SetNormals(new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            mesh.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static void BuildMaterials()
        {
            for (int i = 0; i < Palette.Length; i++)
            {
                Material paint = Material("Paint_" + i, "ParkSort/Toon");
                paint.SetColor("_Tint", Palette[i]);
                paint.SetFloat("_Shine", 0.4f);
                paint.SetFloat("_Desaturate", 0f);
            }

            Material hidden = Material("Paint_Hidden", "ParkSort/Toon");
            hidden.SetColor("_Tint", new Color32(150, 156, 176, 255));
            hidden.SetFloat("_Shine", 0.1f);
            hidden.SetFloat("_Desaturate", 0.85f);

            Material neutral = Material("Neutral", "ParkSort/Toon");
            neutral.SetColor("_Tint", Color.white);
            neutral.SetFloat("_Shine", 0.15f);
            neutral.SetFloat("_Desaturate", 0f);

            Material(MaterialName.Ground, "ParkSort/Ground");

            Material shadow = Material("BlobShadow", "ParkSort/Blob");
            shadow.SetFloat("_SrcBlend", (float)BlendMode.One);
            shadow.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            shadow.SetFloat("_Power", 0.9f);
            shadow.SetColor("_Color", new Color(0.05f, 0.08f, 0.2f, 0.42f));
            shadow.renderQueue = 2900;

            Material confetti = Material("ParticleConfetti", "ParkSort/Particle");
            confetti.SetFloat("_SrcBlend", (float)BlendMode.One);
            confetti.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            confetti.SetFloat("_Soft", 0f);
            confetti.SetFloat("_Boost", 1f);

            Material dust = Material("ParticleDust", "ParkSort/Particle");
            dust.SetFloat("_SrcBlend", (float)BlendMode.One);
            dust.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            dust.SetFloat("_Soft", 1f);
            dust.SetFloat("_Boost", 1f);
        }

        private static class MaterialName
        {
            public const string Ground = "Ground";
        }

        private static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");

        private static GameTheme BuildTheme()
        {
            var theme = LoadOrCreate<GameTheme>(DataFolder + "/GameTheme.asset");
            theme.colors = new Color[Palette.Length];
            theme.paintMaterials = new Material[Palette.Length];
            for (int i = 0; i < Palette.Length; i++)
            {
                theme.colors[i] = Palette[i];
                theme.paintMaterials[i] = Mat("Paint_" + i);
            }

            theme.hiddenMaterial = Mat("Paint_Hidden");
            theme.neutralMaterial = Mat("Neutral");
            theme.shadowMaterial = Mat("BlobShadow");
            theme.car = Model("Car");
            theme.van = Model("Van");
            theme.question = Model("Question");
            theme.garage = Model("Garage");
            theme.waitingSlot = Model("WaitingSlot");
            theme.tunnelBase = Model("TunnelBase");
            theme.tunnelLight = Model("TunnelLight");
            SetupFont();
            theme.font = _font;
            theme.fontMaterial = _fontOutline;
            theme.quad = QuadMesh();
            theme.obstacles = new[] { Model("Planter"), Model("Cone") };
            theme.decor = new[] { Model("Tree"), Model("Bush"), Model("Lamp"), Model("Booth") };
            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static Skin MakeSkin(string title, int price, Color32 grass, Color32 grassAlternate, Color32 lot, Color32 road, Color32 line, Color32 curb, Color light, Color ambient) =>
            new Skin
            {
                title = title, price = price, grass = grass, grassAlternate = grassAlternate, lot = lot, road = road, line = line,
                curb = curb, light = light, ambient = ambient,
            };

        private static void BuildShopCatalog()
        {
            var shop = LoadOrCreate<SkinCatalog>(DataFolder + "/ShopCatalog.asset");
            shop.skins = new[]
            {
                MakeSkin("CLASSIC", 0, new Color32(143, 204, 115, 255), new Color32(130, 194, 107, 255), new Color32(92, 99, 120, 255),
                    new Color32(112, 120, 140, 255), new Color32(242, 245, 255, 255), new Color32(219, 222, 235, 255),
                    new Color(1f, 0.97f, 0.9f), new Color(0.5f, 0.55f, 0.68f)),
                MakeSkin("SUNSET", 400, new Color32(240, 172, 112, 255), new Color32(232, 160, 102, 255), new Color32(98, 86, 116, 255),
                    new Color32(124, 106, 134, 255), new Color32(255, 240, 226, 255), new Color32(255, 222, 196, 255),
                    new Color(1f, 0.84f, 0.66f), new Color(0.62f, 0.5f, 0.6f)),
                MakeSkin("BEACH", 600, new Color32(242, 218, 156, 255), new Color32(234, 208, 146, 255), new Color32(96, 122, 140, 255),
                    new Color32(118, 146, 162, 255), new Color32(250, 252, 255, 255), new Color32(255, 246, 218, 255),
                    new Color(1f, 0.98f, 0.9f), new Color(0.56f, 0.62f, 0.7f)),
                MakeSkin("NIGHT", 900, new Color32(44, 74, 98, 255), new Color32(38, 66, 90, 255), new Color32(46, 52, 78, 255),
                    new Color32(60, 68, 98, 255), new Color32(190, 212, 255, 255), new Color32(120, 142, 196, 255),
                    new Color(0.66f, 0.74f, 1f), new Color(0.36f, 0.4f, 0.62f)),
                MakeSkin("SNOW", 1200, new Color32(204, 222, 242, 255), new Color32(190, 212, 236, 255), new Color32(108, 122, 148, 255),
                    new Color32(134, 148, 172, 255), new Color32(255, 255, 255, 255), new Color32(250, 252, 255, 255),
                    new Color(0.95f, 0.98f, 1f), new Color(0.6f, 0.66f, 0.78f)),
            };
            shop.packs = new[]
            {
                new BoosterPack { kind = BoosterKind.Undo, amount = 3, price = 80 },
                new BoosterPack { kind = BoosterKind.Hint, amount = 3, price = 100 },
                new BoosterPack { kind = BoosterKind.ExtraSlot, amount = 3, price = 150 },
            };
            EditorUtility.SetDirty(shop);
        }

        [MenuItem("ParkSort/Setup/Rebuild Scene Only")]
        public static void BuildScene()
        {
            EnsureFolder(Root + "/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var theme = AssetDatabase.LoadAssetAtPath<GameTheme>(DataFolder + "/GameTheme.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(DataFolder + "/LevelCatalog.asset");
            SetupFont();

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;
            RenderSettings.fog = false;
            RenderSettings.skybox = null;

            Camera camera = BuildCamera(out CameraRig rig);

            var world = new GameObject("World");
            MeshRenderer ground = Mesh("Ground", world.transform, theme.quad, Mat(MaterialName.Ground), new Vector3(0f, 0f, 4f));
            ground.transform.localScale = new Vector3(120f, 1f, 120f);

            var boardGo = new GameObject("Board");
            boardGo.transform.SetParent(world.transform, false);
            var board = boardGo.AddComponent<BoardView>();

            var decor = new List<Object>();
            string[] decorMeshes = { "Tree", "Tree", "Bush", "Bush", "Lamp", "Lamp" };
            float[] decorYaw = { 0f, 40f, 15f, 200f, 90f, -90f };
            for (int i = 0; i < decorMeshes.Length; i++)
            {
                MeshRenderer item = Mesh(decorMeshes[i], world.transform, Model(decorMeshes[i]), theme.neutralMaterial, new Vector3(-5f + i * 2f, 0f, -6f));
                item.transform.localRotation = Quaternion.Euler(0f, decorYaw[i], 0f);
                MeshRenderer shadow = Mesh("Shadow", item.transform, theme.quad, theme.shadowMaterial, new Vector3(0f, 0.02f, 0f));
                shadow.transform.localScale = decorMeshes[i] == "Lamp" ? new Vector3(0.5f, 1f, 0.5f) : new Vector3(1.5f, 1f, 1.5f);
                decor.Add(item.transform);
            }

            ParticleSystem dust = Particles("Dust", world.transform, MaterialFolder + "/ParticleDust.mat", 120);
            ParticleSystem.MainModule main = dust.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            Shape(dust, ParticleSystemShapeType.Hemisphere, 0.2f);
            Shrink(dust);

            ParticleSystem confetti = Particles("Confetti", world.transform, MaterialFolder + "/ParticleConfetti.mat", 400);
            main = confetti.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 1.4f;
            Shape(confetti, ParticleSystemShapeType.Hemisphere, 0.4f);
            Shrink(confetti);

            Set(board, "theme", theme);
            Set(board, "cameraRig", rig);
            Set(board, "ground", ground);
            Set(board, "dust", dust);
            Set(board, "confetti", confetti);
            Set(board, "decor", decor.ToArray());

            var systems = new GameObject("Systems");
            var controller = systems.AddComponent<GameController>();
            Set(controller, "catalog", catalog);
            Set(controller, "shop", AssetDatabase.LoadAssetAtPath<SkinCatalog>(DataFolder + "/ShopCatalog.asset"));
            Set(controller, "board", board);
            Set(controller, "cameraRig", rig);

            BuildAudio(systems.transform);
            BuildEventSystem();
            BuildUi(controller, theme, camera);

            rig.Apply();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static MeshRenderer Mesh(string name, Transform parent, Mesh mesh, Material material, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }

        private static Camera BuildCamera(out CameraRig rig)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 12f;
            camera.nearClipPlane = 1f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(140, 204, 116, 255);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            go.AddComponent<AudioListener>();

            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            data.renderShadows = false;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.Off;

            rig = go.AddComponent<CameraRig>();
            Set(rig, "cam", camera);
            return camera;
        }

        private static void BuildAudio(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            var manager = go.AddComponent<AudioManager>();

            var effects = new List<(Sfx id, float volume, float interval, float jitter)>
            {
                (Sfx.Tap, 0.4f, 0.03f, 0.05f), (Sfx.Drive, 0.4f, 0.05f, 0.12f), (Sfx.Bump, 0.6f, 0.08f, 0.1f),
                (Sfx.Park, 0.6f, 0.04f, 0.08f), (Sfx.Wait, 0.55f, 0.05f, 0.05f), (Sfx.GarageDone, 0.8f, 0.1f, 0f),
                (Sfx.GarageOpen, 0.5f, 0.1f, 0.05f), (Sfx.Reveal, 0.55f, 0.05f, 0.08f), (Sfx.Click, 0.6f, 0.04f, 0f),
                (Sfx.Coin, 0.6f, 0.05f, 0f), (Sfx.Hint, 0.7f, 0.1f, 0f), (Sfx.Win, 0.9f, 1f, 0f), (Sfx.Lose, 0.8f, 1f, 0f),
            };

            var so = new SerializedObject(manager);
            SerializedProperty list = so.FindProperty("effects");
            list.arraySize = effects.Count;
            for (int i = 0; i < effects.Count; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").intValue = (int)effects[i].id;
                e.FindPropertyRelative("clip").objectReferenceValue = Clip("SFX/" + effects[i].id, music: false);
                e.FindPropertyRelative("volume").floatValue = effects[i].volume;
                e.FindPropertyRelative("minInterval").floatValue = effects[i].interval;
                e.FindPropertyRelative("pitchJitter").floatValue = effects[i].jitter;
            }

            so.FindProperty("music").objectReferenceValue = Clip("Music/Loop", music: true);
            so.FindProperty("musicVolume").floatValue = 0.28f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button IconButton(string name, Transform parent, string icon, Color color, Vector2 anchor, Vector2 position, Vector2 size, float iconSize)
        {
            Image back = Image(name, parent, "Button_9s", color);
            Place(back.rectTransform, anchor, new Vector2(0.5f, 0.5f), position, size);
            back.raycastTarget = true;
            back.pixelsPerUnitMultiplier = size.y >= 160f ? 0.55f : 1f;
            var button = back.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            back.gameObject.AddComponent<PressScale>();

            if (icon != null)
            {
                Image image = Image("Icon", back.transform, icon, Color.white);
                Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, size.y * 0.04f), new Vector2(iconSize, iconSize));
            }

            return button;
        }

        private static Button TextButton(string name, Transform parent, string label, Color color, Vector2 anchor, Vector2 position, Vector2 size, float fontSize, out TMP_Text text)
        {
            Button button = IconButton(name, parent, null, color, anchor, position, size, 0f);
            text = Text("Label", button.transform, label, fontSize, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)text.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, size.y * 0.04f), size - new Vector2(30f, 20f));
            return button;
        }

        private static RectTransform Window(string name, Transform parent, Vector2 size, out GameObject root)
        {
            RectTransform full = FullScreen(name, parent, new Color(0.08f, 0.1f, 0.22f, 0.62f));
            root = full.gameObject;
            Image shadow = Image("Shadow", full, "Rounded_9s", new Color(0f, 0f, 0.1f, 0.25f));
            Place(shadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), size);
            Image window = Image("Window", full, "Rounded_9s", Panel);
            Place(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            window.pixelsPerUnitMultiplier = 0.45f;
            shadow.pixelsPerUnitMultiplier = 0.45f;
            return window.rectTransform;
        }

        private static void BuildUi(GameController controller, GameTheme theme, Camera camera)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasGo.AddComponent<GraphicRaycaster>();
            var canvasRect = (RectTransform)canvasGo.transform;

            RectTransform tap = Rect("TapArea", canvasRect);
            tap.anchorMin = Vector2.zero;
            tap.anchorMax = Vector2.one;
            tap.offsetMin = tap.offsetMax = Vector2.zero;
            var tapImage = tap.gameObject.AddComponent<Image>();
            tapImage.color = new Color(0f, 0f, 0f, 0f);
            tapImage.raycastTarget = true;
            var tapArea = tap.gameObject.AddComponent<TapArea>();

            RectTransform safe = Rect("SafeArea", canvasRect);
            safe.anchorMin = Vector2.zero;
            safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            safe.gameObject.AddComponent<ParkSort.UI.SafeArea>();

            RectTransform hudRoot = Rect("Hud", safe);
            hudRoot.anchorMin = Vector2.zero;
            hudRoot.anchorMax = Vector2.one;
            hudRoot.offsetMin = hudRoot.offsetMax = Vector2.zero;

            var top = new Vector2(0.5f, 1f);
            Button settingsButton = IconButton("Settings", hudRoot, "Gear", Blue, new Vector2(0f, 1f), new Vector2(92f, -96f), new Vector2(120f, 120f), 70f);
            Button restartButton = IconButton("Restart", hudRoot, "Restart", Warm, new Vector2(0f, 1f), new Vector2(232f, -96f), new Vector2(120f, 120f), 70f);

            TMP_Text levelLabel = Text("Level", hudRoot, "LEVEL 1", 64f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)levelLabel.transform, top, new Vector2(0.5f, 0.5f), new Vector2(0f, -92f), new Vector2(420f, 90f));

            Image coinPill = Image("Coins", hudRoot, "Pill_9s", new Color(0.12f, 0.16f, 0.34f, 0.55f));
            Place(coinPill.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-36f, -96f), new Vector2(250f, 90f));
            Image coinIcon = Image("Icon", coinPill.transform, "Coin", Color.white);
            Place(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(92f, 92f));
            TMP_Text coinLabel = Text("Count", coinPill.transform, "0", 50f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)coinLabel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30f, 0f), new Vector2(160f, 80f));

            Image queuePill = Image("Queue", hudRoot, "Pill_9s", new Color(0.12f, 0.16f, 0.34f, 0.55f));
            Place(queuePill.rectTransform, top, new Vector2(0.5f, 0.5f), new Vector2(0f, -222f), new Vector2(470f, 82f));
            TMP_Text nextLabel = Text("Next", queuePill.transform, "NEXT", 38f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)nextLabel.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(130f, 60f));
            var chips = new Object[4];
            for (int i = 0; i < chips.Length; i++)
            {
                Image chip = Image("Chip" + i, queuePill.transform, "Rounded_9s", Color.white);
                Place(chip.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(196f + i * 70f, 0f), new Vector2(56f, 56f));
                chip.pixelsPerUnitMultiplier = 1.4f;
                chips[i] = chip;
            }

            var bottom = new Vector2(0.5f, 0f);
            var boosterSize = new Vector2(176f, 176f);
            Button undoButton = IconButton("Undo", hudRoot, "Undo", Accent, bottom, new Vector2(-230f, 140f), boosterSize, 100f);
            TMP_Text undoCount = Badge(undoButton.transform, "x3");
            Button hintButton = IconButton("Hint", hudRoot, "Bulb", Warm, bottom, new Vector2(0f, 140f), boosterSize, 100f);
            TMP_Text hintCount = Badge(hintButton.transform, "x3");
            Button slotButton = IconButton("ExtraSlot", hudRoot, "ParkingPlus", Blue, bottom, new Vector2(230f, 140f), boosterSize, 100f);
            TMP_Text slotCount = Badge(slotButton.transform, "x3");

            Image waitingPill = Image("Waiting", hudRoot, "Pill_9s", new Color(0.12f, 0.16f, 0.34f, 0.7f));
            Place(waitingPill.rectTransform, bottom, new Vector2(0.5f, 0.5f), new Vector2(0f, 282f), new Vector2(210f, 72f));
            Image waitingIcon = Image("Icon", waitingPill.transform, "Parking", Color.white);
            Place(waitingIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(44f, 0f), new Vector2(52f, 52f));
            TMP_Text waitingLabel = Text("Count", waitingPill.transform, "0/5", 44f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)waitingLabel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 0f), new Vector2(130f, 64f));

            Image difficultyPill = Image("Difficulty", hudRoot, "Pill_9s", Red);
            Place(difficultyPill.rectTransform, top, new Vector2(0.5f, 0.5f), new Vector2(0f, -148f), new Vector2(250f, 50f));
            TMP_Text difficultyLabel = Text("Label", difficultyPill.transform, "HARD", 32f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)difficultyLabel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 46f));
            difficultyPill.pixelsPerUnitMultiplier = 1.3f;
            difficultyPill.gameObject.SetActive(false);

            Image captionPill = Image("Caption", hudRoot, "Pill_9s", new Color(0.12f, 0.16f, 0.34f, 0.8f));
            Place(captionPill.rectTransform, top, new Vector2(0.5f, 0.5f), new Vector2(0f, -326f), new Vector2(1000f, 120f));
            var captionGroup = captionPill.gameObject.AddComponent<CanvasGroup>();
            captionGroup.blocksRaycasts = false;
            TMP_Text caption = Text("Text", captionPill.transform, "TAP A CAR", 34f, Color.white, TextAlignmentOptions.Center);
            caption.textWrappingMode = TextWrappingModes.Normal;
            Place((RectTransform)caption.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940f, 110f));
            captionPill.gameObject.SetActive(false);

            Image hand = Image("Hand", canvasRect, "Hand", Color.white);
            Place(hand.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170f, 170f));
            hand.gameObject.SetActive(false);

            var hud = canvasGo.AddComponent<Hud>();
            Set(hud, "controller", controller);
            Set(hud, "theme", theme);
            Set(hud, "tapArea", tapArea);
            Set(hud, "root", hudRoot.gameObject);
            Set(hud, "levelLabel", levelLabel);
            Set(hud, "coinLabel", coinLabel);
            Set(hud, "coinIcon", coinIcon.rectTransform);
            Set(hud, "settingsButton", settingsButton);
            Set(hud, "restartButton", restartButton);
            Set(hud, "queueChips", chips);
            Set(hud, "queueRoot", queuePill.gameObject);
            Set(hud, "hintButton", hintButton);
            Set(hud, "hintCount", hintCount);
            Set(hud, "slotButton", slotButton);
            Set(hud, "slotCount", slotCount);
            Set(hud, "undoButton", undoButton);
            Set(hud, "undoCount", undoCount);
            Set(hud, "waitingLabel", waitingLabel);
            Set(hud, "waitingPill", waitingPill);
            Set(hud, "difficultyBadge", difficultyPill.gameObject);
            Set(hud, "difficultyImage", difficultyPill);
            Set(hud, "difficultyLabel", difficultyLabel);
            Set(hud, "hand", hand.rectTransform);
            Set(hud, "canvasRect", canvasRect);
            Set(hud, "worldCamera", camera);
            Set(hud, "caption", captionPill.GetComponentInChildren<TMP_Text>(true));
            Set(hud, "captionGroup", captionGroup);
            hudRoot.gameObject.SetActive(false);

            var screens = canvasGo.AddComponent<Screens>();
            Set(screens, "controller", controller);
            Set(screens, "hud", hud);
            BuildHome(canvasRect, safe, screens);
            BuildWin(canvasRect, screens);
            BuildFail(canvasRect, screens);
            BuildFeature(canvasRect, screens);
            BuildSettings(canvasRect, screens);
            BuildShop(canvasRect, screens, controller);
        }

        private static TMP_Text Badge(Transform button, string text)
        {
            Image badge = Image("Badge", button, "Pill_9s", Ink);
            Place(badge.rectTransform, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-24f, 20f), new Vector2(104f, 60f));
            TMP_Text label = Text("Count", badge.transform, text, 36f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)label.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 56f));
            return label;
        }

        private static void BuildHome(RectTransform canvasRect, RectTransform safe, Screens screens)
        {
            RectTransform root = Rect("Home", canvasRect);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;

            Image fade = Image("Fade", root, null, new Color(0.1f, 0.14f, 0.3f, 0.1f));
            fade.rectTransform.anchorMin = Vector2.zero;
            fade.rectTransform.anchorMax = Vector2.one;
            fade.rectTransform.offsetMin = fade.rectTransform.offsetMax = Vector2.zero;
            fade.raycastTarget = true;

            var center = new Vector2(0.5f, 0.5f);
            TMP_Text title = Text("Title", root, "PARK SORT", 128f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)title.transform, new Vector2(0.5f, 1f), center, new Vector2(0f, -262f), new Vector2(1040f, 150f));

            Image levelPill = Image("LevelPill", root, "Pill_9s", new Color(0.12f, 0.16f, 0.34f, 0.7f));
            Place(levelPill.rectTransform, new Vector2(0.5f, 0f), center, new Vector2(0f, 318f), new Vector2(380f, 84f));
            TMP_Text level = Text("Level", levelPill.transform, "LEVEL 1", 48f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)level.transform, center, center, Vector2.zero, new Vector2(360f, 76f));

            Button play = TextButton("Play", root, "PLAY", Accent, new Vector2(0.5f, 0f), new Vector2(0f, 156f), new Vector2(520f, 176f), 88f, out _);

            Image coinPill = Image("Coins", root, "Pill_9s", new Color(0.12f, 0.16f, 0.34f, 0.7f));
            Place(coinPill.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-36f, -120f), new Vector2(250f, 90f));
            Image coinIcon = Image("Icon", coinPill.transform, "Coin", Color.white);
            Place(coinIcon.rectTransform, new Vector2(0f, 0.5f), center, new Vector2(40f, 0f), new Vector2(92f, 92f));
            TMP_Text coins = Text("Count", coinPill.transform, "0", 50f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)coins.transform, center, center, new Vector2(30f, 0f), new Vector2(160f, 80f));

            Button settingsButton = IconButton("Settings", root, "Gear", Blue, new Vector2(0f, 1f), new Vector2(92f, -120f), new Vector2(120f, 120f), 70f);
            Button shopButton = IconButton("ShopButton", root, "Shop", Warm, new Vector2(0f, 1f), new Vector2(232f, -120f), new Vector2(120f, 120f), 70f);

            Set(screens, "homeShopButton", shopButton);
            Set(screens, "home", root.gameObject);
            Set(screens, "homeLevel", level);
            Set(screens, "homeCoins", coins);
            Set(screens, "playButton", play);
            Set(screens, "playRect", play.transform);
            Set(screens, "homeSettingsButton", settingsButton);
        }

        private static void BuildWin(RectTransform canvasRect, Screens screens)
        {
            RectTransform window = Window("Win", canvasRect, new Vector2(820f, 1080f), out GameObject root);
            var center = new Vector2(0.5f, 0.5f);

            var stars = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                Image star = Image("Star" + i, window, "Star", Color.white);
                float lift = i == 1 ? 40f : 0f;
                Place(star.rectTransform, new Vector2(0.5f, 1f), center, new Vector2((i - 1) * 210f, -40f + lift), new Vector2(i == 1 ? 250f : 200f, i == 1 ? 250f : 200f));
                star.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (1 - i) * 14f);
                stars[i] = star.rectTransform;
            }

            TMP_Text title = Text("Title", window, "LEVEL 1\nCOMPLETE!", 84f, Ink, TextAlignmentOptions.Center);
            title.fontSharedMaterial = _font.material;
            Place((RectTransform)title.transform, center, center, new Vector2(0f, 200f), new Vector2(760f, 240f));

            Image rewardPill = Image("Reward", window, "Pill_9s", new Color32(255, 236, 170, 255));
            Place(rewardPill.rectTransform, center, center, new Vector2(0f, -20f), new Vector2(330f, 110f));
            Image coin = Image("Icon", rewardPill.transform, "Coin", Color.white);
            Place(coin.rectTransform, new Vector2(0f, 0.5f), center, new Vector2(60f, 0f), new Vector2(104f, 104f));
            TMP_Text reward = Text("Amount", rewardPill.transform, "+25", 64f, new Color32(190, 110, 10, 255), TextAlignmentOptions.Center);
            reward.fontSharedMaterial = _font.material;
            Place((RectTransform)reward.transform, center, center, new Vector2(40f, 0f), new Vector2(220f, 90f));

            Button next = TextButton("Next", window, "NEXT", Accent, new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(520f, 170f), 84f, out _);

            Button doubled = TextButton("Double", window, "x2   +25", new Color32(158, 104, 236, 255), new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(520f, 150f), 54f, out TMP_Text doubleLabel);
            Place((RectTransform)doubleLabel.transform, center, center, new Vector2(50f, 6f), new Vector2(380f, 110f));
            Image video = Image("Video", doubled.transform, "Video", Color.white);
            Place(video.rectTransform, new Vector2(0f, 0.5f), center, new Vector2(84f, 6f), new Vector2(84f, 84f));
            Set(screens, "doubleButton", doubled);
            Set(screens, "doubleLabel", doubleLabel);
            Set(screens, "rewardPill", rewardPill.rectTransform);

            Set(screens, "win", root);
            Set(screens, "winWindow", window);
            Set(screens, "winTitle", title);
            Set(screens, "winReward", reward);
            Set(screens, "nextButton", next);
            Set(screens, "stars", stars);
            root.SetActive(false);
        }

        private static void BuildFail(RectTransform canvasRect, Screens screens)
        {
            RectTransform window = Window("Fail", canvasRect, new Vector2(820f, 760f), out GameObject root);
            var center = new Vector2(0.5f, 0.5f);

            TMP_Text title = Text("Title", window, "NO SPACE\nLEFT!", 96f, Red, TextAlignmentOptions.Center);
            title.fontSharedMaterial = _font.material;
            Place((RectTransform)title.transform, new Vector2(0.5f, 1f), center, new Vector2(0f, -190f), new Vector2(760f, 260f));

            Button revive = TextButton("Revive", window, "+1 SPACE", Blue, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(620f, 170f), 62f, out TMP_Text reviveLabel);
            Button retry = TextButton("Retry", window, "TRY AGAIN", Warm, new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(620f, 170f), 66f, out _);

            Set(screens, "fail", root);
            Set(screens, "failWindow", window);
            Set(screens, "reviveButton", revive);
            Set(screens, "reviveLabel", reviveLabel);
            Set(screens, "retryButton", retry);
            root.SetActive(false);
        }

        private static void BuildFeature(RectTransform canvasRect, Screens screens)
        {
            RectTransform window = Window("Feature", canvasRect, new Vector2(820f, 1000f), out GameObject root);
            var center = new Vector2(0.5f, 0.5f);

            Image ribbon = Image("Ribbon", window, "Pill_9s", Warm);
            Place(ribbon.rectTransform, new Vector2(0.5f, 1f), center, new Vector2(0f, -10f), new Vector2(520f, 104f));
            TMP_Text ribbonLabel = Text("Label", ribbon.transform, "NEW FEATURE", 54f, Color.white, TextAlignmentOptions.Center);
            Place((RectTransform)ribbonLabel.transform, center, center, Vector2.zero, new Vector2(500f, 90f));

            Image glow = Image("Glow", window, "Circle", new Color32(255, 236, 170, 255));
            Place(glow.rectTransform, center, center, new Vector2(0f, 170f), new Vector2(400f, 400f));
            Image icon = Image("Icon", window, "Feature_Van", Color.white);
            Place(icon.rectTransform, center, center, new Vector2(0f, 170f), new Vector2(380f, 380f));

            TMP_Text title = Text("Title", window, "TUNNEL", 88f, Ink, TextAlignmentOptions.Center);
            title.fontSharedMaterial = _font.material;
            Place((RectTransform)title.transform, center, center, new Vector2(0f, -100f), new Vector2(760f, 110f));

            TMP_Text text = Text("Text", window, "Cars come out one by one.", 46f, new Color32(96, 106, 140, 255), TextAlignmentOptions.Center);
            text.fontSharedMaterial = _font.material;
            text.textWrappingMode = TextWrappingModes.Normal;
            Place((RectTransform)text.transform, center, center, new Vector2(0f, -220f), new Vector2(720f, 140f));

            Button button = TextButton("Continue", window, "CONTINUE", Accent, new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(520f, 170f), 72f, out _);

            Set(screens, "feature", root);
            Set(screens, "featureWindow", window);
            Set(screens, "featureIcon", icon);
            Set(screens, "featureTitle", title);
            Set(screens, "featureText", text);
            Set(screens, "featureButton", button);
            root.SetActive(false);
        }

        private static void BuildShop(RectTransform canvasRect, Screens screens, GameController controller)
        {
            RectTransform window = Window("Shop", canvasRect, new Vector2(960f, 1600f), out GameObject root);
            var center = new Vector2(0.5f, 0.5f);
            var topCenter = new Vector2(0.5f, 1f);
            var muted = new Color32(96, 106, 140, 255);

            TMP_Text title = Text("Title", window, "SHOP", 88f, Ink, TextAlignmentOptions.Center);
            title.fontSharedMaterial = _font.material;
            Place((RectTransform)title.transform, topCenter, center, new Vector2(0f, -100f), new Vector2(500f, 120f));
            Button close = IconButton("Close", window, "Close", Red, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(110f, 110f), 56f);

            Image coinPill = Image("Coins", window, "Pill_9s", new Color32(255, 236, 170, 255));
            Place(coinPill.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(40f, -100f), new Vector2(250f, 86f));
            Image coinIcon = Image("Icon", coinPill.transform, "Coin", Color.white);
            Place(coinIcon.rectTransform, new Vector2(0f, 0.5f), center, new Vector2(40f, 0f), new Vector2(88f, 88f));
            TMP_Text coins = Text("Count", coinPill.transform, "0", 48f, new Color32(190, 110, 10, 255), TextAlignmentOptions.Center);
            coins.fontSharedMaterial = _font.material;
            Place((RectTransform)coins.transform, center, center, new Vector2(30f, 0f), new Vector2(160f, 76f));

            TMP_Text boosters = Text("BoostersLabel", window, "BOOSTERS", 44f, muted, TextAlignmentOptions.Center);
            boosters.fontSharedMaterial = _font.material;
            Place((RectTransform)boosters.transform, topCenter, center, new Vector2(0f, -220f), new Vector2(600f, 60f));

            string[] packIcons = { "Undo", "Bulb", "ParkingPlus" };
            Color[] packColors = { Accent, Warm, Blue };
            var packButtons = new Object[3];
            var packAmounts = new Object[3];
            var packPrices = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                Button pack = IconButton("Pack" + i, window, packIcons[i], packColors[i], topCenter, new Vector2((i - 1) * 290f, -400f), new Vector2(260f, 260f), 120f);
                ((RectTransform)pack.transform.Find("Icon")).anchoredPosition = new Vector2(0f, 46f);
                TMP_Text amount = Text("Amount", pack.transform, "x3", 44f, Color.white, TextAlignmentOptions.Center);
                Place((RectTransform)amount.transform, new Vector2(1f, 1f), center, new Vector2(-54f, -44f), new Vector2(100f, 56f));
                Image pricePill = Image("Price", pack.transform, "Pill_9s", new Color(0f, 0f, 0.1f, 0.28f));
                Place(pricePill.rectTransform, new Vector2(0.5f, 0f), center, new Vector2(0f, 62f), new Vector2(200f, 64f));
                Image priceCoin = Image("Coin", pricePill.transform, "Coin", Color.white);
                Place(priceCoin.rectTransform, new Vector2(0f, 0.5f), center, new Vector2(36f, 0f), new Vector2(58f, 58f));
                TMP_Text price = Text("Value", pricePill.transform, "100", 40f, Color.white, TextAlignmentOptions.Center);
                Place((RectTransform)price.transform, center, center, new Vector2(24f, 0f), new Vector2(130f, 56f));
                packButtons[i] = pack;
                packAmounts[i] = amount;
                packPrices[i] = price;
            }

            TMP_Text themes = Text("ThemesLabel", window, "THEMES", 44f, muted, TextAlignmentOptions.Center);
            themes.fontSharedMaterial = _font.material;
            Place((RectTransform)themes.transform, topCenter, center, new Vector2(0f, -600f), new Vector2(600f, 60f));

            const int rows = 5;
            var skinButtons = new Object[rows];
            var skinImages = new Object[rows];
            var skinLabels = new Object[rows];
            var skinTitles = new Object[rows];
            var skinGrass = new Object[rows];
            var skinLot = new Object[rows];
            for (int i = 0; i < rows; i++)
            {
                Image row = Image("Theme" + i, window, "Rounded_9s", new Color32(236, 239, 250, 255));
                Place(row.rectTransform, topCenter, center, new Vector2(0f, -730f - i * 176f), new Vector2(860f, 156f));
                row.pixelsPerUnitMultiplier = 0.8f;

                Image grass = Image("Grass", row.transform, "Rounded_9s", Color.green);
                Place(grass.rectTransform, new Vector2(0f, 0.5f), center, new Vector2(90f, 0f), new Vector2(124f, 124f));
                Image lot = Image("Lot", grass.transform, "Rounded_9s", Color.grey);
                Place(lot.rectTransform, center, center, Vector2.zero, new Vector2(76f, 76f));
                lot.pixelsPerUnitMultiplier = 1.6f;

                TMP_Text name = Text("Title", row.transform, "CLASSIC", 52f, Ink, TextAlignmentOptions.Left);
                name.fontSharedMaterial = _font.material;
                Place((RectTransform)name.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(180f, 0f), new Vector2(360f, 80f));

                Button use = TextButton("Button", row.transform, "USE", Blue, new Vector2(1f, 0.5f), new Vector2(-160f, 0f), new Vector2(280f, 116f), 46f, out TMP_Text useLabel);
                ((Image)use.targetGraphic).pixelsPerUnitMultiplier = 1f;

                skinButtons[i] = use;
                skinImages[i] = use.targetGraphic;
                skinLabels[i] = useLabel;
                skinTitles[i] = name;
                skinGrass[i] = grass;
                skinLot[i] = lot;
            }

            var panel = canvasRect.gameObject.AddComponent<ShopPanel>();
            Set(panel, "controller", controller);
            Set(panel, "root", root);
            Set(panel, "window", window);
            Set(panel, "coinLabel", coins);
            Set(panel, "closeButton", close);
            Set(panel, "packButtons", packButtons);
            Set(panel, "packAmounts", packAmounts);
            Set(panel, "packPrices", packPrices);
            Set(panel, "skinButtons", skinButtons);
            Set(panel, "skinButtonImages", skinImages);
            Set(panel, "skinButtonLabels", skinLabels);
            Set(panel, "skinTitles", skinTitles);
            Set(panel, "skinGrass", skinGrass);
            Set(panel, "skinLot", skinLot);
            Set(screens, "shopPanel", panel);
            root.SetActive(false);
        }

        private static void BuildSettings(RectTransform canvasRect, Screens screens)
        {
            RectTransform window = Window("Settings", canvasRect, new Vector2(780f, 1040f), out GameObject root);
            var center = new Vector2(0.5f, 0.5f);

            TMP_Text title = Text("Title", window, "SETTINGS", 84f, Ink, TextAlignmentOptions.Center);
            title.fontSharedMaterial = _font.material;
            Place((RectTransform)title.transform, new Vector2(0.5f, 1f), center, new Vector2(0f, -110f), new Vector2(700f, 120f));

            Button close = IconButton("Close", window, "Close", Red, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(110f, 110f), 56f);
            var size = new Vector2(560f, 150f);
            Button sound = TextButton("Sound", window, "SOUND: ON", Blue, center, new Vector2(0f, 220f), size, 56f, out TMP_Text soundLabel);
            Button music = TextButton("Music", window, "MUSIC: ON", Blue, center, new Vector2(0f, 50f), size, 56f, out TMP_Text musicLabel);
            Button shopButton = TextButton("ShopButton", window, "SHOP", new Color32(158, 104, 236, 255), center, new Vector2(0f, -130f), size, 60f, out _);
            Button home = TextButton("Home", window, "HOME", Warm, center, new Vector2(0f, -330f), size, 60f, out _);
            Set(screens, "settingsShopButton", shopButton);

            Set(screens, "settings", root);
            Set(screens, "settingsWindow", window);
            Set(screens, "closeSettingsButton", close);
            Set(screens, "soundButton", sound);
            Set(screens, "soundLabel", soundLabel);
            Set(screens, "musicButton", music);
            Set(screens, "musicLabel", musicLabel);
            Set(screens, "homeButton", home);
            root.SetActive(false);
        }
    }
}
