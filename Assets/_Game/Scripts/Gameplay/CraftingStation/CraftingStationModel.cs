using DreamCafe.Core.MVC;
using DreamCafe.Data;

namespace DreamCafe.Gameplay.CraftingStation
{
    public sealed class CraftingStationModel : IModel
    {
        public string StationId;
        public CraftingStationStatus Status = CraftingStationStatus.Idle;
        public string ReadyOrderId;

        public void Reset()
        {
            Status = CraftingStationStatus.Idle;
            ReadyOrderId = null;
        }
    }
}
