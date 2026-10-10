using System;
using UnityEngine;

namespace ParkSort.Save
{
    [Serializable]
    public sealed class SaveData
    {
        public int level;
        public int coins;
        public int hints = 3;
        public int extraSlots = 3;
        public int undos = 3;
        public int ownedSkins = 1;
        public int skin;
        public bool soundOn = true;
        public bool musicOn = true;
    }

    public static class SaveSystem
    {
        private const string Key = "parksort_save_v1";
        private static SaveData _data;

        public static event Action Changed;

        public static SaveData Data
        {
            get
            {
                if (_data == null) Load();
                return _data;
            }
        }

        public static bool SoundOn
        {
            get => Data.soundOn;
            set { Data.soundOn = value; Save(); }
        }

        public static bool MusicOn
        {
            get => Data.musicOn;
            set { Data.musicOn = value; Save(); }
        }

        public static void Load()
        {
            string json = PlayerPrefs.GetString(Key, string.Empty);
            _data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void Reset()
        {
            _data = new SaveData();
            Save();
        }
    }
}
