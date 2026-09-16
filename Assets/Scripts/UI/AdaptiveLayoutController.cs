using System;
using UnityEngine;

namespace CrosswordGame
{
    /// <summary>
    /// Раскладка игрового экрана.
    /// Ландшафт (ПК): слева поле кроссворда, под ним строка ввода и клавиатура; справа список вопросов.
    /// Портрет (телефон): сверху поле, в середине вопросы (ScrollView), снизу строка ввода и клавиатура.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AdaptiveLayoutController : MonoBehaviour
    {
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform crosswordArea;
        [SerializeField] private RectTransform questionsArea;
        [SerializeField] private RectTransform bottomArea;
        [SerializeField] private float topBarHeight = 100f;
        [SerializeField] private float padding = 16f;
        [SerializeField, Range(0.4f, 0.75f)] private float landscapeSplit = 0.6f;

        public event Action LayoutApplied;
        public bool IsPortrait { get; private set; }

        private Vector2 _lastSize = new Vector2(-1, -1);

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        public void Apply()
        {
            var root = (RectTransform)transform;
            Vector2 size = root.rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            if ((size - _lastSize).sqrMagnitude < 0.01f) return;
            _lastSize = size;

            IsPortrait = size.y > size.x;

            topBar.anchorMin = new Vector2(0, 1);
            topBar.anchorMax = new Vector2(1, 1);
            topBar.pivot = new Vector2(0.5f, 1);
            topBar.offsetMin = new Vector2(0, -topBarHeight);
            topBar.offsetMax = Vector2.zero;

            float p = padding;
            float bodyHeight = size.y - topBarHeight;

            if (IsPortrait)
            {
                float bottomHeight = Mathf.Clamp(bodyHeight * 0.3f, 360f, 560f);
                float available = bodyHeight - bottomHeight - p * 4f;
                float crosswordHeight = available * 0.6f;

                Set(crosswordArea, new Vector2(0, 1), new Vector2(1, 1),
                    new Vector2(p, -(topBarHeight + p + crosswordHeight)), new Vector2(-p, -(topBarHeight + p)));
                Set(questionsArea, new Vector2(0, 0), new Vector2(1, 1),
                    new Vector2(p, bottomHeight + p * 2f), new Vector2(-p, -(topBarHeight + p * 2f + crosswordHeight)));
                Set(bottomArea, new Vector2(0, 0), new Vector2(1, 0),
                    new Vector2(p, p), new Vector2(-p, p + bottomHeight));
            }
            else
            {
                float bottomHeight = Mathf.Clamp(bodyHeight * 0.42f, 300f, 470f);

                Set(crosswordArea, new Vector2(0, 0), new Vector2(landscapeSplit, 1),
                    new Vector2(p, bottomHeight + p * 2f), new Vector2(-p * 0.5f, -(topBarHeight + p)));
                Set(bottomArea, new Vector2(0, 0), new Vector2(landscapeSplit, 0),
                    new Vector2(p, p), new Vector2(-p * 0.5f, p + bottomHeight));
                Set(questionsArea, new Vector2(landscapeSplit, 0), new Vector2(1, 1),
                    new Vector2(p * 0.5f, p), new Vector2(-p, -(topBarHeight + p)));
            }

            LayoutApplied?.Invoke();
        }

        private static void Set(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
