using System.Collections.Generic;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.Decor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.UI
{
    /// <summary>
    /// Bảng giao diện chọn và mua nội thất cố định theo từng khu vực (DecorPlacementPanel).
    /// Khi người chơi nhấn vào bất kỳ ô DecorSlot nào trong quán:
    /// - Hiển thị tên phân loại tương ứng (Tranh tường, Bàn ghế phục vụ, Thiết bị, Cây cảnh sàn, v.v.).
    /// - Liệt kê các mẫu nội thất thuộc phân loại đó với đầy đủ chỉ số buff, giá mua hoặc trạng thái đã sở hữu.
    /// - Cho phép Mua, Trang bị hoặc Gỡ bỏ nội thất khỏi ô.
    /// </summary>
    public sealed class DecorPlacementPanel : MonoBehaviour
    {
        [Header("UI Header")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _subtitleText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _unequipButton;

        [Header("Item Cards Container")]
        [SerializeField] private Transform _cardsContainer;
        [SerializeField] private GameObject _cardTemplate;

        private DecorSlot _targetSlot;
        private DecorController _decorController;
        private CurrencyController _currencyController;
        private readonly List<GameObject> _spawnedCards = new();

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }

            if (_unequipButton != null)
            {
                _unequipButton.onClick.AddListener(OnUnequipClicked);
            }

            if (_cardTemplate != null)
            {
                _cardTemplate.SetActive(false);
            }
        }

        /// <summary>
        /// Mở bảng chọn nội thất ứng với một DecorSlot được nhấp chọn.
        /// </summary>
        public void OpenForSlot(DecorSlot slot, DecorController decor, CurrencyController currency)
        {
            if (slot == null || decor == null) return;

            _targetSlot = slot;
            _decorController = decor;
            _currencyController = currency;

            gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>
        /// Đóng bảng giao diện.
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
            _targetSlot = null;
        }

        /// <summary>
        /// Làm mới nội dung hiển thị trong bảng.
        /// </summary>
        public void Refresh()
        {
            if (_targetSlot == null || _decorController == null) return;

            // 1. Tiêu đề khu vực / phân loại
            if (_titleText != null)
            {
                _titleText.text = GetCategoryTitle(_targetSlot.AllowedCategory);
            }

            if (_subtitleText != null)
            {
                string status = _targetSlot.CurrentDecorItem != null
                    ? $"Đang dùng: <color=#F7C844>{_targetSlot.CurrentDecorItem.DisplayName}</color>"
                    : "<color=#95A5A6>Ô đang trống (Chưa đặt nội thất)</color>";
                _subtitleText.text = $"Vị trí: <b>{_targetSlot.SlotId}</b> • {status}";
            }

            // 2. Nút gỡ bỏ (chỉ hiện khi ô có đặt nội thất)
            if (_unequipButton != null)
            {
                _unequipButton.gameObject.SetActive(_targetSlot.CurrentDecorItem != null);
            }

            // 3. Xóa các card cũ
            foreach (var card in _spawnedCards)
            {
                if (card != null) Destroy(card);
            }
            _spawnedCards.Clear();

            // 4. Sinh danh sách các món nội thất thuộc danh mục này
            var items = _decorController.GetItemsByCategory(_targetSlot.AllowedCategory);
            if (_cardTemplate != null && _cardsContainer != null)
            {
                foreach (var item in items)
                {
                    var cardGo = Instantiate(_cardTemplate, _cardsContainer);
                    cardGo.SetActive(true);
                    var cardUi = cardGo.GetComponent<DecorItemCardUI>();
                    if (cardUi != null)
                    {
                        cardUi.Bind(item, _targetSlot, _decorController, _currencyController, Refresh);
                    }
                    _spawnedCards.Add(cardGo);
                }
            }
        }

        private void OnUnequipClicked()
        {
            if (_targetSlot != null && _decorController != null)
            {
                _decorController.TryUnequipDecor(_targetSlot.SlotId);
                Refresh();
            }
        }

        public static string GetCategoryTitle(DecorCategory category) => category switch
        {
            DecorCategory.WallDecor => "TRANG TRÍ TƯỜNG (TRANH & KỆ)",
            DecorCategory.SeatingSet => "KHU VỰC PHỤC VỤ (BÀN GHẾ & SOFA)",
            DecorCategory.Appliance => "THIẾT BỊ QUÁN (MÁY LẠNH / TỦ BÁNH)",
            DecorCategory.FloorDecor => "CÂY CẢNH SÀN (MONSTERA)",
            DecorCategory.OutdoorPlanter => "BỒN HOA NGOẠI CẢNH (CẨM TÚ CẦU)",
            DecorCategory.CounterStation => "QUẦY PHA CHẾ & THU NGÂN",
            _ => category.ToString().ToUpperInvariant()
        };
    }
}
