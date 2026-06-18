using DreamCafe.Core.Services;

namespace DreamCafe.Services.Time
{
    /// <summary>
    /// Tracks in-game time: day number, day progress, day length.
    /// Raises DayStarted / DayEnded events. Tick must be called by GameBootstrap each frame.
    /// TODO: Add pause/resume, speed multiplier, and real-time calendar sync.
    /// </summary>
    public interface ITimeService : IService
    {
        int CurrentDay { get; }
        float DayProgress { get; }
        float DayLengthSeconds { get; set; }
        bool IsDayRunning { get; }
        void StartDay();
        void Tick(float dt);
    }
}
