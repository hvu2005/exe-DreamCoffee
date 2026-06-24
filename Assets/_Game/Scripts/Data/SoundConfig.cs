using TMPro;
using UnityEngine;

namespace DreamCafe.Data
{
    /// <summary>
    /// Maps string keys to AudioClips. Place one at Resources/SoundConfig.asset.
    /// SoundService loads it on Init and calls AudioSource.PlayOneShot per key.
    /// Built-in keys: "serve_ding", "craft_complete", "customer_arrive", "customer_leave".
    /// Leave clip = null to fall back to console log (silent but no error).
    /// </summary>
    [CreateAssetMenu(fileName = "SoundConfig", menuName = "DreamCafé/Sound Config")]
    public sealed class SoundConfig : ScriptableObject
    {
        [System.Serializable]
        public struct SoundEntry
        {
            public string    key;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [SerializeField] private SoundEntry[] sounds = System.Array.Empty<SoundEntry>();

        public bool TryGet(string key, out AudioClip clip, out float volume)
        {
            for (int i = 0; i < sounds.Length; i++)
            {
                if (sounds[i].key == key)
                {
                    clip   = sounds[i].clip;
                    volume = sounds[i].volume > 0f ? sounds[i].volume : 1f;
                    return true;
                }
            }
            clip = null; volume = 1f; return false;
        }
    }
}
