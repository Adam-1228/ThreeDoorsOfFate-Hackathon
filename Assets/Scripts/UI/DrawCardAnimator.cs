using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.UI
{
    public sealed class DrawCardAnimator : MonoBehaviour
    {
        [SerializeField] private Vector2 startPosition;
        [SerializeField] private Vector2 endPosition;
        [SerializeField] private float duration = 0.65f;
        [SerializeField] private float delay;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private float startTime;
        private bool configured;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            startTime = Time.unscaledTime;
        }

        private void Update()
        {
            if (!configured || rectTransform == null)
            {
                return;
            }

            float elapsed = Time.unscaledTime - startTime - delay;
            if (elapsed < 0f)
            {
                canvasGroup.alpha = 0f;
                return;
            }

            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            Vector2 position = Vector2.Lerp(startPosition, endPosition, eased);
            position.y += Mathf.Sin(progress * Mathf.PI) * 58f;
            rectTransform.anchoredPosition = position;
            rectTransform.localScale = Vector3.one * Mathf.Lerp(0.78f, 1.14f, Mathf.Sin(progress * Mathf.PI));
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-14f, 5f, eased));
            canvasGroup.alpha = progress < 0.88f ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.88f) / 0.12f);

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        public void Configure(Vector2 start, Vector2 end, float animationDuration, float animationDelay)
        {
            startPosition = start;
            endPosition = end;
            duration = animationDuration;
            delay = animationDelay;
            configured = true;

            rectTransform ??= GetComponent<RectTransform>();
            rectTransform.anchoredPosition = startPosition;

            Image image = GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }
        }
    }
}
