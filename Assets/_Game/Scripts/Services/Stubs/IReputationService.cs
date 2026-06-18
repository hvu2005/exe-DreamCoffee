using DreamCafe.Core.Services;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Café reputation level and score. Affects spawn rate, VIP chance, and unlocks. Stub for prototype.
    /// TODO: Implement reputation levels, unlock gates, VIP spawn weight curve.
    /// </summary>
    public interface IReputationService : IService
    {
        float ReputationScore { get; }
        int ReputationLevel { get; }
        void AddReputation(float delta);
    }
}
