using System;
using System.Collections;
using DreamCafe.DataControl;
using TMPro;
using UnityEngine;

namespace DreamCafe.SystemControl.Decor
{
    /// <summary>
    /// Quản lý hiển thị trực quan của một phân vùng không gian mở rộng (Expansion Zone View).
    /// Hỗ trợ bạt che thi công (Overlay), huy hiệu ổ khóa hiển thị giá tiền tùy biến theo danh sách,
    /// và tương tác chạm để mở popup xác nhận mở khóa.
    /// </summary>
    public sealed class ExpansionZoneView : MonoBehaviour
    {
        [Header("Định danh phân vùng")]
        [SerializeField] private ExpansionZoneId _zoneId = ExpansionZoneId.Starter_Zone1;

        [Header("Trực quan phân vùng")]
        [SerializeField, Tooltip("Container chứa sàn, tường, thảm, slot của khu vực này")]
        private GameObject _zoneContent;

        [SerializeField, Tooltip("Hình ảnh layout bạt che phủ khu vực khi đang bị khóa")]
        private SpriteRenderer _overlayRenderer;

        [SerializeField, Tooltip("Huy hiệu ổ khóa thế giới hiển thị giá tiền")]
        private GameObject _lockBadge;

        [SerializeField, Tooltip("Text tên khu vực trên huy hiệu")]
        private TextMeshPro _badgeTitle;

        [SerializeField, Tooltip("Text giá tiền trên huy hiệu")]
        private TextMeshPro _badgePrice;

        [Header("Các Decor Slot thuộc khu vực này")]
        [SerializeField] private DecorSlot[] _slots = Array.Empty<DecorSlot>();

        public static event Action<ExpansionZoneId> ZoneUnlockRequested;

        public ExpansionZoneId ZoneId => _zoneId;
        public DecorSlot[] Slots => _slots;
        public GameObject ZoneContent => _zoneContent;
        public SpriteRenderer OverlayRenderer => _overlayRenderer;
        public GameObject LockBadge => _lockBadge;

        private Vector3 _badgeOriginalLocalPos;
        private bool _isUnlocked = true;
        private LockedZoneConfig _currentConfig;

        private void Awake()
        {
            if (_lockBadge != null)
            {
                _badgeOriginalLocalPos = _lockBadge.transform.localPosition;
            }
        }

        private void Update()
        {
            // Hiệu ứng nhấp nhô nhẹ cho huy hiệu ổ khóa
            if (!_isUnlocked && _lockBadge != null && _lockBadge.activeSelf)
            {
                float yOffset = Mathf.Sin(Time.time * 2.5f) * 0.08f;
                _lockBadge.transform.localPosition = _badgeOriginalLocalPos + new Vector3(0f, yOffset, 0f);
            }
        }

        private void OnMouseDown()
        {
            if (!_isUnlocked)
            {
                ZoneUnlockRequested?.Invoke(_zoneId);
            }
        }

        /// <summary>
        /// Áp dụng cấu hình khóa từ danh sách LockedZoneConfig do người dùng thiết lập.
        /// </summary>
        public void ApplyLockConfig(LockedZoneConfig config, bool isUnlocked)
        {
            _currentConfig = config;
            _isUnlocked = isUnlocked;

            if (_zoneContent != null)
            {
                _zoneContent.SetActive(isUnlocked);
            }

            if (_overlayRenderer != null)
            {
                _overlayRenderer.gameObject.SetActive(!isUnlocked);
                if (!isUnlocked && config != null && config.customOverlaySprite != null)
                {
                    _overlayRenderer.sprite = config.customOverlaySprite;
                }
            }

            if (_lockBadge != null)
            {
                _lockBadge.SetActive(!isUnlocked);
            }

            if (!isUnlocked && config != null)
            {
                if (_badgeTitle != null)
                {
                    _badgeTitle.text = config.displayName;
                }

                if (_badgePrice != null)
                {
                    _badgePrice.text = config.unlockPrice.ToString("N0", s_culture);
                }
            }

            // Bật / tắt các slot nội thất
            foreach (var slot in _slots)
            {
                if (slot != null)
                {
                    slot.gameObject.SetActive(isUnlocked);
                }
            }
        }

        private static readonly System.Globalization.CultureInfo s_culture = new System.Globalization.CultureInfo("vi-VN");

        /// <summary>
        /// Cập nhật trạng thái mở khóa cơ bản (tương thích ngược).
        /// </summary>
        public void SetUnlocked(bool unlocked, ExpansionZoneData zoneData = null)
        {
            _isUnlocked = unlocked;

            if (_zoneContent != null)
            {
                _zoneContent.SetActive(unlocked);
            }

            if (_overlayRenderer != null)
            {
                _overlayRenderer.gameObject.SetActive(!unlocked);
            }

            if (_lockBadge != null)
            {
                _lockBadge.SetActive(!unlocked);
            }

            if (!unlocked && zoneData != null)
            {
                if (_badgeTitle != null) _badgeTitle.text = zoneData.ZoneName;
                if (_badgePrice != null) _badgePrice.text = zoneData.UnlockPrice.ToString("N0", s_culture);
            }

            foreach (var slot in _slots)
            {
                if (slot != null)
                {
                    slot.gameObject.SetActive(unlocked);
                }
            }
        }

        /// <summary>
        /// Mở khóa kèm hiệu ứng mờ dần bạt che (Fade-out animation).
        /// </summary>
        public void UnlockWithAnimation(Action onComplete = null)
        {
            StartCoroutine(AnimateUnlockRoutine(onComplete));
        }

        private IEnumerator AnimateUnlockRoutine(Action onComplete)
        {
            _isUnlocked = true;

            if (_lockBadge != null)
            {
                _lockBadge.SetActive(false);
            }

            if (_zoneContent != null)
            {
                _zoneContent.SetActive(true);
            }

            foreach (var slot in _slots)
            {
                if (slot != null)
                {
                    slot.gameObject.SetActive(true);
                }
            }

            // Fade out overlay nếu có
            if (_overlayRenderer != null && _overlayRenderer.gameObject.activeSelf)
            {
                float duration = 0.6f;
                float elapsed = 0f;
                Color startColor = _overlayRenderer.color;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                    _overlayRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                    yield return null;
                }

                _overlayRenderer.gameObject.SetActive(false);
                _overlayRenderer.color = startColor;
            }

            onComplete?.Invoke();
        }
    }
}
