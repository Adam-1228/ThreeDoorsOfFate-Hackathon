using System;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.UI
{
    public sealed class CardView : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CardData cardData;

        [Header("Artwork")]
        [SerializeField] private Image illustrationImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image categoryStripeImage;

        [Header("Text")]
        [SerializeField] private Text nameText;
        [SerializeField] private Text costText;
        [SerializeField] private Text typeText;
        [SerializeField] private Text rulesText;

        [Header("Interaction")]
        [SerializeField] private Button button;

        public event Action<CardView, CardData> Clicked;

        public CardData CardData => cardData;

        private void Awake()
        {
            button ??= GetComponent<Button>();
            ConfigureRulesText();
            Bind(cardData);
        }

        private void OnValidate()
        {
            ConfigureRulesText();
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(HandleClicked);
            }

            GameLocalization.LanguageChanged += RefreshLocalizedLabels;
            RefreshLocalizedLabels();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }

            GameLocalization.LanguageChanged -= RefreshLocalizedLabels;
        }

        public void Bind(CardData value)
        {
            cardData = value;
            if (value == null)
            {
                Clear();
                return;
            }

            SetText(costText, value.Cost.ToString());
            RefreshLocalizedLabels();

            if (illustrationImage != null)
            {
                illustrationImage.sprite = value.Illustration;
                illustrationImage.enabled = value.Illustration != null;
            }

            Color categoryColor = GetCategoryColor(value.Category);
            if (categoryStripeImage != null)
            {
                categoryStripeImage.color = categoryColor;
            }

            if (frameImage != null)
            {
                frameImage.color = Color.Lerp(Color.black, categoryColor, 0.25f);
            }
        }

        public void SetInteractable(bool isInteractable)
        {
            if (button != null)
            {
                button.interactable = isInteractable;
            }
        }

        private void Clear()
        {
            SetText(nameText, string.Empty);
            SetText(costText, string.Empty);
            SetText(typeText, string.Empty);
            SetText(rulesText, string.Empty);

            if (illustrationImage != null)
            {
                illustrationImage.sprite = null;
                illustrationImage.enabled = false;
            }
        }

        private void HandleClicked()
        {
            Clicked?.Invoke(this, cardData);
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private void ConfigureRulesText()
        {
            if (rulesText == null)
            {
                return;
            }

            rulesText.alignment = TextAnchor.MiddleCenter;
            rulesText.alignByGeometry = true;
            rulesText.horizontalOverflow = HorizontalWrapMode.Wrap;
            rulesText.verticalOverflow = VerticalWrapMode.Truncate;
            rulesText.resizeTextForBestFit = true;
            rulesText.resizeTextMinSize = Mathf.Min(9, rulesText.fontSize);
            rulesText.resizeTextMaxSize = rulesText.fontSize;
            rulesText.lineSpacing = 0.92f;
        }

        private void RefreshLocalizedLabels()
        {
            if (cardData == null)
            {
                return;
            }

            SetText(
                nameText,
                CardLocalization.GetName(cardData.CardId, cardData.DisplayName));
            SetText(
                rulesText,
                CardLocalization.GetRules(cardData.CardId, cardData.RulesText));

            string category = cardData.Category switch
            {
                CardCategory.Attack => GameLocalization.Text("card.category.attack"),
                CardCategory.Defense => GameLocalization.Text("card.category.defense"),
                CardCategory.Skill => GameLocalization.Text("card.category.skill"),
                CardCategory.Curse => GameLocalization.Text("card.category.curse"),
                _ => cardData.Category.ToString()
            };

            string rarity = cardData.Rarity switch
            {
                CardRarity.Common => GameLocalization.Text("card.rarity.common"),
                CardRarity.Rare => GameLocalization.Text("card.rarity.rare"),
                CardRarity.Curse => GameLocalization.Text("card.rarity.curse"),
                _ => cardData.Rarity.ToString()
            };

            SetText(typeText, GameLocalization.Format("card.meta", category, rarity));
        }

        private static Color GetCategoryColor(CardCategory category)
        {
            return category switch
            {
                CardCategory.Attack => new Color(0.72f, 0.18f, 0.16f),
                CardCategory.Defense => new Color(0.12f, 0.55f, 0.58f),
                CardCategory.Skill => new Color(0.45f, 0.28f, 0.72f),
                CardCategory.Curse => new Color(0.22f, 0.20f, 0.25f),
                _ => Color.white
            };
        }
    }
}
