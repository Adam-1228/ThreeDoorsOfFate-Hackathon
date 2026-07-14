using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ThreeDoorsOfFate.UI
{
    public sealed class HoverImageSwapAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Graphic idleGraphic;
        [SerializeField] private Graphic hoverGraphic;
        [SerializeField] private float fadeSpeed = 12f;
        [SerializeField] private float idleHoverAlpha;

        private RectTransform rectTransform;
        private bool isHovering;

        public void Configure(Graphic idleGraphic, Graphic hoverGraphic, float fadeSpeed = 12f, float idleHoverAlpha = 0f)
        {
            this.idleGraphic = idleGraphic;
            this.hoverGraphic = hoverGraphic;
            this.fadeSpeed = fadeSpeed;
            this.idleHoverAlpha = idleHoverAlpha;
            rectTransform = GetComponent<RectTransform>();
            ApplyImmediate();
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            ApplyImmediate();
        }

        private void Update()
        {
            if (idleGraphic == null || hoverGraphic == null)
            {
                return;
            }

            if (!UseTouchOptimizedInput() && rectTransform != null && TryGetPointerPosition(out Vector2 pointerPosition))
            {
                isHovering = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pointerPosition);
            }
            else if (UseTouchOptimizedInput())
            {
                isHovering = false;
            }

            float blend = 1f - Mathf.Exp(-fadeSpeed * Time.unscaledDeltaTime);
            SetAlpha(idleGraphic, Mathf.Lerp(idleGraphic.color.a, isHovering ? idleHoverAlpha : 1f, blend));
            SetAlpha(hoverGraphic, Mathf.Lerp(hoverGraphic.color.a, isHovering ? 1f : 0f, blend));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (UseTouchOptimizedInput())
            {
                return;
            }

            isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
        }

        private void ApplyImmediate()
        {
            if (idleGraphic != null)
            {
                SetAlpha(idleGraphic, 1f);
            }

            if (hoverGraphic != null)
            {
                SetAlpha(hoverGraphic, 0f);
            }
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private static bool TryGetPointerPosition(out Vector2 pointerPosition)
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null)
            {
                pointerPosition = Mouse.current.position.ReadValue();
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            pointerPosition = Input.mousePosition;
            return true;
#else
            pointerPosition = default;
            return false;
#endif
        }

        private static bool UseTouchOptimizedInput()
        {
            return Application.isMobilePlatform && Input.touchSupported;
        }
    }
}
