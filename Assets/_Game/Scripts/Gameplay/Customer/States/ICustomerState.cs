namespace DreamCafe.Gameplay.Customer.States
{
    public interface ICustomerState
    {
        void Enter(CustomerController owner);
        void Tick(CustomerController owner, float dt);
        void Exit(CustomerController owner);
    }
}
