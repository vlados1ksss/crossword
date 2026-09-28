using System;
using System.Collections;
using UnityEngine;

namespace CrosswordGame
{
    /// <summary>
    /// Единая точка входа для рекламы и все общие правила показа:
    /// когда реклама допустима, один показ за раз, ровно один вызов колбэка, таймауты и пауза звука.
    /// Конкретный SDK скрыт за IAdsBackend, поэтому игровой код одинаков на Яндекс Играх и на Android.
    /// </summary>
    public class AdsManager
    {
        public const string HintPlacement = "hint";

        // Не показываем межстраничную сразу после запуска и слишком часто.
        private const float MinSecondsAfterLaunch = 60f;
        private const float MinSecondsBetweenInterstitials = 90f;

        // Если реклама не открылась за это время, считаем показ несостоявшимся.
        private const float InterstitialOpenTimeout = 4f;

        // Награда может прийти уже после закрытия рекламы.
        private const float RewardGraceSeconds = 0.3f;

        private readonly IAdsBackend _backend;
        private readonly MonoBehaviour _host;
        private float _lastInterstitialTime = float.NegativeInfinity;
        private bool _showing;

        public AdsManager(IAdsBackend backend, MonoBehaviour host)
        {
            _backend = backend;
            _host = host;
        }

        /// <summary>Открылась / закрылась любая реклама (включая вызванную самим SDK).</summary>
        public event Action AdOpened;
        public event Action AdClosed;

        public string BackendName => _backend.Name;

        public bool IsAdShowing => _showing || _backend.NowShowing;

        public void Initialize()
        {
            _backend.AnyOpened += OnAnyOpened;
            _backend.AnyClosed += OnAnyClosed;
            Debug.Log($"[Ads] backend: {_backend.Name}");
        }

        public void ShowRewarded(string placementId, Action<RewardedAdResult> callback)
        {
            if (_showing)
            {
                Debug.Log("[Ads] rewarded skipped: another ad is showing");
                callback?.Invoke(RewardedAdResult.Unavailable);
                return;
            }

            string reason = _backend.RewardedBlockReason();
            if (reason != null)
            {
                Debug.Log($"[Ads] rewarded skipped: {reason}");
                callback?.Invoke(RewardedAdResult.Unavailable);
                return;
            }

            _showing = true;
            MuteAudio(true);
            bool rewarded = false;
            bool finished = false;

            void Finish(RewardedAdResult result)
            {
                if (finished) return;
                finished = true;
                _showing = false;
                MuteAudio(false);
                // После ролика за награду межстраничную показываем не сразу.
                _lastInterstitialTime = Time.realtimeSinceStartup;
                Debug.Log($"[Ads] rewarded result: {result}");
                callback?.Invoke(result);
            }

            _backend.ShowRewarded(placementId,
                rewarded: () =>
                {
                    rewarded = true;
                    // Некоторые SDK сообщают о награде уже после закрытия рекламы.
                    if (!_backend.NowShowing) Finish(RewardedAdResult.Rewarded);
                },
                closed: () => _host.StartCoroutine(FinishAfterGrace()),
                failed: () => Finish(rewarded ? RewardedAdResult.Rewarded : RewardedAdResult.Unavailable));

            IEnumerator FinishAfterGrace()
            {
                yield return new WaitForSecondsRealtime(RewardGraceSeconds);
                Finish(rewarded ? RewardedAdResult.Rewarded : RewardedAdResult.NotWatched);
            }
        }

        /// <summary>Вызывается только в «безопасные» моменты: переход между уровнями, выход в меню после уровня.</summary>
        public void TryShowInterstitial()
        {
            if (_showing)
            {
                Debug.Log("[Ads] interstitial skipped: another ad is showing");
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (now < MinSecondsAfterLaunch)
            {
                Debug.Log($"[Ads] interstitial skipped: too early after launch ({MinSecondsAfterLaunch - now:0}s left)");
                return;
            }
            if (now - _lastInterstitialTime < MinSecondsBetweenInterstitials)
            {
                Debug.Log($"[Ads] interstitial skipped: interval {MinSecondsBetweenInterstitials - (now - _lastInterstitialTime):0}s");
                return;
            }

            string reason = _backend.InterstitialBlockReason();
            if (reason != null)
            {
                Debug.Log($"[Ads] interstitial skipped: {reason}");
                return;
            }

            _showing = true;
            MuteAudio(true);
            bool opened = false;
            bool finished = false;

            void End()
            {
                if (finished) return;
                finished = true;
                _showing = false;
                MuteAudio(false);
                _lastInterstitialTime = Time.realtimeSinceStartup;
                Debug.Log("[Ads] interstitial finished");
            }

            _backend.ShowInterstitial(() => opened = true, End);
            _host.StartCoroutine(OpenTimeout());

            IEnumerator OpenTimeout()
            {
                yield return new WaitForSecondsRealtime(InterstitialOpenTimeout);
                if (opened || finished) yield break;
                Debug.Log("[Ads] interstitial did not open in time");
                End();
            }
        }

        private void OnAnyOpened()
        {
            MuteAudio(true);
            AdOpened?.Invoke();
        }

        private void OnAnyClosed()
        {
            MuteAudio(false);
            AdClosed?.Invoke();
        }

        private static void MuteAudio(bool mute) => AudioListener.pause = mute;
    }
}
