using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>Короткое всплывающее сообщение (например, «реклама недоступна»).</summary>
    public class ToastMessage : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text messageText;
        [SerializeField] private float showSeconds = 2.2f;

        private Coroutine _routine;

        private void Awake()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        public void Show(string message)
        {
            messageText.text = message;
            AnimationController.Restart(this, ref _routine, ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            yield return AnimationController.Fade(canvasGroup, 1f, 0.2f);
            float t = 0f;
            while (t < showSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return AnimationController.Fade(canvasGroup, 0f, 0.3f);
            _routine = null;
        }
    }
}
