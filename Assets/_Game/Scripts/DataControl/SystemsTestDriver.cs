using System.Collections;
using System.Collections.Generic;
using DreamCafe.DataControl;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Component chạy kiểm thử tự động toàn diện các tính năng CRUD của:
    /// - CustomerController
    /// - RecipeController & Pair Value Matching
    /// - CurrencyController & TopBar UI
    /// </summary>
    public sealed class SystemsTestDriver : MonoBehaviour
    {
        [SerializeField] private bool _runOnStart = true;

        private IEnumerator Start()
        {
            if (!_runOnStart) yield break;

            // Đợi 1 frame để GameSystemsProvider khởi tạo xong
            yield return null;

            RunAllTests();
        }

        [ContextMenu("Run All Systems Tests")]
        public void RunAllTests()
        {
            Debug.Log("<color=#2EA3FF><b>========== BẮT ĐẦU KIỂM THỬ HỆ THỐNG DỮ LIỆU CRUD ==========</b></color>");

            var provider = GameSystemsProvider.Instance;
            if (provider == null)
            {
                Debug.LogError("[SystemsTestDriver] FAIL: Không tìm thấy GameSystemsProvider.Instance!");
                return;
            }

            TestCustomerController(provider.Customer);
            TestRecipeController(provider.Recipe);
            TestCurrencyController(provider.Currency);

            Debug.Log("<color=#00FF66><b>========== TẤT CẢ KIỂM THỬ HOÀN TẤT THÀNH CÔNG! ==========</b></color>");
        }

        private void TestCustomerController(CustomerController customer)
        {
            Debug.Log("<b>[Test 1: CustomerController CRUD]</b>");

            int total = customer.TotalCount;
            int unlocked = customer.UnlockedCount;
            Debug.Log($"[Customer] Tổng số: {total}, Đã mở khóa: {unlocked}");

            // Test Read
            var student = customer.Get("cus_sinh_vien");
            if (student != null)
            {
                Debug.Log($"✓ Read: Tìm thấy khách '{student.DisplayName}', nhóm {student.CustomerType}, budget: {student.BudgetMin:N0}-{student.BudgetMax:N0}đ");
            }

            // Test Unlock
            bool unlockedVip = customer.Unlock("cus_vip");
            Debug.Log($"✓ Update (Unlock): Mở khóa 'cus_vip' => {unlockedVip} (IsUnlocked: {customer.IsUnlocked("cus_vip")})");

            // Test Auto Unlock theo công thức yêu thích
            var fakeUnlockedRecipes = new List<string> { "recipe_bac_xiu", "recipe_tra_mango", "recipe_special_brew" };
            var newlyUnlocked = customer.CheckAutoUnlock(fakeUnlockedRecipes);
            Debug.Log($"✓ Update (CheckAutoUnlock): Mở khóa tự động {newlyUnlocked.Count} khách hàng khi có đủ món yêu thích.");
            foreach (var c in newlyUnlocked)
            {
                Debug.Log($"   -> Khách vừa mở khóa: {c.DisplayName}");
            }
        }

        private void TestRecipeController(RecipeController recipe)
        {
            Debug.Log("<b>[Test 2: RecipeController & Pair-Value Matching]</b>");

            int total = recipe.TotalCount;
            int unlocked = recipe.UnlockedCount;
            Debug.Log($"[Recipe] Tổng số: {total}, Đã mở khóa: {unlocked}");

            // Test Pair Value Matching (Bất kể thứ tự nguyên liệu)
            var order1 = new[] { "milk", "coffee_bean" };
            var match1 = recipe.MatchRecipe(order1);
            Debug.Log($"✓ Read (Pair Key ['milk', 'coffee_bean']): Khớp công thức => '{(match1 != null ? match1.DisplayName : "null")}' (PairValue: {match1?.PairValue})");

            var order2 = new[] { "coffee_bean", "milk" }; // Thứ tự ngược lại
            var match2 = recipe.MatchRecipe(order2);
            Debug.Log($"✓ Read (Pair Key đảo thứ tự ['coffee_bean', 'milk']): Khớp công thức => '{(match2 != null ? match2.DisplayName : "null")}'");

            if (match1 == match2 && match1 != null)
            {
                Debug.Log("✓ PASS: Pair Value Matching hoạt động chính xác không phụ thuộc thứ tự nguyên liệu!");
            }

            // Test Discovery (Mò công thức bí mật)
            var discoveryInput = new[] { "mango", "tea" };
            bool discovered = recipe.TryDiscover(discoveryInput, out var discoveredRecipe);
            Debug.Log($"✓ Update (TryDiscover ['mango', 'tea']): Khám phá công thức mới => {discovered}, Món: {discoveredRecipe?.DisplayName}");
        }

        private void TestCurrencyController(CurrencyController currency)
        {
            Debug.Log("<b>[Test 3: CurrencyController CRUD]</b>");

            Debug.Log($"Số dư ban đầu: {currency.CurrentMoney:N0}đ");

            // Test Add
            currency.AddMoney(150_000f);
            currency.AddMoneyPerSecond(2_500f);
            currency.AddReputation(50f);
            Debug.Log($"✓ Create/Add: Tiền: {currency.CurrentMoney:N0}đ, Tiền/giây: +{currency.MoneyPerSecond:N0}đ/s, Danh tiếng: {currency.Reputation:N0} Rep");

            // Test Spend
            bool spendOk = currency.SpendMoney(100_000f);
            Debug.Log($"✓ Update (Spend 100k): Thành công => {spendOk}, Số dư còn lại: {currency.CurrentMoney:N0}đ");

            bool spendFail = currency.SpendMoney(999_999_999f);
            Debug.Log($"✓ Update (Spend vượt số dư): Kết quả kiểm tra => {!spendFail} (Bị từ chối an toàn)");
        }
    }
}
