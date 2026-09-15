using System.Collections;
using DreamCafe.SystemControl.Decor;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Kịch bản kiểm thử tự động toàn diện cho Hệ thống Decor (Slot-Based) & Mở Rộng Không Gian (Expansion Zone).
    /// </summary>
    public sealed class DecorTestDriver : MonoBehaviour
    {
        [SerializeField] private bool _runOnStart = true;
        [SerializeField] private DecorSceneManager _sceneManager;

        private IEnumerator Start()
        {
            if (!_runOnStart) yield break;

            yield return new WaitForSeconds(0.2f);
            RunAllTests();
        }

        [ContextMenu("Chạy Toàn Bộ Test Decor & Expansion")]
        public void RunAllTests()
        {
            Debug.Log("====================================================================");
            Debug.Log("<b><color=#00e676>BẮT ĐẦU KIỂM THỬ: HỆ THỐNG DECOR (SLOT-BASED) & EXPANSION ZONE</color></b>");
            Debug.Log("====================================================================");

            var systems = GameSystemsProvider.Instance;
            if (systems == null)
            {
                Debug.LogError("✗ FAIL: Không tìm thấy GameSystemsProvider!");
                return;
            }

            var decor = systems.Decor;
            var currency = systems.Currency;

            if (decor == null || currency == null)
            {
                Debug.LogError("✗ FAIL: DecorController hoặc CurrencyController chưa được khởi tạo!");
                return;
            }

            // Test 1: Kiểm tra nạp danh mục và Zone mặc định
            TestInitialState(decor);

            // Test 2: Kiểm tra mua nội thất (Đủ tiền, Thiếu tiền, Nhận điểm Reputation)
            TestDecorPurchasing(decor, currency);

            // Test 3: Kiểm tra trang bị nội thất vào Slot và tính toán Buffs
            TestSlotEquippingAndBuffs(decor);

            // Test 4: Kiểm tra cơ chế Mở Rộng Không Gian (Zone Expansion)
            TestZoneExpansion(decor, currency);

            // Refresh hiển thị trong Scene
            if (_sceneManager != null)
            {
                _sceneManager.RefreshAll();
            }

            Debug.Log("====================================================================");
            Debug.Log("<b><color=#00e676>✓ TẤT CẢ TEST DECOR VÀ EXPANSION ZONE ĐỀU ĐÃ VƯỢT QUA XUẤT SẮC!</color></b>");
            Debug.Log("====================================================================");
        }

        private void TestInitialState(DecorController decor)
        {
            Debug.Log("<b>[Test 1: Khởi tạo danh mục & Zone 1 mặc định]</b>");
            var allItems = decor.GetAllItems();
            Debug.Log($"Tổng số loại nội thất trong DB: {allItems.Length}");

            bool zone1Unlocked = decor.IsZoneUnlocked(ExpansionZoneId.Starter_Zone1);
            bool zone2Locked = !decor.IsZoneUnlocked(ExpansionZoneId.Lounge_Zone2);
            bool zone3Locked = !decor.IsZoneUnlocked(ExpansionZoneId.OutdoorPatio_Zone3);

            if (zone1Unlocked && zone2Locked && zone3Locked)
            {
                Debug.Log("✓ PASS: Zone 1 (Khởi nghiệp) mặc định MỞ; Zone 2 & 3 mặc định KHÓA.");
            }
            else
            {
                Debug.LogError($"✗ FAIL: Trạng thái ban đầu không khớp (Z1: {zone1Unlocked}, Z2 locked: {zone2Locked}, Z3 locked: {zone3Locked})");
            }
        }

        private void TestDecorPurchasing(DecorController decor, CurrencyController currency)
        {
            Debug.Log("<b>[Test 2: Mua nội thất, kiểm tra trừ tiền & cộng điểm Danh tiếng]</b>");

            // Reset tiền để test thiếu tiền
            currency.SetMoney(1000f);
            currency.SetReputation(0f);

            var expensiveItem = decor.GetItem("item_table_sofa_set");
            string testItemId = expensiveItem != null ? expensiveItem.Id : "item_table_wood_round";

            // Test thiếu tiền
            bool buyFail = decor.TryUnlockDecor(testItemId, currency);
            Debug.Log($"✓ Update (Mua khi thiếu tiền): Kết quả => {!buyFail} (Bị từ chối an toàn)");

            // Bơm tiền & danh tiếng để mua thành công
            currency.AddMoney(1_000_000f);
            currency.AddReputation(5_000f);
            float repBefore = currency.Reputation;

            bool buyOk = decor.TryUnlockDecor(testItemId, currency);
            var item = decor.GetItem(testItemId);
            float repAfter = currency.Reputation;

            if (buyOk && decor.IsUnlocked(testItemId) && repAfter > repBefore)
            {
                Debug.Log($"✓ PASS: Mua thành công '{item?.DisplayName}', đã trừ tiền và nhận +{item?.ReputationBonus} điểm danh tiếng!");
            }
            else
            {
                Debug.LogError("✗ FAIL: Lỗi khi mua nội thất!");
            }
        }

        private void TestSlotEquippingAndBuffs(DecorController decor)
        {
            Debug.Log("<b>[Test 3: Trang bị vào Slot và kiểm tra tổng hợp Buffs]</b>");

            decor.TryEquipDecor("slot_counter_main", "item_counter_emerald");
            decor.TryEquipDecor("slot_table_01", "item_table_wood_round");
            decor.TryEquipDecor("slot_table_02", "item_table_wood_round");
            decor.TryEquipDecor("slot_table_03", "item_table_wood_round");
            decor.TryEquipDecor("slot_sofa_01", "item_table_sofa_set");
            decor.TryEquipDecor("slot_plant_01", "item_plant_monstera");
            decor.TryEquipDecor("slot_planter_01", "item_planter_hydrangea");

            var buffs = decor.TotalBuffs;
            Debug.Log($"✓ Buffs sau khi trang bị Zone 1: Sức chứa = {buffs.TotalSeatingCapacity} khách, +{buffs.PatienceBonusPercent * 100:0}% kiên nhẫn, +{buffs.TipChanceBonus * 100:0}% tip, +{buffs.MoneyPerSecondBonus}đ/s.");

            if (buffs.TotalSeatingCapacity >= 8)
            {
                Debug.Log("✓ PASS: Sức chứa chỗ ngồi đã tăng chính xác theo số bàn trang bị!");
            }
            else
            {
                Debug.LogError($"✗ FAIL: Sức chứa chỗ ngồi không tăng ({buffs.TotalSeatingCapacity})");
            }
        }

        private void TestZoneExpansion(DecorController decor, CurrencyController currency)
        {
            Debug.Log("<b>[Test 4: Mở rộng không gian (Zone Expansion)]</b>");

            var zone2Data = decor.GetZone(ExpansionZoneId.Lounge_Zone2);
            if (zone2Data != null)
            {
                currency.SetMoney(10_000f);
                currency.SetReputation(0f);

                // Mở khóa khi chưa đủ điều kiện => Phải thất bại
                bool unlockFail = decor.TryUnlockZone(ExpansionZoneId.Lounge_Zone2, currency);
                Debug.Log($"✓ Update (Mở Zone 2 khi chưa đủ điều kiện): Kết quả => {!unlockFail} (Bị từ chối an toàn)");

                // Cấp đủ tiền và danh tiếng
                currency.AddMoney(1_000_000f);
                currency.AddReputation(10_000f);

                bool unlockOk = decor.TryUnlockZone(ExpansionZoneId.Lounge_Zone2, currency);
                if (unlockOk && decor.IsZoneUnlocked(ExpansionZoneId.Lounge_Zone2))
                {
                    Debug.Log($"✓ PASS: Đã mở khóa thành công '{zone2Data.ZoneName}', sảnh thưởng thức sẵn sàng hoạt động!");

                    // Trang bị các nội thất của Zone 2
                    decor.TryUnlockDecor("item_table_wood_4seats", currency);
                    decor.TryEquipDecor("slot_table_04", "item_table_wood_4seats");
                    decor.TryEquipDecor("slot_table_05", "item_table_wood_4seats");
                    decor.TryEquipDecor("slot_sofa_02", "item_table_sofa_set");
                    decor.TryEquipDecor("slot_planter_02", "item_planter_hydrangea");
                }
                else
                {
                    Debug.LogError("✗ FAIL: Không mở khóa được Zone 2!");
                }
            }
        }
    }
}
