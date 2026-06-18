using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Stub inventory service. Infinite stock — always returns true for consume/has checks.
    /// TODO: Implement stock management, expiry, out-of-stock reputation penalty, daily market.
    /// </summary>
    public sealed class InventoryService : IInventoryService
    {
        public void Init(ServiceContext ctx) => Debug.Log("[InventoryService] Initialized (stub — infinite stock).");
        public void Shutdown() => Debug.Log("[InventoryService] Shutdown.");
        public int GetStock(string ingredientId) => 999;
        public bool ConsumeIngredients(string[] ingredientIds) => true;
        public bool HasIngredients(string[] ingredientIds) => true;
    }
}
