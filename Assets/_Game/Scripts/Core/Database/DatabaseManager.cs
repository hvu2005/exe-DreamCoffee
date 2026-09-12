using System.Collections.Generic;
using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Core.Database
{
    /// <summary>
    /// MonoBehaviour sống trong scene (đặt trên GameObject "DatabaseManagerSystem"). Kéo thả SO
    /// trực tiếp vào <see cref="records"/> trong Inspector — key tra cứu tự động lấy theo tên kiểu
    /// của asset (typeof(T).Name), nên chỗ khác chỉ cần gọi Get&lt;T&gt;() là nhận được, không cần
    /// tự gõ key. Mỗi kiểu chỉ giữ đúng 1 asset (vd: 1 repository asset đại diện cho cả bảng).
    /// </summary>
    public sealed class DatabaseManager : MonoBehaviour, IDatabaseManager
    {
        [SerializeField] private List<ScriptableObject> records = new();

        private readonly Dictionary<string, ScriptableObject> _table = new();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            BuildIndex();
        }

        private void BuildIndex()
        {
            _table.Clear();
            foreach (var asset in records)
            {
                if (asset == null) continue;
                _table[asset.GetType().Name] = asset;
            }
        }

        public void Init(ServiceContext ctx) =>
            Debug.Log($"[DatabaseManager] Init — {_table.Count} bản ghi.");

        public void Shutdown() => _table.Clear();

        /// <summary>Đăng ký thêm 1 SO lúc runtime (ghi đè nếu cùng kiểu đã tồn tại).</summary>
        public void Add(ScriptableObject asset)
        {
            if (asset == null) return;
            _table[asset.GetType().Name] = asset;
        }

        public T Get<T>() where T : ScriptableObject =>
            _table.TryGetValue(typeof(T).Name, out var asset) ? asset as T : null;
    }
}
