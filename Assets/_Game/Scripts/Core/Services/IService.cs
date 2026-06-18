namespace DreamCafe.Core.Services
{
    /// <summary>
    /// Lifecycle contract for all registered game services.
    /// Init receives the full ServiceContext; Shutdown must release all subscriptions and refs.
    /// TODO: Add async Init overload for remote-config or network-bound services.
    /// </summary>
    public interface IService
    {
        void Init(ServiceContext ctx);
        void Shutdown();
    }
}
