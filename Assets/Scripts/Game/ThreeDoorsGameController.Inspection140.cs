using ThreeDoorsOfFate.Audio;
using ThreeDoorsOfFate.Cards;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private enum CardInspectionMode
        {
            CombatUse,
            RewardTake,
            ShopBuy,
            TreasureTake,
            DeckRemove
        }

        private Image cardInspectionBackdrop;
        private CardInspectionMode cardInspectionMode;
        private UnityAction pendingCardInspectionConfirm;
        private bool cardInspectionActive;

        private void ShowCardInspection(
            CardData card,
            string actionLabel,
            UnityAction confirm)
        {
            ShowCardInspection(
                card,
                CardInspectionMode.RewardTake,
                actionLabel,
                confirm);
        }

        private void ShowCardInspection(
            CardData card,
            CardInspectionMode mode,
            string actionLabel,
            UnityAction confirm)
        {
            Sprite sprite = card != null ? GetLocalizedCardFullSprite(card) : null;
            if (sprite == null || phase == GamePhase.GameOver)
            {
                return;
            }

            EnsureCardInspectionOverlay();
            cardInspectionMode = mode;
            pendingCardInspectionConfirm = confirm;
            cardInspectionActive = true;
            cardPreviewTarget = null;

            cardInspectionBackdrop.gameObject.SetActive(true);
            cardInspectionBackdrop.transform.SetAsLastSibling();

            cardPreviewImage.sprite = sprite;
            cardPreviewImage.color = Color.white;
            cardPreviewImage.raycastTarget = true;
            BindLocalizedCardSprite(cardPreviewImage, card);
            Button previewDismiss = cardPreviewImage.GetComponent<Button>();
            if (previewDismiss != null)
            {
                previewDismiss.interactable = true;
            }

            cardPreviewImage.gameObject.SetActive(true);
            cardPreviewImage.transform.SetAsLastSibling();

            cardPreviewUseButton.gameObject.name = mode == CardInspectionMode.CombatUse
                ? "카드 사용"
                : "카드 검사 확정";
            SetButtonLabel(cardPreviewUseButton, actionLabel);
            cardPreviewUseButton.gameObject.SetActive(true);
            cardPreviewUseButton.transform.SetAsLastSibling();
            cardPreviewCancelButton.interactable = true;
        }

        private void HideCardInspection()
        {
            pendingCardInspectionConfirm = null;
            cardInspectionActive = false;
            if (cardInspectionBackdrop != null)
            {
                cardInspectionBackdrop.gameObject.SetActive(false);
            }

            if (cardPreviewImage != null)
            {
                cardPreviewImage.gameObject.SetActive(false);
            }

            if (cardPreviewUseButton != null)
            {
                cardPreviewUseButton.gameObject.SetActive(false);
            }

            if (cardPreviewCancelButton != null)
            {
                cardPreviewCancelButton.interactable = false;
            }

            selectedCombatCardIndex = -1;
            cardPreviewTarget = null;
        }

        private void ConfirmCardInspection()
        {
            UnityAction confirm = pendingCardInspectionConfirm;
            pendingCardInspectionConfirm = null;
            try
            {
                confirm?.Invoke();
            }
            finally
            {
                if (cardInspectionActive)
                {
                    HideCardInspection();
                }
            }
        }

        private void EnsureCardInspectionOverlay()
        {
            if (cardInspectionBackdrop == null)
            {
                cardInspectionBackdrop = AddImage(
                    root,
                    "카드 검사 배경",
                    new Color(0.004f, 0.010f, 0.014f, 0.88f));
                cardInspectionBackdrop.raycastTarget = false;
                Stretch(cardInspectionBackdrop.rectTransform);
                cardInspectionBackdrop.gameObject.SetActive(false);
            }

            if (cardPreviewCancelButton == null)
            {
                cardPreviewCancelButton = AddSfxButton(
                    root.gameObject,
                    GameSfxCue.None);
                cardPreviewCancelButton.transition = Selectable.Transition.None;
                cardPreviewCancelButton.targetGraphic = null;
                Navigation navigation = cardPreviewCancelButton.navigation;
                navigation.mode = Navigation.Mode.None;
                cardPreviewCancelButton.navigation = navigation;
                cardPreviewCancelButton.onClick.AddListener(HideCardInspection);
                cardPreviewCancelButton.interactable = false;
            }

            if (cardPreviewImage == null)
            {
                cardPreviewImage = AddImage(root, "카드 확대 프리뷰", Color.white);
                cardPreviewImage.preserveAspect = true;
                SetAnchors(
                    cardPreviewImage.rectTransform,
                    new Vector2(0.390f, 0.300f),
                    new Vector2(0.610f, 0.850f));

                Button previewDismiss = AddSfxButton(
                    cardPreviewImage.gameObject,
                    GameSfxCue.None);
                previewDismiss.transition = Selectable.Transition.None;
                previewDismiss.targetGraphic = cardPreviewImage;
                previewDismiss.colors = CreateStaticButtonColors();
                previewDismiss.onClick.AddListener(HideCardInspection);
                cardPreviewImage.gameObject.SetActive(false);
            }

            if (cardPreviewUseButton == null)
            {
                cardPreviewUseButton = AddSettingsMenuButton(
                    root,
                    "카드 검사 확정",
                    "사용",
                    24,
                    GameSfxCue.None);
                SetAnchors(
                    cardPreviewUseButton.GetComponent<RectTransform>(),
                    new Vector2(0.425f, 0.200f),
                    new Vector2(0.575f, 0.265f));
                cardPreviewUseButton.onClick.AddListener(ConfirmCardInspection);
                cardPreviewUseButton.gameObject.SetActive(false);
            }
        }

        private void ShowHoverCardPreview(CardData card, RectTransform previewTarget)
        {
            if (!cardInspectionActive)
            {
                ShowCardPreview(card, previewTarget);
            }
        }

        private void HideHoverCardPreview(RectTransform previewTarget)
        {
            if (!cardInspectionActive && cardPreviewTarget == previewTarget)
            {
                HideCardPreview();
            }
        }
    }
}
