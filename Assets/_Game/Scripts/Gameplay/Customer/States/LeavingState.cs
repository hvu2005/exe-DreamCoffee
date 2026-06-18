using DreamCafe.Core.EventBus;
using DreamCafe.Core.Pooling;
using DreamCafe.Data;
using DreamCafe.Services.Customer;

namespace DreamCafe.Gameplay.Customer.States
{
    /// <summary>
    /// Terminal state. Publishes CustomerLeft, frees the table, then despawns to pool.
    /// </summary>
    public sealed class LeavingState : ICustomerState
    {
        public static readonly LeavingState Instance = new();

        public void Enter(CustomerController owner)
        {
            owner.Model.State = CustomerState.Leaving;
            owner.View.ShowPatienceBar(false);

            var m = owner.Model;
            bool wasSatisfied = m.Patience01 > 0f;
            m.Emotion = m.ComputeEmotion();

            owner.Context.Events.Publish(new CustomerLeft(
                m.CustomerId,
                wasSatisfied,
                m.Emotion));

            var customerService = owner.Context.Services.Resolve<ICustomerService>();
            customerService.OnCustomerDespawning(m.CustomerId, m.AssignedTableIndex);

            owner.Context.Pool.Despawn(PoolKey.Customer, owner);
        }

        public void Tick(CustomerController owner, float dt) { }
        public void Exit(CustomerController owner) { }
    }
}
