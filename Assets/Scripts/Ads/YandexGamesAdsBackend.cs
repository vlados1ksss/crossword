#if !UNITY_ANDROID && PLUGIN_YG_2
using System;
using YG;

namespace CrosswordGame
{
    /// <summary>
    /// Реклама Яндекс Игр через PluginYourGames 2 (WebGL-версия игры).
    /// Порядок событий YG2 для rewarded: onOpenRewardedAdv -> onRewardAdv(id) -> onCloseRewardedAdv,
    /// при ошибке — onErrorRewardedAdv.
    /// </summary>
    public class YandexGamesAdsBackend : IAdsBackend, IDisposable
    {
        public string Name => "Yandex Games (PluginYG2)";

        public bool NowShowing => YG2.nowAdsShow;

        public event Action AnyOpened;
        public event Action AnyClosed;

        private Action _interstitialFinished;
        private Action _interstitialOpened;
        private Action _rewarded;
        private Action _rewardedClosed;
        private Action _rewardedFailed;
        private string _rewardId;

        public YandexGamesAdsBackend()
        {
            YG2.onOpenAnyAdv += OnAnyOpened;
            YG2.onCloseAnyAdv += OnAnyClosed;

            YG2.onOpenInterAdv += OnInterstitialOpened;
            YG2.onCloseInterAdv += OnInterstitialClosed;
            YG2.onErrorInterAdv += OnInterstitialClosed;

            YG2.onRewardAdv += OnReward;
            YG2.onCloseRewardedAdv += OnRewardedClosed;
            YG2.onErrorRewardedAdv += OnRewardedError;
        }

        public void Dispose()
        {
            YG2.onOpenAnyAdv -= OnAnyOpened;
            YG2.onCloseAnyAdv -= OnAnyClosed;

            YG2.onOpenInterAdv -= OnInterstitialOpened;
            YG2.onCloseInterAdv -= OnInterstitialClosed;
            YG2.onErrorInterAdv -= OnInterstitialClosed;

            YG2.onRewardAdv -= OnReward;
            YG2.onCloseRewardedAdv -= OnRewardedClosed;
            YG2.onErrorRewardedAdv -= OnRewardedError;
        }

        public string InterstitialBlockReason()
        {
            if (!YG2.isSDKEnabled) return "SDK is not ready";
            if (YG2.nowAdsShow) return "another ad is showing";
            if (!YG2.isTimerAdvCompleted) return $"platform interval {YG2.timerInterAdv:0}s";
            return null;
        }

        public void ShowInterstitial(Action opened, Action finished)
        {
            _interstitialOpened = opened;
            _interstitialFinished = finished;
            YG2.InterstitialAdvShow();
        }

        public string RewardedBlockReason()
        {
            if (!YG2.isSDKEnabled) return "SDK is not ready";
            if (YG2.nowAdsShow) return "another ad is showing";
            return null;
        }

        public void ShowRewarded(string rewardId, Action rewarded, Action closed, Action failed)
        {
            _rewardId = rewardId;
            _rewarded = rewarded;
            _rewardedClosed = closed;
            _rewardedFailed = failed;
            YG2.RewardedAdvShow(rewardId);
        }

        private void OnAnyOpened() => AnyOpened?.Invoke();

        private void OnAnyClosed() => AnyClosed?.Invoke();

        private void OnInterstitialOpened()
        {
            var opened = _interstitialOpened;
            _interstitialOpened = null;
            opened?.Invoke();
        }

        private void OnInterstitialClosed()
        {
            var finished = _interstitialFinished;
            _interstitialOpened = null;
            _interstitialFinished = null;
            finished?.Invoke();
        }

        private void OnReward(string id)
        {
            if (id != _rewardId) return;
            _rewarded?.Invoke();
        }

        private void OnRewardedClosed()
        {
            var closed = _rewardedClosed;
            ClearRewarded();
            closed?.Invoke();
        }

        private void OnRewardedError()
        {
            var failed = _rewardedFailed;
            ClearRewarded();
            failed?.Invoke();
        }

        private void ClearRewarded()
        {
            _rewarded = null;
            _rewardedClosed = null;
            _rewardedFailed = null;
            _rewardId = null;
        }
    }
}
#endif
