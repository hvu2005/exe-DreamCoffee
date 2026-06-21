using DreamCafe.Core.Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DreamCafe.Gameplay.Input
{
    /// <summary>
    /// Translates player tap/click into ITappable.OnTap() calls.
    /// Uses new Input System PointerAction; raycasts with Physics2D.OverlapPoint.
    /// Attach to any scene GameObject — no ServiceContext needed (purely input→raycast→dispatch).
    /// TODO Phase 4: support multi-touch drag for serve gesture.
    /// </summary>
    public sealed class PlayerInputRouter : MonoBehaviour
    {
        [SerializeField] private Camera gameCamera;
        [SerializeField] private LayerMask tappableLayer = ~0; // all layers by default

        private InputAction _tapAction;

        private void Awake()
        {
            if (gameCamera == null)
                gameCamera = Camera.main;

            _tapAction = new InputAction("Tap", InputActionType.Button);
            _tapAction.AddBinding("<Mouse>/leftButton");
            _tapAction.AddBinding("<Touchscreen>/primaryTouch/tap");
        }

        private void OnEnable()
        {
            _tapAction.Enable();
            _tapAction.performed += OnTapPerformed;
        }

        private void OnDisable()
        {
            _tapAction.performed -= OnTapPerformed;
            _tapAction.Disable();
        }

        private void OnDestroy() => _tapAction?.Dispose();

        private void OnTapPerformed(InputAction.CallbackContext _)
        {
            var screenPos  = Pointer.current?.position.ReadValue() ?? Vector2.zero;
            var worldPos   = (Vector2)gameCamera.ScreenToWorldPoint(screenPos);
            var hit        = Physics2D.OverlapPoint(worldPos, tappableLayer);
            if (hit == null) return;

            var tappable = hit.GetComponent<ITappable>();
            tappable?.OnTap();
        }
    }
}
