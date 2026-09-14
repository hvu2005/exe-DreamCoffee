using System;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.Customer;
using DreamCafe.SystemControl.Decor;
using UnityEngine;

namespace DreamCafe.Gameplay.Customer
{
    /// <summary>
    /// Toàn bộ dữ liệu cần thiết để cấu hình một khách hàng ngay sau khi được spawn từ pool.
    /// Được <see cref="DreamCafe.SystemControl.Customer.CustomerSceneManager"/> chuẩn bị sẵn trước khi gọi
    /// <see cref="CustomerController.Configure"/>, để CustomerController không cần tự tìm quầy/ghế trống.
    /// </summary>
    public sealed class CustomerSpawnContext
    {
        public CustomerItem Definition;
        public RecipeItem Order;

        public OrderCounterPoint Counter;
        public int StandIndex;
        public Transform StandPoint;

        public DecorSlot Seat;
        public int SeatIndex;
        public Transform SeatPoint;

        public Transform ExitPoint;
        public Action<CustomerController> OnFinished;
    }
}
