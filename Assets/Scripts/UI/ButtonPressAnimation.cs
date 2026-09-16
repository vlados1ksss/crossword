using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>Лёгкое уменьшение кнопки при нажатии (мышь и touch).</summary>
    public class ButtonPressAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.93f;
        [SerializeField] private float duration = 0.08f;

        private Selectable _selectable;
        private Coroutine _routine;
        private bool _pressed;

        private void Awake() => _selectable = GetComponent<Selectable>();

        private void OnDisable()
        {
            _pressed = false;
            _routine = null;
            transform.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable != null && !_selectable.IsInteractable()) return;
            _pressed = true;
            AnimationController.Restart(this, ref _routine, ScaleTo(pressedScale));
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_pressed) Release();
        }

        private void Release()
        {
            _pressed = false;
            AnimationController.Restart(this, ref _routine, ScaleTo(1f));
        }

        private IEnumerator ScaleTo(float target)
        {
            float from = transform.localScale.x;
            yield return AnimationController.Tween(duration, t =>
            {
                float s = Mathf.Lerp(from, target, t);
                transform.localScale = new Vector3(s, s, 1f);
            });
        }
    }
}
