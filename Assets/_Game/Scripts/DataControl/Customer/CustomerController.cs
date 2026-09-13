using System;
using System.Collections.Generic;
using System.Linq;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Controller quản lý dữ liệu Khách Hàng (Customer) tại Runtime theo mô hình CRUD.
    ///
    ///   Create -> <see cref="Register"/>, <see cref="RegisterRange"/>
    ///   Read   -> <see cref="Get"/>, <see cref="GetAll"/>, <see cref="GetUnlocked"/>, <see cref="GetLocked"/>, <see cref="IsUnlocked"/>
    ///   Update -> <see cref="Unlock"/>, <see cref="Lock"/>, <see cref="CheckAutoUnlock"/>
    ///   Delete -> <see cref="Remove"/>, <see cref="ResetAll"/>
    ///
    /// Kế thừa IService để tích hợp vào ServiceManager và CompositionRoot.
    /// </summary>
    public sealed class CustomerController : IService
    {
        private readonly Dictionary<string, CustomerItem> _customers = new();
        private readonly HashSet<string> _unlockedIds = new();

        /// <summary>Bắn sự kiện khi danh sách hoặc trạng thái mở khóa của khách hàng thay đổi.</summary>
        public event Action Changed;

        /// <summary>Bắn sự kiện khi có một khách hàng mới được mở khóa.</summary>
        public event Action<CustomerItem> CustomerUnlocked;

        /// <summary>Tổng số lượng khách hàng đã đăng ký.</summary>
        public int TotalCount => _customers.Count;

        /// <summary>Số lượng khách hàng đã được mở khóa.</summary>
        public int UnlockedCount => _unlockedIds.Count;

        // =====================================================================
        // IService Lifecycle
        // =====================================================================

        /// <summary>Khởi tạo controller với ServiceContext.</summary>
        public void Init(ServiceContext ctx)
        {
            Debug.Log($"[CustomerController] Initialized — {_customers.Count} customers registered ({_unlockedIds.Count} unlocked).");
        }

        /// <summary>Dọn dẹp tài nguyên khi hệ thống tắt.</summary>
        public void Shutdown()
        {
            Changed = null;
            CustomerUnlocked = null;
            _customers.Clear();
            _unlockedIds.Clear();
        }

        // =====================================================================
        // CREATE / REGISTER
        // =====================================================================

        /// <summary>
        /// Đăng ký một định nghĩa khách hàng vào hệ thống runtime.
        /// </summary>
        /// <param name="definition">Asset định nghĩa khách hàng.</param>
        /// <param name="forceUnlocked">Ghi đè trạng thái mở khóa nếu chỉ định (mặc định lấy theo IsDefaultUnlocked).</param>
        public bool Register(CustomerItem definition, bool? forceUnlocked = null)
        {
            if (definition == null)
            {
                Debug.LogWarning("[CustomerController] Không thể đăng ký khách hàng null.");
                return false;
            }

            string id = definition.Id;
            _customers[id] = definition;

            bool isUnlocked = forceUnlocked ?? definition.IsDefaultUnlocked;
            if (isUnlocked)
            {
                _unlockedIds.Add(id);
            }
            else
            {
                _unlockedIds.Remove(id);
            }

            RaiseChanged();
            return true;
        }

        /// <summary>Đăng ký hàng loạt khách hàng (vd từ Repository).</summary>
        public void RegisterRange(IEnumerable<CustomerItem> definitions)
        {
            if (definitions == null) return;
            foreach (var item in definitions)
            {
                if (item != null)
                {
                    _customers[item.Id] = item;
                    if (item.IsDefaultUnlocked)
                    {
                        _unlockedIds.Add(item.Id);
                    }
                }
            }
            RaiseChanged();
        }

        // =====================================================================
        // READ
        // =====================================================================

        /// <summary>Lấy thông tin khách hàng theo ID.</summary>
        public CustomerItem Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _customers.TryGetValue(id, out var customer) ? customer : null;
        }

        /// <summary>Lấy toàn bộ khách hàng đã đăng ký.</summary>
        public IReadOnlyCollection<CustomerItem> GetAll() => _customers.Values;

        /// <summary>Lấy danh sách các khách hàng ĐÃ MỞ KHÓA.</summary>
        public IReadOnlyList<CustomerItem> GetUnlocked()
        {
            var list = new List<CustomerItem>(_unlockedIds.Count);
            foreach (var id in _unlockedIds)
            {
                if (_customers.TryGetValue(id, out var customer))
                {
                    list.Add(customer);
                }
            }
            return list;
        }

        /// <summary>Lấy danh sách các khách hàng CHƯA MỞ KHÓA.</summary>
        public IReadOnlyList<CustomerItem> GetLocked()
        {
            var list = new List<CustomerItem>();
            foreach (var kvp in _customers)
            {
                if (!_unlockedIds.Contains(kvp.Key))
                {
                    list.Add(kvp.Value);
                }
            }
            return list;
        }

        /// <summary>Kiểm tra xem khách hàng có ID tương ứng đã mở khóa chưa.</summary>
        public bool IsUnlocked(string id)
        {
            return !string.IsNullOrEmpty(id) && _unlockedIds.Contains(id);
        }

        /// <summary>Lọc khách hàng theo phân nhóm (tùy chọn chỉ lấy khách đã mở khóa).</summary>
        public IReadOnlyList<CustomerItem> GetByType(CustomerType type, bool onlyUnlocked = true)
        {
            var source = onlyUnlocked ? GetUnlocked() : (IReadOnlyList<CustomerItem>)_customers.Values.ToList();
            return source.Where(c => c.CustomerType == type).ToList();
        }

        // =====================================================================
        // UPDATE
        // =====================================================================

        /// <summary>Mở khóa một khách hàng.</summary>
        public bool Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || !_customers.ContainsKey(id)) return false;

            if (_unlockedIds.Add(id))
            {
                var customer = _customers[id];
                Debug.Log($"[CustomerController] Đã mở khóa khách hàng: {customer.DisplayName} ({id})");
                CustomerUnlocked?.Invoke(customer);
                RaiseChanged();
                return true;
            }

            return false;
        }

        /// <summary>Khóa lại một khách hàng.</summary>
        public bool Lock(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            if (_unlockedIds.Remove(id))
            {
                RaiseChanged();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tự động kiểm tra và mở khóa các khách hàng nếu người chơi đã mở khóa đủ số món yêu thích.
        /// Theo luật GDD: Mở khóa >= UnlockThreshold (mặc định 3) món trong FavoriteRecipeIds.
        /// </summary>
        /// <param name="unlockedRecipeIds">Tập hợp ID các công thức đã mở khóa.</param>
        /// <returns>Danh sách các khách hàng vừa được mở khóa mới.</returns>
        public IReadOnlyList<CustomerItem> CheckAutoUnlock(IReadOnlyCollection<string> unlockedRecipeIds)
        {
            var newlyUnlocked = new List<CustomerItem>();
            if (unlockedRecipeIds == null || unlockedRecipeIds.Count == 0) return newlyUnlocked;

            var lockedCustomers = GetLocked();
            foreach (var customer in lockedCustomers)
            {
                int matchedFavorites = 0;
                foreach (var favId in customer.FavoriteRecipeIds)
                {
                    if (unlockedRecipeIds.Contains(favId))
                    {
                        matchedFavorites++;
                    }
                }

                if (matchedFavorites >= customer.UnlockThreshold)
                {
                    if (Unlock(customer.Id))
                    {
                        newlyUnlocked.Add(customer);
                    }
                }
            }

            return newlyUnlocked;
        }

        // =====================================================================
        // DELETE / RESET
        // =====================================================================

        /// <summary>Xóa một khách hàng khỏi hệ thống runtime.</summary>
        public bool Remove(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            bool removed = _customers.Remove(id);
            _unlockedIds.Remove(id);

            if (removed)
            {
                RaiseChanged();
            }

            return removed;
        }

        /// <summary>Khôi phục lại toàn bộ trạng thái mở khóa ban đầu theo definition.</summary>
        public void ResetAll()
        {
            _unlockedIds.Clear();
            foreach (var kvp in _customers)
            {
                if (kvp.Value.IsDefaultUnlocked)
                {
                    _unlockedIds.Add(kvp.Key);
                }
            }
            RaiseChanged();
        }

        private void RaiseChanged() => Changed?.Invoke();
    }
}
