using System.Collections.Generic;
using System.Linq;
using DreamCafe.Core.Database;
using DreamCafe.Core.Utils;
using DreamCafe.SystemControl.UI;
using UnityEngine;

namespace DreamCafe.DataControl
{
    /// <summary>
    /// Singleton quản lý và cung cấp toàn bộ các Runtime CRUD Controller của trò chơi:
    /// - <see cref="Customer"/>: Quản lý khách hàng runtime (mở khóa, tra cứu)
    /// - <see cref="Recipe"/>: Quản lý công thức, pair value matching và discovery
    /// - <see cref="Currency"/>: Quản lý số dư tiền mặt, tiền/s, danh tiếng
    /// - <see cref="Inventory"/>: Quản lý số lượng tồn kho
    ///
    /// Tự động liên kết với Scriptable Repositories từ DatabaseManager và bind TopBarCurrencyView.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameSystemsProvider : MonoSingleton<GameSystemsProvider>
    {
        private CustomerController _customerController;
        private RecipeController _recipeController;
        private CurrencyController _currencyController;
        private InventoryController _inventoryController;
        private DecorController _decorController;

        /// <summary>Đang nạp tiến độ cũ — chặn việc ghi save đè lên chính cái vừa đọc lên.</summary>
        private bool _restoringUnlocks;

        /// <summary>Controller quản lý dữ liệu Khách Hàng (CRUD).</summary>
        public CustomerController Customer => _customerController;

        /// <summary>Controller quản lý dữ liệu Công Thức (CRUD & Pair Value).</summary>
        public RecipeController Recipe => _recipeController;

        /// <summary>Controller quản lý Tiền Tệ, Tiền/giây, Danh tiếng (CRUD).</summary>
        public CurrencyController Currency => _currencyController;

        /// <summary>Controller quản lý Kho Nguyên Liệu (CRUD).</summary>
        public InventoryController Inventory => _inventoryController;

        /// <summary>Controller quản lý Nội Thất & Mở Rộng Không Gian (CRUD).</summary>
        public DecorController Decor => _decorController;

        protected override void OnSingletonAwake()
        {
            InitializeControllers();
        }

        private void Start()
        {
            LoadDefinitionsFromDatabase();
            AutoBindTopBar();
        }

        /// <summary>
        /// Nhịp đập của hệ thống tiền tệ: mỗi frame đẩy deltaTime vào CurrencyController,
        /// controller tự gom lại và cứ 1 giây mới cộng MPS vào số dư + ghi xuống DB.
        /// Đặt ở đây (thay vì trên TopBarCurrencyView) để tiền vẫn chạy khi thanh top bar bị ẩn.
        /// </summary>
        private void Update()
        {
            _currencyController?.Tick(Time.deltaTime);
        }

        private void InitializeControllers()
        {
            _customerController = new CustomerController();
            _recipeController = new RecipeController();
            _currencyController = new CurrencyController();
            _inventoryController = new InventoryController();
            _decorController = new DecorController();

            _customerController.Init(null);
            _recipeController.Init(null);
            _currencyController.Init(null);
            _inventoryController.Init(null);
            _decorController.Init(null);

            // Khi một công thức mới được mở khóa => Tự động kiểm tra mở khóa khách hàng nếu đủ món yêu thích!
            _recipeController.RecipeUnlocked += OnRecipeUnlocked;
        }

        private void LoadDefinitionsFromDatabase()
        {
            var db = DatabaseManager.Instance ?? FindFirstObjectByType<DatabaseManager>();
            if (db == null) return;

            // Load khách hàng từ DatabaseManager
            var customerRepo = db.Get<ScriptableCustomerRepository>();
            if (customerRepo != null)
            {
                _customerController.RegisterRange(customerRepo.GetAllCustomers());
            }

            // Load công thức từ DatabaseManager
            var recipeRepo = db.Get<ScriptableRecipeRepository>();
            if (recipeRepo != null)
            {
                _recipeController.AddRecipes(recipeRepo.GetAllRecipes());
                RestoreUnlockedRecipes();
            }

            // Load nguyên liệu kho từ DatabaseManager (nếu có sẵn)
            var invRepo = db.Get<ScriptableInventoryItemRepository>();
            if (invRepo != null)
            {
                foreach (var item in invRepo.GetAllItems())
                {
                    if (item != null && item.Quantity > 0)
                    {
                        _inventoryController.Add(item, item.Quantity);
                    }
                }
            }

            // Load danh mục nội thất & khu vực mở rộng từ DatabaseManager
            var decorRepo = db.Get<ScriptableDecorRepository>();
            if (decorRepo != null)
            {
                _decorController.RegisterFromRepository(decorRepo);
            }

            // Load cấu hình tiền tệ từ DatabaseManager (tương tự Customer & Recipe)
            var currencyRepo = db.Get<ScriptableCurrencyRepository>();
            if (currencyRepo != null)
            {
                _currencyController.RegisterFromRepository(currencyRepo);
            }

            // MPS được dựng lại mỗi lần khởi động: base trong CurrencyRepository + bonus của các công
            // thức đang mở khoá. Phải chạy SAU RegisterFromRepository vì hàm đó kéo MPS về đúng base.
            ApplyRecipeMpsBonuses();
        }

        /// <summary>
        /// Cộng dồn tiền/giây của toàn bộ công thức đang mở khoá vào MPS của quán.
        /// Nhờ vậy mở khoá món mới là quán kiếm thêm tiền thụ động vĩnh viễn, và số đó không mất
        /// sau khi tắt game — nó được tính lại từ danh sách công thức đã lưu chứ không lưu riêng.
        /// </summary>
        private void ApplyRecipeMpsBonuses()
        {
            if (_recipeController == null || _currencyController == null) return;

            float total = 0f;
            foreach (var recipe in _recipeController.GetUnlocked())
            {
                if (recipe != null) total += recipe.MoneyPerSecondBonus;
            }

            if (total <= 0f) return;

            _currencyController.AddMoneyPerSecond(total);
            Debug.Log($"[GameSystemsProvider] Cộng {total:N0}đ/s vào MPS từ {_recipeController.UnlockedCount} công thức đã mở khoá.");
        }

        private void AutoBindTopBar()
        {
            var topBar = FindFirstObjectByType<TopBarCurrencyView>();
            if (topBar != null && _currencyController != null)
            {
                topBar.Bind(_currencyController);
            }
        }

        /// <summary>
        /// Mở khoá lại những công thức người chơi đã mò ra ở các lần chơi trước. Khách hàng đi kèm
        /// cũng tự mở lại theo, vì mỗi lần UnlockRecipe đều chạy qua <see cref="OnRecipeUnlocked"/>.
        /// </summary>
        private void RestoreUnlockedRecipes()
        {
            // Bật cờ để khỏi ghi đè lại đúng cái vừa đọc lên: mỗi lần mở khoá đều kích Save().
            _restoringUnlocks = true;
            int restored = RecipeUnlockStore.Load(_recipeController);
            _restoringUnlocks = false;

            if (restored > 0)
            {
                Debug.Log($"[GameSystemsProvider] Khôi phục {restored} công thức đã mở khoá từ lần chơi trước.");
            }
        }

        private void OnRecipeUnlocked(RecipeItem recipe)
        {
            if (_customerController == null || _recipeController == null) return;

            var unlockedRecipeIds = new HashSet<string>(_recipeController.GetUnlocked().Select(r => r.Id));
            _customerController.CheckAutoUnlock(unlockedRecipeIds);

            // Mò ra món mới là MPS của quán tăng theo luôn. Lúc đang khôi phục save thì bỏ qua, vì
            // ApplyRecipeMpsBonuses() cuối LoadDefinitionsFromDatabase đã cộng gộp một lần rồi.
            if (!_restoringUnlocks && _currencyController != null && recipe != null)
            {
                _currencyController.AddMoneyPerSecond(recipe.MoneyPerSecondBonus);
            }

            // Mò ra món mới là ghi ngay, không chờ tới lúc thoát game — tắt ngang vẫn không mất tiến độ.
            if (!_restoringUnlocks) RecipeUnlockStore.Save(_recipeController);
        }

        private void OnDestroy()
        {
            if (_recipeController != null)
            {
                _recipeController.RecipeUnlocked -= OnRecipeUnlocked;
            }

            _customerController?.Shutdown();
            _recipeController?.Shutdown();
            _currencyController?.Shutdown();
            _inventoryController?.Shutdown();
            _decorController?.Shutdown();
        }
    }
}
