using System;

namespace CrosswordGame
{
    public enum RewardedAdResult
    {
        Rewarded,     // реклама досмотрена, награду можно выдать
        NotWatched,   // пользователь закрыл рекламу раньше времени
        Unavailable   // реклама не загрузилась / ошибка / уже показывается другая
    }

    /// <summary>Абстракция рекламной платформы. Игровая логика не знает о конкретном SDK.</summary>
    public interface IAdsService
    {
        void Initialize();
        void ShowRewarded(string placementId, Action<RewardedAdResult> callback);
        void ShowInterstitial();
        bool IsAdShowing { get; }
    }
}
