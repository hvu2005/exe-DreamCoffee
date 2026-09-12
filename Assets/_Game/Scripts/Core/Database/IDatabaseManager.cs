using DreamCafe.Core.Services;
using UnityEngine;

namespace DreamCafe.Core.Database
{
    /// <summary>
    /// Sổ tra cứu SO chung, key tự động = tên kiểu (typeof(T).Name) — mỗi kiểu chỉ giữ 1 asset
    /// (vd: 1 repository asset đại diện cho cả bảng). Hệ thống khác resolve IDatabaseManager qua
    /// ServiceContext.Services rồi Get&lt;T&gt;() mà không cần biết ai đang giữ asset gốc.
    /// </summary>
    public interface IDatabaseManager : IService
    {
        void Add(ScriptableObject asset);
        T Get<T>() where T : ScriptableObject;
    }
}
