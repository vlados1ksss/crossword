using System;
using UnityEngine;

namespace CrosswordGame
{
    /// <summary>Заглушка для платформ без рекламного SDK: реклама всегда недоступна, игра не ломается.</summary>
    public class NoAdsBackend : IAdsBackend
    {
        public string Name => "No ads";

        public bool NowShowing => false;

        public event Action AnyOpened;
        public event Action AnyClosed;

        public string InterstitialBlockReason() => "ads plugin is not installed";

        public void ShowInterstitial(Action opened, Action finished)
        {
            AnyOpened?.Invoke();
            AnyClosed?.Invoke();
            finished?.Invoke();
        }

        public string RewardedBlockReason() => "ads plugin is not installed";

        public void ShowRewarded(string rewardId, Action rewarded, Action closed, Action failed)
        {
            Debug.Log("[Ads] rewarded is not available: no ads backend");
            failed?.Invoke();
        }
    }
}
