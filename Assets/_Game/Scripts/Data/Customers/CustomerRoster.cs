using UnityEngine;

namespace DreamCafe.Data
{
    /// <summary>
    /// Lists all customer archetypes available for spawning.
    /// Place one instance at Assets/_Game/Resources/CustomerRoster.asset for runtime loading.
    /// Add new CustomerData assets here to unlock new customer types.
    /// </summary>
    [CreateAssetMenu(fileName = "CustomerRoster", menuName = "DreamCafe/Data/CustomerRoster")]
    public sealed class CustomerRoster : ScriptableObject
    {
        public CustomerData[] customers = System.Array.Empty<CustomerData>();
    }
}
