using DreamCafe.Core.Services;
using DreamCafe.Data;

namespace DreamCafe.Services.Stubs
{
    /// <summary>
    /// Staff management: roles, assignment, leveling, salary deduction. Stub — implement post-prototype.
    /// TODO: Implement Barista/Waiter/Baker/Cleaner AI, gacha recruitment, training system.
    /// </summary>
    public interface IStaffService : IService
    {
        int GetStaffCount(StaffRole role);
    }
}
