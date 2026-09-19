using UnityEngine;

namespace DreamCafe.SystemControl.Rendering
{
    /// <summary>Cách quy một vật thể về độ sâu.</summary>
    public enum IsoDepthMode
    {
        /// <summary>
        /// Mỗi SpriteRenderer tự so bằng mép dưới của chính nó. Dùng cho nội thất nhiều mảnh:
        /// trong một bộ bàn ghế, ghế phía trước tự đè lên mặt bàn còn ghế phía sau tự chìm xuống.
        /// </summary>
        PerRenderer = 0,

        /// <summary>
        /// Cả vật thể dùng chung một độ sâu lấy từ chân của gốc, các mảnh chỉ giữ thứ tự tương đối.
        /// Dùng cho vật di chuyển: thanh máu/bong bóng order nằm trên đầu khách vẫn phải đi theo
        /// khách chứ không được tự tính độ sâu theo vị trí lơ lửng của nó.
        /// </summary>
        WholeObject = 1
    }

    /// <summary>
    /// Gán sorting order cho sprite theo quy tắc <see cref="IsoDepth"/>. Gắn lên prefab nội thất và
    /// lên khách; vật đứng yên tính một lần, vật di chuyển bật <see cref="_continuous"/> để cập nhật
    /// mỗi frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IsoDepthSorter : MonoBehaviour
    {
        [SerializeField, Tooltip("PerRenderer cho nội thất nhiều mảnh; WholeObject cho vật di chuyển.")]
        private IsoDepthMode _mode = IsoDepthMode.PerRenderer;

        [SerializeField, Tooltip("Bật cho vật di chuyển — tính lại độ sâu mỗi frame.")]
        private bool _continuous = false;

        [SerializeField, Tooltip("Nhích điểm chạm sàn lên/xuống khi sprite có khoảng trống hoặc bóng đổ ở đáy.")]
        private float _groundOffset = 0f;

        private SpriteRenderer[] _renderers;
        private int[] _tieBreak;
        private int? _orderOverride;

        private void Awake() => Cache();

        private void OnEnable() => Apply();

        private void Start() => Apply();

        private void LateUpdate()
        {
            if (_continuous) Apply();
        }

        /// <summary>
        /// Đọc lại danh sách sprite và thứ tự tương đối gốc. Gọi lại nếu thay/thêm sprite lúc chạy.
        /// </summary>
        public void Cache()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _tieBreak = new int[_renderers.Length];

            // Giữ nguyên thứ tự các mảnh trong prefab bằng cách chuẩn hoá order gốc về 0..n.
            int min = int.MaxValue;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null) min = Mathf.Min(min, _renderers[i].sortingOrder);
            }
            if (min == int.MaxValue) min = 0;

            for (int i = 0; i < _renderers.Length; i++)
            {
                _tieBreak[i] = _renderers[i] != null ? _renderers[i].sortingOrder - min : 0;
            }
        }

        /// <summary>
        /// Ép một order cố định, bỏ qua độ sâu theo toạ độ. Dùng khi vật thể phải chen vào giữa các
        /// mảnh của một món đồ khác — ví dụ khách ngồi thì phải nằm trên ghế nhưng dưới mặt bàn,
        /// mà chân khách lại cao hơn chân ghế nên tính theo độ sâu sẽ ra sai thứ tự.
        /// </summary>
        public void SetOrderOverride(int order)
        {
            _orderOverride = order;
            Apply();
        }

        /// <summary>Bỏ ép order, quay lại xếp lớp theo độ sâu.</summary>
        public void ClearOrderOverride()
        {
            _orderOverride = null;
            Apply();
        }

        /// <summary>Tính và gán lại order cho toàn bộ sprite của vật thể.</summary>
        public void Apply()
        {
            if (_renderers == null || _renderers.Length == 0) Cache();
            if (_renderers == null) return;

            int wholeOrder = _orderOverride ?? IsoDepth.OrderFor(transform.position.y + _groundOffset);

            for (int i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null) continue;

                int order = _mode == IsoDepthMode.WholeObject || _orderOverride.HasValue
                    ? wholeOrder
                    : IsoDepth.OrderFor(renderer.bounds.min.y + _groundOffset);

                renderer.sortingOrder = order + _tieBreak[i];
            }
        }

        /// <summary>Đổi chế độ lúc chạy (chủ yếu để script setup dựng prefab).</summary>
        public void Configure(IsoDepthMode mode, bool continuous, float groundOffset = 0f)
        {
            _mode = mode;
            _continuous = continuous;
            _groundOffset = groundOffset;
            Apply();
        }
    }
}
