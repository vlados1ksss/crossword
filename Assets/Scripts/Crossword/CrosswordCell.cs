using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrosswordGame
{
    public enum CellState
    {
        Empty,       // буква неизвестна
        Filled,      // буква известна (из пересечения)
        Selected,    // активная клетка
        Correct,     // клетка принадлежит разгаданному слову
        HintFilled   // буква открыта подсказкой
    }

    /// <summary>Визуальное представление клетки. Состояние берётся из CellData, сам компонент его не хранит.</summary>
    public class CrosswordCell : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image fill;
        [SerializeField] private Text letterText;
        [SerializeField] private Text numberText;
        [SerializeField] private RectTransform content;

        public CellData Data { get; private set; }
        public CellState State { get; private set; }
        public RectTransform RectTransform => (RectTransform)transform;

        public event Action<CrosswordCell> Clicked;

        private bool _inActiveWord;
        private bool _isActiveCell;
        private Coroutine _colorRoutine;
        private Coroutine _scaleRoutine;

        public void Bind(CellData data)
        {
            Data = data;
            numberText.text = data.Number > 0 ? data.Number.ToString() : string.Empty;
            _inActiveWord = false;
            _isActiveCell = false;
            content.localScale = Vector3.one;
            Refresh(false);
        }

        public void SetLayout(float size, float fontScale = 1f)
        {
            RectTransform.sizeDelta = new Vector2(size, size);
            letterText.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * 0.58f * fontScale));
            numberText.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * 0.3f));
        }

        public void SetHighlight(bool inActiveWord, bool isActiveCell)
        {
            if (_inActiveWord == inActiveWord && _isActiveCell == isActiveCell) return;
            _inActiveWord = inActiveWord;
            _isActiveCell = isActiveCell;
            Refresh(true);
        }

        /// <summary>Обновляет букву и цвет по данным модели.</summary>
        public void Refresh(bool animateColor)
        {
            State = ResolveState();
            letterText.text = Data.IsKnown ? Data.Solution.ToString() : string.Empty;
            letterText.color = Data.RevealedByHint && !IsSolvedCell ? UIPalette.CellLetterHint
                : IsSolvedCell ? UIPalette.CellLetterCorrect : UIPalette.CellLetter;

            Color target = GetFillColor();
            if (animateColor && gameObject.activeInHierarchy)
                AnimationController.Restart(this, ref _colorRoutine, AnimationController.ColorTo(fill, target, 0.15f));
            else
                fill.color = target;
        }

        /// <summary>Анимация появления буквы (после правильного ответа или подсказки).</summary>
        public void PlayLetterAppear()
        {
            Refresh(true);
            if (gameObject.activeInHierarchy)
                AnimationController.Restart(this, ref _scaleRoutine, AnimationController.PopIn(content, 0.3f, 0.5f));
        }

        public void PlaySelectPulse()
        {
            if (gameObject.activeInHierarchy)
                AnimationController.Restart(this, ref _scaleRoutine, AnimationController.Pulse(content, 0.18f, 1.08f));
        }

        private bool IsSolvedCell => (Data.Across != null && Data.Across.IsSolved) || (Data.Down != null && Data.Down.IsSolved);

        private CellState ResolveState()
        {
            if (_isActiveCell) return CellState.Selected;
            if (IsSolvedCell) return CellState.Correct;
            if (Data.RevealedByHint) return CellState.HintFilled;
            if (Data.IsKnown) return CellState.Filled;
            return CellState.Empty;
        }

        private Color GetFillColor()
        {
            if (_isActiveCell) return UIPalette.CellSelected;
            if (_inActiveWord) return UIPalette.CellWord;
            switch (State)
            {
                case CellState.Correct: return UIPalette.CellCorrect;
                case CellState.HintFilled: return UIPalette.CellHint;
                default: return UIPalette.CellEmpty;
            }
        }

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(this);

        private void OnDisable()
        {
            _colorRoutine = null;
            _scaleRoutine = null;
            if (content != null) content.localScale = Vector3.one;
            if (fill != null && Data != null) fill.color = GetFillColor();
        }
    }
}
