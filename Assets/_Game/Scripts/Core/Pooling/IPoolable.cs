namespace DreamCafe.Core.Pooling
{
    /// <summary>
    /// Pool lifecycle callbacks for MonoBehaviour components that participate in the object pool.
    /// OnSpawned fires after activation; OnDespawned fires before deactivation.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
