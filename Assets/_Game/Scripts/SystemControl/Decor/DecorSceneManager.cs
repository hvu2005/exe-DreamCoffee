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

        [Header("Giao diện chọn nội thất")]
        [SerializeField] private DecorPlacementPanel _placementPanel;

        private DecorController _decorController;
        private CurrencyController _currencyController;

        public DecorPlacementPanel PlacementPanel { get => _placementPanel; set => _placementPanel = value; }

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

        private void Start()
        {
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
                _decorController.DecorEquipped += OnDecorEquipped;
                _decorController.DecorUnequipped += OnDecorUnequipped;
                _decorController.ZoneUnlocked += OnZoneUnlocked;

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
                _decorController = null;
            }
        }

        /// <summary>
        /// Đồng bộ toàn bộ trạng thái hiển thị của các Zone và Slot theo dữ liệu Controller.
        /// </summary>
        public void RefreshAll()
        {
            if (_decorController == null) return;

            // 1. Cập nhật các Zone
            foreach (var zoneView in _zones)
            {
                if (zoneView == null) continue;
                bool isUnlocked = _decorController.IsZoneUnlocked(zoneView.ZoneId);
                var zoneData = _decorController.GetZone(zoneView.ZoneId);
                zoneView.SetUnlocked(isUnlocked, zoneData);
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
        }

        private void OnDecorEquipped(string slotId, DecorItem item)
        {
            var slot = _slots.Find(s => s != null && s.SlotId == slotId);
            if (slot != null)
            {
                slot.DisplayItem(item);
            }
        }

        private void OnDecorUnequipped(string slotId)
        {
            var slot = _slots.Find(s => s != null && s.SlotId == slotId);
            if (slot != null)
            {
                slot.ClearDisplay();
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

            _decorController.TryUnlockZone(zoneId, _currencyController);
        }

        // Editor helper
        public void CollectSceneObjects()
        {
            _slots.Clear();
            _slots.AddRange(GetComponentsInChildren<DecorSlot>(true));

            _zones.Clear();
            _zones.AddRange(GetComponentsInChildren<ExpansionZoneView>(true));

            if (_placementPanel == null)
            {
                _placementPanel = FindFirstObjectByType<DecorPlacementPanel>(FindObjectsInactive.Include);
            }
        }
    }
}
