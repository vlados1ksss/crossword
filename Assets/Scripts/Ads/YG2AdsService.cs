#if PLUGIN_YG_2
using System;
using YG;

namespace CrosswordGame
{
    /// <summary>
    /// Реализация рекламы через PluginYourGames 2 (YG2).
    /// Используются модули RewardedAdv и InterstitialAdv. Весь код, зависящий от YG2, находится только здесь.
    /// Порядок событий YG2 для rewarded: onOpenRewardedAdv -> onRewardAdv(id) -> onCloseRewardedAdv,
    /// при ошибке — onErrorRewardedAdv.
    /// </summary>
    public class YG2AdsService : IAdsService
    {
        private Action<RewardedAdResult> _pendingCallback;
        private string _pendingId;
        private bool _rewardReceived;

        public bool IsAdShowing => YG2.nowAdsShow;

        public void Initialize()
        {
#if RewardedAdv_yg
            YG2.onRewardAdv += OnReward;
            YG2.onCloseRewardedAdv += OnRewardedClosed;
            YG2.onErrorRewardedAdv += OnRewardedError;
#endif
        }

        public void ShowRewarded(string placementId, Action<RewardedAdResult> callback)
        {
#if RewardedAdv_yg
            if (_pendingCallback != null || YG2.nowAdsShow)
            {
                callback?.Invoke(RewardedAdResult.Unavailable);
                return;
            }

            _pendingCallback = callback;
            _pendingId = placementId;
            _rewardReceived = false;
            YG2.RewardedAdvShow(placementId);
#else
            callback?.Invoke(RewardedAdResult.Unavailable);
#endif
        }

        public void ShowInterstitial()
        {
#if InterstitialAdv_yg
            if (!YG2.nowAdsShow)
                YG2.InterstitialAdvShow();
#endif
        }

        private void OnReward(string id)
        {
            if (_pendingCallback != null && id == _pendingId)
                _rewardReceived = true;
        }

        private void OnRewardedClosed()
        {
            Complete(_rewardReceived ? RewardedAdResult.Rewarded : RewardedAdResult.NotWatched);
        }

        private void OnRewardedError()
        {
            Complete(_rewardReceived ? RewardedAdResult.Rewarded : RewardedAdResult.Unavailable);
        }

        private void Complete(RewardedAdResult result)
        {
            var callback = _pendingCallback;
            _pendingCallback = null;
            _pendingId = null;
            _rewardReceived = false;
            callback?.Invoke(result);
        }
    }
}
#endif
