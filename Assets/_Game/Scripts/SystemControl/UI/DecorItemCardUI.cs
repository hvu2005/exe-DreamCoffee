using System;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.Decor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.UI
{
    /// <summary>
    /// UI hiển thị một thẻ món nội thất (Decor Item Card) trong bảng chọn/mua nội thất.
    /// Hiển thị Icon, Tên, Chỉ số tăng trưởng (Rep, MPS, Ghế, Buff) và Nút thao tác (Mua / Trang bị / Đang dùng).
    /// </summary>
    public sealed class DecorItemCardUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _themeText;
        [SerializeField] private TMP_Text _statsText;
        [SerializeField] private Button _actionButton;
        [SerializeField] private Image _actionButtonBg;
        [SerializeField] private TMP_Text _actionButtonText;

        public void Bind(DecorItem item, DecorSlot slot, DecorController decor, CurrencyController currency, Action onActionCompleted)
        {
            if (item == null) return;

            if (_iconImage != null)
            {
                _iconImage.sprite = item.Icon;
                _iconImage.enabled = item.Icon != null;
            }

            if (_nameText != null)
            {
                _nameText.text = item.DisplayName;
            }

            if (_themeText != null)
            {
                _themeText.text = $"Cấp {item.Tier} • {item.Theme}";
            }

            if (_statsText != null)
            {
                var sb = new System.Text.StringBuilder();
                if (item.ReputationBonus > 0) sb.Append($"+{item.ReputationBonus} Danh tiếng  ");
                if (item.MoneyPerSecondBonus > 0) sb.Append($"+{item.MoneyPerSecondBonus:0.#}/s Tiền  ");
                if (item.SeatingCapacity > 0) sb.Append($"{item.SeatingCapacity} Chỗ ngồi  ");
                if (item.PatienceBonusPercent > 0) sb.Append($"+{Mathf.RoundToInt(item.PatienceBonusPercent * 100)}% Kiên nhẫn  ");
                if (item.TipChanceBonus > 0) sb.Append($"+{Mathf.RoundToInt(item.TipChanceBonus * 100)}% Tip");
                _statsText.text = sb.ToString().Trim();
            }

            bool isEquippedHere = slot != null && slot.CurrentDecorItem != null && slot.CurrentDecorItem.Id == item.Id;
            bool isUnlocked = decor != null && decor.IsUnlocked(item.Id);

            if (_actionButton != null)
            {
                _actionButton.onClick.RemoveAllListeners();

                if (isEquippedHere)
                {
                    if (_actionButtonBg != null) _actionButtonBg.color = new Color(0.18f, 0.65f, 0.35f); // Xanh ngọc lục bảo
                    if (_actionButtonText != null) _actionButtonText.text = "ĐANG DÙNG";
                    _actionButton.interactable = false;
                }
                else if (isUnlocked)
                {
                    if (_actionButtonBg != null) _actionButtonBg.color = new Color(0.20f, 0.50f, 0.85f); // Xanh dương
                    if (_actionButtonText != null) _actionButtonText.text = "TRANG BỊ";
                    _actionButton.interactable = true;
                    _actionButton.onClick.AddListener(() =>
                    {
                        if (slot != null && decor != null)
                        {
                            decor.TryEquipDecor(slot.SlotId, item.Id);
                            onActionCompleted?.Invoke();
                        }
                    });
                }
                else
                {
                    // Chưa mở khóa -> Nút Mua
                    bool canAfford = currency != null && currency.CurrentMoney >= item.Price && currency.Reputation >= item.RequiredReputation;
                    if (_actionButtonBg != null)
                    {
                        _actionButtonBg.color = canAfford ? new Color(0.88f, 0.50f, 0.12f) : new Color(0.38f, 0.38f, 0.42f);
                    }

                    if (_actionButtonText != null)
                    {
                        _actionButtonText.text = $"MUA {item.Price:N0} đ";
                    }

                    _actionButton.interactable = canAfford;
                    _actionButton.onClick.AddListener(() =>
                    {
                        if (slot != null && decor != null && currency != null)
                        {
                            if (decor.TryUnlockDecor(item.Id, currency))
                            {
                                decor.TryEquipDecor(slot.SlotId, item.Id);
                                onActionCompleted?.Invoke();
                            }
                        }
                    });
                }
            }
        }
    }
}
