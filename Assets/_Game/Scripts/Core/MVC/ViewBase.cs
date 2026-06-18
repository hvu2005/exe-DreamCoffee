using UnityEngine;

namespace DreamCafe.Core.MVC
{
    /// <summary>
    /// Base for all prefab View MonoBehaviours. Pure presentation — no bus, no services.
    /// Cache SpriteRenderers, transforms, and UI elements in Awake; update in Render().
    /// TODO: Add theme/style injection in Phase 4.
    /// </summary>
    public abstract class ViewBase : MonoBehaviour, IView
    {
        public abstract void Render(IModel model);
    }
}
