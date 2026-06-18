using DreamCafe.Gameplay.Customer.States;

namespace DreamCafe.Gameplay.Customer
{
    public sealed class CustomerStateMachine
    {
        private ICustomerState _current;
        private readonly CustomerController _owner;

        public CustomerStateMachine(CustomerController owner) => _owner = owner;

        public void Enter(ICustomerState state)
        {
            _current?.Exit(_owner);
            _current = state;
            _current.Enter(_owner);
        }

        public void Tick(float dt) => _current?.Tick(_owner, dt);

        public ICustomerState Current => _current;
    }
}
