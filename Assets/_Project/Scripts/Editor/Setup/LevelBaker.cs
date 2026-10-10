using System.Collections.Generic;
using System.Text;
using ParkSort.Core;
using ParkSort.Data;
using UnityEditor;
using UnityEngine;

namespace ParkSort.EditorTools
{
    public static class LevelBaker
    {
        public const int LevelCount = 60;
        private const int Candidates = 80;
        private const int Playouts = 160;

        private struct Tier
        {
            public int fromLevel;
            public GeneratorSettings settings;
        }

        private static GeneratorSettings Make(int width, int height, int cars, int colors, float longChance, float hidden, int obstacles,
            int waiting, float direct, int lookahead, int planned, int tunnels = 0) =>
            new GeneratorSettings
            {
                width = width, height = height, cars = cars, colors = colors, longCarChance = longChance, hiddenChance = hidden,
                obstacles = obstacles, waitingSlots = waiting, directChance = direct, lookahead = lookahead, plannedWaitingLimit = planned,
                tunnels = tunnels, carsPerTunnel = 3,
            };

        private static readonly Tier[] Tiers =
        {
            new Tier { fromLevel = 1, settings = Make(4, 4, 6, 2, 0f, 0f, 0, 5, 1f, 1, 0) },
            new Tier { fromLevel = 2, settings = Make(4, 5, 6, 3, 0f, 0f, 0, 5, 0.7f, 2, 2) },
            new Tier { fromLevel = 3, settings = Make(5, 5, 9, 3, 0f, 0f, 0, 5, 0.75f, 2, 2) },
            new Tier { fromLevel = 6, settings = Make(5, 6, 9, 3, 0.4f, 0f, 0, 5, 0.7f, 2, 3) },
            new Tier { fromLevel = 8, settings = Make(5, 6, 12, 4, 0.3f, 0f, 0, 5, 0.65f, 2, 3) },
            new Tier { fromLevel = 11, settings = Make(6, 6, 12, 4, 0.25f, 0.45f, 0, 5, 0.6f, 2, 3) },
            new Tier { fromLevel = 16, settings = Make(6, 7, 12, 4, 0.25f, 0.3f, 3, 5, 0.55f, 3, 4) },
            new Tier { fromLevel = 21, settings = Make(6, 7, 15, 5, 0.25f, 0.3f, 2, 5, 0.5f, 3, 4) },
            new Tier { fromLevel = 31, settings = Make(6, 7, 12, 4, 0.25f, 0f, 0, 5, 0.6f, 2, 3, 1) },
            new Tier { fromLevel = 32, settings = Make(7, 7, 15, 5, 0.3f, 0.3f, 1, 5, 0.45f, 3, 5, 1) },
            new Tier { fromLevel = 41, settings = Make(7, 8, 18, 6, 0.3f, 0.35f, 2, 5, 0.38f, 3, 5, 1) },
            new Tier { fromLevel = 51, settings = Make(7, 8, 18, 6, 0.3f, 0.4f, 2, 4, 0.34f, 3, 4, 2) },
        };

        private struct Feature
        {
            public string title;
            public string text;
            public string icon;
        }

        private static readonly Dictionary<int, string> Captions = new Dictionary<int, string>
        {
            { 1, "TAP A CAR TO DRIVE IT INTO THE GARAGE OF ITS COLOUR" },
            { 2, "NO OPEN GARAGE? THE CAR WAITS BELOW. DO NOT FILL THE BAY!" },
        };

        private static readonly Dictionary<int, Feature> Features = new Dictionary<int, Feature>
        {
            { 6, new Feature { title = "VANS", text = "Vans are longer.\nPlan your way around them!", icon = "Feature_Van" } },
            { 11, new Feature { title = "MYSTERY CARS", text = "Their colour shows\nwhen the way is clear!", icon = "Feature_Mystery" } },
            { 16, new Feature { title = "OBSTACLES", text = "Planters and cones\nnever move!", icon = "Feature_Obstacle" } },
            { 31, new Feature { title = "TUNNEL", text = "Cars come out one by one.\nTap it to send the next one!", icon = "Feature_Tunnel" } },
        };

        public static LevelDifficulty DifficultyOf(int levelNumber)
        {
            if (levelNumber < 15 || levelNumber % 5 != 0) return LevelDifficulty.Normal;
            return (levelNumber / 5) % 2 == 1 ? LevelDifficulty.Hard : LevelDifficulty.VeryHard;
        }

        private static GeneratorSettings SettingsFor(int levelNumber)
        {
            GeneratorSettings settings = Tiers[0].settings;
            for (int i = 0; i < Tiers.Length; i++)
                if (levelNumber >= Tiers[i].fromLevel) settings = Tiers[i].settings;
            return settings;
        }

        public static float TargetWinRate(int levelNumber)
        {
            if (levelNumber <= 2) return 1f;
            float t = Mathf.Clamp01((levelNumber - 3) / (float)(LevelCount - 3));
            float baseline = Mathf.Lerp(0.97f, 0.5f, Mathf.Pow(t, 0.8f));
            LevelDifficulty difficulty = DifficultyOf(levelNumber);
            if (difficulty == LevelDifficulty.Hard) baseline -= 0.13f;
            else if (difficulty == LevelDifficulty.VeryHard) baseline -= 0.22f;
            else if (levelNumber % 5 == 0) baseline -= 0.08f;
            else if (levelNumber % 5 == 1 && levelNumber > 5) baseline += 0.08f;
            if (Features.ContainsKey(levelNumber)) baseline = Mathf.Max(baseline, 0.92f);
            return Mathf.Clamp(baseline, 0.2f, 1f);
        }

        public static float Blend(DifficultyReport report) => report.greedyWinRate * 0.6f + report.randomWinRate * 0.4f;

        [MenuItem("ParkSort/Setup/Bake Levels")]
        public static void BakeMenu()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/_Project/Data/LevelCatalog.asset");
            if (catalog == null)
            {
                Debug.LogError("LevelCatalog.asset is missing: run ParkSort > Setup > Build Everything first.");
                return;
            }

            Debug.Log(Bake(catalog));
            AssetDatabase.SaveAssets();
        }

        public static string Bake(LevelCatalog catalog)
        {
            var generator = new LevelGenerator();
            var bot = new AutoPlayer();
            var entries = new LevelEntry[LevelCount];
            var log = new StringBuilder();

            for (int number = 1; number <= LevelCount; number++)
            {
                GeneratorSettings settings = SettingsFor(number);
                float target = TargetWinRate(number);
                var rng = new Rng((uint)(number * 7919 + 13));

                LevelDef best = null;
                DifficultyReport bestReport = default;
                float bestError = float.MaxValue;

                for (int candidate = 0; candidate < Candidates; candidate++)
                {
                    GeneratorSettings varied = settings;
                    varied.directChance = Mathf.Clamp01(settings.directChance + (rng.Value() - 0.5f) * 0.3f);
                    uint seed = rng.NextUInt() | 1u;

                    LevelDef level = generator.Generate(varied, seed, 6);
                    if (level == null) continue;

                    DifficultyReport report = bot.Measure(level, Playouts, seed);
                    float error = Mathf.Abs(Blend(report) - target);
                    if (error >= bestError) continue;

                    best = level;
                    bestReport = report;
                    bestError = error;
                }

                if (best == null)
                {
                    log.AppendLine($"level {number}: no candidate");
                    best = entries[Mathf.Max(0, number - 2)].level;
                }

                Captions.TryGetValue(number, out string caption);
                var entry = new LevelEntry
                {
                    level = best, greedyWinRate = bestReport.greedyWinRate, randomWinRate = bestReport.randomWinRate,
                    caption = caption ?? string.Empty, difficulty = DifficultyOf(number), featureTitle = string.Empty,
                };
                if (Features.TryGetValue(number, out Feature feature))
                {
                    entry.featureTitle = feature.title;
                    entry.caption = feature.text;
                    entry.featureIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/UI/" + feature.icon + ".png");
                }

                entries[number - 1] = entry;
                log.AppendLine($"level {number}: {best.width}x{best.height} cars={best.cars.Length} tunnels={best.tunnels.Length} {DifficultyOf(number)} target={target:F2} greedy={bestReport.greedyWinRate:F2} random={bestReport.randomWinRate:F2}");
            }

            catalog.levels = entries;
            catalog.endlessSettings = Tiers[Tiers.Length - 1].settings;
            EditorUtility.SetDirty(catalog);
            return log.ToString();
        }
    }
}
