using DreamCafe.DataControl;
using UnityEngine;

namespace DreamCafe.Gameplay.Customer
{
    /// <summary>
    /// Làm khách "sống" với đúng hai khung hình: một hình đi xuống (thấy mặt), một hình đi lên
    /// (thấy lưng). Không có bộ khung chạy bộ, nên cảm giác chuyển động phải sinh ra từ biến dạng
    /// liên tục của chính cái sticker — nhún, nảy, nghiêng — chứ không phải từ việc đổi khung hình.
    ///
    /// Ba việc, tách bạch:
    ///
    ///   1. ĐỔI HÌNH theo hướng dọc. Lưới isometric: đi về phía trong quán là y tăng, ra phía người
    ///      xem là y giảm. Chỉ đổi khi thành phần dọc đủ lớn (<see cref="_flipDeadZone"/>), nếu
    ///      không thì đường đi zigzag qua các ô sẽ làm hình chớp qua lại.
    ///
    ///   2. LẬT NGANG bằng scale.x âm. Trái/phải không cần khung riêng.
    ///
    ///   3. NHỊP THỞ chạy KHÔNG NGỪNG, kể cả lúc đứng và lúc ngồi. Đây là chỗ quyết định sticker
    ///      trông sống hay sượng: đứng im tuyệt đối một khung hình phẳng thì lập tức lộ ra là tấm
    ///      dán. Lúc đứng thở chậm và nhẹ, lúc đi nhún nhanh và mạnh hơn, và biên độ chuyển dần
    ///      giữa hai trạng thái (<see cref="_blendSeconds"/>) nên không có cú giật lúc dừng chân.
    ///
    /// Nhún theo kiểu giữ khối: bè ngang ra thì lùn xuống và ngược lại, nên khách không phồng to
    /// thu nhỏ như quả bóng.
    ///
    /// Mọi biến dạng ghi lên NÚT HÌNH (con), không phải nút gốc — nút gốc do
    /// <see cref="GridPathFollower"/> lái, đụng vào là hỏng đường đi.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CustomerStickerAnimator : MonoBehaviour
    {
        [Header("Tham chiếu")]
        [SerializeField, Tooltip("Nút chứa hình khách. Bỏ trống thì tìm con tên 'Body'.")]
        private Transform _visual;

        [SerializeField, Tooltip("Sprite của khách. Bỏ trống thì lấy trên nút hình.")]
        private SpriteRenderer _renderer;

        [Header("Bộ hình dự phòng")]
        [SerializeField, Tooltip("Dùng khi CustomerItem của khách chưa khai bộ hình của riêng nó. " +
            "Mọi loại khách chưa có art riêng sẽ chạy bằng bộ này.")]
        private CustomerAnimSet _fallbackAnim;

        [Header("Cỡ trên màn hình")]
        [SerializeField, Min(0.05f), Tooltip("Chiều cao khách trên màn hình, đơn vị thế giới. Một ô sàn cao " +
            "0.5, cái ghế cao khoảng 1.36. Để 0 thì giữ nguyên cỡ gốc của ảnh.")]
        private float _targetHeight = 1.3f;

        [SerializeField, Min(0f), Tooltip("Chiều cao phần VẼ THẬT của ảnh, đơn vị thế giới (không tính viền " +
            "trong suốt). Hai ảnh đã được chuẩn hoá về cùng một con số nên chỉ cần một ô này.")]
        private float _artHeight = 1.3f;

        [Header("Nhận hướng")]
        [SerializeField, Min(0f), Tooltip("Đi chậm hơn mức này coi như đứng yên.")]
        private float _moveThreshold = 0.05f;

        [SerializeField, Range(0f, 1f), Tooltip("Thành phần dọc phải vượt mức này mới đổi hình. " +
            "Để 0 thì đi gần như ngang sẽ làm hình chớp qua lại liên tục.")]
        private float _flipDeadZone = 0.25f;

        [SerializeField, Min(0.01f), Tooltip("Thời gian chuyển giữa nhịp đứng và nhịp đi.")]
        private float _blendSeconds = 0.28f;

        [Header("Nhịp lúc đứng")]
        [SerializeField, Min(0f), Tooltip("Số nhịp thở mỗi giây.")]
        private float _idleRate = 0.5f;

        [SerializeField, Range(0f, 0.3f), Tooltip("Biên độ nhún lúc đứng.")]
        private float _idleSquash = 0.05f;

        [Header("Nhịp lúc đi")]
        [SerializeField, Min(0f), Tooltip("Số bước mỗi giây.")]
        private float _walkRate = 1.6f;

        [SerializeField, Range(0f, 0.4f), Tooltip("Biên độ nhún lúc đi.")]
        private float _walkSquash = 0.11f;

        [SerializeField, Range(0f, 0.5f), Tooltip("Độ nảy lên xuống mỗi bước.")]
        private float _hopHeight = 0.06f;

        [SerializeField, Range(0f, 20f), Tooltip("Độ nghiêng người mỗi bước (độ).")]
        private float _leanDegrees = 5f;

        [Header("Bay vào / bật ra khỏi ghế")]
        [SerializeField, Min(0.01f), Tooltip("Thời gian phần hình bay nốt quãng cuối vào ghế (giây).")]
        private float _seatFlySeconds = 0.22f;

        [SerializeField, Range(0f, 0.6f), Tooltip("Độ cong của cú bay — 0 là trượt thẳng.")]
        private float _seatFlyArc = 0.18f;

        private const float Tau = Mathf.PI * 2f;

        private CustomerItem _definition;
        private Vector3 _lastPosition;
        private Vector3 _velocity;
        private Vector3 _basePosition;
        private float _phase;
        private float _walkBlend;
        private bool _facingBack;
        private bool _facingLeft;
        private bool _seated;
        private bool _showingBackArt;
        private Vector2 _seatVisualOffset;
        private Vector2 _seatFlyFrom;
        private float _seatFlyRemain;
        private int _sitPose;

        private void Awake()
        {
            Resolve();
            if (_visual != null) _basePosition = _visual.localPosition;
            _lastPosition = transform.position;

            // Lệch pha mỗi khách một ít, nếu không cả đám thở cùng nhịp trông như múa đồng diễn.
            _phase = Random.value * Tau;

            // Đặt hình ngay từ đầu. Thiếu dòng này thì trước lúc Configure chạy, khách vẫn đeo
            // sprite tạm còn sót trong prefab — nhìn như animation không chạy.
            ApplySprite();
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;
            _velocity = Vector3.zero;
            _walkBlend = 0f;
        }

        private void Resolve()
        {
            if (_visual == null) _visual = transform.Find("Body");
            if (_visual == null) _visual = transform;
            if (_renderer == null) _renderer = _visual.GetComponent<SpriteRenderer>();
            if (_renderer == null) _renderer = GetComponentInChildren<SpriteRenderer>();
        }

        /// <summary>
        /// Nhận dữ liệu khách để lấy đúng bộ hình của loại khách đó. CustomerController gọi lúc
        /// Configure, ngay sau khi lấy khách ra khỏi pool.
        /// </summary>
        public void SetDefinition(CustomerItem definition)
        {
            _definition = definition;
            Resolve();

            // Lấy ra khỏi pool là xoá tư thế khách lượt trước để lại: có thể đang nhún dở, đang
            // lật ngược, hoặc đang ngồi.
            if (_visual != null)
            {
                _visual.localPosition = _basePosition;
                _visual.localRotation = Quaternion.identity;
            }
            _facingBack = false;
            _facingLeft = false;
            _seated = false;
            _seatVisualOffset = Vector2.zero;
            _seatFlyFrom = Vector2.zero;
            _seatFlyRemain = 0f;
            _walkBlend = 0f;
            _velocity = Vector3.zero;

            // Chọn kiểu ngồi MỘT LẦN cho cả lượt khách này. Chọn lúc ngồi xuống thì mỗi lần đổi
            // bàn khách lại đổi thứ đang cầm trên tay; chọn ở đây thì cả quán mỗi người một kiểu
            // mà từng người vẫn nhất quán.
            _sitPose = Random.Range(0, 3);
            _lastPosition = transform.position;
            ApplySprite();
        }

        /// <summary>
        /// Báo khách vừa ngồi xuống / đứng lên. Ngồi thì đổi sang hình ngồi (nếu bộ hình có khai)
        /// và tắt nảy với nghiêng, nhưng VẪN THỞ — ngồi im tuyệt đối là lộ ngay ra tấm dán.
        ///
        /// <paramref name="visualOffset"/> nhích RIÊNG phần hình lên mặt ghế. Thân khách vẫn đứng
        /// đúng tâm ô ghế, nếu không thì lúc đứng dậy A* quy khách về một ô cách đó hai ô và khách
        /// lao ngược về phía sau trước khi tìm được đường ra.
        /// </summary>
        public void SetSeated(bool seated, Vector2 visualOffset, Vector2 faceDirection)
        {
            _seatVisualOffset = seated ? visualOffset : Vector2.zero;
            if (_seated == seated) return;
            _seated = seated;

            if (seated)
            {
                _walkBlend = 0f;
                if (_visual != null) _visual.localRotation = Quaternion.identity;

                // Hướng ngồi lấy theo chỗ cái bàn, KHÔNG giữ theo hướng vừa đi tới: khách đi vòng
                // tới ghế từ phía nào cũng được, nhưng ngồi xuống thì luôn phải quay mặt vào bàn.
                if (faceDirection.sqrMagnitude > 0.0001f)
                {
                    Vector2 dir = faceDirection.normalized;
                    if (Mathf.Abs(dir.y) > _flipDeadZone) _facingBack = dir.y > 0f;
                    if (Mathf.Abs(dir.x) > _flipDeadZone) _facingLeft = dir.x < 0f;
                }
            }

            // Đổi hình cho CẢ HAI chiều. Đứng dậy mà không gọi lại thì khách đi tiếp với hình ngồi:
            // chỗ duy nhất còn gọi ApplySprite là UpdateFacing, mà nó chỉ gọi khi _facingBack ĐỔI —
            // khách đứng dậy đi tiếp cùng chiều dọc thì không có gì đổi, hình ngồi dính luôn.
            ApplySprite();
        }

        /// <summary>
        /// Bắt phần hình bay một cung ngắn từ chỗ cũ về chỗ mới, sau khi THÂN khách đã bị đặt
        /// thẳng sang chỗ mới. <paramref name="worldDelta"/> là "chỗ cũ trừ chỗ mới".
        ///
        /// Dùng cho quãng cuối vào ghế và lúc bật dậy. Quãng đó không đi bộ được — ô ghế bị đồ đạc
        /// chiếm — mà đặt đánh phụp một cái thì sượng, đúng cái mà kiểu hình dán sợ nhất.
        /// </summary>
        public void FlyFrom(Vector2 worldDelta)
        {
            if (_seatFlySeconds <= 0f || worldDelta.sqrMagnitude < 0.000001f)
            {
                _seatFlyRemain = 0f;
                return;
            }

            _seatFlyFrom = worldDelta;
            _seatFlyRemain = _seatFlySeconds;

            // Thân vừa bị đặt sang chỗ khác trong MỘT khung hình. Không xoá mốc đo thì khung sau
            // ReadMovement thấy một quãng cả ô chia cho một khung, tưởng khách vừa phi nước đại:
            // nhịp đi vọt lên và hình lật hướng theo chiều bị đặt.
            _lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || _visual == null) return;

            ReadMovement(dt);
            UpdateFacing();
            ApplyPulse(dt);
        }

        /// <summary>
        /// Đo vận tốc từ chính vị trí nút gốc thay vì hỏi GridPathFollower: khách còn bị đặt chỗ
        /// bằng Teleport lúc spawn và bị ghim khi ngồi, đo vị trí thì trường hợp nào cũng đúng.
        /// </summary>
        private void ReadMovement(float dt)
        {
            Vector3 position = transform.position;
            Vector3 raw = (position - _lastPosition) / dt;
            _lastPosition = position;

            // Làm mượt: một khung hình lẻ đứng yên giữa lúc đang đi không được phép tắt nhịp đi.
            _velocity = Vector3.Lerp(_velocity, raw, 1f - Mathf.Exp(-12f * dt));

            bool moving = !_seated && _velocity.sqrMagnitude > _moveThreshold * _moveThreshold;
            _walkBlend = Mathf.MoveTowards(_walkBlend, moving ? 1f : 0f,
                                           dt / Mathf.Max(0.01f, _blendSeconds));
        }

        private void UpdateFacing()
        {
            if (_seated || _velocity.sqrMagnitude <= _moveThreshold * _moveThreshold) return;

            Vector3 dir = _velocity.normalized;

            // Chỉ đổi hình khi hướng đi nghiêng hẳn về dọc. Đi gần như ngang thì giữ nguyên hình
            // đang có, nếu không mỗi lần đường đi zigzag qua các ô là hình chớp liên tục.
            if (Mathf.Abs(dir.y) > _flipDeadZone)
            {
                bool wantBack = dir.y > 0f;
                if (wantBack != _facingBack)
                {
                    _facingBack = wantBack;
                    ApplySprite();
                }
            }

            if (Mathf.Abs(dir.x) > _flipDeadZone) _facingLeft = dir.x < 0f;
        }

        /// <summary>
        /// Bộ hình của khách nếu nó có khai, không thì bộ dự phòng. Cố ý không có đường lui nào
        /// khác — từng lui về avatar, mà avatar không bao giờ null nên bộ dự phòng không bao giờ
        /// được dùng tới, và mỗi loại khách lại hiện ảnh chân dung riêng của nó.
        /// </summary>
        private void ApplySprite()
        {
            if (_renderer == null) return;

            CustomerAnimSet set = _definition != null && _definition.Anim.HasWalk
                ? _definition.Anim
                : _fallbackAnim;

            Sprite seatedPose = _seated ? set.SitPose(_sitPose) : null;
            Sprite wanted = seatedPose != null ? seatedPose : set.Walk(_facingBack);
            if (wanted != null && _renderer.sprite != wanted) _renderer.sprite = wanted;

            // Ghi lại ảnh đang hiện là ảnh nhìn từ LƯNG hay từ MẶT. Phép lật phải bám theo đây chứ
            // không theo _facingBack: ảnh ngồi vẽ từ mặt, nên khách ngồi quay vào trong quán mà
            // dùng luật của ảnh lưng thì lật ngược hướng.
            _showingBackArt = wanted != null && wanted == set.walkBack;
        }

        /// <summary>
        /// Có phải lật gương hình không.
        ///
        /// Hai hình vẽ cùng một người quay hai chiều ngược nhau: hình đi xuống nghiêng xuống-PHẢI,
        /// nên chính người đó quay lưng lại sẽ nghiêng lên-TRÁI. Vì vậy phép lật của hình lưng
        /// phải NGƯỢC với hình mặt — dùng chung một điều kiện thì đi lên sang phải lại thành quay
        /// mặt sang trái.
        ///
        /// Đi thẳng lên (không có thành phần ngang) thì <see cref="_facingLeft"/> giữ giá trị cũ,
        /// mặc định là false, nên hình lưng vẫn được lật — đúng hướng đi thẳng vào trong quán.
        ///
        /// Bám theo ẢNH ĐANG HIỆN chứ không theo <see cref="_facingBack"/>: ảnh ngồi vẽ từ mặt, nên
        /// khách ngồi quay vào trong quán mà xét theo _facingBack thì lại lật ngược.
        /// </summary>
        private bool Mirrored => _showingBackArt ? !_facingLeft : _facingLeft;

        private void ApplyPulse(float dt)
        {
            float rate = Mathf.Lerp(_idleRate, _walkRate, _walkBlend);
            _phase += dt * rate * Tau;
            if (_phase > Tau) _phase -= Tau;

            float wave = Mathf.Sin(_phase);
            float squash = Mathf.Lerp(_idleSquash, _walkSquash, _walkBlend) * wave;

            // Quy ảnh về chiều cao đích. Hai ảnh đã chuẩn hoá cùng cỡ nên chỉ một phép chia.
            float fit = _artHeight > 0.01f && _targetHeight > 0.01f ? _targetHeight / _artHeight : 1f;

            // Giữ khối: cao lên thì thon lại, lùn xuống thì bè ra.
            float scaleY = (1f + squash) * fit;
            float scaleX = (1f - squash * 0.85f) * fit;
            if (Mirrored) scaleX = -scaleX;

            // Ghi đè hẳn chứ không nhân vào tỉ lệ có sẵn của nút hình: tỉ lệ đó thường là con số
            // dành cho sprite tạm lúc dựng prefab (ở đây từng là 0.6 x 1) và sẽ bóp méo art thật.
            _visual.localScale = new Vector3(scaleX, scaleY, 1f);

            // Nảy và nghiêng chỉ có lúc đi. Dùng trị tuyệt đối cho nảy: mỗi nửa vòng là một bước
            // chân, nên tần số nảy gấp đôi tần số nhún — đúng nhịp đi bộ.
            float hop = Mathf.Abs(wave) * _hopHeight * _walkBlend;

            // Quãng cuối vào ghế: thân đã sang ô ghế rồi, phần hình đuổi theo sau một cung ngắn.
            // Chậm dần về cuối (ease-out) nên nó đáp xuống mặt ghế chứ không phanh gấp.
            Vector2 fly = Vector2.zero;
            float arc = 0f;
            if (_seatFlyRemain > 0f)
            {
                _seatFlyRemain = Mathf.Max(0f, _seatFlyRemain - dt);
                float t = 1f - _seatFlyRemain / _seatFlySeconds;
                fly = _seatFlyFrom * ((1f - t) * (1f - t));
                arc = Mathf.Sin(t * Mathf.PI) * _seatFlyArc;
            }

            _visual.localPosition = _basePosition
                                    + new Vector3(_seatVisualOffset.x + fly.x,
                                                  hop + arc + _seatVisualOffset.y + fly.y, 0f);

            float lean = Mathf.Sin(_phase * 0.5f) * _leanDegrees * _walkBlend;
            _visual.localRotation = Quaternion.Euler(0f, 0f, Mirrored ? -lean : lean);
        }
    }
}
