using System.Collections.Generic;
using DreamCafe.DataControl;
using DreamCafe.SystemControl.UI;
using UnityEngine;

namespace DreamCafe.SystemControl.Decor
{
    /// <summary>
    /// Điều phối viên trong Scene kết nối các DecorSlot và ExpansionZoneView với DecorController và CurrencyController.
    /// </summary>
    public sealed class DecorSceneManager : MonoBehaviour
    {
        [Header("Danh sách quản lý trong Scene")]
        [SerializeField] private List<DecorSlot> _slots = new();
        [SerializeField] private List<ExpansionZoneView> _zones = new();

        [Header("Giao diện chọn nội thất & Mở rộng")]
        [SerializeField] private DecorPlacementPanel _placementPanel;
        [SerializeField] private ZoneUnlockPopupUI _unlockPopup;

        [Header("Danh sách khu vực khóa (Tự do tùy chỉnh trong Inspector)")]
        [SerializeField] private List<LockedZoneConfig> _lockedZones = new()
        {
            new LockedZoneConfig { zoneId = ExpansionZoneId.Lounge_Zone2, unlockPrice = 400000, displayName = "Sảnh Trong Nhà", reputationBonus = 1000 },
            new LockedZoneConfig { zoneId = ExpansionZoneId.OutdoorPatio_Zone3, unlockPrice = 1000000, displayName = "Sân Hiên Ngoài Trời", reputationBonus = 2500 }
        };

        private DecorController _decorController;
        private CurrencyController _currencyController;

        public DecorPlacementPanel PlacementPanel { get => _placementPanel; set => _placementPanel = value; }
        public ZoneUnlockPopupUI UnlockPopup { get => _unlockPopup; set => _unlockPopup = value; }
        public IReadOnlyList<LockedZoneConfig> LockedZones => _lockedZones;

        public bool TryGetLockedConfig(ExpansionZoneId zoneId, out LockedZoneConfig config)
        {
            config = _lockedZones.Find(z => z != null && z.zoneId == zoneId);
            return config != null;
        }

        private void OnEnable()
        {
            DecorSlot.SlotTapped += OnSlotTapped;
            ExpansionZoneView.ZoneUnlockRequested += OnZoneUnlockRequested;
        }

        private void OnDisable()
        {
            DecorSlot.SlotTapped -= OnSlotTapped;
            ExpansionZoneView.ZoneUnlockRequested -= OnZoneUnlockRequested;
            UnbindController();
        }

        private void Awake()
        {
            ValidateOrCollectSlots();
        }

        private void Start()
        {
            ValidateOrCollectSlots();

            var systems = GameSystemsProvider.Instance;
            if (systems != null)
            {
                Bind(systems.Decor, systems.Currency);
            }
        }

        public void Bind(DecorController decor, CurrencyController currency)
        {
            UnbindController();

            _decorController = decor;
            _currencyController = currency;

            if (_decorController != null)
            {
                // Đồng bộ các khu vực cần khóa theo danh sách Inspector chỉ định
                foreach (var cfg in _lockedZones)
                {
                    if (cfg != null)
                    {
                        _decorController.OverrideZoneLocked(cfg.zoneId, true);
                    }
                }

                _decorController.DecorEquipped += OnDecorEquipped;
                _decorController.DecorUnequipped += OnDecorUnequipped;
                _decorController.ZoneUnlocked += OnZoneUnlocked;
                _decorController.Changed += RefreshAll;

                RefreshAll();
            }
        }

        private void UnbindController()
        {
            if (_decorController != null)
            {
                _decorController.DecorEquipped -= OnDecorEquipped;
                _decorController.DecorUnequipped -= OnDecorUnequipped;
                _decorController.ZoneUnlocked -= OnZoneUnlocked;
                _decorController.Changed -= RefreshAll;
                _decorController = null;
            }
        }

        /// <summary>
        /// Đồng bộ toàn bộ trạng thái hiển thị của các Zone và Slot theo dữ liệu Controller.
        /// </summary>
        public void RefreshAll()
        {
            if (_decorController == null || _decorController.GetAllItems().Length == 0) return;
            ValidateOrCollectSlots();

            // 1. Cập nhật các Zone
            foreach (var zoneView in _zones)
            {
                if (zoneView == null) continue;

                if (TryGetLockedConfig(zoneView.ZoneId, out var lockCfg))
                {
                    bool isUnlocked = _decorController.IsZoneUnlocked(zoneView.ZoneId);
                    zoneView.ApplyLockConfig(lockCfg, isUnlocked);
                }
                else
                {
                    zoneView.SetUnlocked(true);
                }
            }

            // 2. Cập nhật các Slot
            foreach (var slot in _slots)
            {
                if (slot == null) continue;
                var equipped = _decorController.GetEquippedItem(slot.SlotId);
                if (equipped != null)
                {
                    slot.DisplayItem(equipped);
                }
                else
                {
                    slot.ClearDisplay();
                }
            }

            RebuildGrid();
        }

        /// <summary>
        /// Dựng lại bản đồ ô gạch sau mỗi lần bố cục nội thất đổi — NavMesh khoét lỗ theo đó nên
        /// khách đi vòng qua bàn mới kê mà không phải bake lại.
        /// </summary>
        private void RebuildGrid() => ShopGrid.Instance?.RebuildFromScene();

        private DecorSlot FindSlot(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return null;

            // 1. Tìm trong danh sách hiện tại
            var slot = _slots.Find(s => s != null && s.SlotId == slotId);
            if (slot != null) return slot;

            // 2. Tự phục hồi: Quét lại toàn bộ DecorSlot trong Scene
            ValidateOrCollectSlots();
            slot = _slots.Find(s => s != null && s.SlotId == slotId);
            if (slot != null) return slot;

            // 3. Tìm toàn cục nếu slot nằm ngoài ShopLayout
            var allSceneSlots = FindObjectsByType<DecorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in allSceneSlots)
            {
                if (s != null && s.SlotId == slotId)
                {
                    if (!_slots.Contains(s)) _slots.Add(s);
                    return s;
                }
            }

            return null;
        }

        private void OnDecorEquipped(string slotId, DecorItem item)
        {
            var slot = FindSlot(slotId);
            if (slot != null)
            {
                slot.DisplayItem(item);
                RebuildGrid();
            }
            else
            {
                Debug.LogWarning($"[DecorSceneManager] Không tìm thấy slot với Id '{slotId}' để hiển thị '{item?.DisplayName}'.");
            }
        }

        private void OnDecorUnequipped(string slotId)
        {
            var slot = FindSlot(slotId);
            if (slot != null)
            {
                slot.ClearDisplay();
                RebuildGrid();
            }
        }

        private void OnZoneUnlocked(ExpansionZoneId zoneId)
        {
            var zoneView = _zones.Find(z => z != null && z.ZoneId == zoneId);
            if (zoneView != null)
            {
                var zoneData = _decorController.GetZone(zoneId);
                zoneView.SetUnlocked(true, zoneData);
            }
        }

        private void OnSlotTapped(DecorSlot slot)
        {
            if (_decorController == null || _currencyController == null || slot == null) return;

            Debug.Log($"[DecorSceneManager] Đã chọn slot '{slot.SlotId}' (Category: {slot.AllowedCategory}).");

            if (_placementPanel == null)
            {
                _placementPanel = FindFirstObjectByType<DecorPlacementPanel>(FindObjectsInactive.Include);
            }

            if (_placementPanel != null)
            {
                _placementPanel.OpenForSlot(slot, _decorController, _currencyController);
            }
        }

        private void OnZoneUnlockRequested(ExpansionZoneId zoneId)
        {
            if (_decorController == null || _currencyController == null) return;

            var zoneView = _zones.Find(z => z != null && z.ZoneId == zoneId);

            if (_unlockPopup == null)
            {
                _unlockPopup = FindFirstObjectByType<ZoneUnlockPopupUI>(FindObjectsInactive.Include);
            }

            if (_unlockPopup != null && TryGetLockedConfig(zoneId, out var config))
            {
                _unlockPopup.Show(config, _currencyController, _decorController, onSuccess: () =>
                {
                    if (zoneView != null)
                    {
                        zoneView.UnlockWithAnimation(() => RebuildGrid());
                    }
                    else
                    {
                        RebuildGrid();
                    }
                });
            }
            else
            {
                _decorController.TryUnlockZone(zoneId, _currencyController);
            }
        }

        public void ValidateOrCollectSlots()
        {
            _slots.RemoveAll(s => s == null);
            _zones.RemoveAll(z => z == null);

            var childSlots = GetComponentsInChildren<DecorSlot>(true);
            foreach (var s in childSlots)
            {
                if (s != null && !_slots.Contains(s))
                {
                    _slots.Add(s);
                }
            }

            var childZones = GetComponentsInChildren<ExpansionZoneView>(true);
            foreach (var z in childZones)
            {
                if (z != null && !_zones.Contains(z))
                {
                    _zones.Add(z);
                }
            }

            if (_placementPanel == null)
            {
                _placementPanel = FindFirstObjectByType<DecorPlacementPanel>(FindObjectsInactive.Include);
            }
        }

        // Editor helper
        public void CollectSceneObjects()
        {
            _slots.Clear();
            _zones.Clear();
            ValidateOrCollectSlots();
        }
    }
}
