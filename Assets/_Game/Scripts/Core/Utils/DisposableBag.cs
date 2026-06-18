using System;
using System.Collections.Generic;

namespace DreamCafe.Core.Utils
{
    /// <summary>
    /// Collects unsubscribe callbacks and invokes all on Dispose.
    /// Use in ControllerBase to cleanly remove EventBus subscriptions on Unbind/Despawn.
    /// TODO: Add priority ordering if teardown sequence matters.
    /// </summary>
    public sealed class DisposableBag : IDisposable
    {
        private readonly List<Action> _disposables = new();

        public void Add(Action onDispose) => _disposables.Add(onDispose);

        public void Dispose()
        {
            foreach (var d in _disposables)
                d?.Invoke();
            _disposables.Clear();
        }
    }
}
