using System;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// ScriptableObject định nghĩa một khu vực mở rộng không gian quán cà phê (Expansion Zone Definition).
    /// </summary>
    [CreateAssetMenu(fileName = "ExpansionZoneData", menuName = "DreamCafe/Data/ExpansionZoneData")]
    public sealed class ExpansionZoneData : ScriptableObject
    {
        [Header("Thông tin khu vực")]
        [SerializeField] private ExpansionZoneId _zoneId = ExpansionZoneId.Starter_Zone1;
        [SerializeField] private string _zoneName = "Khu Vực Mới";
        [SerializeField, TextArea(2, 4)] private string _description;

        [Header("Điều kiện & Chi phí mở khóa")]
        [SerializeField, Min(0)] private int _unlockPrice = 0;
        [SerializeField, Min(0)] private int _requiredReputation = 0;
        [SerializeField, Min(0)] private int _reputationBonus = 500;
        [SerializeField] private bool _isDefaultUnlocked = false;

        [Header("Danh sách các Decor Slot thuộc khu vực này")]
        [SerializeField] private string[] _slotIdsInZone = Array.Empty<string>();

        // Public Getters
        public ExpansionZoneId ZoneId => _zoneId;
        public string ZoneName => _zoneName;
        public string Description => _description;
        public int UnlockPrice => _unlockPrice;
        public int RequiredReputation => _requiredReputation;
        public int ReputationBonus => _reputationBonus;
        public bool IsDefaultUnlocked => _isDefaultUnlocked;
        public string[] SlotIdsInZone => _slotIdsInZone;
    }
}
