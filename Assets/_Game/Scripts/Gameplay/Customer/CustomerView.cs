using DreamCafe.Core.MVC;
using UnityEngine;

namespace DreamCafe.Gameplay.Customer
{
    /// <summary>
    /// Presentation thuần cho khách hàng: chỉ vẽ thanh đếm ngược (patience bar) đổi màu theo
    /// thời gian còn lại. Không truy cập service/bus — mọi dữ liệu đi qua <see cref="Render"/>.
    /// </summary>
    public sealed class CustomerView : ViewBase
    {
        [Header("Tham chiếu hiển thị")]
        [SerializeField] private Transform _body;
        [SerializeField] private GameObject _patienceBarRoot;
        [SerializeField] private Transform _patienceBarFill;
        [SerializeField] private SpriteRenderer _patienceBarFillRenderer;

        [Header("Màu theo thời gian còn lại (từ ít -> nhiều)")]
        [SerializeField] private Color _colorAngry = new(0.85f, 0.15f, 0.15f);
        [SerializeField] private Color _colorDisappoint = new(0.95f, 0.55f, 0.1f);
        [SerializeField] private Color _colorNeutral = new(0.95f, 0.85f, 0.2f);
        [SerializeField] private Color _colorHappy = new(0.3f, 0.85f, 0.3f);

        public override void Render(IModel model)
        {
            if (model is not CustomerViewModel vm) return;

            if (_patienceBarRoot != null)
            {
                _patienceBarRoot.SetActive(vm.ShowTimer);
            }

            if (!vm.ShowTimer) return;

            float t = Mathf.Clamp01(vm.TimerProgress01);

            if (_patienceBarFill != null)
            {
                Vector3 scale = _patienceBarFill.localScale;
                scale.x = t;
                _patienceBarFill.localScale = scale;
            }

            if (_patienceBarFillRenderer != null)
            {
                _patienceBarFillRenderer.color = EvaluateColor(t);
            }
        }

        private Color EvaluateColor(float t01)
        {
            if (t01 > 0.66f) return Color.Lerp(_colorNeutral, _colorHappy, (t01 - 0.66f) / 0.34f);
            if (t01 > 0.33f) return Color.Lerp(_colorDisappoint, _colorNeutral, (t01 - 0.33f) / 0.33f);
            return Color.Lerp(_colorAngry, _colorDisappoint, t01 / 0.33f);
        }
    }
}
