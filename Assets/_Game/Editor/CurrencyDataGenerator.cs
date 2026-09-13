using System.IO;
using DreamCafe.DataControl;
using UnityEditor;
using UnityEngine;

namespace DreamCafe.EditorTools
{
    /// <summary>
    /// Công cụ sinh dữ liệu mẫu cho Hệ thống Tiền Tệ:
    /// - currency_money.asset (Tiền mặt, 500.000đ khởi đầu)
    /// - currency_mps.asset (Tiền/giây, 0đ/s)
    /// - currency_reputation.asset (Danh tiếng, 0 Rep)
    /// - CurrencyRepository.asset (Database chứa toàn bộ định nghĩa tiền tệ)
    /// Menu: DreamCafe > Setup > Generate Currency Data
    /// </summary>
    public static class CurrencyDataGenerator
    {
        private const string CurrencyDir = "Assets/_Game/Data/Currency";

        [MenuItem("DreamCafe/Setup/Generate Currency Data")]
        public static void Generate()
        {
            Directory.CreateDirectory(CurrencyDir);

            // Load Icons
            var iconCoin = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/icon_coin2 1.png");
            var iconHeart = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/ico_live 3.png");
            var iconMps = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/ico_special 2.png");

            // 1. Tạo CurrencyItem ScriptableObjects
            var itemMoney = CreateOrGetCurrency(
                "currency_money",
                CurrencyType.Money,
                "Tiền Mặt",
                "đ",
                "Tiền tệ chính dùng để mua sắm nguyên liệu, mở rộng không gian và trang trí nội thất cho quán.",
                iconCoin,
                500_000f,
                0f,
                999_999_999f
            );

            var itemMps = CreateOrGetCurrency(
                "currency_mps",
                CurrencyType.MoneyPerSecond,
                "Tiền Mỗi Giây",
                "đ/s",
                "Tốc độ sinh tiền tự động mỗi giây thu được từ doanh thu quán và buff từ các món nội thất.",
                iconMps,
                0f,
                0f,
                9_999_999f
            );

            var itemRep = CreateOrGetCurrency(
                "currency_reputation",
                CurrencyType.Reputation,
                "Danh Tiếng Quán",
                "Rep",
                "Điểm uy tín tích lũy từ sự hài lòng của khách hàng và trang trí quán, mở khóa công thức và khách VIP.",
                iconHeart,
                0f,
                0f,
                9_999_999f
            );

            var allCurrencies = new[] { itemMoney, itemMps, itemRep };

            // 2. Tạo ScriptableCurrencyRepository
            string repoPath = $"{CurrencyDir}/CurrencyRepository.asset";
            var repo = AssetDatabase.LoadAssetAtPath<ScriptableCurrencyRepository>(repoPath);
            if (repo == null)
            {
                repo = ScriptableObject.CreateInstance<ScriptableCurrencyRepository>();
                AssetDatabase.CreateAsset(repo, repoPath);
            }

            var soRepo = new SerializedObject(repo);
            var propCurrencies = soRepo.FindProperty("_currencies");
            propCurrencies.arraySize = allCurrencies.Length;
            for (int i = 0; i < allCurrencies.Length; i++)
            {
                propCurrencies.GetArrayElementAtIndex(i).objectReferenceValue = allCurrencies[i];
            }
            soRepo.FindProperty("_enableAutoSave").boolValue = true;
            soRepo.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CurrencyDataGenerator] Đã tạo thành công {allCurrencies.Length} định nghĩa tiền tệ tại '{CurrencyDir}'!");
        }

        private static CurrencyItem CreateOrGetCurrency(string id, CurrencyType type, string name, string unit, string desc,
            Sprite icon, float startingValue, float minValue, float maxValue)
        {
            string path = $"{CurrencyDir}/{id}.asset";
            var item = AssetDatabase.LoadAssetAtPath<CurrencyItem>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<CurrencyItem>();
                AssetDatabase.CreateAsset(item, path);
            }

            var so = new SerializedObject(item);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_type").enumValueIndex = (int)type;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_unitSymbol").stringValue = unit;
            so.FindProperty("_description").stringValue = desc;
            so.FindProperty("_icon").objectReferenceValue = icon;
            so.FindProperty("_startingValue").floatValue = startingValue;
            so.FindProperty("_minValue").floatValue = minValue;
            so.FindProperty("_maxValue").floatValue = maxValue;
            so.ApplyModifiedProperties();

            return item;
        }
    }
}
