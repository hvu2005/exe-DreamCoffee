using DreamCafe.Core.MVC;
using DreamCafe.Data;
using UnityEngine;

namespace DreamCafe.Gameplay.CraftingStation
{
    /// <summary>
    /// Renders the crafting station as a colored square.
    /// Beige = Idle (tap me), Green = Ready (crafted item waiting to be served).
    /// TODO Phase 4: add DOTween pulse feedback on tap; animate Ready → Idle on serve.
    /// </summary>
    public sealed class CraftingStationView : ViewBase
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer statusIndicator;

        [Header("Colors")]
        [SerializeField] private Color colorIdle  = new(0.85f, 0.75f, 0.55f);
        [SerializeField] private Color colorReady = new(0.2f,  0.85f, 0.35f);

        public override void Render(IModel model)
        {
            var m = (CraftingStationModel)model;
            if (body != null)
                body.color = colorIdle;
            if (statusIndicator != null)
                statusIndicator.color = m.Status == CraftingStationStatus.Ready ? colorReady : colorIdle * 0.6f;
        }
    }
}
