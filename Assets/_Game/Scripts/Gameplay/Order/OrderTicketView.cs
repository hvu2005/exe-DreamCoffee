using DreamCafe.Core.MVC;
using DreamCafe.Data;
using TMPro;
using UnityEngine;

namespace DreamCafe.Gameplay.Order
{
    /// <summary>
    /// Renders an order ticket as a colored square with a world-space item name label.
    /// Yellow = Pending, Green = Ready (crafted, waiting to serve).
    /// itemLabel is a TextMeshPro (world-space) child — wired via Phase5Setup or manually.
    /// </summary>
    public sealed class OrderTicketView : ViewBase
    {
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private TextMeshPro     itemLabel;

        [Header("Status Colors")]
        [SerializeField] private Color colorPending = new(1f, 0.85f, 0.1f);
        [SerializeField] private Color colorReady   = new(0.2f, 0.9f, 0.3f);

        public override void Render(IModel model)
        {
            var m = (OrderTicketModel)model;
            if (background != null)
                background.color = m.Status == OrderStatus.Ready ? colorReady : colorPending;
            if (itemLabel != null)
                itemLabel.text = string.IsNullOrEmpty(m.ItemName) ? m.ItemId : m.ItemName;
        }
    }
}
