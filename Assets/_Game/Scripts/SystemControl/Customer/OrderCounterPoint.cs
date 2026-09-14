using System;
using UnityEngine;

namespace DreamCafe.SystemControl.Customer
{
    /// <summary>
    /// Đánh dấu các điểm đứng trước quầy order để AI khách hàng tìm đến khi gọi món.
    /// Cùng dạng bookkeeping với <see cref="DreamCafe.SystemControl.Decor.DecorSlot"/> nhưng tách riêng
    /// vì đây là điểm đứng tạm thời (chỉ giữ trong lúc gọi món), không phải nội thất có thể mua/nâng cấp.
    /// </summary>
    public sealed class OrderCounterPoint : MonoBehaviour
    {
        [SerializeField, Tooltip("Các điểm đứng trước quầy để khách tìm đến gọi món")]
        private Transform[] _standPoints = Array.Empty<Transform>();

        private bool[] _occupiedFlags = Array.Empty<bool>();

        private void Awake()
        {
            _occupiedFlags = new bool[_standPoints.Length];
        }

        /// <summary>Tìm điểm đứng trống đầu tiên. Trả về null nếu quầy đang đầy hoặc không có điểm đứng.</summary>
        public Transform GetFreeStandPoint(out int index)
        {
            for (int i = 0; i < _standPoints.Length; i++)
            {
                if (!_occupiedFlags[i] && _standPoints[i] != null)
                {
                    index = i;
                    return _standPoints[i];
                }
            }

            index = -1;
            return null;
        }

        /// <summary>Đánh dấu điểm đứng đang có khách hoặc vừa được trả lại.</summary>
        public void SetStandOccupied(int index, bool occupied)
        {
            if (index >= 0 && index < _occupiedFlags.Length)
            {
                _occupiedFlags[index] = occupied;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            if (_standPoints == null) return;

            foreach (var stand in _standPoints)
            {
                if (stand != null)
                {
                    Gizmos.DrawWireSphere(stand.position, 0.2f);
                }
            }
        }
    }
}
