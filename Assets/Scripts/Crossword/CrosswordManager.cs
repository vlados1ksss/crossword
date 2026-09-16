using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>
    /// Игровой процесс одного уровня: выбор слова, ввод, проверка, подсказки, сохранение, завершение.
    /// Связывает между собой поле, список вопросов, строку ввода и клавиатуру.
    /// </summary>
    public class CrosswordManager : MonoBehaviour
    {
        [Header("Crossword")]
        [SerializeField] private CrosswordGridManager grid;
        [SerializeField] private QuestionListController questionList;

        [Header("Input")]
        [SerializeField] private AnswerInputController answerInput;
        [SerializeField] private KeyboardController keyboard;
        [SerializeField] private PhysicalKeyboardInput physicalKeyboard;

        [Header("UI")]
        [SerializeField] private HintButton hintButton;
        [SerializeField] private LevelCompletePanel levelCompletePanel;
        [SerializeField] private ToastMessage toast;
        [SerializeField] private Text levelTitleText;
        [SerializeField] private Text counterText;
        [SerializeField] private Button menuButton;

        private GameManager _game;
        private LevelInfo _level;
        private CrosswordBoard _board;
        private LevelProgress _progress;

        private CrosswordQuestion _activeWord;
        private CellData _activeCell;
        private CellData _lastClickedCell;
        private bool _busy;
        private float _sessionStart;

        private void Awake()
        {
            menuButton.onClick.AddListener(OnMenuClicked);
        }

        private void Start()
        {
            _game = GameManager.Instance;
            _game.WhenReady(StartLevel);
        }

        private void OnEnable()
        {
            grid.CellClicked += OnCellClicked;
            grid.LayoutChanged += OnGridLayoutChanged;
            questionList.QuestionClicked += OnQuestionClicked;
            answerInput.CursorChanged += OnCursorChanged;

            keyboard.LetterPressed += OnLetter;
            keyboard.BackspacePressed += OnBackspace;
            keyboard.ClearPressed += OnClear;
            keyboard.SubmitPressed += OnSubmit;

            physicalKeyboard.LetterTyped += OnPhysicalLetter;
            physicalKeyboard.BackspacePressed += OnPhysicalBackspace;
            physicalKeyboard.SubmitPressed += OnPhysicalSubmit;
            physicalKeyboard.CancelPressed += Deselect;

            hintButton.Clicked += OnHintClicked;
            levelCompletePanel.NextClicked += OnNextLevelClicked;
            levelCompletePanel.MenuClicked += OnMenuAfterCompleteClicked;
            levelCompletePanel.ReplayClicked += OnReplayClicked;
        }

        private void OnDisable()
        {
            grid.CellClicked -= OnCellClicked;
            grid.LayoutChanged -= OnGridLayoutChanged;
            questionList.QuestionClicked -= OnQuestionClicked;
            answerInput.CursorChanged -= OnCursorChanged;

            keyboard.LetterPressed -= OnLetter;
            keyboard.BackspacePressed -= OnBackspace;
            keyboard.ClearPressed -= OnClear;
            keyboard.SubmitPressed -= OnSubmit;

            physicalKeyboard.LetterTyped -= OnPhysicalLetter;
            physicalKeyboard.BackspacePressed -= OnPhysicalBackspace;
            physicalKeyboard.SubmitPressed -= OnPhysicalSubmit;
            physicalKeyboard.CancelPressed -= Deselect;

            hintButton.Clicked -= OnHintClicked;
            levelCompletePanel.NextClicked -= OnNextLevelClicked;
            levelCompletePanel.MenuClicked -= OnMenuAfterCompleteClicked;
            levelCompletePanel.ReplayClicked -= OnReplayClicked;
        }

        #region Level lifecycle

        private void StartLevel()
        {
            int levelNumber = _game.SelectedLevel;
            _level = _game.Levels.GetLevel(levelNumber);
            answerInput.Hide();
            keyboard.SetOpen(false);

            if (_level == null || !_level.IsValid)
            {
                levelTitleText.text = LocalizationManager.Format("level_title", levelNumber);
                counterText.text = string.Empty;
                toast.Show(LocalizationManager.Get("error_level_load"));
                return;
            }

            _board = new CrosswordBoard(_level.Data);
            _progress = _game.Save.GetLevel(levelNumber);
            RestoreProgress();

            grid.Build(_board);
            questionList.Build(_board);

            levelTitleText.text = $"{LocalizationManager.Format("level_title", levelNumber)} · {LocalizationManager.Get($"difficulty_{_level.Difficulty}")}";
            UpdateCounter();

            _sessionStart = Time.realtimeSinceStartup;
            PlatformBridge.GameReady();

            if (_progress.completed || _board.IsCompleted)
            {
                if (!_progress.completed) CompleteLevel(false);
                else ShowCompletePanel(false);
            }
            else
            {
                PlatformBridge.GameplayStart();
            }
        }

        private void RestoreProgress()
        {
            foreach (var id in _progress.solvedWords)
            {
                var question = _board.GetQuestion(id);
                if (question != null) question.IsSolved = true;
            }
            foreach (var pos in _progress.hintCells)
            {
                var cell = _board.GetCell(pos);
                if (cell != null) cell.RevealedByHint = true;
            }
            // Слова, все буквы которых уже известны, считаются решёнными.
            foreach (var question in _board.Questions)
            {
                if (!question.IsSolved && question.AllLettersKnown)
                {
                    question.IsSolved = true;
                    if (!_progress.solvedWords.Contains(question.Id)) _progress.solvedWords.Add(question.Id);
                }
            }
        }

        private void AccumulatePlayTime()
        {
            if (_progress == null || _progress.completed) return;
            float now = Time.realtimeSinceStartup;
            _progress.playTimeSeconds += now - _sessionStart;
            _sessionStart = now;
        }

        private void SaveProgress()
        {
            AccumulatePlayTime();
            _game.Save.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _progress != null) SaveProgress();
        }

        private void OnApplicationQuit()
        {
            if (_progress != null) SaveProgress();
        }

        #endregion

        #region Selection

        private void OnCellClicked(CrosswordCell view)
        {
            if (!CanSelect) return;
            CellData cell = view.Data;
            CrosswordQuestion word;

            bool cellInActiveWord = _activeWord != null && (cell.Across == _activeWord || cell.Down == _activeWord);
            if (cellInActiveWord && cell == _lastClickedCell && cell.BelongsToTwoWords)
            {
                // Повторное нажатие на пересечение переключает направление.
                word = cell.Across == _activeWord ? cell.Down : cell.Across;
            }
            else if (cellInActiveWord)
            {
                // Та же клетка активного слова: переносим курсор, не сбрасывая введённые буквы.
                int index = _activeWord.IndexOf(cell);
                if (!cell.IsKnown) answerInput.SetCursor(index);
                _lastClickedCell = cell;
                view.PlaySelectPulse();
                return;
            }
            else if (cell.BelongsToTwoWords)
            {
                if (cell.Across.IsSolved && !cell.Down.IsSolved) word = cell.Down;
                else word = cell.Across; // оба не разгаданы, оба разгаданы, или неразгадано горизонтальное
            }
            else
            {
                word = cell.Across ?? cell.Down;
            }

            SelectWord(word, cell);
            _lastClickedCell = cell;
            view.PlaySelectPulse();
        }

        private void OnQuestionClicked(CrosswordQuestion question)
        {
            if (!CanSelect || question == _activeWord) return;
            CellData start = question.Cells.Find(c => !c.IsKnown) ?? question.Cells[0];
            SelectWord(question, start);
        }

        private void SelectWord(CrosswordQuestion word, CellData cell)
        {
            _activeWord = word;
            _lastClickedCell = null;
            answerInput.Show(word, word.IndexOf(cell));
            // Активная клетка совпадает с курсором ввода (известные буквы курсор пропускает).
            _activeCell = word.IsSolved || answerInput.Cursor < 0 ? cell : word.Cells[answerInput.Cursor];

            questionList.SetActive(word);
            keyboard.SetOpen(!word.IsSolved);
            RefreshSelectionVisuals();
        }

        private void OnCursorChanged(int index)
        {
            if (_activeWord == null || index < 0 || index >= _activeWord.Length) return;
            _activeCell = _activeWord.Cells[index];
            RefreshSelectionVisuals();
        }

        private void Deselect()
        {
            if (_busy) return;
            ClearSelection();
        }

        private void ClearSelection()
        {
            _activeWord = null;
            _activeCell = null;
            _lastClickedCell = null;
            answerInput.Hide();
            keyboard.SetOpen(false);
            questionList.SetActive(null);
            grid.SetHighlight(null, null);
            hintButton.Hide();
        }

        private void RefreshSelectionVisuals()
        {
            grid.SetHighlight(_activeWord, _activeCell);
            UpdateHintButton();
        }

        private void UpdateHintButton()
        {
            bool show = !_busy && _progress != null && !_progress.completed && _activeCell != null && _activeWord != null && !_activeWord.IsSolved && !_activeCell.IsKnown;
            if (show) hintButton.ShowFor(grid.GetView(_activeCell), _activeWord.IsAcross);
            else hintButton.Hide();
        }

        private void OnGridLayoutChanged() => hintButton.UpdatePosition();

        #endregion

        #region Typing

        private bool CanSelect => !_busy && _board != null && !_progress.completed;
        private bool CanType => CanSelect && _activeWord != null && !_activeWord.IsSolved;

        private void OnLetter(char letter)
        {
            if (!CanType) return;
            answerInput.TypeLetter(letter);
        }

        private void OnBackspace()
        {
            if (!CanType) return;
            answerInput.Backspace();
        }

        private void OnClear()
        {
            if (!CanType) return;
            answerInput.ClearTyped();
        }

        private void OnSubmit()
        {
            if (!CanType) return;
            StartCoroutine(SubmitRoutine());
        }

        private void OnPhysicalLetter(char letter)
        {
            if (!CanType) return;
            keyboard.FlashKey(KeyType.Letter, letter);
            OnLetter(letter);
        }

        private void OnPhysicalBackspace()
        {
            if (!CanType) return;
            keyboard.FlashKey(KeyType.Backspace, '\0');
            OnBackspace();
        }

        private void OnPhysicalSubmit()
        {
            if (!CanType) return;
            keyboard.FlashKey(KeyType.Submit, '\0');
            OnSubmit();
        }

        private IEnumerator SubmitRoutine()
        {
            if (!answerInput.IsComplete)
            {
                yield return answerInput.PlayIncomplete();
                yield break;
            }

            SetBusy(true);
            CrosswordQuestion word = _activeWord;

            if (answerInput.GetAnswer() == word.Answer)
            {
                yield return answerInput.PlayCorrect();
                yield return SolveWordRoutine(word);
            }
            else
            {
                yield return answerInput.PlayWrong();
            }

            SetBusy(false);
        }

        #endregion

        #region Solving

        /// <summary>Отмечает слово решённым, переносит буквы на поле, решает слова, которые стали полностью известны.</summary>
        private IEnumerator SolveWordRoutine(CrosswordQuestion word)
        {
            MarkSolved(word);
            SaveProgress();

            foreach (var cell in word.Cells)
            {
                grid.GetView(cell).PlayLetterAppear();
                yield return Wait(0.06f);
            }
            questionList.MarkSolved(word, true);
            UpdateCounter();

            // Буквы пересечений могли полностью открыть другие слова.
            foreach (var other in _board.Questions)
            {
                if (other.IsSolved || !other.AllLettersKnown) continue;
                MarkSolved(other);
                foreach (var cell in other.Cells) grid.GetView(cell).PlayLetterAppear();
                questionList.MarkSolved(other, true);
            }
            SaveProgress();
            UpdateCounter();
            grid.RefreshAll();

            yield return Wait(0.2f);
            ClearSelection();

            if (_board.IsCompleted)
                CompleteLevel(true);
        }

        private void MarkSolved(CrosswordQuestion word)
        {
            word.IsSolved = true;
            if (!_progress.solvedWords.Contains(word.Id))
                _progress.solvedWords.Add(word.Id);
        }

        private void UpdateCounter()
        {
            counterText.text = LocalizationManager.Format("game_counter", _board.SolvedCount, _board.Questions.Count);
        }

        #endregion

        #region Hint

        private void OnHintClicked()
        {
            if (!CanType || _activeCell == null || _activeCell.IsKnown) return;

            CellData targetCell = _activeCell;
            CrosswordQuestion targetWord = _activeWord;
            SetBusy(true);
            AccumulatePlayTime();

            _game.Ads.ShowRewarded(AdsManager.HintPlacement, result =>
            {
                if (this == null) return; // сцена могла быть выгружена
                AccumulatePlayTimeAfterAd();
                switch (result)
                {
                    case RewardedAdResult.Rewarded:
                        StartCoroutine(RevealHintRoutine(targetWord, targetCell));
                        break;
                    case RewardedAdResult.NotWatched:
                        toast.Show(LocalizationManager.Get("hint_not_watched"));
                        SetBusy(false);
                        break;
                    default:
                        toast.Show(LocalizationManager.Get("hint_unavailable"));
                        SetBusy(false);
                        break;
                }
            });
        }

        private void AccumulatePlayTimeAfterAd()
        {
            // Время просмотра рекламы не учитывается в статистике уровня.
            _sessionStart = Time.realtimeSinceStartup;
        }

        private IEnumerator RevealHintRoutine(CrosswordQuestion word, CellData cell)
        {
            cell.RevealedByHint = true;
            _progress.hintCells.Add(cell.Pos);
            _progress.hintsUsed++;
            SaveProgress();

            grid.GetView(cell).PlayLetterAppear();
            if (_activeWord == word)
                answerInput.RefreshKnownLetters(word.IndexOf(cell));
            hintButton.Hide();

            yield return Wait(0.3f);

            if (word.AllLettersKnown)
            {
                if (_activeWord == word) yield return answerInput.PlayCorrect();
                yield return SolveWordRoutine(word);
            }
            else
            {
                // Пересекающее слово тоже могло стать полностью известным.
                foreach (var other in cell.Words)
                {
                    if (other == word || other.IsSolved || !other.AllLettersKnown) continue;
                    MarkSolved(other);
                    foreach (var c in other.Cells) grid.GetView(c).PlayLetterAppear();
                    questionList.MarkSolved(other, true);
                    UpdateCounter();
                    SaveProgress();
                }
                if (_board.IsCompleted) CompleteLevel(true);
            }

            SetBusy(false);
        }

        #endregion

        #region Completion and navigation

        private void CompleteLevel(bool animate)
        {
            AccumulatePlayTime();
            _progress.completed = true;

            int next = _level.Number + 1;
            if (_game.Levels.GetLevel(next) != null) _game.Save.UnlockLevel(next);
            _game.Save.Save();

            PlatformBridge.GameplayStop();
            ClearSelection();

            if (animate) StartCoroutine(CompleteRoutine());
            else ShowCompletePanel(false);
        }

        private IEnumerator CompleteRoutine()
        {
            SetBusy(true);
            // Волна по всем клеткам поля.
            foreach (var view in grid.Views)
            {
                float delay = (view.Data.Pos.row - _board.MinRow + view.Data.Pos.col - _board.MinCol) * 0.03f;
                StartCoroutine(DelayedPulse(view, delay));
            }
            yield return Wait(0.3f + (_board.Rows + _board.Cols) * 0.03f);
            ShowCompletePanel(true);
            SetBusy(false);
        }

        private IEnumerator DelayedPulse(CrosswordCell view, float delay)
        {
            yield return Wait(delay);
            view.PlaySelectPulse();
        }

        private void ShowCompletePanel(bool animate)
        {
            bool hasNext = _game.Levels.GetLevel(_level.Number + 1) != null;
            levelCompletePanel.Show(_board.Questions.Count, _progress.playTimeSeconds, _progress.hintsUsed, hasNext, animate);
        }

        private void OnNextLevelClicked()
        {
            levelCompletePanel.SetButtonsInteractable(false);
            _game.Ads.TryShowInterstitial();
            _game.OpenLevel(_level.Number + 1);
        }

        private void OnMenuAfterCompleteClicked()
        {
            levelCompletePanel.SetButtonsInteractable(false);
            _game.Ads.TryShowInterstitial();
            _game.OpenMainMenu();
        }

        private void OnReplayClicked()
        {
            levelCompletePanel.SetButtonsInteractable(false);
            _game.Save.ResetLevel(_level.Number);
            _game.OpenLevel(_level.Number);
        }

        private void OnMenuClicked()
        {
            if (_progress != null) SaveProgress();
            _game.OpenMainMenu();
        }

        #endregion

        private void SetBusy(bool busy)
        {
            _busy = busy;
            hintButton.SetInteractable(!busy);
            UpdateHintButton();
        }

        private static IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
