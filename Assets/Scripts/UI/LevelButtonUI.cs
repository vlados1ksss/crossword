using System;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>Карточка уровня в меню выбора.</summary>
    public class LevelButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text titleText;
        [SerializeField] private Text difficultyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Image[] difficultyDots;

        public int LevelNumber { get; private set; }

        public event Action<int> Clicked;

        private void Awake()
        {
            button.onClick.AddListener(() => Clicked?.Invoke(LevelNumber));
        }

        public void Setup(LevelInfo level, LevelProgress progress, bool unlocked)
        {
            LevelNumber = level.Number;
            titleText.text = LocalizationManager.Format("level_title", level.Number);
            difficultyText.text = LocalizationManager.Get($"difficulty_{level.Difficulty}");

            for (int i = 0; i < difficultyDots.Length; i++)
                difficultyDots[i].color = i < level.Difficulty ? UIPalette.Warning : UIPalette.SlotLocked;

            bool completed = progress != null && progress.completed;
            if (!level.IsValid)
            {
                statusText.text = LocalizationManager.Get("level_invalid");
                statusText.color = UIPalette.Error;
            }
            else if (!unlocked)
            {
                statusText.text = LocalizationManager.Get("level_locked");
                statusText.color = UIPalette.TextSecondary;
            }
            else if (completed)
            {
                statusText.text = LocalizationManager.Get("level_completed");
                statusText.color = UIPalette.Success;
            }
            else if (progress != null && progress.solvedWords.Count > 0)
            {
                statusText.text = LocalizationManager.Format("level_progress", progress.solvedWords.Count, level.WordCount);
                statusText.color = UIPalette.Accent;
            }
            else
            {
                statusText.text = LocalizationManager.Get("level_new");
                statusText.color = UIPalette.TextSecondary;
            }

            bool interactable = unlocked && level.IsValid;
            button.interactable = interactable;
            background.color = !interactable ? UIPalette.Hex("E2E8F0") : completed ? UIPalette.CellCorrect : UIPalette.Panel;
        }
    }
}
