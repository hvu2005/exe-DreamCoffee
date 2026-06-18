using System;
using UnityEngine;

namespace DreamCafe.Core.Utils
{
    /// <summary>
    /// Per-handler exception isolation so one throwing subscriber doesn't break the dispatch chain.
    /// TODO: Route caught exceptions to AnalyticsService once wired.
    /// </summary>
    public static class SafeInvoke
    {
        public static void Call<T>(Action<T> action, T arg)
        {
            try { action(arg); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
