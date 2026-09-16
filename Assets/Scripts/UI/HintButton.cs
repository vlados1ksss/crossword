using System;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>Кнопка «Подсказка», которая появляется рядом с выбранной клеткой поля.</summary>
    public class HintButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform area;   // область, в пределах которой держится кнопка
        [SerializeField] private float gap = 10f;

        public event Action Clicked;

        private RectTransform Rect => (RectTransform)transform;
        private CrosswordCell _target;
        private Coroutine _routine;

        private void Awake()
        {
            // Объект изначально выключен в сцене; Awake вызывается при первом показе.
            button.onClick.AddListener(() => Clicked?.Invoke());
        }

        public void SetInteractable(bool interactable) => button.interactable = interactable;

        private bool _wordIsAcross;

        /// <summary>Показывает кнопку у клетки. Для горизонтального слова кнопка ставится сверху/снизу, для вертикального — сбоку.</summary>
        public void ShowFor(CrosswordCell cell, bool wordIsAcross)
        {
            bool wasVisible = gameObject.activeSelf && _target != null;
            _target = cell;
            _wordIsAcross = wordIsAcross;
            gameObject.SetActive(true);
            UpdatePosition();
            if (!wasVisible)
                AnimationController.Restart(this, ref _routine, AnimationController.PopIn(transform, 0.2f, 0.6f));
        }

        public void Hide()
        {
            _target = null;
            gameObject.SetActive(false);
        }

        /// <summary>Ставит кнопку справа от клетки (или слева/сверху/снизу, если не помещается).</summary>
        public void UpdatePosition()
        {
            if (_target == null) return;

            transform.SetAsLastSibling();
            Rect.anchorMin = Rect.anchorMax = new Vector2(0.5f, 0.5f);
            Rect.pivot = new Vector2(0.5f, 0.5f);

            Rect areaRect = area.rect;
            Vector2 size = Rect.rect.size;
            var cellRect = _target.RectTransform;
            Vector2 cellCenter = area.InverseTransformPoint(cellRect.TransformPoint(cellRect.rect.center));
            float half = cellRect.rect.width * 0.5f;

            Vector2 right = cellCenter + new Vector2(half + gap + size.x * 0.5f, 0f);
            Vector2 left = cellCenter - new Vector2(half + gap + size.x * 0.5f, 0f);
            Vector2 up = cellCenter + new Vector2(0f, half + gap + size.y * 0.5f);
            Vector2 down = cellCenter - new Vector2(0f, half + gap + size.y * 0.5f);
            Vector2[] candidates = _wordIsAcross
                ? new[] { up, down, right, left }
                : new[] { right, left, up, down };

            Vector2 chosen = candidates[0];
            foreach (var c in candidates)
            {
                if (c.x - size.x * 0.5f >= areaRect.xMin && c.x + size.x * 0.5f <= areaRect.xMax &&
                    c.y - size.y * 0.5f >= areaRect.yMin && c.y + size.y * 0.5f <= areaRect.yMax)
                {
                    chosen = c;
                    break;
                }
            }

            chosen.x = Mathf.Clamp(chosen.x, areaRect.xMin + size.x * 0.5f, areaRect.xMax - size.x * 0.5f);
            chosen.y = Mathf.Clamp(chosen.y, areaRect.yMin + size.y * 0.5f, areaRect.yMax - size.y * 0.5f);
            Rect.anchoredPosition = chosen;
        }
    }
}
