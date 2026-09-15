using System;
using System.Collections.Generic;
using DreamCafe.Core.Database;
using DreamCafe.DataControl;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.Brew
{
    /// <summary>
    /// Panel pha chế: bấm B để bật/tắt. Kệ nguyên liệu bên trái đổ ra từ database
    /// (<see cref="ScriptableInventoryItemRepository"/> trong <see cref="DatabaseManager"/>), số lượng
    /// lấy theo kho runtime của <see cref="InventoryController"/>. Bấm nguyên liệu để bỏ vào cốc
    /// (mỗi thứ tự rót đầy một tầng theo màu hex của nó), BREW để pha — trúng công thức chưa biết
    /// thì mở khoá luôn. Không có bước đổ nước riêng: nguyên liệu đã mang nước theo sẵn.
    ///
    /// Script nằm trên node gốc (luôn active) còn <see cref="window"/> mới là phần bật/tắt — tắt node
    /// gốc thì Update() không chạy, không bấm B mở lại được (cùng cách làm với InventoryPanelView).
    /// </summary>
    public sealed class BrewingPanelView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject window;
        [SerializeField] private Transform ingredientGrid;
        [SerializeField] private BrewIngredientSlotView ingredientSlotPrefab;
        [SerializeField, Tooltip("3 ô công thức đang thử ở góc phải, theo đúng thứ tự trái sang phải.")]
        private BrewMixtureSlotView[] mixtureSlots = Array.Empty<BrewMixtureSlotView>();

        [Header("Cốc")]
        [SerializeField, Tooltip("Các tầng màu trong cốc, xếp từ đáy lên — mỗi nguyên liệu đổ đầy đúng một tầng.")]
        private Image[] cupLayers = Array.Empty<Image>();
        [SerializeField, Min(0.01f), Tooltip("Thời gian rót đầy một tầng (giây).")]
        private float pourSeconds = 0.22f;

        [Header("Bảng gợi ý & kết quả")]
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField, Tooltip("Màn chờ 'đang pha' trượt vào giữa màn hình trước khi lộ kết quả.")]
        private BrewLoadingView loadingView;
        [SerializeField, Tooltip("Popup ăn mừng / thất bại hiện sau mỗi lần pha xong.")]
        private BrewResultPopupView resultPopup;
        [SerializeField, Tooltip("Bảng chi tiết công thức mới, hiện tiếp sau khi tắt popup ăn mừng.")]
        private RecipeDiscoveryPopupView discoveryPopup;

        [Header("Nút")]
        [SerializeField] private Button brewButton;
        [SerializeField] private Button closeButton;

        [Header("Nguồn dữ liệu")]
        [SerializeField, Tooltip("Bỏ trống thì dùng DatabaseManager.Instance.")]
        private DatabaseManager database;

        [Header("Input")]
        [SerializeField] private Key toggleKey = Key.B;

        private readonly List<BrewIngredientSlotView> _slots = new();
        private readonly BrewingController _brewing = new();

        private InventoryController _inventory;
        private RecipeController _recipes;
        private CustomerController _customers;
        private int _hintCursor;

        // Công thức vừa mò ra, đang chờ popup ăn mừng tắt đi để mở bảng chi tiết.
        private RecipeItem _pendingDiscovery;
        // Khách hàng được mở khoá kèm theo — sự kiện này bắn ngay trong lúc Brew(), tức là TRƯỚC
        // khi BrewResult về tay panel, nên phải hứng sẵn rồi mới ghép vào bảng.
        private CustomerItem _pendingCustomer;

        public bool IsOpen => window != null && window.activeSelf;

        /// <summary>Bộ máy pha chế đứng sau panel — hệ thống khác (nhân viên tự pha) dùng chung được.</summary>
        public BrewingController Brewing => _brewing;

        /// <summary>Lấy lúc dùng chứ không phải lúc Awake — thứ tự Awake giữa các object không đảm bảo.</summary>
        private DatabaseManager Database => database != null ? database : DatabaseManager.Instance;

        // =====================================================================
        // Vòng đời
        // =====================================================================

        private void Awake()
        {
            _brewing.Init(null);
            _brewing.Changed += Refresh;
            _brewing.Brewed += OnBrewed;

            if (resultPopup != null) resultPopup.Dismissed += OnResultDismissed;

            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (brewButton != null) brewButton.onClick.AddListener(OnBrewClicked);

            for (int i = 0; i < mixtureSlots.Length; i++)
            {
                if (mixtureSlots[i] != null) mixtureSlots[i].Setup(i, OnMixtureSlotClicked);
            }

            if (window != null) window.SetActive(false);
        }

        private void OnDestroy()
        {
            _brewing.Changed -= Refresh;
            _brewing.Brewed -= OnBrewed;
            _brewing.Shutdown();

            if (resultPopup != null) resultPopup.Dismissed -= OnResultDismissed;
            if (_inventory != null) _inventory.Changed -= Refresh;
            if (_customers != null) _customers.CustomerUnlocked -= OnCustomerUnlocked;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame) Toggle();

            if (IsOpen) PourCupLayers();
        }

        // =====================================================================
        // Đóng / mở
        // =====================================================================

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (window == null) return;

            window.SetActive(true);
            ClearPendingDiscovery();
            if (loadingView != null) loadingView.Cancel();
            if (resultPopup != null) resultPopup.Hide();
            if (discoveryPopup != null) discoveryPopup.Hide();
            // Cạn sạch ngay chứ không rót ngược từ từ — cốc của lần pha trước đã đổ đi rồi.
            ResetCupLayers();
            BindSystems();
            BuildIngredientShelf();
            NextHint();
            SetResult(string.Empty, Color.white);
            Refresh();
        }

        public void Close()
        {
            // Trả lại nguyên liệu đang cầm trên tay — cốc dở không được giữ qua lần mở sau.
            _brewing.ClearMixture();
            // Xoá trước khi Hide(): Hide() bắn Dismissed, còn công thức đang chờ thì bảng chi tiết
            // sẽ bật lên ngay lúc người chơi vừa đóng panel.
            ClearPendingDiscovery();
            // Đang pha dở mà đóng panel thì bỏ luôn popup đang treo ở cuối màn chờ.
            if (loadingView != null) loadingView.Cancel();
            if (resultPopup != null) resultPopup.Hide();
            if (discoveryPopup != null) discoveryPopup.Hide();
            if (window != null) window.SetActive(false);
        }

        // =====================================================================
        // Dữ liệu
        // =====================================================================

        /// <summary>Nối bộ pha chế vào kho + sổ công thức của GameSystemsProvider.</summary>
        private void BindSystems()
        {
            var provider = GameSystemsProvider.Instance;
            if (provider == null)
            {
                Debug.LogWarning("[BrewingPanel] Chưa có GameSystemsProvider trong scene — không pha chế được.");
                return;
            }

            if (_inventory != provider.Inventory)
            {
                if (_inventory != null) _inventory.Changed -= Refresh;
                _inventory = provider.Inventory;
                if (_inventory != null) _inventory.Changed += Refresh;
            }

            if (_customers != provider.Customer)
            {
                if (_customers != null) _customers.CustomerUnlocked -= OnCustomerUnlocked;
                _customers = provider.Customer;
                if (_customers != null) _customers.CustomerUnlocked += OnCustomerUnlocked;
            }

            _recipes = provider.Recipe;
            _brewing.Bind(_inventory, _recipes);
        }

        /// <summary>Đổ toàn bộ nguyên liệu trong database ra kệ bên trái.</summary>
        private void BuildIngredientShelf()
        {
            var db = Database;
            var repository = db != null ? db.Get<ScriptableInventoryItemRepository>() : null;
            if (repository == null)
            {
                Debug.LogWarning("[BrewingPanel] Chưa có ScriptableInventoryItemRepository trong DatabaseManager.");
            }

            var items = repository != null ? repository.GetAllItems() : Array.Empty<InventoryItem>();

            foreach (var slot in _slots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            _slots.Clear();

            if (ingredientSlotPrefab == null || ingredientGrid == null) return;

            foreach (var item in items)
            {
                if (item == null) continue;
                var slot = Instantiate(ingredientSlotPrefab, ingredientGrid);
                slot.Bind(item, OnIngredientClicked);
                _slots.Add(slot);
            }
        }

        // =====================================================================
        // Tương tác
        // =====================================================================

        private void OnIngredientClicked(InventoryItem item)
        {
            if (!_brewing.TryAddIngredient(item, out string reason))
            {
                SetResult(reason, Warning);
            }
            else
            {
                SetResult(string.Empty, Color.white);
            }
        }

        private void OnMixtureSlotClicked(int index) => _brewing.RemoveAt(index);

        private void OnBrewClicked()
        {
            if (!_brewing.IsBound) BindSystems();
            ClearPendingDiscovery();
            _brewing.Brew();
        }

        /// <summary>Khách hàng mở khoá trong lúc Brew() — giữ lại để ghép vào bảng công thức mới.</summary>
        private void OnCustomerUnlocked(CustomerItem customer) => _pendingCustomer = customer;

        /// <summary>Tắt popup ăn mừng xong; nếu vừa mò ra công thức thì mở tiếp bảng chi tiết.</summary>
        private void OnResultDismissed()
        {
            if (_pendingDiscovery == null || discoveryPopup == null)
            {
                ClearPendingDiscovery();
                return;
            }

            var db = Database;
            var items = db != null ? db.Get<ScriptableInventoryItemRepository>() : null;

            discoveryPopup.Show(_pendingDiscovery, _pendingCustomer, items);
            ClearPendingDiscovery();
        }

        private void ClearPendingDiscovery()
        {
            _pendingDiscovery = null;
            _pendingCustomer = null;
        }

        private void OnBrewed(BrewResult result)
        {
            AnnounceResult(result);

            // Popup chỉ bật cho kết quả pha thật (ra món / hỏng món); thao tác sai thì bỏ qua cả
            // màn chờ lẫn popup, chỉ còn dòng chữ dưới bảng gợi ý — đỡ làm phiền người chơi.
            if (!result.IsBrewCompleted) return;

            if (loadingView == null)
            {
                ShowResultPopup(result);
                return;
            }

            // Máy pha "chạy" đúng bằng thời gian pha của công thức, rồi mới lộ ra pha được gì.
            float craftSeconds = result.Recipe != null ? result.Recipe.CraftTimeSeconds : 0f;
            loadingView.Play(loadingView.HoldFor(craftSeconds), null, () => ShowResultPopup(result));
        }

        private void ShowResultPopup(BrewResult result)
        {
            if (resultPopup != null) resultPopup.Show(result);
        }

        /// <summary>Cập nhật dòng chữ dưới bảng gợi ý — chạy ngay, không đợi màn chờ (bị lớp mờ che).</summary>
        private void AnnounceResult(BrewResult result)
        {
            switch (result.Outcome)
            {
                case BrewOutcome.Discovered:
                    SetResult($"NEW RECIPE UNLOCKED: {result.Recipe.DisplayName}!", Good);
                    // Bảng chi tiết chờ người chơi tắt popup ăn mừng trước (xem OnResultDismissed).
                    _pendingDiscovery = result.Recipe;
                    NextHint();
                    break;

                case BrewOutcome.Success:
                    SetResult($"Served: {result.Recipe.DisplayName} (+{result.Recipe.BasePrice:N0}d)", Good);
                    break;

                case BrewOutcome.NoMatch:
                    SetResult(result.ConsumedIngredients
                        ? "That combination is not a drink. Ingredients wasted."
                        : "That combination is not a drink.", Warning);
                    break;

                case BrewOutcome.EmptyMixture:
                    SetResult("Add at least one ingredient first.", Warning);
                    break;

                case BrewOutcome.MissingIngredient:
                    SetResult("Not enough ingredients in stock.", Warning);
                    break;
            }
        }

        // =====================================================================
        // Hiển thị
        // =====================================================================

        /// <summary>Vẽ lại toàn bộ trạng thái panel theo cốc hiện tại và kho.</summary>
        public void Refresh()
        {
            foreach (var slot in _slots)
            {
                if (slot != null) slot.SetAvailable(_brewing.GetAvailable(slot.Item));
            }

            for (int i = 0; i < mixtureSlots.Length; i++)
            {
                if (mixtureSlots[i] == null) continue;
                mixtureSlots[i].Bind(i < _brewing.Mixture.Count ? _brewing.Mixture[i] : null);
            }

            TintCupLayers();

            if (brewButton != null) brewButton.interactable = !_brewing.IsEmpty;
        }

        /// <summary>
        /// Tô màu các tầng đang có nguyên liệu. Tầng trống giữ nguyên màu cũ — nó đang rút cạn dần
        /// nên đổi màu giữa chừng sẽ thành nhấp nháy.
        /// </summary>
        private void TintCupLayers()
        {
            for (int i = 0; i < cupLayers.Length && i < _brewing.Mixture.Count; i++)
            {
                var item = _brewing.Mixture[i];
                if (cupLayers[i] != null && item != null) cupLayers[i].color = item.LiquidColor;
            }
        }

        /// <summary>Cạn sạch mọi tầng ngay lập tức, không rót ngược từ từ.</summary>
        private void ResetCupLayers()
        {
            foreach (var layer in cupLayers)
            {
                if (layer != null) layer.fillAmount = 0f;
            }
        }

        /// <summary>
        /// Rót/rút từng tầng về mức của nó. Chạy mỗi khung hình thay vì dùng coroutine để bỏ
        /// nguyên liệu ra giữa lúc đang rót cũng chạy ngược lại mượt mà.
        /// </summary>
        private void PourCupLayers()
        {
            int filled = _brewing.Mixture.Count;
            float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, pourSeconds);

            for (int i = 0; i < cupLayers.Length; i++)
            {
                var layer = cupLayers[i];
                if (layer == null) continue;

                float target = i < filled ? 1f : 0f;
                if (!Mathf.Approximately(layer.fillAmount, target))
                {
                    layer.fillAmount = Mathf.MoveTowards(layer.fillAmount, target, step);
                }
            }
        }

        /// <summary>Lật sang gợi ý của một công thức chưa mở khoá khác.</summary>
        public void NextHint()
        {
            if (hintLabel == null) return;

            var locked = _recipes != null ? _recipes.GetLocked() : null;
            if (locked == null || locked.Count == 0)
            {
                hintLabel.text = "Every recipe has been discovered. Nice work!";
                return;
            }

            _hintCursor = (_hintCursor + 1) % locked.Count;
            var recipe = locked[_hintCursor];

            string clue = !string.IsNullOrWhiteSpace(recipe.Description)
                ? recipe.Description
                : $"Uses {recipe.RequiredIngredientIds.Count} ingredients.";

            hintLabel.text = clue;
        }

        private void SetResult(string message, Color color)
        {
            if (resultLabel == null) return;
            resultLabel.text = message;
            resultLabel.color = color;
        }

        private static readonly Color Good = new(0.44f, 0.78f, 0.42f);
        private static readonly Color Warning = new(0.93f, 0.55f, 0.35f);
    }
}
