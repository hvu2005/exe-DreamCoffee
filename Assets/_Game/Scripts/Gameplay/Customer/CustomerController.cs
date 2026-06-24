using System;
using DreamCafe.Core.EventBus;
using DreamCafe.Core.MVC;
using DreamCafe.Core.Pooling;
using DreamCafe.Core.Services;
using DreamCafe.Data;
using DreamCafe.Gameplay.Customer.States;
using DreamCafe.Gameplay.Patience;
using DreamCafe.Services.Customer;
using UnityEngine;

namespace DreamCafe.Gameplay.Customer
{
    /// <summary>
    /// Drives a single customer NPC: initialises the model, runs the FSM each frame,
    /// and bridges pool lifecycle (IPoolable) with ServiceContext injection (ControllerBase).
    /// Implements ICustomerSpawnable so CustomerService (Services layer) can call Initialize
    /// without a Services→Gameplay dependency — the interface lives in Services, we implement up.
    /// </summary>
    [RequireComponent(typeof(CustomerView))]
    public sealed class CustomerController : ControllerBase, ICustomerSpawnable
    {
        public CustomerModel Model { get; private set; }
        public CustomerView  View  { get; private set; }
        public CustomerStateMachine FSM { get; private set; }
        public ServiceContext Context => Ctx;

        /// <summary>Exposes protected Subscribe&lt;T&gt; to states (which are not subclasses).</summary>
        public void AddSubscription<T>(Action<T> handler) where T : IEvent => Subscribe<T>(handler);

        private void Awake()
        {
            View = GetComponent<CustomerView>();
            Model = new CustomerModel();
            FSM = new CustomerStateMachine(this);
        }

        /// <summary>
        /// Called by CustomerService immediately after pool-spawn.
        /// </summary>
        public void Initialize(ServiceContext ctx, CustomerData data, bool isTakeaway, string customerId)
        {
            Bind(ctx);
            Model.Reset();
            Model.CustomerId = customerId;
            Model.Data = data;
            Model.Type = data.customerType;
            Model.IsTakeaway = isTakeaway;
            Model.PatienceStrategy = new LinearPatienceStrategy(data.patienceSeconds);

            View.Render(Model);
            View.PlaySpawnAnim();
            FSM.Enter(WaitingForSeatState.Instance);
        }

        /// <summary>
        /// Called by CustomerService once a table seat is assigned.
        /// </summary>
        public void AssignSeat(Vector3 worldPos, int tableIndex)
        {
            transform.position = worldPos;
            Model.AssignedTableIndex = tableIndex;
            FSM.Enter(SeatedState.Instance);
        }

        /// <summary>
        /// Called by CustomerService for takeaway customers (no seat needed).
        /// </summary>
        public void AssignTakeawayQueue(Vector3 worldPos)
        {
            transform.position = worldPos;
            Model.AssignedTableIndex = -1;
            FSM.Enter(SeatedState.Instance); // same patience logic; table = none
        }

        private void Update()
        {
            if (Model == null || Ctx == null) return;
            FSM.Tick(Time.deltaTime);
        }

        // ── IPoolable ─────────────────────────────────────────────────────────

        public override void OnSpawned()
        {
            // Initialization happens via Initialize(); nothing extra needed here.
        }

        public override void OnDespawned()
        {
            Unbind();
            Model.Reset();
        }
    }
}
