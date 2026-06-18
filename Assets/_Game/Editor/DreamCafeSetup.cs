using DreamCafe.Data;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.Editor
{
    /// <summary>
    /// One-click demo asset creation for DreamCafé prototype.
    /// Run via menu: Tools > DreamCafé > Create Demo Assets
    /// Creates starter ItemData, RecipeData, CustomerData, and RecipeRepository in Assets/_Game/Data/
    /// </summary>
    public static class DreamCafeSetup
    {
        private const string DataPath    = "Assets/_Game/Data";
        private const string ResourcePath = "Assets/_Game/Resources";

        [MenuItem("Tools/DreamCafé/Create Demo Assets")]
        public static void CreateDemoAssets()
        {
            EnsureFolder(DataPath + "/Items");
            EnsureFolder(DataPath + "/Recipes");
            EnsureFolder(DataPath + "/Customers");
            EnsureFolder(ResourcePath);

            var caPheD = CreateItem("item_caphe_den", "Cà phê đen", ItemType.Drink, ItemCategory.Coffee, 25000f, "Items/item_caphe_den");
            var traSua  = CreateItem("item_tra_sua",   "Trà sữa",    ItemType.Drink, ItemCategory.Tea,    35000f, "Items/item_tra_sua");
            var banhMi  = CreateItem("item_banh_mi",   "Bánh mì",    ItemType.Food,  ItemCategory.Bread,  20000f, "Items/item_banh_mi");

            CreateRecipe("recipe_caphe_den", caPheD,  new[] { "ng_ca_phe", "ng_nuoc" },        "Recipes/recipe_caphe_den");
            CreateRecipe("recipe_tra_sua",   traSua,  new[] { "ng_tra", "ng_sua", "ng_duong" }, "Recipes/recipe_tra_sua");
            CreateRecipe("recipe_banh_mi",   banhMi,  new[] { "ng_bot_mi", "ng_nhan" },         "Recipes/recipe_banh_mi");

            var sinh_vien    = CreateCustomer("cus_sinh_vien",     "Sinh viên",       CustomerType.Student, 25f, 20000f,  40000f,  0.3f, "Customers/cus_sinh_vien");
            var van_phong    = CreateCustomer("cus_dan_van_phong", "Dân văn phòng",   CustomerType.Worker,  35f, 35000f,  60000f,  0.5f, "Customers/cus_dan_van_phong");
            var du_khach     = CreateCustomer("cus_du_khach",      "Du khách",        CustomerType.Tourist, 30f, 40000f,  80000f,  0.6f, "Customers/cus_du_khach");
            var vip          = CreateCustomer("cus_vip",           "Khách VIP",       CustomerType.VIP,     60f, 80000f, 200000f,  0.9f, "Customers/cus_vip");

            CreateRecipeRepository("Recipes/RecipeRepository");
            CreateCustomerRoster(new[] { sinh_vien, van_phong, du_khach, vip });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DreamCafeSetup] ✓ Demo assets created in Assets/_Game/Data/");
            EditorUtility.DisplayDialog("DreamCafé Setup", "Demo assets created!\n\nAssets/_Game/Data/ and Resources/CustomerRoster.asset are ready.", "OK");
        }

        [MenuItem("Tools/DreamCafé/Create Main Scene")]
        public static void CreateMainScene()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            var bootstrapGO = new GameObject("GameBootstrap");
            bootstrapGO.AddComponent<DreamCafe.App.GameBootstrap>();

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Main.unity");
            AssetDatabase.Refresh();
            Debug.Log("[DreamCafeSetup] ✓ Main scene created at Assets/_Game/Scenes/Main.unity");
        }

        // --- Helpers ---

        private static ItemData CreateItem(string id, string displayName, ItemType type, ItemCategory cat, float price, string assetPath)
        {
            var full = $"{DataPath}/{assetPath}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ItemData>(full);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<ItemData>();
            so.itemId = id;
            so.displayName = displayName;
            so.itemType = type;
            so.category = cat;
            so.basePrice = price;
            so.craftTimeSeconds = 0f; // tap-to-complete = instant
            AssetDatabase.CreateAsset(so, full);
            return so;
        }

        private static void CreateRecipe(string id, ItemData output, string[] ingredients, string assetPath)
        {
            var full = $"{DataPath}/{assetPath}.asset";
            if (AssetDatabase.LoadAssetAtPath<RecipeData>(full) != null) return;

            var so = ScriptableObject.CreateInstance<RecipeData>();
            so.recipeId = id;
            so.outputItem = output;
            so.requiredIngredientIds = ingredients;
            so.isUnlockedByDefault = true;
            AssetDatabase.CreateAsset(so, full);
        }

        private static CustomerData CreateCustomer(string id, string name, CustomerType type, float patience, float budMin, float budMax, float tipChance, string assetPath)
        {
            var full = $"{DataPath}/{assetPath}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CustomerData>(full);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<CustomerData>();
            so.customerId = id;
            so.displayName = name;
            so.customerType = type;
            so.patienceSeconds = patience;
            so.budgetMin = budMin;
            so.budgetMax = budMax;
            so.tipChance = tipChance;
            so.favouriteItemIds = System.Array.Empty<string>();
            so.tintColor = GetTypeColor(type);
            AssetDatabase.CreateAsset(so, full);
            return so;
        }

        private static void CreateCustomerRoster(CustomerData[] customers)
        {
            var full = $"{ResourcePath}/CustomerRoster.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CustomerRoster>(full);
            if (existing != null)
            {
                existing.customers = customers;
                EditorUtility.SetDirty(existing);
                return;
            }

            var so = ScriptableObject.CreateInstance<CustomerRoster>();
            so.customers = customers;
            AssetDatabase.CreateAsset(so, full);
        }

        private static void CreateRecipeRepository(string assetPath)
        {
            var full = $"{DataPath}/{assetPath}.asset";
            if (AssetDatabase.LoadAssetAtPath<ScriptableRecipeRepository>(full) != null) return;
            var so = ScriptableObject.CreateInstance<ScriptableRecipeRepository>();
            AssetDatabase.CreateAsset(so, full);
            Debug.Log($"[DreamCafeSetup] RecipeRepository created — open it to assign the 3 recipes, or re-run setup.");
        }

        private static Color GetTypeColor(CustomerType type) => type switch
        {
            CustomerType.Student    => new Color(0.4f, 0.8f, 1f),
            CustomerType.Worker     => new Color(0.8f, 0.8f, 0.4f),
            CustomerType.Tourist    => new Color(0.4f, 1f, 0.6f),
            CustomerType.VIP        => new Color(1f, 0.8f, 0.2f),
            _                       => Color.white
        };

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                var parent = string.Join("/", parts, 0, parts.Length - 1);
                AssetDatabase.CreateFolder(parent, parts[parts.Length - 1]);
            }
        }
    }
}
