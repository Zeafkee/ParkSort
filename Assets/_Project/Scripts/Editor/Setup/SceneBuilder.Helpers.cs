using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ParkSort.EditorTools
{
    public static partial class SceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string DataFolder = Root + "/Data";
        private const string MaterialFolder = Root + "/Art/Materials";

        private static TMP_FontAsset _font;
        private static Material _fontOutline;

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Material Material(string name, string shaderName)
        {
            EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(shaderName);
            if (shader == null) Debug.LogError($"Shader not found: {shaderName}");

            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetupFont()
        {
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (_font == null)
            {
                Debug.LogError("TMP font missing: extract 'TMP Essential Resources' first.");
                return;
            }

            string path = MaterialFolder + "/UIText_Outline.mat";
            _fontOutline = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (_fontOutline == null)
            {
                _fontOutline = new Material(_font.material) { name = "UIText_Outline" };
                AssetDatabase.CreateAsset(_fontOutline, path);
            }

            _fontOutline.shader = _font.material.shader;
            _fontOutline.CopyPropertiesFromMaterial(_font.material);
            _fontOutline.SetTexture(ShaderUtilities.ID_MainTex, _font.atlasTexture);
            _fontOutline.EnableKeyword("OUTLINE_ON");
            _fontOutline.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
            _fontOutline.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
            _fontOutline.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(40, 50, 90, 255));
            EditorUtility.SetDirty(_fontOutline);
        }

        private static Sprite UiSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/UI/{name}.png");

        private static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image Image(string name, Transform parent, string sprite, Color color)
        {
            RectTransform rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite != null ? UiSprite(sprite) : null;
            image.color = color;
            image.raycastTarget = false;
            if (image.sprite != null && image.sprite.border.sqrMagnitude > 0f) image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }

        private static TMP_Text Text(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            RectTransform rect = Rect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = _font;
            if (_fontOutline != null) label.fontSharedMaterial = _fontOutline;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
            label.alignment = align;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.richText = false;
            return label;
        }

        private static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"{target.GetType().Name} has no serialized field '{field}'.", target);
                return;
            }

            switch (value)
            {
                case null: prop.objectReferenceValue = null; break;
                case Object obj: prop.objectReferenceValue = obj; break;
                case Object[] array:
                    prop.arraySize = array.Length;
                    for (int i = 0; i < array.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
                    break;
                case int[] ints:
                    prop.arraySize = ints.Length;
                    for (int i = 0; i < ints.Length; i++)
                        prop.GetArrayElementAtIndex(i).intValue = ints[i];
                    break;
                case string s: prop.stringValue = s; break;
                case System.Enum e: prop.intValue = System.Convert.ToInt32(e); break;
                case float f: prop.floatValue = f; break;
                case int i: prop.intValue = i; break;
                case bool b: prop.boolValue = b; break;
                case Color c: prop.colorValue = c; break;
                default: Debug.LogError($"Unsupported value type {value.GetType()} for '{field}'."); break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ParticleSystem Particles(string name, Transform parent, string materialPath, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        private static void Shape(ParticleSystem system, ParticleSystemShapeType type, float radius)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = type;
            shape.radius = radius;
            shape.rotation = new Vector3(-90f, 0f, 0f);
        }

        private static void Shrink(ParticleSystem system)
        {
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        }

        private static AudioClip Clip(string name, bool music)
        {
            string path = $"{Root}/Audio/{name}.wav";
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogError($"Audio clip missing: {path} (run Tools/generate_audio.py)");
                return null;
            }

            importer.forceToMono = true;
            importer.loadInBackground = music;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? 0.5f : 0.7f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static RectTransform FullScreen(string name, Transform parent, Color dim)
        {
            RectTransform root = Rect(name, parent);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            Image back = Image("Dim", root, null, dim);
            back.rectTransform.anchorMin = Vector2.zero;
            back.rectTransform.anchorMax = Vector2.one;
            back.rectTransform.offsetMin = back.rectTransform.offsetMax = Vector2.zero;
            back.raycastTarget = true;
            return root;
        }

        [MenuItem("ParkSort/Setup/Apply Project Settings")]
        [MenuItem("ParkSort/Setup/Apply Project Settings")]
        public static void ApplyProjectSettings()
        {
            PlayerSettings.companyName = "Zeafkee";
            PlayerSettings.productName = "ParkSort";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.zeafkee.parksort");

            PlayerSettings.gcIncremental = true;
            PlayerSettings.accelerometerFrequency = 0;
            PlayerSettings.runInBackground = true;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.optimizedFramePacing = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Icon/AppIcon.png");
            if (icon != null) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;

            Physics.simulationMode = SimulationMode.Script;
            Physics2D.simulationMode = SimulationMode2D.Script;

            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
            AssetDatabase.SaveAssets();
        }
    }
}
