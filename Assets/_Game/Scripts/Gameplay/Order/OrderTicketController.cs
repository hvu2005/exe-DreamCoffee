using DreamCafe.Core.MVC;
using DreamCafe.DataControl;
using UnityEngine;

namespace DreamCafe.Gameplay.Order
{
    /// <summary>
    /// Điều khiển bong bóng order hiển thị phía trên đầu khách hàng trong lúc gọi món.
    /// Được pool bởi <see cref="DreamCafe.Core.Pooling.PoolManager"/> qua <c>PoolKey.OrderTicket</c>.
    /// </summary>
    public sealed class OrderTicketController : ControllerBase
    {
        [SerializeField] private OrderTicketView _view;

        /// <summary>Hiển thị tên món khách vừa gọi.</summary>
        public void Show(RecipeItem recipe)
        {
            string displayName = recipe != null ? recipe.DisplayName : string.Empty;
            _view?.Render(new OrderTicketViewModel(displayName, isReady: false));
        }
    }
}
