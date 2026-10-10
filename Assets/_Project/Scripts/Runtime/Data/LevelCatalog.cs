using System;
using ParkSort.Core;
using UnityEngine;

namespace ParkSort.Data
{
    [Serializable]
    public sealed class LevelEntry
    {
        public LevelDef level;
        public float greedyWinRate;
        public float randomWinRate;
        public string caption;
        public LevelDifficulty difficulty;
        public string featureTitle;
        public Sprite featureIcon;
    }

    [CreateAssetMenu(menuName = "ParkSort/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public LevelEntry[] levels = new LevelEntry[0];
        public GeneratorSettings endlessSettings = GeneratorSettings.Default;

        public int Count => levels.Length;
    }
}
