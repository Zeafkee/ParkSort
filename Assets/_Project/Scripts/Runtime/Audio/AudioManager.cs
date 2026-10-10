using System;
using UnityEngine;

namespace ParkSort.Audio
{
    public enum Sfx
    {
        Tap = 0,
        Drive,
        Bump,
        Park,
        Wait,
        GarageDone,
        GarageOpen,
        Reveal,
        Click,
        Coin,
        Hint,
        Win,
        Lose,
    }

    [Serializable]
    public struct SfxEntry
    {
        public Sfx id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
        public float minInterval;
        [Range(0f, 0.3f)] public float pitchJitter;
    }

    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private SfxEntry[] effects = new SfxEntry[0];
        [SerializeField] private AudioClip music;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.3f;
        [SerializeField] private int voiceCount = 8;

        private AudioSource[] _voices;
        private AudioSource _musicSource;
        private SfxEntry[] _byId;
        private float[] _lastPlayed;
        private int _nextVoice;

        private void Awake()
        {
            Instance = this;

            int count = Enum.GetValues(typeof(Sfx)).Length;
            _byId = new SfxEntry[count];
            _lastPlayed = new float[count];
            for (int i = 0; i < count; i++) _lastPlayed[i] = float.NegativeInfinity;
            for (int i = 0; i < effects.Length; i++) _byId[(int)effects[i].id] = effects[i];

            _voices = new AudioSource[voiceCount];
            for (int i = 0; i < voiceCount; i++)
            {
                _voices[i] = gameObject.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
            }

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.clip = music;
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.volume = musicVolume;
        }

        private void OnEnable() => Save.SaveSystem.Changed += ApplySettings;
        private void OnDisable() => Save.SaveSystem.Changed -= ApplySettings;
        private void Start() => ApplySettings();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Play(Sfx id, float volumeScale = 1f)
        {
            if (Instance != null) Instance.PlayInternal(id, volumeScale);
        }

        private void ApplySettings()
        {
            if (music == null) return;
            bool on = Save.SaveSystem.MusicOn;
            if (on && !_musicSource.isPlaying) _musicSource.Play();
            else if (!on && _musicSource.isPlaying) _musicSource.Stop();
        }

        private void PlayInternal(Sfx id, float volumeScale)
        {
            if (!Save.SaveSystem.SoundOn) return;

            int index = (int)id;
            SfxEntry entry = _byId[index];
            if (entry.clip == null) return;

            float now = Time.unscaledTime;
            if (now - _lastPlayed[index] < entry.minInterval) return;
            _lastPlayed[index] = now;

            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;

            voice.pitch = 1f + UnityEngine.Random.Range(-entry.pitchJitter, entry.pitchJitter);
            voice.volume = entry.volume * volumeScale;
            voice.clip = entry.clip;
            voice.Play();
        }
    }
}
