using System;
using DreamCafe.DataControl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.Brew
{
    /// <summary>
    /// Một ô trong dải "công thức đang thử" ở góc phải (Coffee Beans + Tea + ???).
    /// Ô trống hiện dấu "???"; ô đã có nguyên liệu thì bấm vào để nhấc ra khỏi cốc.
    /// </summary>
    public sealed class BrewMixtureSlotView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private Button button;

        private int _index;
        private Action<int> _onClick;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>Gán vị trí ô trong cốc và callback nhấc nguyên liệu ra.</summary>
        public void Setup(int index, Action<int> onClick)
        {
            _index = index;
            _onClick = onClick;
        }

        /// <summary>Đổ nội dung ô — truyền null nghĩa là ô còn trống ("???").</summary>
        public void Bind(InventoryItem item)
        {
            bool filled = item != null;

            if (nameLabel != null) nameLabel.text = filled ? item.DisplayName : "???";

            if (icon != null)
            {
                icon.sprite = filled ? item.Icon : null;
                icon.color = !filled ? EmptySlot
                    : item.Icon != null ? item.Tint
                    : FilledPlaceholder;
            }

            if (button != null) button.interactable = filled;
        }

        private void HandleClick() => _onClick?.Invoke(_index);

        private static readonly Color EmptySlot = new(0.78f, 0.72f, 0.61f, 0.55f);
        private static readonly Color FilledPlaceholder = new(0.55f, 0.38f, 0.22f);
    }
}
