using DreamCafe.Core.MVC;
using TMPro;
using UnityEngine;

namespace DreamCafe.Gameplay.Order
{
    /// <summary>
    /// Presentation thuần cho bong bóng order phía trên đầu khách hàng. Không truy cập service/bus.
    /// </summary>
    public sealed class OrderTicketView : ViewBase
    {
        [Header("Tham chiếu hiển thị")]
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private TMP_Text _itemLabel;

        [Header("Màu theo trạng thái")]
        [SerializeField] private Color _colorPending = Color.white;
        [SerializeField] private Color _colorReady = Color.green;

        public override void Render(IModel model)
        {
            if (model is not OrderTicketViewModel vm) return;

            if (_itemLabel != null)
            {
                _itemLabel.text = vm.DisplayName;
            }

            if (_background != null)
            {
                _background.color = vm.IsReady ? _colorReady : _colorPending;
            }
        }
    }
}
