using DreamCafe.Core.MVC;
using DreamCafe.Data;
using UnityEngine;

namespace DreamCafe.Gameplay.Customer
{
    /// <summary>
    /// Renders the customer using Unity primitives: a colored quad body and a world-space patience bar.
    /// Pure presentation — never touches the EventBus or services.
    /// TODO Phase 4: replace primitives with sprite sheets; add DOTween emotion pop animations.
    /// </summary>
    public sealed class CustomerView : ViewBase
    {
        [Header("Body")]
        [SerializeField] private SpriteRenderer body;

        [Header("Patience Bar")]
        [SerializeField] private GameObject patienceBarRoot;
        [SerializeField] private Transform patienceBarFill;
        [SerializeField] private SpriteRenderer patienceBarFillRenderer;

        [Header("Colors")]
        [SerializeField] private Color colorHappy      = new(0.2f, 0.85f, 0.2f);
        [SerializeField] private Color colorNeutral    = new(1f, 0.85f, 0.1f);
        [SerializeField] private Color colorDisappoint = new(1f, 0.5f, 0f);
        [SerializeField] private Color colorAngry      = new(0.9f, 0.1f, 0.1f);

        public override void Render(IModel model)
        {
            var m = (CustomerModel)model;

            if (body != null && m.Data != null)
                body.color = m.Data.tintColor;

            UpdatePatienceBar(m.Patience01, m.ComputeEmotion());
        }

        public void ShowPatienceBar(bool show)
        {
            if (patienceBarRoot != null)
                patienceBarRoot.SetActive(show);
        }

        private void UpdatePatienceBar(float t, CustomerEmotion emotion)
        {
            if (patienceBarFill != null)
            {
                var s = patienceBarFill.localScale;
                s.x = Mathf.Lerp(0f, 1f, t);
                patienceBarFill.localScale = s;
            }

            if (patienceBarFillRenderer != null)
            {
                patienceBarFillRenderer.color = emotion switch
                {
                    CustomerEmotion.Happy        => colorHappy,
                    CustomerEmotion.Disappointed => colorDisappoint,
                    CustomerEmotion.Angry        => colorAngry,
                    _                            => colorNeutral
                };
            }
        }
    }
}
