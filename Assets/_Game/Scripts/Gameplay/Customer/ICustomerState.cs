namespace DreamCafe.Gameplay.Customer
{
    /// <summary>
    /// Một trạng thái đơn lẻ trong FSM của khách hàng. Mỗi trạng thái tự quản lý hành vi của mình,
    /// cho phép bổ sung các trạng thái mới (Waiting, Queueing, ...) ở các iteration sau mà không cần
    /// sửa lại các trạng thái đã có.
    /// </summary>
    public interface ICustomerState
    {
        /// <summary>Gọi một lần khi khách hàng chuyển vào trạng thái này.</summary>
        void Enter(CustomerController ctx);

        /// <summary>Gọi mỗi frame trong khi khách hàng đang ở trạng thái này.</summary>
        void Tick(CustomerController ctx, float deltaTime);

        /// <summary>Gọi một lần khi khách hàng rời khỏi trạng thái này.</summary>
        void Exit(CustomerController ctx);
    }
}
