using DreamCafe.Core.Pooling;
using DreamCafe.Core.Services;

namespace DreamCafe.Core.MVC
{
    /// <summary>
    /// Glue layer between Model, View, EventBus, and Services. Extends IPoolable for pool lifecycle.
    /// Bind receives the ServiceContext; Unbind must release all subscriptions via DisposableBag.
    /// TODO: Add debug validation in Bind for missing required services.
    /// </summary>
    public interface IController : IPoolable
    {
        void Bind(ServiceContext ctx);
        void Unbind();
    }
}
