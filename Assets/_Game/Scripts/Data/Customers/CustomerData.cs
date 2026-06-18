using UnityEngine;

namespace DreamCafe.Data
{
    /// <summary>
    /// Config for a customer archetype. Controls patience, budget, and favourite items.
    /// Create via Assets > Create > DreamCafe > Data > Customer.
    /// </summary>
    [CreateAssetMenu(fileName = "CustomerData", menuName = "DreamCafe/Data/Customer")]
    public sealed class CustomerData : ScriptableObject
    {
        public string customerId;
        public string displayName;
        public CustomerType customerType;
        [Min(5f)]  public float patienceSeconds = 30f;
        [Min(0f)]  public float budgetMin = 20000f;
        [Min(0f)]  public float budgetMax = 50000f;
        [Range(0f, 1f)] public float tipChance = 0.5f;
        public string[] favouriteItemIds;
        public Sprite sprite;
        public Color tintColor = Color.white;
    }
}
