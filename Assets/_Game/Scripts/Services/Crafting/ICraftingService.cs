using DreamCafe.Core.Services;

namespace DreamCafe.Services.Crafting
{
    /// <summary>
    /// Tap-to-complete crafting. A single tap on a station = oldest pending order instantly crafted.
    /// No progress bar, no energy (dropped permanently).
    /// </summary>
    public interface ICraftingService : IService
    {
        bool TryCraft(string stationId);
        void RegisterStation(ICraftingStation station);
        void UnregisterStation(ICraftingStation station);
    }
}
