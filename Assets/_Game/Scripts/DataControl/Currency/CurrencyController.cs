using System;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Controller quản lý các loại tiền tệ và chỉ số tài chính của quán tại Runtime theo mô hình CRUD:
    /// - Tiền hiện có (Current Money / Balance)
    /// - Tiền trên giây (Money Per Second / Passive Income)
    /// - Danh tiếng quán (Reputation EXP)
    ///
    ///   Create/Add -> <see cref="Add"/>, <see cref="AddMoney"/>, <see cref="AddMoneyPerSecond"/>, <see cref="AddReputation"/>
    ///   Read       -> <see cref="Get"/>, <see cref="CurrentMoney"/>, <see cref="MoneyPerSecond"/>, <see cref="Reputation"/>, <see cref="CanAfford"/>
    ///   Update     -> <see cref="Set"/>, <see cref="SetMoney"/>, <see cref="SetMoneyPerSecond"/>, <see cref="SetReputation"/>,
    ///                 <see cref="SpendMoney"/>, <see cref="SpendReputation"/>, <see cref="Tick"/>
    ///   Delete     -> <see cref="Reset"/>, <see cref="ResetAll"/>
    ///
    /// Kế thừa IService để tham gia ServiceManager.
    /// </summary>
    public sealed class CurrencyController : IService
    {
        private const string PrefKeyMoney = "DreamCafe_Save_Money";
        private const string PrefKeyMPS = "DreamCafe_Save_MPS";
        private const string PrefKeyRep = "DreamCafe_Save_Rep";
        private const string PrefKeyHasSave = "DreamCafe_Save_HasSave";

        private float _currentMoney = 500_000f; // Mặc định khởi đầu: 500.000đ
        private float _moneyPerSecond = 0f;
        private float _reputation = 0f;

        /// <summary>Chu kỳ cộng tiền thụ động & ghi xuống DB: đúng 1 giây 1 lần.</summary>
        public const float IncomeTickInterval = 1f;

        // Bộ đếm thời gian dồn tới mốc 1 giây mới cộng tiền + ghi DB, tránh cập nhật mỗi frame
        private float _tickTimer = 0f;

        private readonly System.Collections.Generic.Dictionary<CurrencyType, CurrencyItem> _definitions = new();
        private ICurrencyRepository _repository;
        private bool _enableAutoSave = true;

        /// <summary>Bắn sự kiện chung khi bất kỳ chỉ số tiền tệ nào thay đổi.</summary>
        public event Action Changed;

        /// <summary>Bắn sự kiện chi tiết kèm loại tiền tệ, giá trị mới và độ lệch (delta).</summary>
        public event Action<CurrencyType, float, float> CurrencyChanged;

        // =====================================================================
        // Properties (Read-Only)
        // =====================================================================

        /// <summary>Số dư tiền mặt hiện tại (VNĐ).</summary>
        public float CurrentMoney => _currentMoney;

        /// <summary>Tốc độ sinh tiền tự động mỗi giây (VNĐ/s).</summary>
        public float MoneyPerSecond => _moneyPerSecond;

        /// <summary>Điểm danh tiếng quán hiện tại.</summary>
        public float Reputation => _reputation;

        /// <summary>Có bật tự động lưu trữ hay không.</summary>
        public bool EnableAutoSave { get => _enableAutoSave; set => _enableAutoSave = value; }

        // =====================================================================
        // IService Lifecycle
        // =====================================================================

        /// <summary>Khởi tạo controller với ServiceContext.</summary>
        public void Init(ServiceContext ctx)
        {
            Debug.Log($"[CurrencyController] Initialized — Money: {_currentMoney:N0}đ, MPS: {_moneyPerSecond:N0}đ/s, Rep: {_reputation:N0}");
        }

        /// <summary>Dọn dẹp tài nguyên khi hệ thống tắt.</summary>
        public void Shutdown()
        {
            Changed = null;
            CurrencyChanged = null;
            _definitions.Clear();
            _repository = null;
        }

        // =====================================================================
        // REPOSITORY / DATABASE INTEGRATION
        // =====================================================================

        /// <summary>
        /// Đăng ký kho định nghĩa tĩnh từ DatabaseManager (tương tự Customer & Recipe).
        /// Tự động nạp giá trị khởi đầu hoặc khôi phục dữ liệu đã lưu nếu có.
        /// </summary>
        public void RegisterFromRepository(ICurrencyRepository repository)
        {
            if (repository == null) return;
            _repository = repository;
            _enableAutoSave = repository.EnableAutoSave;
            _definitions.Clear();

            foreach (var def in repository.GetAllCurrencies())
            {
                if (def != null)
                {
                    _definitions[def.Type] = def;
                }
            }

            // Nếu người chơi đã có save từ trước -> Load; Nếu là lần đầu -> Nạp starting values từ DB
            if (HasSavedData())
            {
                Load();

                // Tiền & danh tiếng là tiến độ của người chơi nên giữ nguyên từ save, nhưng MPS là
                // chỉ số cấu hình: luôn nạp lại base từ DB để chỉnh starting value là có hiệu lực ngay,
                // không bị save cũ (thường là 0) đè lên.
                SetMoneyPerSecond(_repository.GetStartingValue(CurrencyType.MoneyPerSecond));
            }
            else
            {
                ApplyDefaultsFromRepository();
            }

            Debug.Log($"[CurrencyController] Đã nạp thành công {_definitions.Count} định nghĩa tiền tệ từ Repository!");
        }

        /// <summary>Lấy định nghĩa tĩnh của loại tiền tệ theo enum.</summary>
        public CurrencyItem GetDefinition(CurrencyType type) =>
            _definitions.TryGetValue(type, out var def) ? def : null;

        /// <summary>Lấy toàn bộ định nghĩa tĩnh của các loại tiền tệ.</summary>
        public CurrencyItem[] GetAllDefinitions()
        {
            var arr = new CurrencyItem[_definitions.Count];
            _definitions.Values.CopyTo(arr, 0);
            return arr;
        }

        /// <summary>Nạp các giá trị khởi tạo từ Database.</summary>
        public void ApplyDefaultsFromRepository()
        {
            if (_repository != null)
            {
                SetMoney(_repository.GetStartingValue(CurrencyType.Money));
                SetMoneyPerSecond(_repository.GetStartingValue(CurrencyType.MoneyPerSecond));
                SetReputation(_repository.GetStartingValue(CurrencyType.Reputation));
            }
        }

        // =====================================================================
        // PERSISTENT SAVE / LOAD (DATABASE LƯU TRỮ)
        // =====================================================================

        public bool HasSavedData() => PlayerPrefs.GetInt(PrefKeyHasSave, 0) == 1;

        public void Save()
        {
            PlayerPrefs.SetFloat(PrefKeyMoney, _currentMoney);
            PlayerPrefs.SetFloat(PrefKeyMPS, _moneyPerSecond);
            PlayerPrefs.SetFloat(PrefKeyRep, _reputation);
            PlayerPrefs.SetInt(PrefKeyHasSave, 1);
            PlayerPrefs.Save();
        }

        public void Load()
        {
            if (!HasSavedData()) return;
            _currentMoney = PlayerPrefs.GetFloat(PrefKeyMoney, 500_000f);
            _moneyPerSecond = PlayerPrefs.GetFloat(PrefKeyMPS, 0f);
            _reputation = PlayerPrefs.GetFloat(PrefKeyRep, 0f);
            _tickTimer = 0f;
            NotifyChanged(CurrencyType.Money, _currentMoney, 0);
            NotifyChanged(CurrencyType.MoneyPerSecond, _moneyPerSecond, 0);
            NotifyChanged(CurrencyType.Reputation, _reputation, 0);
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(PrefKeyMoney);
            PlayerPrefs.DeleteKey(PrefKeyMPS);
            PlayerPrefs.DeleteKey(PrefKeyRep);
            PlayerPrefs.DeleteKey(PrefKeyHasSave);
            PlayerPrefs.Save();
        }

        // =====================================================================
        // CREATE / ADD
        // =====================================================================

        /// <summary>Cộng tiền vào số dư hiện có.</summary>
        public void AddMoney(float amount)
        {
            if (amount <= 0f) return;
            _currentMoney += amount;
            NotifyChanged(CurrencyType.Money, _currentMoney, amount);
        }

        /// <summary>Cộng tốc độ sinh tiền mỗi giây.</summary>
        public void AddMoneyPerSecond(float amount)
        {
            if (Mathf.Approximately(amount, 0f)) return;
            _moneyPerSecond += amount;
            if (_moneyPerSecond < 0f) _moneyPerSecond = 0f;
            NotifyChanged(CurrencyType.MoneyPerSecond, _moneyPerSecond, amount);
        }

        /// <summary>Cộng điểm danh tiếng quán.</summary>
        public void AddReputation(float amount)
        {
            if (amount <= 0f) return;
            _reputation += amount;
            NotifyChanged(CurrencyType.Reputation, _reputation, amount);
        }

        /// <summary>Cộng lượng giá trị theo loại tiền tệ chỉ định.</summary>
        public void Add(CurrencyType type, float amount)
        {
            switch (type)
            {
                case CurrencyType.Money:
                    AddMoney(amount);
                    break;
                case CurrencyType.MoneyPerSecond:
                    AddMoneyPerSecond(amount);
                    break;
                case CurrencyType.Reputation:
                    AddReputation(amount);
                    break;
            }
        }

        // =====================================================================
        // READ
        // =====================================================================

        /// <summary>Đọc giá trị theo loại tiền tệ.</summary>
        public float Get(CurrencyType type)
        {
            return type switch
            {
                CurrencyType.Money => _currentMoney,
                CurrencyType.MoneyPerSecond => _moneyPerSecond,
                CurrencyType.Reputation => _reputation,
                _ => 0f
            };
        }

        /// <summary>Kiểm tra số dư tiền mặt có đủ để thanh toán khoản tiền chỉ định không.</summary>
        public bool CanAfford(float amount) => _currentMoney >= amount;

        /// <summary>Kiểm tra điểm danh tiếng có đủ điều kiện yêu cầu không.</summary>
        public bool CanAffordReputation(float amount) => _reputation >= amount;

        // =====================================================================
        // UPDATE
        // =====================================================================

        /// <summary>Gán giá trị tuyệt đối cho một loại tiền tệ.</summary>
        public void Set(CurrencyType type, float value)
        {
            switch (type)
            {
                case CurrencyType.Money:
                    SetMoney(value);
                    break;
                case CurrencyType.MoneyPerSecond:
                    SetMoneyPerSecond(value);
                    break;
                case CurrencyType.Reputation:
                    SetReputation(value);
                    break;
            }
        }

        /// <summary>Gán số dư tiền mặt tuyệt đối.</summary>
        public void SetMoney(float value)
        {
            if (value < 0f) value = 0f;
            float delta = value - _currentMoney;
            _currentMoney = value;
            NotifyChanged(CurrencyType.Money, _currentMoney, delta);
        }

        /// <summary>Gán tốc độ tiền mỗi giây tuyệt đối.</summary>
        public void SetMoneyPerSecond(float value)
        {
            if (value < 0f) value = 0f;
            float delta = value - _moneyPerSecond;
            _moneyPerSecond = value;
            NotifyChanged(CurrencyType.MoneyPerSecond, _moneyPerSecond, delta);
        }

        /// <summary>Gán điểm danh tiếng tuyệt đối.</summary>
        public void SetReputation(float value)
        {
            if (value < 0f) value = 0f;
            float delta = value - _reputation;
            _reputation = value;
            NotifyChanged(CurrencyType.Reputation, _reputation, delta);
        }

        /// <summary>
        /// Trừ tiền mặt để thanh toán chi phí.
        /// </summary>
        /// <returns>True nếu thanh toán thành công, False nếu không đủ số dư.</returns>
        public bool SpendMoney(float amount)
        {
            if (amount <= 0f) return true;
            if (!CanAfford(amount))
            {
                Debug.LogWarning($"[CurrencyController] Không đủ tiền ({_currentMoney:N0}đ < {amount:N0}đ).");
                return false;
            }

            _currentMoney -= amount;
            NotifyChanged(CurrencyType.Money, _currentMoney, -amount);
            return true;
        }

        /// <summary>
        /// Trừ điểm danh tiếng (nếu có tính năng tiêu thụ danh tiếng).
        /// </summary>
        public bool SpendReputation(float amount)
        {
            if (amount <= 0f) return true;
            if (!CanAffordReputation(amount)) return false;

            _reputation -= amount;
            NotifyChanged(CurrencyType.Reputation, _reputation, -amount);
            return true;
        }

        /// <summary>
        /// Cập nhật thời gian thực — được gọi mỗi frame từ một Runner/MonoBehaviour (vd: Update loop),
        /// nhưng chỉ thực sự xử lý mỗi <see cref="IncomeTickInterval"/> giây 1 lần:
        /// cộng MoneyPerSecond vào số dư, bắn sự kiện cho UI và ghi xuống DB (qua autosave).
        /// </summary>
        /// <returns>True nếu vừa chạy qua một mốc 1 giây (đã cộng tiền &amp; ghi DB).</returns>
        public bool Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return false;

            _tickTimer += deltaTime;
            if (_tickTimer < IncomeTickInterval) return false;

            // Gom nhiều chu kỳ lại nếu một frame bị khựng lâu hơn 1 giây (tránh mất tiền)
            int elapsedTicks = Mathf.FloorToInt(_tickTimer / IncomeTickInterval);
            _tickTimer -= elapsedTicks * IncomeTickInterval;

            if (_moneyPerSecond <= 0f) return false;

            float gained = _moneyPerSecond * elapsedTicks;
            _currentMoney += gained;

            // NotifyChanged -> Changed (UI refresh) + Save() xuống DB nếu bật autosave
            NotifyChanged(CurrencyType.Money, _currentMoney, gained);
            return true;
        }

        // =====================================================================
        // DELETE / RESET
        // =====================================================================

        /// <summary>Reset một loại tiền tệ về 0.</summary>
        public void Reset(CurrencyType type) => Set(type, 0f);

        /// <summary>Reset toàn bộ tiền tệ về mặc định ban đầu.</summary>
        public void ResetAll()
        {
            SetMoney(500_000f);
            SetMoneyPerSecond(0f);
            SetReputation(0f);
            _tickTimer = 0f;
        }

        private void NotifyChanged(CurrencyType type, float newValue, float delta)
        {
            CurrencyChanged?.Invoke(type, newValue, delta);
            Changed?.Invoke();

            if (_enableAutoSave && !Mathf.Approximately(delta, 0f))
            {
                Save();
            }
        }
    }
}
