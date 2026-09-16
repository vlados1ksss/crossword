using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>Главное меню: название, «Продолжить», «Выбор уровня» и список уровней.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup mainPanel;
        [SerializeField] private CanvasGroup levelSelectionPanel;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text continueLabel;
        [SerializeField] private Text continueSubLabel;
        [SerializeField] private Button selectLevelButton;
        [SerializeField] private Button backButton;
        [SerializeField] private RectTransform levelsContainer;
        [SerializeField] private LevelButtonUI levelButtonPrefab;
        [SerializeField] private Text loadingText;

        private readonly List<LevelButtonUI> _buttons = new List<LevelButtonUI>();
        private GameManager _game;

        private void Awake()
        {
            continueButton.onClick.AddListener(OnContinueClicked);
            selectLevelButton.onClick.AddListener(() => ShowPanel(levelSelectionPanel, mainPanel));
            backButton.onClick.AddListener(() => ShowPanel(mainPanel, levelSelectionPanel));

            SetPanel(mainPanel, true);
            SetPanel(levelSelectionPanel, false);
            continueButton.interactable = false;
            selectLevelButton.interactable = false;
            loadingText.gameObject.SetActive(true);
        }

        private void Start()
        {
            _game = GameManager.Instance;
            PlatformBridge.GameplayStop();
            _game.WhenReady(OnGameReady);
        }

        private void OnGameReady()
        {
            loadingText.gameObject.SetActive(false);
            continueButton.interactable = _game.Levels.Count > 0;
            selectLevelButton.interactable = _game.Levels.Count > 0;

            int continueLevel = _game.GetContinueLevel();
            continueLabel.text = LocalizationManager.Get(_game.HasAnyProgress() ? "menu_continue" : "menu_start");
            var info = _game.Levels.GetLevel(continueLevel);
            continueSubLabel.text = info != null
                ? $"{LocalizationManager.Format("level_title", continueLevel)} · {LocalizationManager.Get($"difficulty_{info.Difficulty}")}"
                : string.Empty;

            BuildLevelButtons();
            PlatformBridge.GameReady();

            StartCoroutine(AnimationController.PopIn(continueButton.transform, 0.35f, 0.85f));
        }

        private void BuildLevelButtons()
        {
            foreach (var level in _game.Levels.Levels)
            {
                var button = Instantiate(levelButtonPrefab, levelsContainer);
                button.Setup(level, _game.Save.FindLevel(level.Number), _game.Save.IsUnlocked(level.Number));
                button.Clicked += OnLevelClicked;
                _buttons.Add(button);
            }
        }

        private void OnContinueClicked()
        {
            _game.OpenLevel(_game.GetContinueLevel());
        }

        private void OnLevelClicked(int levelNumber)
        {
            _game.OpenLevel(levelNumber);
        }

        private void ShowPanel(CanvasGroup show, CanvasGroup hide)
        {
            SetPanel(hide, false);
            SetPanel(show, true);
            show.alpha = 0f;
            StartCoroutine(FadeIn(show));
        }

        private IEnumerator FadeIn(CanvasGroup group)
        {
            yield return AnimationController.Fade(group, 1f, 0.2f);
        }

        private static void SetPanel(CanvasGroup group, bool visible)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            group.gameObject.SetActive(visible);
        }
    }
}
