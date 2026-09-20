using System;
using System.Collections.Generic;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.UI;
using UnityEngine;

namespace DreamCafe.SystemControl.Decor
{
    /// <summary>
    /// Quản lý toàn bộ hệ thống mở rộng không gian quán theo cụm khu vực:
    /// - Vào game các cụm chưa sử dụng đều có giá chung (bậc giá khởi điểm 400.000 đ).
    /// - Người chơi tự do chọn mở bất kỳ cụm nào trước.
    /// - Mỗi lần mở 1 cụm, giá để mở các cụm còn lại sẽ tăng lên bậc tiếp theo.
    /// - UI giá trên các cụm còn lại tự động cập nhật linh hoạt thời gian thực.
    /// </summary>
    public sealed class ExpansionMilestoneManager : MonoBehaviour
    {
        [Header("Danh sách các Cụm Mở Rộng (Clusters)")]
        [SerializeField] private List<ExpansionMilestoneItem> _milestones = new();

        [Header("Bảng giá các lần mở (VNĐ)")]
        [SerializeField, Tooltip("Mỗi lần mở bất kỳ 1 cụm, giá các cụm còn lại sẽ chuyển sang bậc giá tiếp theo")]
        private int[] _priceTiers = new int[]
        {
            400000,    // Lần 1: 400k (giá chung ban đầu của tất cả các cụm)
            1000000,   // Lần 2: 1 Triệu
            2500000,   // Lần 3: 2.5 Triệu
            5000000,   // Lần 4: 5 Triệu
            10000000   // Lần 5: 10 Triệu
        };

        [Header("Giao diện Popup mở rộng")]
        [SerializeField] private ZoneUnlockPopupUI _popup;

        [Header("Tiến độ hiện tại")]
        [SerializeField, Tooltip("Số lượng cụm đã mở")]
        private int _unlockedCount = 0;

        private CurrencyController _currencyController;
        private DecorController _decorController;

        public int UnlockedCount => _unlockedCount;
        public IReadOnlyList<ExpansionMilestoneItem> Milestones => _milestones;

        private void OnEnable()
        {
            ExpansionMilestoneItem.MilestoneTapped += OnMilestoneTapped;
        }

        private void OnDisable()
        {
            ExpansionMilestoneItem.MilestoneTapped -= OnMilestoneTapped;
        }

        private void Start()
        {
            CollectMilestones();

            var systems = GameSystemsProvider.Instance;
            if (systems != null)
            {
                _currencyController = systems.Currency;
                _decorController = systems.Decor;
            }

            if (_popup == null)
            {
                _popup = FindFirstObjectByType<ZoneUnlockPopupUI>(FindObjectsInactive.Include);
            }

            RefreshMilestonesVisual();
        }

        public void CollectMilestones()
        {
            _milestones.Clear();
            var items = GetComponentsInChildren<ExpansionMilestoneItem>(true);
            _milestones.AddRange(items);
        }

        /// <summary>
        /// Lấy mức giá hiện tại áp dụng chung cho tất cả các cụm chưa mở.
        /// </summary>
        public int GetCurrentPrice()
        {
            if (_priceTiers == null || _priceTiers.Length == 0) return 400000;

            if (_unlockedCount < _priceTiers.Length)
            {
                return _priceTiers[_unlockedCount];
            }

            // Nếu người chơi mở nhiều hơn số bậc cấu hình, tăng gấp đôi mức giá cuối cùng
            int lastPrice = _priceTiers[_priceTiers.Length - 1];
            int extra = _unlockedCount - _priceTiers.Length + 1;
            return lastPrice * (int)Mathf.Pow(2, extra);
        }

        /// <summary>
        /// Cập nhật giá linh hoạt trên UI badge của tất cả các cụm chưa mở.
        /// </summary>
        public void RefreshAllClusterPrices()
        {
            int currentPrice = GetCurrentPrice();
            for (int i = 0; i < _milestones.Count; i++)
            {
                var m = _milestones[i];
                if (m != null && !m.IsUnlocked)
                {
                    m.SetCurrentPrice(currentPrice);
                }
            }
        }

        public void RefreshMilestonesVisual()
        {
            int count = 0;
            for (int i = 0; i < _milestones.Count; i++)
            {
                if (_milestones[i] != null && _milestones[i].IsUnlocked)
                {
                    count++;
                }
            }
            _unlockedCount = count;

            RefreshAllClusterPrices();
        }

        private void OnMilestoneTapped(ExpansionMilestoneItem item)
        {
            if (item == null || item.IsUnlocked) return;

            var systems = GameSystemsProvider.Instance;
            if (systems != null)
            {
                _currencyController = systems.Currency;
                _decorController = systems.Decor;
            }

            if (_popup == null)
            {
                _popup = FindFirstObjectByType<ZoneUnlockPopupUI>(FindObjectsInactive.Include);
            }

            int currentPrice = GetCurrentPrice();

            if (_popup != null)
            {
                _popup.ShowClusterUnlock(item, currentPrice, _currencyController, () =>
                {
                    OnMilestoneUnlocked(item, currentPrice);
                });
            }
            else
            {
                Debug.LogWarning("[ExpansionMilestoneManager] Chưa gán ZoneUnlockPopupUI trên Canvas!");
            }
        }

        private void OnMilestoneUnlocked(ExpansionMilestoneItem item, int pricePaid)
        {
            if (item == null) return;

            _unlockedCount++;

            // Mở khóa cụm với hiệu ứng mờ dần lớp phủ
            item.UnlockWithAnimation(() =>
            {
                ShopGrid.Instance?.RebuildFromScene();
            });

            // Focus Camera vào cụm vừa mở
            var cam = Camera.main?.GetComponent<CameraController>();
            if (cam != null)
            {
                cam.FocusOn(item.transform.position, 3.2f);
            }

            // Cập nhật giá mới cho tất cả các cụm còn lại ngay lập tức
            RefreshAllClusterPrices();

            Debug.Log($"[ExpansionMilestoneManager] Mở thành công cụm '{item.DisplayName}' với giá {pricePaid.ToString("N0", s_culture)}. Giá cho các cụm còn lại tăng lên: {GetCurrentPrice().ToString("N0", s_culture)}!");
        }

        private static readonly System.Globalization.CultureInfo s_culture = new System.Globalization.CultureInfo("vi-VN");

        /// <summary>
        /// Cho phép thiết lập số cụm đã mở (phục hồi save hoặc test trong editor).
        /// </summary>
        public void SetUnlockedCount(int count)
        {
            _unlockedCount = count;
            RefreshAllClusterPrices();
        }
    }
}
