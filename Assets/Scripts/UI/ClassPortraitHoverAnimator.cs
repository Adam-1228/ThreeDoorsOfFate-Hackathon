using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ThreeDoorsOfFate.UI
{
    public sealed class ClassPortraitHoverAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image idlePortrait;
        [SerializeField] private Image hoverPortrait;
        [SerializeField] private float fadeSpeed = 9f;
        [SerializeField] private float idleHoverAlpha = 0.18f;
        [SerializeField] private float hoverScale = 1.025f;

        private RectTransform rectTransform;
        private bool isHovering;

        public void Configure(Image idlePortrait, Image hoverPortrait)
        {
            this.idlePortrait = idlePortrait;
            this.hoverPortrait = hoverPortrait;
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
            if (idlePortrait == null || hoverPortrait == null)
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
            SetAlpha(idlePortrait, Mathf.Lerp(idlePortrait.color.a, isHovering ? idleHoverAlpha : 1f, blend));
            SetAlpha(hoverPortrait, Mathf.Lerp(hoverPortrait.color.a, isHovering ? 1f : 0f, blend));

            Vector3 targetScale = Vector3.one * (isHovering ? hoverScale : 1f);
            hoverPortrait.rectTransform.localScale = Vector3.Lerp(hoverPortrait.rectTransform.localScale, targetScale, blend);
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
            if (idlePortrait != null)
            {
                SetAlpha(idlePortrait, 1f);
            }

            if (hoverPortrait != null)
            {
                SetAlpha(hoverPortrait, 0f);
                hoverPortrait.rectTransform.localScale = Vector3.one;
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
