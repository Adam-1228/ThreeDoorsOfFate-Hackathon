using UnityEngine;
using UnityEngine.EventSystems;

namespace ThreeDoorsOfFate.UI
{
    public sealed class HoverFloatAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.055f;
        [SerializeField] private float pressedScale = 0.985f;
        [SerializeField] private float hoverLift = 18f;
        [SerializeField] private float floatAmplitude = 5f;
        [SerializeField] private float floatSpeed = 2.4f;
        [SerializeField] private float smoothTime = 0.10f;

        private RectTransform rectTransform;
        private Vector2 basePosition;
        private Vector2 velocity;
        private bool isHovering;
        private bool isPressed;

        public void Configure(float hoverScale, float pressedScale, float hoverLift, float floatAmplitude, float smoothTime)
        {
            this.hoverScale = hoverScale;
            this.pressedScale = pressedScale;
            this.hoverLift = hoverLift;
            this.floatAmplitude = floatAmplitude;
            this.smoothTime = smoothTime;
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            basePosition = rectTransform.anchoredPosition;
        }

        private void OnEnable()
        {
            if (rectTransform != null)
            {
                basePosition = rectTransform.anchoredPosition;
            }
        }

        private void Update()
        {
            if (rectTransform == null)
            {
                return;
            }

            bool canHover = !UseTouchOptimizedInput();
            float lift = canHover && isHovering ? hoverLift : 0f;
            float drift = canHover && isHovering ? Mathf.Sin(Time.unscaledTime * floatSpeed) * floatAmplitude : 0f;
            Vector2 targetPosition = basePosition + new Vector2(0f, lift + drift);
            Vector2 nextPosition = Vector2.SmoothDamp(rectTransform.anchoredPosition, targetPosition, ref velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            rectTransform.anchoredPosition = new Vector2(Mathf.Round(nextPosition.x), Mathf.Round(nextPosition.y));

            float targetScale = isPressed ? pressedScale : canHover && isHovering ? hoverScale : 1f;
            Vector3 currentScale = rectTransform.localScale;
            rectTransform.localScale = Vector3.Lerp(currentScale, Vector3.one * targetScale, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (UseTouchOptimizedInput())
            {
                return;
            }

            isHovering = true;
            transform.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            isPressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            transform.SetAsLastSibling();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
        }

        private static bool UseTouchOptimizedInput()
        {
            return Application.isMobilePlatform && Input.touchSupported;
        }
    }
}
