using System;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.Decor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.UI
{
    /// <summary>
    /// Popup giao diện xác nhận mở rộng không gian quán cà phê:
    /// - Hiển thị tên khu vực, mức giá mở khóa (đọc từ LockedZoneConfig), danh tiếng thưởng thêm.
    /// - Kiểm tra số dư tiền mặt của người chơi thời gian thực.
    /// - Trừ tiền, mở khóa khu vực và kích hoạt hiệu ứng trực quan khi người chơi xác nhận.
    /// </summary>
    public sealed class ZoneUnlockPopupUI : MonoBehaviour
    {
        [Header("Khung cửa sổ")]
        [SerializeField] private GameObject _window;

        [Header("Thông tin khu vực")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _balanceText;
        [SerializeField] private TextMeshProUGUI _reputationBonusText;

        [Header("Các nút bấm")]
        [SerializeField] private Button _unlockButton;
        [SerializeField] private Image _unlockButtonBg;
        [SerializeField] private TextMeshProUGUI _unlockButtonText;
        [SerializeField] private Button _closeButton;

        private LockedZoneConfig _currentConfig;
        private ExpansionZoneId _currentZoneId;
        private CurrencyController _currency;
        private DecorController _decor;
        private Action _onSuccess;

        private static readonly System.Globalization.CultureInfo s_culture = new System.Globalization.CultureInfo("vi-VN");

        private readonly Color _colorSufficient = new Color(0.18f, 0.65f, 0.32f); // Xanh lá
        private readonly Color _colorInsufficient = new Color(0.50f, 0.52f, 0.55f); // Xám

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Hide);
            }

            if (_unlockButton != null)
            {
                _unlockButton.onClick.AddListener(OnUnlockClicked);
            }

            Hide();
        }

        /// <summary>
        /// Mở popup xác nhận mở rộng khu vực.
        /// </summary>
        public void Show(LockedZoneConfig config, CurrencyController currency, DecorController decor, Action onSuccess = null)
        {
            if (config == null) return;

            _currentConfig = config;
            _currentZoneId = config.zoneId;
            _currency = currency;
            _decor = decor;
            _onSuccess = onSuccess;

            if (_titleText != null)
            {
                _titleText.text = $"MỞ RỘNG {config.displayName.ToUpper()}";
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = $"Khai mở thêm không gian mới để bố trí thêm các bàn ghế và trang thiết bị đón thêm khách hàng!";
            }

            if (_costText != null)
            {
                _costText.text = config.unlockPrice.ToString("N0", s_culture);
            }

            if (_reputationBonusText != null)
            {
                _reputationBonusText.text = $"+{config.reputationBonus:N0} Danh tiếng";
            }

            UpdateBalanceAndButton();

            gameObject.SetActive(true);
            if (_window != null)
            {
                _window.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_window != null)
            {
                _window.SetActive(false);
            }
            gameObject.SetActive(false);
        }

        private void UpdateBalanceAndButton()
        {
            if (_currency == null || _currentConfig == null) return;

            double currentMoney = _currency.CurrentMoney;

            if (_balanceText != null)
            {
                _balanceText.text = $"Số dư của quán: <color=#F7C844>{currentMoney.ToString("N0", s_culture)}</color>";
            }

            bool canAfford = currentMoney >= _currentConfig.unlockPrice;

            if (_unlockButton != null)
            {
                _unlockButton.interactable = canAfford;
            }

            if (_unlockButtonBg != null)
            {
                _unlockButtonBg.color = canAfford ? _colorSufficient : _colorInsufficient;
            }

            if (_unlockButtonText != null)
            {
                _unlockButtonText.text = canAfford ? "MỞ RỘNG NGAY" : "CHƯA ĐỦ TIỀN";
            }
        }

        private void OnUnlockClicked()
        {
            if (_currentMilestone != null)
            {
                HandleMilestoneUnlock();
                return;
            }

            if (_currency == null || _currentConfig == null) return;

            if (_currency.CurrentMoney < _currentConfig.unlockPrice)
            {
                Debug.LogWarning("[ZoneUnlockPopupUI] Số dư không đủ để mở khóa khu vực này.");
                return;
            }

            // 1. Trừ tiền
            bool spent = _currency.SpendMoney(_currentConfig.unlockPrice);
            if (!spent) return;

            // 2. Thưởng danh tiếng
            if (_currentConfig.reputationBonus > 0)
            {
                _currency.AddReputation(_currentConfig.reputationBonus);
            }

            // 3. Đánh dấu mở khóa trên DecorController
            if (_decor != null)
            {
                _decor.TryUnlockZone(_currentZoneId, null);
            }

            Debug.Log($"[ZoneUnlockPopupUI] Đã mở rộng thành công '{_currentConfig.displayName}' với giá {_currentConfig.unlockPrice:N0} đ!");

            _onSuccess?.Invoke();
            Hide();
        }

        private ExpansionMilestoneItem _currentMilestone;
        private int _currentClusterPrice;

        /// <summary>
        /// Mở popup mở khóa cụm không gian với giá chung linh hoạt.
        /// </summary>
        public void ShowClusterUnlock(ExpansionMilestoneItem cluster, int currentPrice, CurrencyController currency, Action onSuccess)
        {
            if (cluster == null) return;

            _currentMilestone = cluster;
            _currentClusterPrice = currentPrice;
            _currentMilestone.SetCurrentPrice(currentPrice);
            _currentConfig = null;
            _currency = currency;
            _onSuccess = onSuccess;

            if (_titleText != null)
            {
                _titleText.text = string.IsNullOrEmpty(cluster.DisplayName) ? "MỞ RỘNG KHÔNG GIAN" : cluster.DisplayName.ToUpper();
            }

            if (_costText != null)
            {
                _costText.text = currentPrice.ToString("N0", s_culture);
            }

            if (_reputationBonusText != null)
            {
                _reputationBonusText.text = $"+{cluster.ReputationBonus:N0} Danh tiếng";
            }

            double currentMoney = (currency != null) ? currency.CurrentMoney : 0;
            if (_balanceText != null)
            {
                _balanceText.text = $"Số dư của quán: <color=#F7C844>{currentMoney.ToString("N0", s_culture)}</color>";
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = "Khai mở thêm cụm không gian mới để bố trí thêm bàn ghế và thiết bị đón thêm khách hàng!";
            }

            bool canAfford = currentMoney >= currentPrice;
            if (_unlockButton != null) _unlockButton.interactable = canAfford;
            if (_unlockButtonBg != null) _unlockButtonBg.color = canAfford ? _colorSufficient : _colorInsufficient;
            if (_unlockButtonText != null) _unlockButtonText.text = canAfford ? "MỞ RỘNG NGAY" : "CHƯA ĐỦ TIỀN";

            gameObject.SetActive(true);
            if (_window != null)
            {
                _window.SetActive(true);
            }
        }

        /// <summary>
        /// Mở popup theo tiến trình Milestone (Lần 1: 400k, Lần 2: 1 Tr...).
        /// </summary>
        public void ShowMilestone(ExpansionMilestoneItem milestone, bool isPrerequisiteMet, int requiredPriorMilestone, CurrencyController currency, Action onSuccess)
        {
            if (milestone == null) return;

            _currentMilestone = milestone;
            _currentClusterPrice = milestone.UnlockPrice;
            _currentConfig = null;
            _currency = currency;
            _onSuccess = onSuccess;

            if (_titleText != null)
            {
                _titleText.text = $"MỞ RỘNG (LẦN {milestone.MilestoneIndex})";
            }

            if (_costText != null)
            {
                _costText.text = milestone.UnlockPrice.ToString("N0", s_culture);
            }

            if (_reputationBonusText != null)
            {
                _reputationBonusText.text = $"+{milestone.ReputationBonus:N0} Danh tiếng";
            }

            double currentMoney = (currency != null) ? currency.CurrentMoney : 0;
            if (_balanceText != null)
            {
                _balanceText.text = $"Số dư của quán: <color=#F7C844>{currentMoney.ToString("N0", s_culture)}</color>";
            }

            if (!isPrerequisiteMet)
            {
                if (_descriptionText != null)
                {
                    _descriptionText.text = $"<color=#FF5555>Cần hoàn thành mở rộng Lần {requiredPriorMilestone} trước khi mở đợt này!</color>";
                }

                if (_unlockButton != null) _unlockButton.interactable = false;
                if (_unlockButtonBg != null) _unlockButtonBg.color = _colorInsufficient;
                if (_unlockButtonText != null) _unlockButtonText.text = $"CẦN MỞ LẦN {requiredPriorMilestone} TRƯỚC";
            }
            else
            {
                if (_descriptionText != null)
                {
                    _descriptionText.text = $"Khai mở thêm không gian mới để bố trí thêm bàn ghế và thiết bị đón thêm khách hàng!";
                }

                bool canAfford = currentMoney >= milestone.UnlockPrice;
                if (_unlockButton != null) _unlockButton.interactable = canAfford;
                if (_unlockButtonBg != null) _unlockButtonBg.color = canAfford ? _colorSufficient : _colorInsufficient;
                if (_unlockButtonText != null) _unlockButtonText.text = canAfford ? "MỞ RỘNG NGAY" : "CHƯA ĐỦ TIỀN";
            }

            gameObject.SetActive(true);
            if (_window != null)
            {
                _window.SetActive(true);
            }
        }

        private void HandleMilestoneUnlock()
        {
            if (_currentMilestone == null) return;

            int priceToPay = _currentClusterPrice > 0 ? _currentClusterPrice : _currentMilestone.UnlockPrice;

            if (_currency != null)
            {
                if (_currency.CurrentMoney < priceToPay)
                {
                    Debug.LogWarning("[ZoneUnlockPopupUI] Số dư không đủ.");
                    return;
                }

                _currency.SpendMoney(priceToPay);
                if (_currentMilestone.ReputationBonus > 0)
                {
                    _currency.AddReputation(_currentMilestone.ReputationBonus);
                }
            }

            _onSuccess?.Invoke();
            Hide();
        }
    }
}
