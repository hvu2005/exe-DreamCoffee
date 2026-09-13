using System;
using DreamCafe.DataControl;
using TMPro;
using UnityEngine;

namespace DreamCafe.SystemControl.Decor
{
    /// <summary>
    /// Quản lý hiển thị trực quan của một phân vùng không gian mở rộng (Expansion Zone View).
    /// Bật/tắt các cấu trúc sàn/tường khi mở khóa và hiển thị biển báo ổ khóa khi chưa mở.
    /// </summary>
    public sealed class ExpansionZoneView : MonoBehaviour
    {
        [Header("Định danh phân vùng")]
        [SerializeField] private ExpansionZoneId _zoneId = ExpansionZoneId.Starter_Zone1;

        [Header("Trực quan phân vùng")]
        [SerializeField, Tooltip("Container chứa sàn, tường, thảm của khu vực này")]
        private GameObject _zoneContent;

        [SerializeField, Tooltip("Biển báo ổ khóa hoặc rào chắn xuất hiện khi khu vực chưa mở khóa")]
        private GameObject _lockedBarrier;

        [SerializeField, Tooltip("Text hiển thị giá tiền & điều kiện mở khóa trên biển báo")]
        private TextMeshPro _costLabel;

        [Header("Các Decor Slot thuộc khu vực này")]
        [SerializeField] private DecorSlot[] _slots = Array.Empty<DecorSlot>();

        public static event Action<ExpansionZoneId> ZoneUnlockRequested;

        public ExpansionZoneId ZoneId => _zoneId;
        public DecorSlot[] Slots => _slots;

        private void OnMouseDown()
        {
            if (_lockedBarrier != null && _lockedBarrier.activeSelf)
            {
                ZoneUnlockRequested?.Invoke(_zoneId);
            }
        }

        /// <summary>
        /// Cập nhật trạng thái hiển thị mở khóa của khu vực.
        /// </summary>
        public void SetUnlocked(bool unlocked, ExpansionZoneData zoneData = null)
        {
            if (_zoneContent != null)
            {
                _zoneContent.SetActive(unlocked);
            }

            if (_lockedBarrier != null)
            {
                _lockedBarrier.SetActive(!unlocked);
            }

            if (!unlocked && _costLabel != null && zoneData != null)
            {
                _costLabel.text = $"{zoneData.UnlockPrice:N0} đ\n(Rep: {zoneData.RequiredReputation:N0})";
            }

            // Kích hoạt hoặc vô hiệu hóa tương tác của các slot trong khu vực
            foreach (var slot in _slots)
            {
                if (slot != null)
                {
                    slot.gameObject.SetActive(unlocked);
                }
            }
        }
    }
}
