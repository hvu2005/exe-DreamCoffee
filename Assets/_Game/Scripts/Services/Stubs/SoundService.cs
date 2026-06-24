using DreamCafe.Core.Services;
using DreamCafe.Data;
using UnityEngine;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Plays AudioClips via a dedicated AudioSource GameObject created at runtime.
    /// Loads SoundConfig from Resources/SoundConfig.asset; missing keys fall back to console log.
    /// Stop() halts all sounds (single-source prototype — fine for demo builds).
    /// TODO: Multi-channel AudioSource pool, volume channels (SFX/BGM), Addressables support.
    /// </summary>
    public sealed class SoundService : ISoundService
    {
        private AudioSource _source;
        private SoundConfig _config;

        public void Init(ServiceContext ctx)
        {
            _config = Resources.Load<SoundConfig>("SoundConfig");
            if (_config == null)
                Debug.LogWarning("[SoundService] Resources/SoundConfig.asset not found — audio will log-only. Run Tools > DreamCafé > Create Sound Config Asset.");

            var go = new GameObject("[SoundService]");
            Object.DontDestroyOnLoad(go);
            _source             = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            Debug.Log("[SoundService] Initialized.");
        }

        public void Shutdown()
        {
            if (_source != null)
                Object.Destroy(_source.gameObject);
            _source = null;
            Debug.Log("[SoundService] Shutdown.");
        }

        public void Play(string soundId)
        {
            if (_config != null && _source != null
                && _config.TryGet(soundId, out var clip, out var vol)
                && clip != null)
            {
                _source.PlayOneShot(clip, vol);
            }
            else
            {
                Debug.Log($"[SoundService] ♪ Play: {soundId}");
            }
        }

        public void Stop(string soundId)
        {
            _source?.Stop();
            Debug.Log($"[SoundService] ■ Stop: {soundId}");
        }
    }
}
