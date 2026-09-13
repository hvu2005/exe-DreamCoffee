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
        private float _currentMoney = 500_000f; // Mặc định khởi đầu: 500.000đ
        private float _moneyPerSecond = 0f;
        private float _reputation = 0f;

        // Bộ đệm cộng dồn tiền theo giây để tránh cập nhật quá dày đặc
        private float _mpsAccumulator = 0f;

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
        /// Cập nhật thời gian thực: cộng tiền thụ động theo tốc độ MoneyPerSecond.
        /// Được gọi từ một Runner/MonoBehaviour (vd: Update loop).
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_moneyPerSecond <= 0f || deltaTime <= 0f) return;

            _mpsAccumulator += _moneyPerSecond * deltaTime;

            // Cộng tiền vào tài khoản khi bộ đệm đạt ít nhất 1 đồng
            if (_mpsAccumulator >= 1f)
            {
                float gained = Mathf.Floor(_mpsAccumulator);
                _mpsAccumulator -= gained;
                _currentMoney += gained;
                NotifyChanged(CurrencyType.Money, _currentMoney, gained);
            }
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
            _mpsAccumulator = 0f;
        }

        private void NotifyChanged(CurrencyType type, float newValue, float delta)
        {
            CurrencyChanged?.Invoke(type, newValue, delta);
            Changed?.Invoke();
        }
    }
}
