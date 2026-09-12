using UnityEngine;

namespace DreamCafe.Core.Utils
{
    /// <summary>
    /// Base cho manager MonoBehaviour sống xuyên scene. Đặt sẵn trên 1 GameObject trong scene đầu
    /// tiên; bản thứ hai xuất hiện (load lại scene, mở scene khác cũng có manager này) sẽ tự huỷ
    /// để luôn chỉ còn đúng 1 instance.
    ///
    /// Kế thừa: <c>class Foo : MonoSingleton&lt;Foo&gt;</c>, rồi override
    /// <see cref="OnSingletonAwake"/> thay vì viết Awake() — viết Awake() ở lớp con sẽ che mất
    /// Awake() của lớp này và hỏng luôn cơ chế singleton.
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        public static T Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[{typeof(T).Name}] Đã có instance khác — huỷ bản trùng trên '{name}'.");
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;
            // DontDestroyOnLoad chỉ có tác dụng với GameObject gốc.
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            OnSingletonAwake();
        }

        private void OnDestroy()
        {
            // Bản trùng bị huỷ ở Awake() chưa từng chạy OnSingletonAwake() nên cũng không dọn gì.
            if (Instance != this) return;

            Instance = null;
            OnSingletonDestroy();
        }

        /// <summary>Chạy 1 lần khi instance thật được nhận — thay cho Awake().</summary>
        protected virtual void OnSingletonAwake() { }

        /// <summary>Dọn dẹp khi instance bị huỷ — thay cho OnDestroy().</summary>
        protected virtual void OnSingletonDestroy() { }
    }
}
