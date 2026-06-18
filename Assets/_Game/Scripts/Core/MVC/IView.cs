namespace DreamCafe.Core.MVC
{
    /// <summary>
    /// Pure presentation contract. Views receive a model snapshot and render — nothing more.
    /// Never subscribes to the EventBus or resolves services directly.
    /// TODO: Add theme/style injection in Phase 4.
    /// </summary>
    public interface IView
    {
        void Render(IModel model);
    }
}
