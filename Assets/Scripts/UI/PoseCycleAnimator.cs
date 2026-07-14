using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.UI
{
    public sealed class PoseCycleAnimator : MonoBehaviour
    {
        [SerializeField] private Graphic firstPose;
        [SerializeField] private Graphic secondPose;
        [SerializeField] private float cycleSpeed = 0.55f;
        [SerializeField] private float phaseOffset;

        public void Configure(Graphic firstPose, Graphic secondPose, float phaseOffset)
        {
            this.firstPose = firstPose;
            this.secondPose = secondPose;
            this.phaseOffset = phaseOffset;
            ApplyAlpha(0f);
        }

        private void Update()
        {
            if (firstPose == null || secondPose == null)
            {
                return;
            }

            float blend = (Mathf.Sin(Time.unscaledTime * cycleSpeed + phaseOffset) + 1f) * 0.5f;
            blend = Mathf.SmoothStep(0f, 1f, blend);
            ApplyAlpha(blend);
        }

        private void ApplyAlpha(float blend)
        {
            if (firstPose != null)
            {
                SetAlpha(firstPose, Mathf.Lerp(1f, 0.16f, blend));
            }

            if (secondPose != null)
            {
                SetAlpha(secondPose, Mathf.Lerp(0f, 1f, blend));
            }
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
