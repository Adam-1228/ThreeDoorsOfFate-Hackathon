using System;
using ThreeDoorsOfFate.Audio;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private Button koreanLanguageButton;
        private Button englishLanguageButton;

        private static string L(string key)
        {
            return GameLocalization.Text(key);
        }

        private static string LF(string key, params object[] args)
        {
            return GameLocalization.Format(key, args);
        }

        private Text AddLocalizedText(
            RectTransform parent,
            string name,
            string key,
            int fontSize,
            TextAnchor alignment,
            Color color,
            params object[] args)
        {
            Text target = AddText(
                parent,
                name,
                args != null && args.Length > 0 ? LF(key, args) : L(key),
                fontSize,
                alignment,
                color);
            BindLocalizedText(target, key, args);
            return target;
        }

        private static void BindLocalizedText(
            Text target,
            string key,
            params object[] args)
        {
            object[] capturedArgs = args ?? Array.Empty<object>();
            LocalizedTextBinding binding =
                target.GetComponent<LocalizedTextBinding>()
                ?? target.gameObject.AddComponent<LocalizedTextBinding>();
            binding.Configure(
                target,
                () => capturedArgs.Length > 0 ? LF(key, capturedArgs) : L(key));
        }

        private static void BindLocalizedSourceText(Text target, string source)
        {
            LocalizedTextBinding binding =
                target.GetComponent<LocalizedTextBinding>()
                ?? target.gameObject.AddComponent<LocalizedTextBinding>();
            binding.Configure(
                target,
                () => GameLocalization.TextFromSource(source));
        }

        private static void BindLocalizedCardText(
            Text target,
            CardData card,
            bool useRules)
        {
            LocalizedTextBinding binding =
                target.GetComponent<LocalizedTextBinding>()
                ?? target.gameObject.AddComponent<LocalizedTextBinding>();
            binding.Configure(
                target,
                () => useRules
                    ? GetLocalizedCardRules(card)
                    : GetLocalizedCardName(card));
        }

        private static void BindLocalizedCardSprite(
            Image target,
            CardData card)
        {
            if (target == null || card == null)
            {
                return;
            }

            LocalizedCardSpriteBinding binding =
                target.GetComponent<LocalizedCardSpriteBinding>()
                ?? target.gameObject.AddComponent<LocalizedCardSpriteBinding>();
            binding.Configure(target, card.CardId, card.FullCardSprite);
        }

        private Button AddLocalizedMainMenuButton(
            RectTransform parent,
            string name,
            string key,
            int fontSize)
        {
            Button button = AddMainMenuButton(parent, name, L(key), fontSize);
            BindLocalizedText(button.GetComponentInChildren<Text>(), key);
            return button;
        }

        private Button AddLocalizedSettingsMenuButton(
            RectTransform parent,
            string name,
            string key,
            int fontSize,
            GameSfxCue cue = GameSfxCue.UiAccept)
        {
            Button button = AddSettingsMenuButton(parent, name, L(key), fontSize, cue);
            BindLocalizedText(button.GetComponentInChildren<Text>(), key);
            return button;
        }

        private Button AddLocalizedOptionToggleButton(
            RectTransform parent,
            string name,
            string key,
            int fontSize)
        {
            Button button = AddOptionToggleButton(parent, name, L(key), fontSize);
            BindLocalizedText(button.GetComponentInChildren<Text>(), key);
            return button;
        }

        private Button AddMainMenuIconButton(
            RectTransform parent,
            string name,
            string localizationKey,
            Sprite icon,
            int fontSize)
        {
            Button button = AddMainMenuButton(
                parent,
                name,
                L(localizationKey),
                fontSize);
            Text label = button.GetComponentInChildren<Text>();
            BindLocalizedText(label, localizationKey);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = fontSize;
            SetAnchors(
                label.rectTransform,
                new Vector2(0.36f, 0.12f),
                new Vector2(0.95f, 0.88f));

            Image iconImage = AddImage(
                button.GetComponent<RectTransform>(),
                "설정 톱니바퀴",
                Color.white);
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            SetAnchors(
                iconImage.rectTransform,
                new Vector2(0.035f, 0.12f),
                new Vector2(0.345f, 0.88f));
            return button;
        }

        private void SetGameLanguage(GameLanguage language)
        {
            GameLocalization.SetLanguage(language);
            UpdateLanguageSelectionState();
            RefreshTopBar();
            RefreshLog();
            RefreshRewardedRelicAction();

            if (howToPlayOverlay != null)
            {
                ShowHowToPlayPage(howToPlayPageIndex);
            }
        }

        private void UpdateLanguageSelectionState()
        {
            SetLanguageButtonSelected(
                koreanLanguageButton,
                GameLocalization.CurrentLanguage == GameLanguage.Korean);
            SetLanguageButtonSelected(
                englishLanguageButton,
                GameLocalization.CurrentLanguage == GameLanguage.English);
        }

        private static void SetLanguageButtonSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = selected
                ? new Color(0.72f, 1.0f, 0.94f, 1f)
                : Color.white;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;

            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = colors.normalColor;
            }
        }

        private void RegisterCardLocalizationSources()
        {
            foreach (CardData card in cardPool)
            {
                if (card == null)
                {
                    continue;
                }

                CardLocalization.RegisterKoreanSource(card.CardId, card.DisplayName);
            }
        }

        private static string GetLocalizedCardName(CardData card)
        {
            return card == null
                ? string.Empty
                : CardLocalization.GetName(card.CardId, card.DisplayName);
        }

        private static string GetLocalizedCardRules(CardData card)
        {
            return card == null
                ? string.Empty
                : CardLocalization.GetRules(card.CardId, card.RulesText);
        }

        private static Sprite GetLocalizedCardFullSprite(CardData card)
        {
            return card == null
                ? null
                : CardLocalization.GetFullCardSprite(card.CardId, card.FullCardSprite);
        }
    }
}
