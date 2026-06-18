using DreamCafe.Core.MVC;
using DreamCafe.Data;
using DreamCafe.Gameplay.Patience;

namespace DreamCafe.Gameplay.Customer
{
    /// <summary>
    /// Runtime state for a single customer. POCO — no MonoBehaviour, no bus, no services.
    /// Owned and mutated by CustomerController; read by CustomerView for rendering.
    /// TODO Phase 2: add orderId, itemId, eating timer fields.
    /// </summary>
    public sealed class CustomerModel : IModel
    {
        public string CustomerId;
        public CustomerData Data;
        public CustomerType Type;
        public CustomerEmotion Emotion;
        public float Patience01 = 1f;
        public bool IsTakeaway;
        public int AssignedTableIndex = -1;
        public CustomerState State;
        public IPatienceStrategy PatienceStrategy;
        public float ReputationFactor = 1f;

        public bool PatienceDepleted => Patience01 <= 0f;

        public void TickPatience(float dt) =>
            Patience01 = PatienceStrategy.Drain(Patience01, dt, Type, ReputationFactor);

        public CustomerEmotion ComputeEmotion() =>
            Patience01 >= 0.6f ? CustomerEmotion.Happy :
            Patience01 >= 0.3f ? CustomerEmotion.Disappointed :
            CustomerEmotion.Angry;

        public void Reset()
        {
            Patience01 = 1f;
            Emotion = CustomerEmotion.Neutral;
            AssignedTableIndex = -1;
            State = CustomerState.Idle;
        }
    }
}
