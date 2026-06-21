namespace DreamCafe.Core.Interfaces
{
    /// <summary>
    /// Implemented by any scene object that reacts to a player tap/click.
    /// PlayerInputRouter raycasts and calls OnTap() on the hit object — no knowledge of concrete types.
    /// </summary>
    public interface ITappable
    {
        void OnTap();
    }
}
