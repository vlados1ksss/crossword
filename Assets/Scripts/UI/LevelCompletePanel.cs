using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>Окно «Уровень пройден» со статистикой. Переход дальше — только по кнопке игрока.</summary>
    public class LevelCompletePanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform card;
        [SerializeField] private Text titleText;
        [SerializeField] private Text wordsText;
        [SerializeField] private Text timeText;
        [SerializeField] private Text hintsText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button replayButton;

        public event Action NextClicked;
        public event Action MenuClicked;
        public event Action ReplayClicked;

        private void Awake()
        {
            nextButton.onClick.AddListener(() => NextClicked?.Invoke());
            menuButton.onClick.AddListener(() => MenuClicked?.Invoke());
            // Панель изначально выключена в сцене; Awake вызывается при первом показе.
            replayButton.onClick.AddListener(() => ReplayClicked?.Invoke());
        }

        public void Show(int words, float seconds, int hints, bool hasNextLevel, bool animate)
        {
            titleText.text = LocalizationManager.Get(hasNextLevel ? "complete_title" : "complete_all_title");
            wordsText.text = LocalizationManager.Format("complete_words", words);
            int total = Mathf.FloorToInt(seconds);
            timeText.text = LocalizationManager.Format("complete_time", $"{total / 60:00}:{total % 60:00}");
            hintsText.text = LocalizationManager.Format("complete_hints", hints);
            nextButton.gameObject.SetActive(hasNextLevel);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            SetButtonsInteractable(true);

            if (animate)
            {
                canvasGroup.alpha = 0f;
                StartCoroutine(ShowRoutine());
            }
            else
            {
                canvasGroup.alpha = 1f;
                card.localScale = Vector3.one;
            }
        }

        public void Hide() => gameObject.SetActive(false);

        public void SetButtonsInteractable(bool value)
        {
            nextButton.interactable = value;
            menuButton.interactable = value;
            replayButton.interactable = value;
        }

        private IEnumerator ShowRoutine()
        {
            card.localScale = Vector3.one * 0.8f;
            StartCoroutine(AnimationController.Fade(canvasGroup, 1f, 0.3f));
            yield return AnimationController.PopIn(card, 0.4f, 0.8f);
            yield return AnimationController.Pulse(titleText.transform, 0.35f, 1.1f);
        }
    }
}
