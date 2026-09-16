using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CrosswordGame
{
    /// <summary>
    /// Набор коротких плавных анимаций на корутинах (без tween-библиотек).
    /// Используется unscaled time, чтобы анимации не зависели от паузы во время рекламы.
    /// </summary>
    public static class AnimationController
    {
        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static IEnumerator Tween(float duration, Action<float> step)
        {
            float time = 0f;
            while (time < duration)
            {
                step(time / duration);
                yield return null;
                time += Time.unscaledDeltaTime;
            }
            step(1f);
        }

        /// <summary>Мягкое «появление» — масштаб от from до 1 с небольшим перелётом.</summary>
        public static IEnumerator PopIn(Transform target, float duration = 0.25f, float from = 0.4f)
        {
            yield return Tween(duration, t =>
            {
                float s = Mathf.LerpUnclamped(from, 1f, EaseOutBack(t));
                target.localScale = new Vector3(s, s, 1f);
            });
        }

        /// <summary>Короткий «пульс» масштаба: 1 → peak → 1.</summary>
        public static IEnumerator Pulse(Transform target, float duration = 0.22f, float peak = 1.12f)
        {
            yield return Tween(duration, t =>
            {
                float s = 1f + (peak - 1f) * Mathf.Sin(t * Mathf.PI);
                target.localScale = new Vector3(s, s, 1f);
            });
        }

        public static IEnumerator ColorTo(Graphic graphic, Color to, float duration = 0.2f)
        {
            Color from = graphic.color;
            yield return Tween(duration, t => graphic.color = Color.Lerp(from, to, EaseOutCubic(t)));
        }

        public static IEnumerator Fade(CanvasGroup group, float to, float duration = 0.2f)
        {
            float from = group.alpha;
            yield return Tween(duration, t => group.alpha = Mathf.Lerp(from, to, EaseOutCubic(t)));
        }

        /// <summary>Затухающее горизонтальное покачивание.</summary>
        public static IEnumerator Shake(RectTransform target, float duration = 0.35f, float strength = 14f)
        {
            Vector2 origin = target.anchoredPosition;
            yield return Tween(duration, t =>
            {
                float offset = Mathf.Sin(t * Mathf.PI * 6f) * strength * (1f - t);
                target.anchoredPosition = origin + new Vector2(offset, 0f);
            });
            target.anchoredPosition = origin;
        }

        public static IEnumerator SlideY(RectTransform target, float fromOffset, float duration = 0.25f)
        {
            Vector2 end = target.anchoredPosition;
            Vector2 start = end + new Vector2(0f, fromOffset);
            yield return Tween(duration, t => target.anchoredPosition = Vector2.LerpUnclamped(start, end, EaseOutCubic(t)));
        }

        /// <summary>Запускает корутину, предварительно остановив предыдущую, сохранённую в handle.</summary>
        public static void Restart(MonoBehaviour host, ref Coroutine handle, IEnumerator routine)
        {
            if (handle != null) host.StopCoroutine(handle);
            handle = host.isActiveAndEnabled ? host.StartCoroutine(routine) : null;
        }
    }
}
