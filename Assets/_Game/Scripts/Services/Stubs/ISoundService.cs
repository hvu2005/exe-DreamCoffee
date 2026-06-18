using DreamCafe.Core.Services;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Sound playback. All audio calls route through EventBus → this service — never direct AudioSource.
    /// Wired: OrderServed → Play("serve_ding"). Stub — implement post-prototype.
    /// TODO: Implement AudioSource pool, Addressable audio clips, music/sfx volume channels.
    /// </summary>
    public interface ISoundService : IService
    {
        void Play(string soundId);
        void Stop(string soundId);
    }
}
