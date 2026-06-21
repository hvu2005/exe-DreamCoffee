namespace DreamCafe.Services.Crafting
{
    /// <summary>
    /// Implemented by CraftingStationController (Gameplay layer) so CraftingService can
    /// enumerate registered stations without a Services→Gameplay dependency.
    /// </summary>
    public interface ICraftingStation
    {
        string StationId { get; }
    }
}
