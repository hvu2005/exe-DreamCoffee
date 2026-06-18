using DreamCafe.Core.EventBus;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Services.Time
{
    /// <summary>
    /// Tracks in-game day cycle. 3 minutes real-time = 1 game day (configurable via DayLengthSeconds).
    /// Tick() must be called from GameBootstrap.Update() for deterministic timing.
    /// TODO Phase 3: populate DayEnded summary fields from EconomyService/CustomerService.
    /// </summary>
    public sealed class TimeService : ITimeService
    {
        private IEventBus _events;
        private float _elapsed;

        public int CurrentDay { get; private set; }
        public float DayProgress => DayLengthSeconds > 0f ? Mathf.Clamp01(_elapsed / DayLengthSeconds) : 0f;
        public float DayLengthSeconds { get; set; } = 180f;
        public bool IsDayRunning { get; private set; }

        public void Init(ServiceContext ctx)
        {
            _events = ctx.Events;
            Debug.Log("[TimeService] Initialized. Day length: 180s.");
        }

        public void Shutdown()
        {
            IsDayRunning = false;
            Debug.Log("[TimeService] Shutdown.");
        }

        public void StartDay()
        {
            CurrentDay++;
            _elapsed = 0f;
            IsDayRunning = true;
            _events.Publish(new DayStarted(CurrentDay));
            Debug.Log($"[TimeService] Day {CurrentDay} started.");
        }

        public void Tick(float dt)
        {
            if (!IsDayRunning) return;
            _elapsed += dt;
            if (_elapsed >= DayLengthSeconds)
            {
                IsDayRunning = false;
                _events.Publish(new DayEnded(CurrentDay, 0f, 0, 0));
                Debug.Log($"[TimeService] Day {CurrentDay} ended.");
            }
        }
    }
}
