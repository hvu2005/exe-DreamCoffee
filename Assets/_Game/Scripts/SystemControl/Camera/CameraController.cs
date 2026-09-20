using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DreamCafe.SystemControl
{
    /// <summary>
    /// Hệ thống điều khiển Camera 2D Orthographic cho quán cà phê sử dụng New Input System:
    /// - Kéo chuột trái / Touch drag để di chuyển (Pan).
    /// - Cuộn bánh xe chuột / Pinch để phóng to, thu nhỏ (Zoom In/Out).
    /// - Tự động giới hạn toạ độ (Clamp) theo diện tích mặt sàn quán, không làm lộ khoảng trống ngoài mép.
    /// - Bỏ qua tương tác khi thao tác trên các phần tử UI.
    /// - Hỗ trợ Focus lướt mượt tới toạ độ đích (ví dụ khi mở khu vực mới hoặc chọn góc nhìn).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        [Header("Cấu hình Zoom")]
        [SerializeField, Tooltip("Độ phóng to tối đa (cận cảnh từng bàn)")]
        private float _minOrthographicSize = 1.8f;

        [SerializeField, Tooltip("Độ thu nhỏ tối đa (toàn cảnh cả quán)")]
        private float _maxOrthographicSize = 5.2f;

        [SerializeField, Tooltip("Độ nhạy cuộn chuột / pinch zoom")]
        private float _zoomSensitivity = 0.8f;

        [SerializeField, Tooltip("Thời gian trượt mượt khi zoom")]
        private float _zoomSmoothTime = 0.12f;

        [Header("Cấu hình Pan (Di chuyển)")]
        [SerializeField, Tooltip("Tốc độ kéo di chuyển")]
        private float _panSpeed = 1.0f;

        [SerializeField, Tooltip("Thời gian trượt mượt khi di chuyển")]
        private float _panSmoothTime = 0.08f;

        [Header("Giới hạn biên mặt bằng quán (World Bounds)")]
        [SerializeField] private float _boundsMinX = -5.0f;
        [SerializeField] private float _boundsMaxX = 8.5f;
        [SerializeField] private float _boundsMinY = -6.5f;
        [SerializeField] private float _boundsMaxY = 0.5f;

        private Camera _cam;
        private Vector3 _targetPosition;
        private float _targetOrthographicSize;

        private Vector3 _panVelocity;
        private float _zoomVelocity;

        /// <summary>
        /// Cờ báo cho biết lần nhả chuột/touch gần nhất có phải là kết quả của việc KÉO DI CHUYỂN CAMERA hay không.
        /// Dùng để các đối tượng tương tác trong Scene (ExpansionCluster, DecorSlot...) không bị kích hoạt nhầm khi người chơi kéo camera.
        /// </summary>
        public static bool WasDragAction { get; private set; }

        /// <summary>
        /// Đang trong quá trình kéo di chuyển camera.
        /// </summary>
        public static bool IsDraggingCamera { get; private set; }

        private Vector2 _lastDragScreenPos;
        private Vector2 _pressStartScreenPos;
        private bool _isDragging;
        private bool _hasMovedSignificantly;
        private const float DragThresholdPixels = 6f;

        private static readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();

        private bool _isFocusing;
        private Vector3 _focusTargetPos;
        private float _focusTargetZoom;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;

            _targetPosition = transform.position;
            _targetOrthographicSize = _cam.orthographicSize;
        }

        private void Start()
        {
            ClampTargetPosition();
        }

        private void Update()
        {
            if (_cam == null) return;

            HandleInput();
            SmoothUpdate();
        }

        private void HandleInput()
        {
            HandleZoomInput();
            HandlePanInput();
        }

        private void HandleZoomInput()
        {
            // 1. Mouse Scroll Wheel (New Input System)
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scrollY = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > 0.01f && !IsPointerOverUI())
                {
                    _isFocusing = false;
                    // Chuẩn hóa scroll delta (120f là 1 notch chuột thông thường)
                    float normScroll = scrollY / 120f;
                    _targetOrthographicSize -= normScroll * _zoomSensitivity * 0.8f;
                    _targetOrthographicSize = Mathf.Clamp(_targetOrthographicSize, _minOrthographicSize, _maxOrthographicSize);
                    ClampTargetPosition();
                }
            }

            // 2. Mobile Touchscreen Pinch Zoom
            var touchScreen = Touchscreen.current;
            if (touchScreen != null)
            {
                var touches = touchScreen.touches;
                int activeTouchCount = 0;
                int touch0Idx = -1, touch1Idx = -1;

                for (int i = 0; i < touches.Count; i++)
                {
                    if (touches[i].isInProgress)
                    {
                        if (activeTouchCount == 0) touch0Idx = i;
                        else if (activeTouchCount == 1) touch1Idx = i;
                        activeTouchCount++;
                    }
                }

                if (activeTouchCount == 2 && touch0Idx >= 0 && touch1Idx >= 0)
                {
                    _isDragging = false;
                    _isFocusing = false;

                    Vector2 p0 = touches[touch0Idx].position.ReadValue();
                    Vector2 p1 = touches[touch1Idx].position.ReadValue();
                    Vector2 prev0 = p0 - touches[touch0Idx].delta.ReadValue();
                    Vector2 prev1 = p1 - touches[touch1Idx].delta.ReadValue();

                    float currentDist = Vector2.Distance(p0, p1);
                    float prevDist = Vector2.Distance(prev0, prev1);
                    float delta = currentDist - prevDist;

                    _targetOrthographicSize -= delta * 0.005f * _zoomSensitivity;
                    _targetOrthographicSize = Mathf.Clamp(_targetOrthographicSize, _minOrthographicSize, _maxOrthographicSize);
                    ClampTargetPosition();
                }
            }
        }

        private void HandlePanInput()
        {
            // 1. Mobile Touch Drag
            var touchScreen = Touchscreen.current;
            if (touchScreen != null)
            {
                var touches = touchScreen.touches;
                int activeTouchCount = 0;
                int touch0Idx = -1;

                for (int i = 0; i < touches.Count; i++)
                {
                    if (touches[i].isInProgress)
                    {
                        if (activeTouchCount == 0) touch0Idx = i;
                        activeTouchCount++;
                    }
                }

                if (activeTouchCount == 1 && touch0Idx >= 0)
                {
                    var touch = touches[touch0Idx];
                    if (touch.press.wasPressedThisFrame)
                    {
                        if (!IsPointerOverUI())
                        {
                            _isDragging = true;
                            _isFocusing = false;
                            _lastDragScreenPos = touch.position.ReadValue();
                            _pressStartScreenPos = _lastDragScreenPos;
                            _hasMovedSignificantly = false;
                            WasDragAction = false;
                        }
                    }
                    else if (touch.press.isPressed && _isDragging)
                    {
                        Vector2 currentPos = touch.position.ReadValue();
                        Vector2 deltaScreen = currentPos - _lastDragScreenPos;

                        if (Vector2.Distance(currentPos, _pressStartScreenPos) > DragThresholdPixels)
                        {
                            _hasMovedSignificantly = true;
                            WasDragAction = true;
                            IsDraggingCamera = true;
                        }

                        if (deltaScreen.sqrMagnitude > 0.01f)
                        {
                            float worldHeight = _cam.orthographicSize * 2f;
                            float worldUnitsPerPixel = worldHeight / Screen.height;

                            Vector3 deltaWorld = new Vector3(-deltaScreen.x * worldUnitsPerPixel, -deltaScreen.y * worldUnitsPerPixel, 0f) * _panSpeed;
                            _targetPosition += deltaWorld;
                            ClampTargetPosition();
                            _lastDragScreenPos = currentPos;
                        }
                    }
                    else if (touch.press.wasReleasedThisFrame)
                    {
                        _isDragging = false;
                        IsDraggingCamera = false;
                    }
                    return;
                }
            }

            // 2. Mouse Drag (Desktop)
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    if (!IsPointerOverUI())
                    {
                        _isDragging = true;
                        _isFocusing = false;
                        _lastDragScreenPos = mouse.position.ReadValue();
                        _pressStartScreenPos = _lastDragScreenPos;
                        _hasMovedSignificantly = false;
                        WasDragAction = false;
                    }
                }
                else if (mouse.leftButton.isPressed && _isDragging)
                {
                    Vector2 currentPos = mouse.position.ReadValue();
                    Vector2 deltaScreen = currentPos - _lastDragScreenPos;

                    if (Vector2.Distance(currentPos, _pressStartScreenPos) > DragThresholdPixels)
                    {
                        _hasMovedSignificantly = true;
                        WasDragAction = true;
                        IsDraggingCamera = true;
                    }

                    if (deltaScreen.sqrMagnitude > 0.01f)
                    {
                        float worldHeight = _cam.orthographicSize * 2f;
                        float worldUnitsPerPixel = worldHeight / Screen.height;

                        Vector3 deltaWorld = new Vector3(-deltaScreen.x * worldUnitsPerPixel, -deltaScreen.y * worldUnitsPerPixel, 0f) * _panSpeed;
                        _targetPosition += deltaWorld;
                        ClampTargetPosition();
                        _lastDragScreenPos = currentPos;
                    }
                }
                else if (mouse.leftButton.wasReleasedThisFrame)
                {
                    _isDragging = false;
                    IsDraggingCamera = false;
                }
            }
        }

        private void SmoothUpdate()
        {
            if (_isFocusing)
            {
                _targetPosition = Vector3.Lerp(_targetPosition, _focusTargetPos, Time.deltaTime * 5f);
                if (_focusTargetZoom > 0f)
                {
                    _targetOrthographicSize = Mathf.Lerp(_targetOrthographicSize, _focusTargetZoom, Time.deltaTime * 5f);
                }

                if (Vector3.Distance(_targetPosition, _focusTargetPos) < 0.05f)
                {
                    _isFocusing = false;
                }
            }

            // Cập nhật vị trí camera mượt mà
            transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _panVelocity, _panSmoothTime);

            // Cập nhật orthographic size mượt mà
            _cam.orthographicSize = Mathf.SmoothDamp(_cam.orthographicSize, _targetOrthographicSize, ref _zoomVelocity, _zoomSmoothTime);
        }

        /// <summary>
        /// Giới hạn toạ độ camera sao cho khung nhìn (Frustum) không vượt quá biên sàn quán.
        /// </summary>
        private void ClampTargetPosition()
        {
            float vertExtent = _targetOrthographicSize;
            float horzExtent = vertExtent * _cam.aspect;

            float minX = _boundsMinX + horzExtent;
            float maxX = _boundsMaxX - horzExtent;
            float minY = _boundsMinY + vertExtent;
            float maxY = _boundsMaxY - vertExtent;

            // Nếu zoom out rộng hơn cả không gian quán, căn giữa toạ độ
            float clampedX = (minX > maxX) ? (_boundsMinX + _boundsMaxX) * 0.5f : Mathf.Clamp(_targetPosition.x, minX, maxX);
            float clampedY = (minY > maxY) ? (_boundsMinY + _boundsMaxY) * 0.5f : Mathf.Clamp(_targetPosition.y, minY, maxY);

            _targetPosition = new Vector3(clampedX, clampedY, transform.position.z);
        }

        /// <summary>
        /// Di chuyển camera lướt mượt tới toạ độ thế giới chỉ định.
        /// </summary>
        public void FocusOn(Vector3 worldPos, float targetZoom = -1f)
        {
            _isFocusing = true;
            _focusTargetPos = new Vector3(worldPos.x, worldPos.y, transform.position.z);
            _focusTargetZoom = targetZoom;

            if (targetZoom > 0f)
            {
                _targetOrthographicSize = Mathf.Clamp(targetZoom, _minOrthographicSize, _maxOrthographicSize);
            }

            ClampTargetPosition();
        }

        /// <summary>
        /// Thiết lập lại vùng giới hạn quán nếu kích thước quán thay đổi.
        /// </summary>
        public void SetBounds(float minX, float maxX, float minY, float maxY)
        {
            _boundsMinX = minX;
            _boundsMaxX = maxX;
            _boundsMinY = minY;
            _boundsMaxY = maxY;
            ClampTargetPosition();
        }

        /// <summary>
        /// Kiểm tra xem con trỏ chuột/touch có đang nằm trên phần tử Canvas UI (nút bấm, panel menu) hay không.
        /// Bỏ qua hoàn toàn các đối tượng 2D Collider trong Scene (sàn, overlay, decor...) để đảm bảo kéo camera hoạt động mọi nơi.
        /// </summary>
        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;

            Vector2 screenPos = Vector2.zero;
            if (Mouse.current != null)
            {
                screenPos = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            {
                screenPos = Touchscreen.current.touches[0].position.ReadValue();
            }
            else
            {
                return false;
            }

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };

            _uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _uiRaycastResults);

            int uiLayer = LayerMask.NameToLayer("UI");
            for (int i = 0; i < _uiRaycastResults.Count; i++)
            {
                var hitObj = _uiRaycastResults[i].gameObject;
                if (hitObj == null) continue;

                // Chỉ chặn nếu đối tượng thuộc layer UI hoặc nằm trong một Canvas UI
                if (hitObj.layer == uiLayer || hitObj.GetComponentInParent<Canvas>() != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
