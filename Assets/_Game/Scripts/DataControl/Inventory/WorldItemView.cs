using DreamCafe.Core.Pooling;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Presentation cho 1 InventoryItem ngoài thế giới (vd: bao nguyên liệu trên kệ/quầy).
    /// Prefab dùng chung cho mọi loại nguyên liệu — sprite/màu được gán runtime qua SetItem().
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WorldItemView : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetItem(InventoryItem item)
        {
            spriteRenderer.sprite = item != null ? item.Icon : null;
            spriteRenderer.color = item != null ? item.Tint : Color.white;
        }

        public void OnSpawned() { }

        public void OnDespawned() => SetItem(null);
    }
}
