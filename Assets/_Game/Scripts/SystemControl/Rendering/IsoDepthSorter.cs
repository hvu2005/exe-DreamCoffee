using System.Collections.Generic;
using UnityEngine;

namespace DreamCafe.SystemControl.Rendering
{
    /// <summary>Cách quy một vật thể về độ sâu.</summary>
    public enum IsoDepthMode
    {
        /// <summary>
        /// Mỗi SpriteRenderer tự so bằng ĐIỂM ĐẶT của chính nó. Dùng cho nội thất nhiều mảnh:
        /// trong một bộ bàn ghế, ghế phía trước tự đè lên mặt bàn còn ghế phía sau tự chìm xuống.
        ///
        /// Cố ý lấy <c>transform.position</c> chứ KHÔNG lấy <c>bounds.min.y</c>: bounds tính cả
        /// viền trong suốt quanh ảnh, mà viền đó mỗi sprite một kiểu (có ảnh 5% có ảnh 50%). Khách
        /// thì so bằng gót chân, nên hai bên hoá ra đo bằng hai thước khác nhau — ghế lùi về sau
        /// vẫn vẽ đè lên khách đứng trước nó tận hai ô.
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

        [SerializeField, Tooltip("Nhích điểm chạm sàn. Với nội thất, DecorSlot tự điền -artOffset để " +
            "quy điểm đặt của từng mảnh về đúng tâm ô nó đứng.")]
        private Vector2 _groundOffset = Vector2.zero;

        [SerializeField, Tooltip("Thứ tự trong CÙNG một ô: 0 sàn, 1 nội thất, 3 khách. Khách để 3 nên " +
            "ngồi lên ghế là tự nổi trên mặt ghế, không cần luật riêng.")]
        private int _layerInCell = IsoDepth.SlotFurniture;

        private SpriteRenderer[] _renderers;
        private int[] _tieBreak;

        private void Awake() => Cache();

        private void OnEnable() => Apply();

        private void Start() => Apply();

        private void LateUpdate()
        {
            if (_continuous) Apply();
        }

        /// <summary>
        /// Điểm chạm sàn ghim sẵn cho một số sprite, thay cho vị trí transform của nó.
        ///
        /// Cần vì vị trí nút hình của một mảnh nội thất là chỗ nó trông cho ĐẸP, không nhất thiết
        /// là ô nó ĐỨNG — bộ sofa hiện đang lệch gần nửa đơn vị, tức gần hai hàng ô. Chỗ nào có
        /// dữ liệu nói thẳng mảnh này thuộc ô nào (ghế biết ô ghế của nó) thì ghim theo dữ liệu đó
        /// chứ đừng đoán qua transform.
        /// </summary>
        private Dictionary<SpriteRenderer, Vector2> _groundPins;

        /// <summary>Ghim điểm chạm sàn cho một sprite. Gọi TRƯỚC <see cref="Configure"/>.</summary>
        public void PinGround(SpriteRenderer renderer, Vector2 worldGround)
        {
            if (renderer == null) return;
            _groundPins ??= new Dictionary<SpriteRenderer, Vector2>();
            _groundPins[renderer] = worldGround;
        }

        /// <summary>Bỏ hết ghim (dựng lại món nội thất thì gọi cái này trước).</summary>
        public void ClearGroundPins() => _groundPins?.Clear();

        /// <summary>
        /// Đọc lại danh sách sprite và thứ tự tương đối gốc. Gọi lại nếu thay/thêm sprite lúc chạy.
        /// </summary>
        public void Cache()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _tieBreak = new int[_renderers.Length];

            // Thứ tự tương đối gốc giữa các mảnh, chuẩn hoá về 0..n. CHỈ dùng cho WholeObject —
            // xem chú thích chỗ áp dụng trong Apply().
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

        /// <summary>Tính và gán lại order cho toàn bộ sprite của vật thể.</summary>
        public void Apply()
        {
            if (_renderers == null || _renderers.Length == 0) Cache();
            if (_renderers == null) return;

            int wholeOrder = IsoDepth.OrderFor(transform.position.x + _groundOffset.x,
                                               transform.position.y + _groundOffset.y, _layerInCell);

            for (int i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null) continue;

                if (_mode == IsoDepthMode.WholeObject)
                {
                    // Mọi mảnh chung một độ sâu, nên thứ tự tương đối gốc là thứ DUY NHẤT tách
                    // chúng ra: thanh máu vẫn phải nằm trên đầu khách.
                    renderer.sortingOrder = wholeOrder + _tieBreak[i];
                    continue;
                }

                // PerRenderer thì KHÔNG cộng thứ tự gốc. Mỗi mảnh đã tự có điểm chạm sàn riêng nên
                // công thức đủ tách chúng ra rồi; cộng thêm là để số gán tay từ thời trước lọt vào
                // và ăn mất phần slot. Ghế trong Wood4Seats từng mang order gốc +2, cộng vào slot
                // nội thất 1 thành 3 — đúng bằng slot của khách, nên khách ngồi lên ghế thì hai bên
                // hoà nhau và cái ghế đè lên khách.
                Vector2 ground;
                if (_groundPins != null && _groundPins.TryGetValue(renderer, out var pinned))
                {
                    ground = pinned;
                }
                else
                {
                    Vector3 p = renderer.transform.position;
                    ground = new Vector2(p.x + _groundOffset.x, p.y + _groundOffset.y);
                }

                renderer.sortingOrder = IsoDepth.OrderFor(ground.x, ground.y, _layerInCell);
            }
        }

        /// <summary>Đổi chế độ lúc chạy (chủ yếu để script setup dựng prefab).</summary>
        public void Configure(IsoDepthMode mode, bool continuous, Vector2 groundOffset = default,
                              int layerInCell = IsoDepth.SlotFurniture)
        {
            _mode = mode;
            _continuous = continuous;
            _groundOffset = groundOffset;
            _layerInCell = layerInCell;
            Apply();
        }
    }
}
