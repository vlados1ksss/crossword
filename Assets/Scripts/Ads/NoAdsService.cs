using System;
using UnityEngine;

namespace CrosswordGame
{
    /// <summary>Заглушка для сборок без рекламного плагина: реклама всегда недоступна, игра не ломается.</summary>
    public class NoAdsService : IAdsService
    {
        public bool IsAdShowing => false;

        public void Initialize()
        {
            Debug.Log("Ads: plugin is not installed, NoAdsService is used.");
        }

        public void ShowRewarded(string placementId, Action<RewardedAdResult> callback)
        {
            callback?.Invoke(RewardedAdResult.Unavailable);
        }

        public void ShowInterstitial()
        {
        }
    }
}
