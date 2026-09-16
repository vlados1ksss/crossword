using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>
    /// Экранная клавиатура. Раскладка берётся из файла локализации, клавиши создаются один раз.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class KeyboardController : MonoBehaviour
    {
        [SerializeField] private RectTransform rowsRoot;
        [SerializeField] private RectTransform rowPrefab;
        [SerializeField] private KeyboardKey keyPrefab;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float spacing = 8f;
        [SerializeField] private float disabledAlpha = 0.45f;

        public event Action<char> LetterPressed;
        public event Action BackspacePressed;
        public event Action ClearPressed;
        public event Action SubmitPressed;

        private readonly List<List<KeyboardKey>> _rows = new List<List<KeyboardKey>>();
        private readonly List<RectTransform> _rowRects = new List<RectTransform>();
        private Vector2 _lastSize;
        private bool _isOpen = true;
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            Build();
        }

        private void Build()
        {
            LocalizationManager.EnsureInitialized();
            var layout = LocalizationManager.Current;

            for (int r = 0; r < layout.keyboardRows.Length; r++)
            {
                var row = CreateRow();
                if (r == layout.keyboardRows.Length - 1)
                {
                    foreach (char c in layout.keyboardRows[r]) AddKey(row, KeyType.Letter, c, c.ToString(), 1f);
                    AddKey(row, KeyType.Backspace, '\0', "←", 1.6f);
                }
                else
                {
                    foreach (char c in layout.keyboardRows[r]) AddKey(row, KeyType.Letter, c, c.ToString(), 1f);
                }
            }

            var actions = CreateRow();
            AddKey(actions, KeyType.Clear, '\0', LocalizationManager.Get("key_clear"), 4f);
            AddKey(actions, KeyType.Submit, '\0', LocalizationManager.Get("key_done"), 6f);
        }

        private List<KeyboardKey> CreateRow()
        {
            var rowRect = Instantiate(rowPrefab, rowsRoot);
            rowRect.gameObject.SetActive(true);
            rowRect.name = $"Row_{_rows.Count + 1}";
            var group = rowRect.GetComponent<HorizontalLayoutGroup>();
            if (group != null) group.spacing = spacing;
            var keys = new List<KeyboardKey>();
            _rows.Add(keys);
            _rowRects.Add(rowRect);
            return keys;
        }

        private void AddKey(List<KeyboardKey> row, KeyType type, char letter, string text, float width)
        {
            var key = Instantiate(keyPrefab, _rowRects[_rows.IndexOf(row)]);
            key.gameObject.SetActive(true);
            key.Setup(type, letter, text, width);
            key.Pressed += OnKeyPressed;
            row.Add(key);
        }

        private void OnRectTransformDimensionsChange() => Relayout();

        private void Start() => Relayout();

        /// <summary>Все буквенные клавиши одинаковой ширины: ширина рассчитывается по самому длинному ряду.</summary>
        private void Relayout()
        {
            if (_rows.Count == 0) return;
            Vector2 size = ((RectTransform)transform).rect.size;
            if (size.x <= 0 || size.y <= 0 || (size - _lastSize).sqrMagnitude < 0.01f) return;
            _lastSize = size;

            float maxUnits = 0f;
            int maxKeys = 0;
            foreach (var row in _rows)
            {
                float units = 0f;
                foreach (var key in row) units += key.WidthMultiplier;
                maxUnits = Mathf.Max(maxUnits, units);
                maxKeys = Mathf.Max(maxKeys, row.Count);
            }

            float unitWidth = (size.x - spacing * (maxKeys - 1)) / maxUnits;
            float rowHeight = (size.y - spacing * (_rows.Count - 1)) / _rows.Count;
            unitWidth = Mathf.Min(unitWidth, rowHeight * 1.1f);

            var vertical = rowsRoot.GetComponent<VerticalLayoutGroup>();
            if (vertical != null) vertical.spacing = spacing;

            for (int r = 0; r < _rows.Count; r++)
            {
                // Кнопки нижнего ряда растягиваются на всю ширину буквенного ряда.
                float rowUnitWidth = unitWidth;
                if (r == _rows.Count - 1)
                {
                    float units = 0f;
                    foreach (var key in _rows[r]) units += key.WidthMultiplier;
                    float fullWidth = unitWidth * maxUnits + spacing * (maxKeys - 1);
                    rowUnitWidth = (fullWidth - spacing * (_rows[r].Count - 1)) / units;
                }
                foreach (var key in _rows[r]) key.SetSize(rowUnitWidth, rowHeight);
                _rowRects[r].GetComponent<LayoutElement>().preferredHeight = rowHeight;
            }
        }

        private void OnKeyPressed(KeyboardKey key)
        {
            if (!_isOpen) return;
            switch (key.Type)
            {
                case KeyType.Letter: LetterPressed?.Invoke(key.Letter); break;
                case KeyType.Backspace: BackspacePressed?.Invoke(); break;
                case KeyType.Clear: ClearPressed?.Invoke(); break;
                case KeyType.Submit: SubmitPressed?.Invoke(); break;
            }
        }

        /// <summary>«Открытая» клавиатура активна; «закрытая» остаётся на месте, но приглушена и не реагирует.</summary>
        public void SetOpen(bool open)
        {
            if (_isOpen == open) return;
            _isOpen = open;
            canvasGroup.interactable = open;
            canvasGroup.blocksRaycasts = open;
            float alpha = open ? 1f : disabledAlpha;
            if (isActiveAndEnabled)
                AnimationController.Restart(this, ref _fadeRoutine, AnimationController.Fade(canvasGroup, alpha, 0.2f));
            else
                canvasGroup.alpha = alpha;
        }

        /// <summary>Визуальный отклик клавиши при вводе с физической клавиатуры.</summary>
        public void FlashKey(KeyType type, char letter)
        {
            foreach (var row in _rows)
            foreach (var key in row)
            {
                if (key.Type != type || (type == KeyType.Letter && key.Letter != letter)) continue;
                if (key.isActiveAndEnabled)
                    key.StartCoroutine(AnimationController.Pulse(key.transform, 0.15f, 0.9f));
                return;
            }
        }
    }
}
