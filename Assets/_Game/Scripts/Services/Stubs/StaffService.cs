using DreamCafe.Core.Services;
using DreamCafe.Data;
using UnityEngine;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Stub staff service. Returns 0 for all staff counts.
    /// TODO: Implement role slots, gacha recruitment, level-up via training books, salary deduction.
    /// </summary>
    public sealed class StaffService : IStaffService
    {
        public void Init(ServiceContext ctx) => Debug.Log("[StaffService] Initialized (stub).");
        public void Shutdown() => Debug.Log("[StaffService] Shutdown.");
        public int GetStaffCount(StaffRole role) => 0;
    }
}
