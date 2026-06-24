using System;
using DreamCafe.Core.MVC;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamCafe.UI.Shop
{
    /// <summary>
    /// Canvas presenter for the end-of-day upgrade shop.
    /// Shows the day summary at top, 4 upgrade cards in the center, and a "Start Day" button.
    /// All refs wired by Phase6Setup or manually in the Inspector.
    /// </summary>
    public sealed class UpgradeShopView : ViewBase
    {
        [System.Serializable]
        public struct UpgradeCardUI
        {
            public string   upgradeId;
            public TMP_Text nameLabel;
            public TMP_Text descriptionLabel;
            public TMP_Text levelLabel;
            public TMP_Text costLabel;
            public Button   buyButton;
        }

        [Header("Summary")]
        [SerializeField] private TMP_Text summaryLabel;

        [Header("Upgrade Cards — one per upgrade in UpgradeConfig")]
        [SerializeField] private UpgradeCardUI[] cards;

        [Header("Footer")]
        [SerializeField] private Button startDayButton;

        public override void Render(IModel model) { }

        public void RenderSummary(int day, float revenue, int served, int lost)
        {
            if (summaryLabel != null)
                summaryLabel.text =
                    $"Day {day} Complete!\n" +
                    $"Revenue  {revenue:N0}đ    Served  {served}    Lost  {lost}";
        }

        /// <param name="levels">upgradeId → current level</param>
        /// <param name="costs">upgradeId → next cost (-1 if maxed)</param>
        /// <param name="balance">current player balance for affordability check</param>
        public void RenderCards(
            System.Func<string, int>   levels,
            System.Func<string, float> costs,
            float balance)
        {
            foreach (var card in cards)
            {
                int   level    = levels(card.upgradeId);
                float nextCost = costs(card.upgradeId);
                bool  isMaxed  = nextCost < 0f;

                if (card.levelLabel != null)
                    card.levelLabel.text = isMaxed ? "MAX" : $"Lv {level}";

                if (card.costLabel != null)
                    card.costLabel.text = isMaxed ? "—" : $"{nextCost:N0}đ";

                if (card.buyButton != null)
                    card.buyButton.interactable = !isMaxed && balance >= nextCost;
            }
        }

        public void ShowShop(bool show) => gameObject.SetActive(show);

        /// <summary>Wires "Start Day" button and Buy buttons. Called once in UpgradeShopController.Bind.</summary>
        public void Init(Action onStartDay, Action<string>[] onBuyCallbacks)
        {
            if (startDayButton != null)
                startDayButton.onClick.AddListener(() => onStartDay?.Invoke());

            for (int i = 0; i < cards.Length && i < onBuyCallbacks.Length; i++)
            {
                var id = cards[i].upgradeId;
                var cb = onBuyCallbacks[i];
                if (cards[i].buyButton != null)
                    cards[i].buyButton.onClick.AddListener(() => cb?.Invoke(id));
            }
            gameObject.SetActive(false);
        }
    }
}
