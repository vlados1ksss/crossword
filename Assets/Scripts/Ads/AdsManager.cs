using System;
using UnityEngine;

namespace CrosswordGame
{
    /// <summary>
    /// Единая точка входа для рекламы. Решает, КОГДА можно показывать рекламу,
    /// а конкретный показ делегирует IAdsService.
    /// </summary>
    public class AdsManager
    {
        public const string HintPlacement = "hint";

        // Не показываем межстраничную рекламу сразу после запуска и слишком часто.
        private const float MinSecondsAfterLaunch = 60f;
        private const float MinSecondsBetweenInterstitials = 90f;

        private readonly IAdsService _service;
        private float _lastInterstitialTime = float.NegativeInfinity;

        public AdsManager(IAdsService service)
        {
            _service = service;
        }

        public bool IsAdShowing => _service.IsAdShowing;

        public void Initialize() => _service.Initialize();

        public void ShowRewarded(string placementId, Action<RewardedAdResult> callback)
        {
            _service.ShowRewarded(placementId, result =>
            {
                // Успешный rewarded откладывает ближайший interstitial, чтобы не показывать рекламу подряд.
                if (result == RewardedAdResult.Rewarded)
                    _lastInterstitialTime = Time.realtimeSinceStartup;
                callback?.Invoke(result);
            });
        }

        /// <summary>Вызывается только в «безопасных» моментах: переход между уровнями, выход в меню после уровня.</summary>
        public void TryShowInterstitial()
        {
            float now = Time.realtimeSinceStartup;
            if (now < MinSecondsAfterLaunch) return;
            if (now - _lastInterstitialTime < MinSecondsBetweenInterstitials) return;
            if (_service.IsAdShowing) return;

            _lastInterstitialTime = now;
            _service.ShowInterstitial();
        }

        public static IAdsService CreateDefaultService()
        {
#if PLUGIN_YG_2
            return new YG2AdsService();
#else
            return new NoAdsService();
#endif
        }
    }
}
