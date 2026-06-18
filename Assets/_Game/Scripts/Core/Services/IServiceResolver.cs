namespace DreamCafe.Core.Services
{
    /// <summary>
    /// Read-only view of the ServiceManager used for lazy peer resolution inside service methods.
    /// Never call Resolve inside a constructor or Init — resolve lazily on first use.
    /// </summary>
    public interface IServiceResolver
    {
        T Resolve<T>() where T : class, IService;
        bool TryResolve<T>(out T service) where T : class, IService;
    }
}
