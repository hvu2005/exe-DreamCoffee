using UnityEngine.AI;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>Kiểm tra NavMeshAgent đã tới đích hay chưa — dùng chung cho các state di chuyển.</summary>
    internal static class NavMeshArrivalUtility
    {
        public static bool HasArrived(NavMeshAgent agent)
        {
            if (agent == null || agent.pathPending) return false;
            return agent.remainingDistance <= agent.stoppingDistance;
        }
    }
}
