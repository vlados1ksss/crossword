using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>
    /// Подстраивает CanvasScaler под ориентацию: в ландшафте масштаб по высоте (1920x1080),
    /// в портрете — по ширине (1080x1920). Так размеры элементов одинаково удобны на ПК, телефоне и планшете.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class AdaptiveCanvasScaler : MonoBehaviour
    {
        [SerializeField] private Vector2 landscapeReference = new Vector2(1920, 1080);
        [SerializeField] private Vector2 portraitReference = new Vector2(1080, 1920);

        private CanvasScaler _scaler;
        private int _lastState = -1;

        public static bool IsPortraitScreen => Screen.height > Screen.width;

        private void Awake() => _scaler = GetComponent<CanvasScaler>();

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (_scaler == null) _scaler = GetComponent<CanvasScaler>();
            int state = IsPortraitScreen ? 1 : 0;
            if (state == _lastState) return;
            _lastState = state;

            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            if (state == 1)
            {
                _scaler.referenceResolution = portraitReference;
                _scaler.matchWidthOrHeight = 0f;
            }
            else
            {
                _scaler.referenceResolution = landscapeReference;
                _scaler.matchWidthOrHeight = 1f;
            }
        }
    }
}
