using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>
    /// Строка ввода активного слова. Известные буквы (пересечения, подсказки) заблокированы
    /// и не удаляются; пользователь вводит только недостающие.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AnswerInputController : MonoBehaviour
    {
        [SerializeField] private RectTransform slotsRoot;
        [SerializeField] private AnswerSlot slotPrefab;
        [SerializeField] private Text promptText;
        [SerializeField] private float maxSlotSize = 84f;
        [SerializeField] private float spacing = 8f;

        /// <summary>Позиция курсора изменилась (индекс буквы в слове).</summary>
        public event Action<int> CursorChanged;

        public CrosswordQuestion Word { get; private set; }
        public int Cursor { get; private set; } = -1;

        private readonly List<AnswerSlot> _slots = new List<AnswerSlot>();
        private char[] _typed = Array.Empty<char>();

        public void Show(CrosswordQuestion word, int cursorIndex)
        {
            Word = word;
            _typed = new char[word.Length];
            promptText.gameObject.SetActive(false);
            slotsRoot.gameObject.SetActive(true);

            while (_slots.Count < word.Length)
            {
                var slot = Instantiate(slotPrefab, slotsRoot);
                slot.Index = _slots.Count;
                slot.Clicked += OnSlotClicked;
                _slots.Add(slot);
            }
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].gameObject.SetActive(i < word.Length);
                _slots[i].ResetVisual();
            }

            Relayout();
            Cursor = -1;
            SetCursor(IsLocked(cursorIndex) ? FindEditable(cursorIndex, 1, true) : cursorIndex);
            RefreshSlots();
        }

        public void Hide()
        {
            Word = null;
            Cursor = -1;
            slotsRoot.gameObject.SetActive(false);
            promptText.gameObject.SetActive(true);
        }

        public bool IsLocked(int index) => Word != null && index >= 0 && index < Word.Length && Word.Cells[index].IsKnown;

        public bool IsComplete
        {
            get
            {
                if (Word == null) return false;
                for (int i = 0; i < Word.Length; i++)
                    if (!IsLocked(i) && _typed[i] == '\0') return false;
                return true;
            }
        }

        public bool HasTypedLetters
        {
            get
            {
                foreach (char c in _typed) if (c != '\0') return true;
                return false;
            }
        }

        public string GetAnswer()
        {
            var chars = new char[Word.Length];
            for (int i = 0; i < Word.Length; i++)
                chars[i] = IsLocked(i) ? Word.Cells[i].Solution : _typed[i];
            return new string(chars);
        }

        public void SetCursor(int index)
        {
            if (Word == null) return;
            if (index < 0 || index >= Word.Length) index = FindEditable(0, 1, true);
            if (index < 0) index = 0;
            if (Cursor == index) return;
            Cursor = index;
            RefreshSlots();
            CursorChanged?.Invoke(Cursor);
        }

        public void TypeLetter(char letter)
        {
            if (Word == null) return;
            int index = Cursor;
            if (IsLocked(index)) index = FindEditable(index, 1, true);
            if (index < 0) return;

            _typed[index] = letter;
            StartCoroutine(AnimationController.PopIn(_slots[index].Content, 0.18f, 0.7f));

            int next = FindEditable(index + 1, 1, false);
            if (next < 0) next = FindEmpty(index + 1);
            if (next < 0) next = index;
            Cursor = -1;
            SetCursor(next);
        }

        public void Backspace()
        {
            if (Word == null) return;
            int index = Cursor;
            if (index >= 0 && !IsLocked(index) && _typed[index] != '\0')
            {
                _typed[index] = '\0';
                RefreshSlots();
                return;
            }

            int previous = FindEditable(index - 1, -1, false);
            if (previous < 0) return;
            _typed[previous] = '\0';
            SetCursor(previous);
            RefreshSlots();
        }

        public void ClearTyped()
        {
            if (Word == null) return;
            for (int i = 0; i < _typed.Length; i++) _typed[i] = '\0';
            Cursor = -1;
            SetCursor(FindEditable(0, 1, true));
            RefreshSlots();
        }

        /// <summary>Вызывается после подсказки или решения пересекающегося слова.</summary>
        public void RefreshKnownLetters(int animatedIndex = -1)
        {
            if (Word == null) return;
            for (int i = 0; i < Word.Length; i++)
                if (IsLocked(i)) _typed[i] = '\0';

            if (animatedIndex >= 0 && animatedIndex < Word.Length)
                StartCoroutine(AnimationController.PopIn(_slots[animatedIndex].Content, 0.3f, 0.4f));

            if (IsLocked(Cursor))
            {
                int next = FindEditable(Cursor, 1, true);
                Cursor = -1;
                SetCursor(next >= 0 ? next : 0);
            }
            RefreshSlots();
        }

        public void RefreshSlots()
        {
            if (Word == null) return;
            for (int i = 0; i < Word.Length; i++)
            {
                bool locked = IsLocked(i);
                char letter = locked ? Word.Cells[i].Solution : _typed[i];
                _slots[i].SetLetter(letter, locked, i == Cursor && !locked);
            }
        }

        /// <summary>Правильный ответ: клетки последовательно и плавно становятся зелёными.</summary>
        public IEnumerator PlayCorrect()
        {
            for (int i = 0; i < Word.Length; i++)
            {
                var slot = _slots[i];
                StartCoroutine(AnimationController.ColorTo(slot.Background, UIPalette.SlotCorrect, 0.2f));
                StartCoroutine(AnimationController.Pulse(slot.Content, 0.25f, 1.15f));
                yield return WaitUnscaled(0.05f);
            }
            yield return WaitUnscaled(0.3f);
        }

        /// <summary>Неправильный ответ: красная подсветка, shake, очистка введённых букв.</summary>
        public IEnumerator PlayWrong()
        {
            for (int i = 0; i < Word.Length; i++)
                StartCoroutine(AnimationController.ColorTo(_slots[i].Background, UIPalette.SlotWrong, 0.12f));

            yield return AnimationController.Shake(slotsRoot, 0.35f, 16f);

            for (int i = 0; i < _typed.Length; i++) _typed[i] = '\0';
            Cursor = -1;
            SetCursor(FindEditable(0, 1, true)); // обновляет буквы и итоговые цвета клеток

            // Плавный возврат из красного к нормальному цвету.
            for (int i = 0; i < Word.Length; i++)
            {
                var background = _slots[i].Background;
                Color target = background.color;
                background.color = UIPalette.SlotWrong;
                StartCoroutine(AnimationController.ColorTo(background, target, 0.25f));
            }
            yield return WaitUnscaled(0.25f);
        }

        /// <summary>Неполный ответ: только мягкое покачивание без очистки.</summary>
        public IEnumerator PlayIncomplete()
        {
            yield return AnimationController.Shake(slotsRoot, 0.25f, 8f);
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void OnSlotClicked(AnswerSlot slot)
        {
            if (Word == null || slot.Index >= Word.Length) return;
            SetCursor(slot.Index);
        }

        /// <summary>Ищет незаблокированную клетку начиная с from в направлении step.</summary>
        private int FindEditable(int from, int step, bool wrap)
        {
            if (Word == null) return -1;
            int length = Word.Length;
            for (int n = 0; n < length; n++)
            {
                int i = from + n * step;
                if (wrap) i = ((i % length) + length) % length;
                else if (i < 0 || i >= length) return -1;
                if (!IsLocked(i)) return i;
            }
            return -1;
        }

        private int FindEmpty(int from)
        {
            for (int n = 0; n < Word.Length; n++)
            {
                int i = (from + n) % Word.Length;
                if (!IsLocked(i) && _typed[i] == '\0') return i;
            }
            return -1;
        }

        private void OnRectTransformDimensionsChange() => Relayout();

        private void Relayout()
        {
            if (Word == null) return;
            var rect = ((RectTransform)transform).rect;
            if (rect.width <= 0) return;
            int count = Word.Length;
            float size = Mathf.Min(maxSlotSize, rect.height, (rect.width - spacing * (count - 1)) / count);
            size = Mathf.Floor(size);
            var group = slotsRoot.GetComponent<HorizontalLayoutGroup>();
            if (group != null) group.spacing = spacing;
            for (int i = 0; i < count; i++) _slots[i].SetSize(size);
        }
    }
}
