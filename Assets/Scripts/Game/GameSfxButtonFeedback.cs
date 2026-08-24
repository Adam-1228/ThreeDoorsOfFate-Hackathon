using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Game
{
    public sealed class GameSfxButtonFeedback : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        private Button targetButton;
        private UnityAction playFeedback;

        public void Configure(Button button, UnityAction callback)
        {
            targetButton = button;
            playFeedback = callback;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            PlayIfInteractable();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            PlayIfInteractable();
        }

        private void PlayIfInteractable()
        {
            if (targetButton != null && targetButton.IsActive() && targetButton.IsInteractable())
            {
                playFeedback?.Invoke();
            }
        }
    }
}
