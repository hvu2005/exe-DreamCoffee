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
        }

        private void AutoBindTopBar()
        {
            var topBar = FindFirstObjectByType<TopBarCurrencyView>();
            if (topBar != null && _currencyController != null)
            {
                topBar.Bind(_currencyController);
            }
        }

        private void OnRecipeUnlocked(RecipeItem recipe)
        {
            if (_customerController == null || _recipeController == null) return;

            var unlockedRecipeIds = new HashSet<string>(_recipeController.GetUnlocked().Select(r => r.Id));
            _customerController.CheckAutoUnlock(unlockedRecipeIds);
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
