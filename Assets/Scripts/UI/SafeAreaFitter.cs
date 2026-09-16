using UnityEngine;

namespace CrosswordGame
{
    /// <summary>
    /// Ограничивает RectTransform безопасной областью экрана (вырезы, скругления).
    /// Пересчёт выполняется только при изменении размеров, без Update.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private Rect _lastSafeArea;
        private Vector2Int _lastScreen;

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        public void Apply()
        {
            Rect safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (safe == _lastSafeArea && screen == _lastScreen) return;
            if (screen.x <= 0 || screen.y <= 0) return;

            _lastSafeArea = safe;
            _lastScreen = screen;

            var rect = (RectTransform)transform;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= screen.x;
            min.y /= screen.y;
            max.x /= screen.x;
            max.y /= screen.y;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
