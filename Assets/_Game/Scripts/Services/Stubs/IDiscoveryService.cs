using DreamCafe.Core.Services;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Recipe discovery via ingredient combination experiments. Stub — implement post-prototype.
    /// TODO: Implement mix-and-match mechanic, research log, discovery unlock flow.
    /// </summary>
    public interface IDiscoveryService : IService
    {
        bool TryDiscover(string[] ingredientIds, out string discoveredItemId);
    }
}
