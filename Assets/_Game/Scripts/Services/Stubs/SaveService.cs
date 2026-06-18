using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Stub save service. Logs calls but does not persist anything.
    /// TODO: Implement JSON serialisation to PlayerPrefs/Application.persistentDataPath, cloud sync.
    /// </summary>
    public sealed class SaveService : ISaveService
    {
        public void Init(ServiceContext ctx) => Debug.Log("[SaveService] Initialized (stub).");
        public void Shutdown() => Debug.Log("[SaveService] Shutdown.");
        public void Save() => Debug.Log("[SaveService] Save (stub — no data written).");
        public void Load() => Debug.Log("[SaveService] Load (stub — no data read).");
    }
}
