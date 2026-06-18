using DreamCafe.Core.Services;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Game state persistence. Stub — implement post-prototype.
    /// TODO: Implement PlayerPrefs/JSON save, cloud sync, save slots.
    /// </summary>
    public interface ISaveService : IService
    {
        void Save();
        void Load();
    }
}
