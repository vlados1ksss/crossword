#if UNITY_ANDROID
using System;
using System.Collections;
using UnityEngine;
using YandexMobileAds;
using YandexMobileAds.Base;

namespace CrosswordGame
{
    /// <summary>
    /// Реклама Yandex Mobile Ads (РСЯ) для Android-версии игры.
    /// Инициализация SDK не требуется. Объявление показывается один раз, после показа его нужно
    /// уничтожить и загрузить новое, поэтому оба формата держатся предзагруженными.
    /// В редакторе SDK подменён заглушкой, поэтому показ имитируется.
    /// </summary>
    public class YandexMobileAdsBackend : IAdsBackend, IDisposable
    {
        private const float MaxRetryDelay = 60f;
        private const float EditorShowSeconds = 1f;

        private readonly MonoBehaviour _host;
        private readonly MobileAdsConfig _config;

        private InterstitialAdLoader _interstitialLoader;
        private RewardedAdLoader _rewardedLoader;
        private Interstitial _interstitial;
        private RewardedAd _rewarded;
        private Coroutine _interstitialRetry;
        private Coroutine _rewardedRetry;
        private int _interstitialAttempt;
        private int _rewardedAttempt;
        private int _showing;
        private float _nextInterstitialTime;
        private bool _disposed;

        public YandexMobileAdsBackend(MonoBehaviour host, MobileAdsConfig config)
        {
            _host = host;
            _config = config;

            _interstitialLoader = new InterstitialAdLoader();
            _rewardedLoader = new RewardedAdLoader();

            // Первую межстраничную показываем не раньше, чем через интервал после запуска.
            _nextInterstitialTime = Time.realtimeSinceStartup + _config.InterstitialInterval;

            LoadInterstitial();
            LoadRewarded();
        }

        public string Name => $"Yandex Mobile Ads ({(_config.UsesDemoUnits ? "demo units" : "own units")})";

        public bool NowShowing => _showing > 0;

        public event Action AnyOpened;
        public event Action AnyClosed;

        public void Dispose()
        {
            _disposed = true;
            StopRetry(ref _interstitialRetry);
            StopRetry(ref _rewardedRetry);
            DestroyInterstitial();
            DestroyRewarded();
            _interstitialLoader = null;
            _rewardedLoader = null;
        }

        #region Interstitial

        public string InterstitialBlockReason()
        {
            if (NowShowing) return "another ad is showing";
            if (_interstitial == null) return "interstitial is not loaded yet";
            float left = _nextInterstitialTime - Time.realtimeSinceStartup;
            if (left > 0f) return $"interval {left:0}s";
            return null;
        }

        public void ShowInterstitial(Action opened, Action finished)
        {
            var ad = _interstitial;
            if (ad == null)
            {
                finished?.Invoke();
                return;
            }

            _interstitial = null;
            _showing++;
            AnyOpened?.Invoke();
            // Объявление уже загружено, поэтому показ считаем открытым сразу: пока идёт рекламная
            // Activity, Unity стоит на паузе и таймаут менеджера успел бы сработать.
            opened?.Invoke();

            bool ended = false;
            void End()
            {
                if (ended) return;
                ended = true;
                ad.Destroy();
                _showing = Mathf.Max(0, _showing - 1);
                RestartInterstitialInterval();
                AnyClosed?.Invoke();
                finished?.Invoke();
                LoadInterstitial();
            }

            ad.OnAdDismissed += (_, __) => End();
            ad.OnAdFailedToShow += (_, args) =>
            {
                Debug.LogWarning($"[Ads] interstitial failed to show: {args.Message}");
                End();
            };
            ad.Show();
#if UNITY_EDITOR
            _host.StartCoroutine(SimulateShow(() => End()));
#endif
        }

        private void LoadInterstitial()
        {
            if (_disposed || _interstitial != null || _interstitialLoader == null) return;

            _interstitialLoader.LoadAd(new AdRequest(_config.InterstitialAdUnitId),
                ad =>
                {
                    _interstitialAttempt = 0;
                    _interstitial = ad;
                    Debug.Log("[Ads] interstitial loaded");
                },
                error =>
                {
                    Debug.LogWarning($"[Ads] interstitial load failed: {error.Message}");
                    ScheduleRetry(ref _interstitialRetry, ref _interstitialAttempt, LoadInterstitial);
                });
        }

        private void RestartInterstitialInterval()
        {
            _nextInterstitialTime = Time.realtimeSinceStartup + _config.InterstitialInterval;
        }

        private void DestroyInterstitial()
        {
            _interstitial?.Destroy();
            _interstitial = null;
        }

        #endregion

        #region Rewarded

        public string RewardedBlockReason()
        {
            if (NowShowing) return "another ad is showing";
            if (_rewarded == null) return "rewarded is not loaded yet";
            return null;
        }

        public void ShowRewarded(string rewardId, Action rewarded, Action closed, Action failed)
        {
            var ad = _rewarded;
            if (ad == null)
            {
                failed?.Invoke();
                return;
            }

            _rewarded = null;
            _showing++;
            AnyOpened?.Invoke();

            bool ended = false;
            void End(bool withFailure)
            {
                if (ended) return;
                ended = true;
                ad.Destroy();
                _showing = Mathf.Max(0, _showing - 1);
                // После ролика за награду межстраничную тоже откладываем — как на Яндекс Играх.
                RestartInterstitialInterval();
                AnyClosed?.Invoke();
                if (withFailure) failed?.Invoke();
                else closed?.Invoke();
                LoadRewarded();
            }

            ad.OnRewarded += (_, reward) =>
            {
                Debug.Log($"[Ads] rewarded: {reward.amount} {reward.type}");
                rewarded?.Invoke();
            };
            ad.OnAdDismissed += (_, __) => End(false);
            ad.OnAdFailedToShow += (_, args) =>
            {
                Debug.LogWarning($"[Ads] rewarded failed to show: {args.Message}");
                End(true);
            };
            ad.Show();
#if UNITY_EDITOR
            _host.StartCoroutine(SimulateShow(() =>
            {
                rewarded?.Invoke();
                End(false);
            }));
#endif
        }

        private void LoadRewarded()
        {
            if (_disposed || _rewarded != null || _rewardedLoader == null) return;

            _rewardedLoader.LoadAd(new AdRequest(_config.RewardedAdUnitId),
                ad =>
                {
                    _rewardedAttempt = 0;
                    _rewarded = ad;
                    Debug.Log("[Ads] rewarded loaded");
                },
                error =>
                {
                    Debug.LogWarning($"[Ads] rewarded load failed: {error.Message}");
                    ScheduleRetry(ref _rewardedRetry, ref _rewardedAttempt, LoadRewarded);
                });
        }

        private void DestroyRewarded()
        {
            _rewarded?.Destroy();
            _rewarded = null;
        }

        #endregion

        /// <summary>Повторная загрузка с растущей задержкой: 5, 10, 20, 40, 60 секунд.</summary>
        private void ScheduleRetry(ref Coroutine handle, ref int attempt, Action load)
        {
            if (_disposed || _host == null) return;
            attempt++;
            float delay = Mathf.Min(MaxRetryDelay, 5f * Mathf.Pow(2f, attempt - 1));
            StopRetry(ref handle);
            handle = _host.StartCoroutine(RetryRoutine(delay, load));
        }

        private IEnumerator RetryRoutine(float delay, Action load)
        {
            yield return new WaitForSecondsRealtime(delay);
            load();
        }

        private void StopRetry(ref Coroutine handle)
        {
            if (handle != null && _host != null) _host.StopCoroutine(handle);
            handle = null;
        }

#if UNITY_EDITOR
        /// <summary>В редакторе рекламы нет: имитируем показ, чтобы игровая логика проверялась целиком.</summary>
        private IEnumerator SimulateShow(Action end)
        {
            Debug.Log("[Ads] editor simulation: ad is showing");
            yield return new WaitForSecondsRealtime(EditorShowSeconds);
            end();
        }
#endif
    }
}
#endif
