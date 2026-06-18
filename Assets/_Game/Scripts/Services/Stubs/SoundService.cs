using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Stub sound service. Logs play/stop calls to the console.
    /// Wired: EventBus.OrderServed → Play("serve_ding") in GameBootstrap.
    /// TODO: Implement AudioSource pool, Addressable audio clips, volume channels.
    /// </summary>
    public sealed class SoundService : ISoundService
    {
        public void Init(ServiceContext ctx) => Debug.Log("[SoundService] Initialized (stub).");
        public void Shutdown() => Debug.Log("[SoundService] Shutdown.");
        public void Play(string soundId) => Debug.Log($"[SoundService] ♪ Play: {soundId}");
        public void Stop(string soundId) => Debug.Log($"[SoundService] ■ Stop: {soundId}");
    }
}
