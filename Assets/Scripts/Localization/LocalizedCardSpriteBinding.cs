using System;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Localization
{
    public sealed class LocalizedCardSpriteBinding : MonoBehaviour
    {
        private Image target;
        private string cardId = string.Empty;
        private Sprite koreanFallback;
        private bool subscribed;

        public void Configure(
            Image target,
            string cardId,
            Sprite koreanFallback)
        {
            if (subscribed)
            {
                GameLocalization.LanguageChanged -= Refresh;
            }

            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.cardId = cardId ?? string.Empty;
            this.koreanFallback = koreanFallback;
            GameLocalization.LanguageChanged += Refresh;
            subscribed = true;
            Refresh();
        }

        public void Refresh()
        {
            if (target == null)
            {
                return;
            }

            target.sprite = CardLocalization.GetFullCardSprite(
                cardId,
                koreanFallback);
            target.enabled = target.sprite != null;
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
