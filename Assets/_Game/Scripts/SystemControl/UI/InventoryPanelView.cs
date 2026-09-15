using System;
using System.Collections.Generic;
using DreamCafe.Core.Database;
using DreamCafe.DataControl;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.UI
{
    /// <summary>
    /// Panel kho: bấm E để bật/tắt. Mỗi lần mở sẽ hỏi DatabaseManager lấy repository nguyên liệu
    /// rồi đổ toàn bộ item ra lưới. Script nằm trên node gốc (luôn active) còn <see cref="window"/>
    /// mới là phần bật/tắt — tắt node gốc thì Update() không chạy, không bấm E mở lại được.
    /// </summary>
    public sealed class InventoryPanelView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject window;
        [SerializeField] private Transform grid;
        [SerializeField] private InventorySlotView slotPrefab;
        [SerializeField] private TMP_Text summaryLabel;
        [SerializeField] private Button closeButton;
        [SerializeField, Tooltip("Bỏ trống thì dùng DatabaseManager.Instance.")]
        private DatabaseManager database;

        [Header("Input")]
        [SerializeField] private Key toggleKey = Key.E;

        private readonly List<InventorySlotView> _slots = new();

        public bool IsOpen => window != null && window.activeSelf;

        /// <summary>Lấy lúc dùng chứ không phải lúc Awake — thứ tự Awake giữa các object không đảm bảo.</summary>
        private DatabaseManager Database => database != null ? database : DatabaseManager.Instance;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (window != null) window.SetActive(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame) Toggle();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            window.SetActive(true);
            Refresh();
        }

        public void Close() => window.SetActive(false);

        public void Refresh()
        {
            var db = Database;
            var repository = db != null ? db.Get<ScriptableInventoryItemRepository>() : null;
            if (repository == null)
            {
                Debug.LogWarning("[InventoryPanel] Chưa có ScriptableInventoryItemRepository trong DatabaseManager.");
            }

            var items = repository != null ? repository.GetAllItems() : Array.Empty<InventoryItem>();

            foreach (var slot in _slots)
                if (slot != null) Destroy(slot.gameObject);
            _slots.Clear();

            // Số lượng thật nằm ở kho runtime (đã bị pha chế/bán hàng trừ đi), không phải trên asset
            // định nghĩa — asset chỉ giữ số tồn khởi điểm.
            var inventory = GameSystemsProvider.Instance != null ? GameSystemsProvider.Instance.Inventory : null;

            foreach (var item in items)
            {
                if (item == null) continue;
                var slot = Instantiate(slotPrefab, grid);
                slot.Bind(item, inventory != null ? inventory.GetQuantity(item.Id) : item.Quantity);
                _slots.Add(slot);
            }

            if (summaryLabel != null) summaryLabel.text = $"{_slots.Count} ingredients";
        }
    }
}
