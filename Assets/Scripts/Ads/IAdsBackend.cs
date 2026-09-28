using System;

namespace CrosswordGame
{
    public enum RewardedAdResult
    {
        Rewarded,     // реклама досмотрена, награду можно выдать
        NotWatched,   // пользователь закрыл рекламу раньше времени
        Unavailable   // реклама не загрузилась / ошибка / уже показывается другая
    }

    /// <summary>
    /// Рекламная платформа. Единственная задача бэкенда — перевести вызовы на конкретный SDK;
    /// общие правила показа (когда можно, один показ за раз, таймауты) живут в AdsManager.
    /// </summary>
    public interface IAdsBackend
    {
        string Name { get; }

        /// <summary>Реклама показывается прямо сейчас (в том числе вызванная самим SDK).</summary>
        bool NowShowing { get; }

        /// <summary>Любая реклама открылась / закрылась — игра может поставить паузу.</summary>
        event Action AnyOpened;
        event Action AnyClosed;

        /// <summary>Причина, по которой показ невозможен, или null, если показывать можно.</summary>
        string InterstitialBlockReason();

        /// <summary>finished может прийти несколько раз — AdsManager отсекает повторы.</summary>
        void ShowInterstitial(Action opened, Action finished);

        string RewardedBlockReason();

        void ShowRewarded(string rewardId, Action rewarded, Action closed, Action failed);
    }
}
