using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DreamCafe.SystemControl.Decor
{
    /// <summary>
    /// Đại diện cho một cụm khu vực mở rộng quán cà phê:
    /// - Bị che phủ bởi Sprite hình chữ nhật (dễ dàng xoay Z và co giãn scale theo mặt bằng sàn).
    /// - Giá mở khóa linh hoạt tự động tăng theo số cụm đã mở.
    /// - Hiển thị huy hiệu ổ khóa kèm tiêu đề và giá tiền tự động cập nhật thời gian thực.
    /// - Phân biệt giữa kéo di chuyển camera và bấm mở khóa.
    /// </summary>
    public sealed class ExpansionMilestoneItem : MonoBehaviour, IPointerClickHandler
    {
        [Header("Thông tin Cụm Khu Vực")]
        [SerializeField, Min(1), Tooltip("Thứ tự định danh của cụm")]
        private int _milestoneIndex = 1;

        [SerializeField, Tooltip("Tên hiển thị của khu vực")]
        private string _displayName = "Khu Vực Mở Rộng";

        [SerializeField, Min(0), Tooltip("Giá tiền mở khóa hiện tại (VNĐ)")]
        private int _unlockPrice = 400000;

        [SerializeField, Min(0), Tooltip("Điểm danh tiếng thưởng khi mở")]
        private int _reputationBonus = 1000;

        [Header("Trực quan & Đối tượng")]
        [SerializeField, Tooltip("Container chứa sàn và các vật thể nội thất khi mở")]
        private GameObject _contentRoot;

        [SerializeField, Tooltip("Sprite hình vuông che phủ khi đang khóa (dễ dàng xoay Z và co giãn kích thước)")]
        private SpriteRenderer _overlayRenderer;

        [SerializeField, Tooltip("Danh sách các sprite hình vuông bổ sung nếu cụm có mặt bằng phức tạp")]
        private List<SpriteRenderer> _extraOverlayRenderers = new();

        [SerializeField, Tooltip("Huy hiệu ổ khóa hiển thị giá tiền")]
        private GameObject _lockBadge;

        [SerializeField, Tooltip("Text hiển thị tên đợt trên huy hiệu")]
        private TextMeshPro _badgeTitle;

        [SerializeField, Tooltip("Text hiển thị giá tiền trên huy hiệu")]
        private TextMeshPro _badgePrice;

        [Header("Trạng thái")]
        [SerializeField] private bool _isUnlocked = false;

        public static event Action<ExpansionMilestoneItem> MilestoneTapped;

        public int MilestoneIndex { get => _milestoneIndex; set => _milestoneIndex = value; }
        public string DisplayName { get => _displayName; set => _displayName = value; }
        public int UnlockPrice { get => _unlockPrice; set => _unlockPrice = value; }
        public int ReputationBonus { get => _reputationBonus; set => _reputationBonus = value; }
        public bool IsUnlocked { get => _isUnlocked; set => _isUnlocked = value; }
        public GameObject ContentRoot => _contentRoot;
        public SpriteRenderer OverlayRenderer => _overlayRenderer;
        public GameObject LockBadge => _lockBadge;

        private Vector3 _badgeOriginalLocalPos;

        private void Awake()
        {
            if (_lockBadge != null)
            {
                _badgeOriginalLocalPos = _lockBadge.transform.localPosition;
            }
        }

        private void Start()
        {
            ApplyVisualState(_isUnlocked);
        }

        private void Update()
        {
            // Hiệu ứng nhấp nhô nhẹ cho huy hiệu ổ khóa khi đang khóa
            if (!_isUnlocked && _lockBadge != null && _lockBadge.activeSelf)
            {
                float yOffset = Mathf.Sin(Time.time * 2.5f + _milestoneIndex * 0.8f) * 0.08f;
                _lockBadge.transform.localPosition = _badgeOriginalLocalPos + new Vector3(0f, yOffset, 0f);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Bỏ qua nếu người chơi vừa thực hiện kéo camera hoặc đang drag
            if (CameraController.WasDragAction || eventData.dragging) return;

            if (!_isUnlocked)
            {
                MilestoneTapped?.Invoke(this);
            }
        }

        private void OnMouseUpAsButton()
        {
            if (CameraController.WasDragAction) return;

            if (!_isUnlocked)
            {
                MilestoneTapped?.Invoke(this);
            }
        }

        private static readonly System.Globalization.CultureInfo s_culture = new System.Globalization.CultureInfo("vi-VN");

        /// <summary>
        /// Cập nhật giá tiền linh hoạt trên UI của cụm này.
        /// </summary>
        public void SetCurrentPrice(int price)
        {
            _unlockPrice = price;

            if (_badgePrice != null)
            {
                _badgePrice.text = price.ToString("N0", s_culture);
            }
        }

        /// <summary>
        /// Cập nhật hiển thị theo trạng thái khóa/mở.
        /// </summary>
        public void ApplyVisualState(bool unlocked)
        {
            _isUnlocked = unlocked;

            if (_contentRoot != null)
            {
                _contentRoot.SetActive(unlocked);
            }

            SetOverlaysActive(!unlocked);

            if (_lockBadge != null)
            {
                _lockBadge.SetActive(!unlocked);
            }

            if (!unlocked)
            {
                if (_badgeTitle != null)
                {
                    _badgeTitle.text = string.IsNullOrEmpty(_displayName) ? "MỞ RỘNG KHÔNG GIAN" : _displayName.ToUpper();
                }

                if (_badgePrice != null)
                {
                    _badgePrice.text = _unlockPrice.ToString("N0", s_culture);
                }
            }
        }

        private void SetOverlaysActive(bool active)
        {
            if (_overlayRenderer != null)
            {
                _overlayRenderer.gameObject.SetActive(active);
            }

            if (_extraOverlayRenderers != null)
            {
                for (int i = 0; i < _extraOverlayRenderers.Count; i++)
                {
                    if (_extraOverlayRenderers[i] != null)
                    {
                        _extraOverlayRenderers[i].gameObject.SetActive(active);
                    }
                }
            }
        }

        /// <summary>
        /// Mở khóa kèm hiệu ứng mờ dần lớp phủ hình chữ nhật (Fade-out).
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

            if (_contentRoot != null)
            {
                _contentRoot.SetActive(true);
            }

            var activeRenderers = new List<SpriteRenderer>();
            if (_overlayRenderer != null && _overlayRenderer.gameObject.activeSelf)
            {
                activeRenderers.Add(_overlayRenderer);
            }

            if (_extraOverlayRenderers != null)
            {
                for (int i = 0; i < _extraOverlayRenderers.Count; i++)
                {
                    if (_extraOverlayRenderers[i] != null && _extraOverlayRenderers[i].gameObject.activeSelf)
                    {
                        activeRenderers.Add(_extraOverlayRenderers[i]);
                    }
                }
            }

            if (activeRenderers.Count > 0)
            {
                float duration = 0.5f;
                float elapsed = 0f;
                var initialColors = new List<Color>();
                for (int i = 0; i < activeRenderers.Count; i++)
                {
                    initialColors.Add(activeRenderers[i].color);
                }

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / duration;
                    for (int i = 0; i < activeRenderers.Count; i++)
                    {
                        Color c = initialColors[i];
                        activeRenderers[i].color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, progress));
                    }
                    yield return null;
                }

                for (int i = 0; i < activeRenderers.Count; i++)
                {
                    activeRenderers[i].gameObject.SetActive(false);
                    activeRenderers[i].color = initialColors[i];
                }
            }

            onComplete?.Invoke();
        }
    }
}
