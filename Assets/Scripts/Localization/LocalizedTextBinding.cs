using System;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Localization
{
    public sealed class LocalizedTextBinding : MonoBehaviour
    {
        private Text target;
        private Func<string> resolver;
        private string renderedText = string.Empty;
        private bool subscribed;

        public void Configure(Text target, Func<string> resolver)
        {
            if (subscribed)
            {
                GameLocalization.LanguageChanged -= Refresh;
            }

            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            GameLocalization.LanguageChanged += Refresh;
            subscribed = true;
            Refresh();
        }

        public void Refresh()
        {
            if (target == null || resolver == null)
            {
                return;
            }

            target.text = resolver();
            renderedText = target.text ?? string.Empty;
        }

        private void LateUpdate()
        {
            if (target == null || resolver == null || target.text == renderedText)
            {
                return;
            }

            string source = target.text ?? string.Empty;
            resolver = () => GameLocalization.TextFromSource(source);
            Refresh();
        }

        private void OnDestroy()
        {
            if (!subscribed)
            {
                return;
            }

            GameLocalization.LanguageChanged -= Refresh;
            subscribed = false;
        }
    }
}
