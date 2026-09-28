using UnityEngine;

namespace CrosswordGame
{
    /// <summary>
    /// Идентификаторы рекламных блоков РСЯ (Android-версия). Ассет: Assets/Resources/MobileAdsConfig.asset.
    /// Пока включён useDemoAdUnits, используются демо-блоки Яндекса: реклама показывается, но не монетизируется.
    /// </summary>
    [CreateAssetMenu(fileName = "MobileAdsConfig", menuName = "Crossword/Mobile Ads Config")]
    public class MobileAdsConfig : ScriptableObject
    {
        public const string ResourcesPath = "MobileAdsConfig";
        public const string DemoInterstitialAdUnitId = "demo-interstitial-yandex";
        public const string DemoRewardedAdUnitId = "demo-rewarded-yandex";

        [Tooltip("Использовать демо-блоки Яндекса вместо своих. Снимите галку перед релизом.")]
        [SerializeField] private bool useDemoAdUnits = true;

        [Tooltip("ID межстраничного блока из кабинета РСЯ, формат R-M-XXXXXXX-N")]
        [SerializeField] private string interstitialAdUnitId = string.Empty;

        [Tooltip("ID блока с вознаграждением из кабинета РСЯ, формат R-M-XXXXXXX-N")]
        [SerializeField] private string rewardedAdUnitId = string.Empty;

        [Tooltip("Минимальный интервал между межстраничными показами, секунды")]
        [SerializeField] private int interstitialInterval = 60;

        public string InterstitialAdUnitId => Resolve(interstitialAdUnitId, DemoInterstitialAdUnitId);

        public string RewardedAdUnitId => Resolve(rewardedAdUnitId, DemoRewardedAdUnitId);

        public int InterstitialInterval => Mathf.Max(0, interstitialInterval);

        /// <summary>true, если хотя бы один блок остался демонстрационным (релиз с такими блоками не собирается).</summary>
        public bool UsesDemoUnits =>
            InterstitialAdUnitId == DemoInterstitialAdUnitId || RewardedAdUnitId == DemoRewardedAdUnitId;

        private string Resolve(string id, string demoId) =>
            useDemoAdUnits || string.IsNullOrWhiteSpace(id) ? demoId : id.Trim();

        public static MobileAdsConfig Load()
        {
            var config = Resources.Load<MobileAdsConfig>(ResourcesPath);
            if (config != null) return config;

            Debug.LogWarning($"[Ads] Resources/{ResourcesPath}.asset not found, demo ad units are used.");
            return CreateInstance<MobileAdsConfig>();
        }
    }
}
