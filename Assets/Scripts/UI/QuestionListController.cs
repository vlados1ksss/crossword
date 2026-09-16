using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>Список вопросов «По горизонтали» / «По вертикали» внутри ScrollView.</summary>
    public class QuestionListController : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform acrossContainer;
        [SerializeField] private RectTransform downContainer;
        [SerializeField] private QuestionItemUI itemPrefab;

        public event Action<CrosswordQuestion> QuestionClicked;

        private readonly List<QuestionItemUI> _pool = new List<QuestionItemUI>();
        private readonly Dictionary<CrosswordQuestion, QuestionItemUI> _items = new Dictionary<CrosswordQuestion, QuestionItemUI>();
        private QuestionItemUI _activeItem;

        public void Build(CrosswordBoard board)
        {
            _items.Clear();
            _activeItem = null;

            while (_pool.Count < board.Questions.Count)
            {
                var item = Instantiate(itemPrefab);
                item.Clicked += i => QuestionClicked?.Invoke(i.Question);
                _pool.Add(item);
            }

            int index = 0;
            // Вопросы уже отсортированы по номеру.
            foreach (var question in board.Questions)
            {
                var item = _pool[index++];
                item.transform.SetParent(question.IsAcross ? acrossContainer : downContainer, false);
                item.transform.SetAsLastSibling();
                item.gameObject.SetActive(true);
                item.Bind(question);
                _items[question] = item;
            }
            for (; index < _pool.Count; index++)
                _pool[index].gameObject.SetActive(false);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)scrollRect.content);
            scrollRect.verticalNormalizedPosition = 1f;
        }

        public void SetActive(CrosswordQuestion question)
        {
            if (_activeItem != null) _activeItem.SetActive(false);
            _activeItem = question != null && _items.TryGetValue(question, out var item) ? item : null;
            if (_activeItem == null) return;

            _activeItem.SetActive(true);
            ScrollTo(_activeItem.RectTransform);
        }

        public void MarkSolved(CrosswordQuestion question, bool animate)
        {
            if (!_items.TryGetValue(question, out var item)) return;
            if (animate) item.PlaySolved();
            else item.Refresh();
        }

        public void RefreshAll()
        {
            foreach (var item in _items.Values) item.Refresh();
        }

        /// <summary>Прокручивает список так, чтобы элемент был виден.</summary>
        private void ScrollTo(RectTransform target)
        {
            var content = scrollRect.content;
            var viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
            float contentHeight = content.rect.height;
            float viewportHeight = viewport.rect.height;
            if (contentHeight <= viewportHeight) return;

            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            float top = -content.InverseTransformPoint(corners[1]).y;      // расстояние от верха content
            float bottom = -content.InverseTransformPoint(corners[0]).y;

            float scrollable = contentHeight - viewportHeight;
            float currentTop = (1f - scrollRect.verticalNormalizedPosition) * scrollable;
            float newTop = currentTop;
            if (top < currentTop) newTop = top - 8f;
            else if (bottom > currentTop + viewportHeight) newTop = bottom - viewportHeight + 8f;

            newTop = Mathf.Clamp(newTop, 0f, scrollable);
            scrollRect.verticalNormalizedPosition = 1f - newTop / scrollable;
        }
    }
}
