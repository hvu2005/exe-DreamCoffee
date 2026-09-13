using System.IO;
using DreamCafe.DataControl;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Công cụ sinh dữ liệu mẫu (Demo Assets) cho CustomerItem, RecipeItem và Repository.
    /// Menu: DreamCafe > Setup > Generate Demo Customers & Recipes
    /// </summary>
    public static class DemoDataGenerator
    {
        private const string CustomerDir = "Assets/_Game/Data/Customers";
        private const string RecipeDir = "Assets/_Game/Data/Recipes";

        [MenuItem("DreamCafe/Setup/Generate Demo Customers & Recipes")]
        public static void Generate()
        {
            Directory.CreateDirectory(CustomerDir);
            Directory.CreateDirectory(RecipeDir);

            // 1. Tạo Recipes
            var rCapheDen = CreateOrGetRecipe("recipe_caphe_den", "Cà Phê Đen", "Cà phê đen nguyên chất đậm đà vị Việt.", 20000, 2.5f, true, "coffee_bean");
            var rCapheSua = CreateOrGetRecipe("recipe_caphe_sua", "Cà Phê Sữa", "Cà phê đen pha cùng sữa đặc thơm ngọt.", 25000, 3f, true, "coffee_bean", "milk");
            var rBacXiu = CreateOrGetRecipe("recipe_bac_xiu", "Bạc Xỉu", "Nhiều sữa ít cà phê, béo ngậy vani.", 30000, 3.5f, true, "coffee_bean", "milk", "gelato_vanilla");
            var rTraMango = CreateOrGetRecipe("recipe_tra_mango", "Trà Xoài Nhiệt Đới", "Trà thanh mát kết hợp xoài tươi.", 35000, 3.5f, false, "tea", "mango");
            var rSpecial = CreateOrGetRecipe("recipe_special_brew", "Cà Phê Đặc Biệt", "Công thức bí truyền của quán.", 50000, 4f, false, "coffee_bean", "special");

            var allRecipes = new[] { rCapheDen, rCapheSua, rBacXiu, rTraMango, rSpecial };

            // Tạo RecipeRepository
            var recipeRepoPath = $"{RecipeDir}/RecipeRepository.asset";
            var recipeRepo = AssetDatabase.LoadAssetAtPath<ScriptableRecipeRepository>(recipeRepoPath);
            if (recipeRepo == null)
            {
                recipeRepo = ScriptableObject.CreateInstance<ScriptableRecipeRepository>();
                AssetDatabase.CreateAsset(recipeRepo, recipeRepoPath);
            }
            var soRecipe = new SerializedObject(recipeRepo);
            var propRecipes = soRecipe.FindProperty("_recipes");
            propRecipes.arraySize = allRecipes.Length;
            for (int i = 0; i < allRecipes.Length; i++)
            {
                propRecipes.GetArrayElementAtIndex(i).objectReferenceValue = allRecipes[i];
            }
            soRecipe.ApplyModifiedProperties();

            // 2. Tạo Customers
            var cSinhVien = CreateOrGetCustomer("cus_sinh_vien", "Bạn Sinh Viên", "Thích ngồi học bài, thích món ngọt thanh giá sinh viên.",
                CustomerType.Student, 35f, 15000, 35000, 0.25f, 8f, 3, true,
                "recipe_caphe_den", "recipe_caphe_sua", "recipe_tra_mango");

            var cVanPhong = CreateOrGetCustomer("cus_van_phong", "Dân Văn Phòng", "Cần nạp caffein buổi sáng, uống nhanh đi làm.",
                CustomerType.Worker, 25f, 25000, 60000, 0.5f, 6f, 3, true,
                "recipe_caphe_den", "recipe_caphe_sua", "recipe_bac_xiu");

            var cDuKhach = CreateOrGetCustomer("cus_du_khach", "Khách Du Lịch", "Muốn thử hương vị cà phê đường phố đặc trưng.",
                CustomerType.Tourist, 45f, 30000, 80000, 0.65f, 12f, 2, false,
                "recipe_bac_xiu", "recipe_tra_mango", "recipe_special_brew");

            var cVip = CreateOrGetCustomer("cus_vip", "Khách Hàng VIP", "Sành điệu, sẵn sàng chi mạnh cho món độc quyền.",
                CustomerType.VIP, 20f, 60000, 200000, 0.9f, 10f, 2, false,
                "recipe_special_brew", "recipe_bac_xiu", "recipe_tra_mango");

            var allCustomers = new[] { cSinhVien, cVanPhong, cDuKhach, cVip };

            // Tạo CustomerRepository
            var customerRepoPath = $"{CustomerDir}/CustomerRepository.asset";
            var customerRepo = AssetDatabase.LoadAssetAtPath<ScriptableCustomerRepository>(customerRepoPath);
            if (customerRepo == null)
            {
                customerRepo = ScriptableObject.CreateInstance<ScriptableCustomerRepository>();
                AssetDatabase.CreateAsset(customerRepo, customerRepoPath);
            }
            var soCust = new SerializedObject(customerRepo);
            var propCust = soCust.FindProperty("_customers");
            propCust.arraySize = allCustomers.Length;
            for (int i = 0; i < allCustomers.Length; i++)
            {
                propCust.GetArrayElementAtIndex(i).objectReferenceValue = allCustomers[i];
            }
            soCust.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[DemoDataGenerator] Đã tạo thành công {allRecipes.Length} recipes và {allCustomers.Length} customers kèm repositories.");
        }

        private static RecipeItem CreateOrGetRecipe(string id, string displayName, string desc, int price, float craftTime, bool unlocked, params string[] ingredients)
        {
            string path = $"{RecipeDir}/{id}.asset";
            var item = AssetDatabase.LoadAssetAtPath<RecipeItem>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<RecipeItem>();
                AssetDatabase.CreateAsset(item, path);
            }

            var so = new SerializedObject(item);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = desc;
            so.FindProperty("_basePrice").intValue = price;
            so.FindProperty("_craftTimeSeconds").floatValue = craftTime;
            so.FindProperty("_isDefaultUnlocked").boolValue = unlocked;

            var propIng = so.FindProperty("_requiredIngredientIds");
            propIng.arraySize = ingredients.Length;
            for (int i = 0; i < ingredients.Length; i++)
            {
                propIng.GetArrayElementAtIndex(i).stringValue = ingredients[i];
            }

            so.ApplyModifiedProperties();
            return item;
        }

        private static CustomerItem CreateOrGetCustomer(string id, string name, string desc, CustomerType type, float patience, int minB, int maxB, float tip, float dining, int threshold, bool unlocked, params string[] favorites)
        {
            string path = $"{CustomerDir}/{id}.asset";
            var item = AssetDatabase.LoadAssetAtPath<CustomerItem>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<CustomerItem>();
                AssetDatabase.CreateAsset(item, path);
            }

            var so = new SerializedObject(item);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_description").stringValue = desc;
            so.FindProperty("_customerType").enumValueIndex = (int)type;
            so.FindProperty("_basePatienceSeconds").floatValue = patience;
            so.FindProperty("_budgetMin").intValue = minB;
            so.FindProperty("_budgetMax").intValue = maxB;
            so.FindProperty("_tipChance").floatValue = tip;
            so.FindProperty("_diningTimeSeconds").floatValue = dining;
            so.FindProperty("_unlockThreshold").intValue = threshold;
            so.FindProperty("_isDefaultUnlocked").boolValue = unlocked;

            var propFav = so.FindProperty("_favoriteRecipeIds");
            propFav.arraySize = favorites.Length;
            for (int i = 0; i < favorites.Length; i++)
            {
                propFav.GetArrayElementAtIndex(i).stringValue = favorites[i];
            }

            so.ApplyModifiedProperties();
            return item;
        }
    }
}
