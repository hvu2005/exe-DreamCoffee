using System.Collections;
using System.Collections.Generic;
using DreamCafe.DataControl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.SystemControl.Brew
{
    /// <summary>
    /// Bảng "NEW RECIPE DISCOVERED!" — hiện NGAY SAU khi người chơi tắt popup ăn mừng
    /// (<see cref="BrewResultPopupView"/>) của một lần mò trúng công thức mới. Đây là bảng đọc kỹ:
    /// công thức gồm những gì, bán được bao nhiêu, pha mất mấy giây, và vừa mở ra phần thưởng nào.
    ///
    /// Toàn bộ số liệu suy ra từ <see cref="RecipeItem"/> đã có sẵn — không thêm trường dữ liệu mới.
    /// Điểm danh tiếng thì tính theo giá bán (xem <see cref="ReputationFor"/>) và cộng thật vào
    /// <see cref="CurrencyController"/>, để con số "+EXP" trên bảng không phải chữ trang trí.
    /// </summary>
    public sealed class RecipeDiscoveryPopupView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField, Tooltip("Node bật/tắt của cả popup (gồm cả lớp làm mờ nền).")]
        private GameObject root;
        [SerializeField, Tooltip("Node được phóng to dần lúc hiện — chính là tấm thẻ nội dung.")]
        private RectTransform content;
        [SerializeField, Tooltip("Nút X góc trên phải.")]
        private Button closeButton;
        [SerializeField, Tooltip("Nút ADD TO MENU — công thức đã mở khoá sẵn rồi nên nút chỉ đóng bảng.")]
        private Button addToMenuButton;

        [Header("Món")]
        [SerializeField] private Image drinkIcon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private GameObject newBadge;

        [Header("Nguyên liệu")]
        [SerializeField, Tooltip("Node chứa dải ô nguyên liệu (nên có HorizontalLayoutGroup).")]
        private Transform ingredientRow;
        [SerializeField] private DiscoveryIngredientChipView chipPrefab;

        [Header("Chỉ số")]
        [SerializeField] private TMP_Text revenueLabel;
        [SerializeField] private TMP_Text prepTimeLabel;
        [SerializeField] private TMP_Text levelLabel;

        [Header("Phần thưởng")]
        [SerializeField] private TMP_Text reputationLabel;
        [SerializeField, Tooltip("Cả dòng 'New Customer Unlocked' — tắt khi lần này không mở ra khách nào.")]
        private GameObject customerRow;
        [SerializeField] private TMP_Text customerLabel;
        [SerializeField] private Image customerAvatar;

        [Header("Tinh chỉnh")]
        [SerializeField, Tooltip("Chữ hiện ở dòng Initial Level — món mới luôn bắt đầu ở cấp 1.")]
        private string initialLevelText = "Lv.1";
        [SerializeField, Min(0), Tooltip("Số điểm danh tiếng nhận được trên mỗi 1.000đ giá bán.")]
        private float reputationPerThousand = 1f;
        [SerializeField, Min(0), Tooltip("Sàn điểm danh tiếng — món rẻ vẫn được thưởng chừng này.")]
        private int minReputation = 10;
        [SerializeField, Tooltip("Cộng thật số điểm danh tiếng vào CurrencyController lúc hiện bảng.")]
        private bool grantReputation = true;
        [SerializeField, Tooltip("Thời gian phóng to lúc hiện bảng (giây).")]
        private float popInSeconds = 0.18f;

        private readonly List<DiscoveryIngredientChipView> _chips = new();
        private readonly List<string> _ingredientOrder = new();
        private readonly Dictionary<string, int> _ingredientCounts = new();

        private Coroutine _popRoutine;

        public bool IsShowing => root != null && root.activeSelf;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (addToMenuButton != null) addToMenuButton.onClick.AddListener(Hide);
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
            if (addToMenuButton != null) addToMenuButton.onClick.RemoveListener(Hide);
        }

        /// <summary>
        /// Hiện bảng cho một công thức vừa mò ra.
        /// </summary>
        /// <param name="recipe">Công thức vừa mở khoá.</param>
        /// <param name="newCustomer">Khách hàng vừa được mở kèm (nếu có) — null thì ẩn dòng đó.</param>
        /// <param name="items">Repository nguyên liệu để tra tên và ảnh theo ID.</param>
        public bool Show(RecipeItem recipe, CustomerItem newCustomer, ScriptableInventoryItemRepository items)
        {
            if (root == null || recipe == null) return false;

            if (nameLabel != null) nameLabel.text = recipe.DisplayName.ToUpperInvariant();
            if (newBadge != null) newBadge.SetActive(true);

            if (drinkIcon != null)
            {
                drinkIcon.sprite = recipe.Icon;
                drinkIcon.enabled = recipe.Icon != null;
            }

            BuildIngredientChips(recipe, items);

            if (revenueLabel != null) revenueLabel.text = RecipeStatFormat.MoneyPerSecond(recipe);
            if (prepTimeLabel != null) prepTimeLabel.text = $"{recipe.CraftTimeSeconds:0.#} sec";
            if (levelLabel != null) levelLabel.text = initialLevelText;

            int reputation = ReputationFor(recipe);
            if (reputationLabel != null) reputationLabel.text = $"+{reputation} EXP";
            if (grantReputation) GrantReputation(reputation);

            ShowCustomer(newCustomer);

            root.SetActive(true);
            PlayPopIn();
            return true;
        }

        public void Hide()
        {
            if (_popRoutine != null)
            {
                StopCoroutine(_popRoutine);
                _popRoutine = null;
            }
            if (root != null) root.SetActive(false);
        }

        /// <summary>Điểm danh tiếng của một công thức — món càng đắt càng nở mày nở mặt.</summary>
        public int ReputationFor(RecipeItem recipe)
        {
            if (recipe == null) return 0;
            int scaled = Mathf.RoundToInt(recipe.BasePrice / 1000f * reputationPerThousand);
            return Mathf.Max(minReputation, scaled);
        }

        // =====================================================================
        // Nội bộ
        // =====================================================================

        private void BuildIngredientChips(RecipeItem recipe, ScriptableInventoryItemRepository items)
        {
            foreach (var chip in _chips)
                if (chip != null) Destroy(chip.gameObject);
            _chips.Clear();

            if (ingredientRow == null || chipPrefab == null) return;

            // Nguyên liệu lặp lại trong RequiredIngredientIds chính là số lượng cần — gom lại
            // thành "x2" thay vì hiện hai ô giống hệt nhau.
            _ingredientOrder.Clear();
            _ingredientCounts.Clear();
            foreach (var id in recipe.RequiredIngredientIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (_ingredientCounts.TryGetValue(id, out int count))
                {
                    _ingredientCounts[id] = count + 1;
                }
                else
                {
                    _ingredientCounts[id] = 1;
                    _ingredientOrder.Add(id);
                }
            }

            for (int i = 0; i < _ingredientOrder.Count; i++)
            {
                string id = _ingredientOrder[i];
                var item = items != null ? items.GetItem(id) : null;
                var chip = Instantiate(chipPrefab, ingredientRow);
                chip.Bind(item, id, _ingredientCounts[id], showPlus: i > 0);
                _chips.Add(chip);
            }
        }

        private void ShowCustomer(CustomerItem customer)
        {
            if (customerRow != null) customerRow.SetActive(customer != null);
            if (customer == null) return;

            if (customerLabel != null) customerLabel.text = customer.DisplayName;
            if (customerAvatar != null)
            {
                customerAvatar.sprite = customer.Avatar;
                customerAvatar.enabled = customer.Avatar != null;
            }
        }

        private void GrantReputation(int amount)
        {
            if (amount <= 0) return;

            var provider = GameSystemsProvider.Instance;
            var currency = provider != null ? provider.Currency : null;
            if (currency == null) return;

            currency.AddReputation(amount);
        }

        private void PlayPopIn()
        {
            if (content == null) return;
            if (_popRoutine != null) StopCoroutine(_popRoutine);
            _popRoutine = StartCoroutine(PopInRoutine());
        }

        private IEnumerator PopInRoutine()
        {
            const float startScale = 0.86f;
            float elapsed = 0f;

            while (elapsed < popInSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popInSeconds);
                float eased = Mathf.LerpUnclamped(startScale, 1f, 1f - Mathf.Pow(1f - t, 3f));
                content.localScale = Vector3.one * eased;
                yield return null;
            }

            content.localScale = Vector3.one;
            _popRoutine = null;
        }
    }
}
