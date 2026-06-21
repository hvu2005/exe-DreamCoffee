using DreamCafe.Core.MVC;
using DreamCafe.Data;
using UnityEngine;

namespace DreamCafe.Gameplay.Order
{
    /// <summary>
    /// Renders an order ticket as a colored square.
    /// Yellow = Pending (waiting to be crafted), Green = Ready (crafted, waiting to serve).
    /// TODO Phase 4: show item icon, add DOTween bounce on Ready.
    /// </summary>
    public sealed class OrderTicketView : ViewBase
    {
        [SerializeField] private SpriteRenderer background;

        [Header("Status Colors")]
        [SerializeField] private Color colorPending = new(1f, 0.85f, 0.1f);
        [SerializeField] private Color colorReady   = new(0.2f, 0.9f, 0.3f);

        public override void Render(IModel model)
        {
            var m = (OrderTicketModel)model;
            if (background == null) return;
            background.color = m.Status == OrderStatus.Ready ? colorReady : colorPending;
        }
    }
}
