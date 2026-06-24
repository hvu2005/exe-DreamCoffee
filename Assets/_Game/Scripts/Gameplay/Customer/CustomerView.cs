using System;
using System.Collections;
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

        // ── Animations ────────────────────────────────────────────────────────

        /// <summary>Scales from 0 to 1. Call on spawn before assigning a seat position.</summary>
        public void PlaySpawnAnim()
        {
            StopAllCoroutines();
            transform.localScale = Vector3.zero;
            StartCoroutine(ScaleTo(Vector3.one, 0.2f, null));
        }

        /// <summary>Scales to 0 then invokes onComplete (which should call Pool.Despawn).</summary>
        public void PlayLeaveAnim(Action onComplete)
        {
            StopAllCoroutines();
            StartCoroutine(ScaleTo(Vector3.zero, 0.15f, onComplete));
        }

        private IEnumerator ScaleTo(Vector3 target, float duration, Action onComplete)
        {
            var start   = transform.localScale;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            transform.localScale = target;
            onComplete?.Invoke();
        }
    }
}
