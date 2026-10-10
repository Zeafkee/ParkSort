using ParkSort.Core;
using ParkSort.Dev;
using ParkSort.Game;
using ParkSort.Save;
using ParkSort.View;
using UnityEditor;
using UnityEngine;

namespace ParkSort.EditorTools
{
    public static class DevTools
    {
        [MenuItem("ParkSort/Dev/Auto-Play (normal speed)")]
        public static void AutoPlayNormal() => Debug.Log(AutoPlay(0.45f, 0f, 1f));

        [MenuItem("ParkSort/Dev/Auto-Play (fast)")]
        public static void AutoPlayFast() => Debug.Log(AutoPlay(0.12f, 0f, 4f));

        public static string AutoPlay(float interval, float mistakes, float timeScale)
        {
            if (!Application.isPlaying) return "not playing";
            var controller = Object.FindAnyObjectByType<GameController>();
            if (controller == null) return "no controller";

            var bot = controller.GetComponent<DebugAutoPlayer>();
            if (bot == null) bot = controller.gameObject.AddComponent<DebugAutoPlayer>();
            bot.Bind(controller, interval, mistakes);
            Time.timeScale = timeScale;
            EditorApplication.playModeStateChanged -= ResetTime;
            EditorApplication.playModeStateChanged += ResetTime;
            return "auto-play attached";
        }

        private static void ResetTime(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;
            Time.timeScale = 1f;
            EditorApplication.playModeStateChanged -= ResetTime;
        }

        [MenuItem("ParkSort/Dev/Log Status")]
        public static void LogStatus() => Debug.Log(Status());

        public static string Status()
        {
            if (!Application.isPlaying) return "not playing";
            var controller = Object.FindAnyObjectByType<GameController>();
            var bot = Object.FindAnyObjectByType<DebugAutoPlayer>();
            ParkingGame game = controller.Game;
            string botText = bot == null ? string.Empty : " won=" + bot.LevelsWon + " failed=" + bot.LevelsFailed;
            return "t=" + Time.time.ToString("F0") + " screen=" + controller.Screen + " level=" + controller.LevelNumber +
                   " cars=" + game.Remaining + "/" + game.Level.cars.Length + " waiting=" + game.WaitingCount + "/" + game.WaitingCapacity +
                   " coins=" + controller.Coins + " busy=" + controller.Board.IsBusy + " fps=" + (1f / Time.unscaledDeltaTime).ToString("F0") + botText;
        }

        [MenuItem("ParkSort/Dev/Delete Save Data")]
        public static void DeleteSave()
        {
            SaveSystem.Reset();
            Debug.Log("Save data deleted.");
        }

        public static string SetLevel(int levelNumber)
        {
            SaveSystem.Data.level = Mathf.Max(0, levelNumber - 1);
            SaveSystem.Save();
            if (!Application.isPlaying) return "saved";
            Object.FindAnyObjectByType<GameController>().GoHome();
            return "level set to " + levelNumber;
        }

        public static string CapturePortrait(string path, int width = 1080, int height = 1920)
        {
            Camera cam = Camera.main;
            if (cam == null) return "no camera";

            var target = new RenderTexture(width, height, 24);
            Canvas[] canvases = Object.FindObjectsByType<Canvas>();
            var modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                if (!canvases[i].isRootCanvas) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam;
                canvases[i].planeDistance = cam.nearClipPlane + 0.1f;
            }

            cam.targetTexture = target;
            var rig = cam.GetComponent<CameraRig>();
            if (rig != null) rig.Apply();
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = null;

            cam.targetTexture = null;
            if (rig != null) rig.Apply();
            for (int i = 0; i < canvases.Length; i++) canvases[i].renderMode = modes[i];

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            return path;
        }
    }
}
