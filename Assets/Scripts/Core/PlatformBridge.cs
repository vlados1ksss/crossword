using System;
#if PLUGIN_YG_2
using YG;
#endif

namespace CrosswordGame
{
    /// <summary>
    /// Изолированный доступ к API платформы (Яндекс Игры через YG2): Game Ready, Gameplay, язык.
    /// Без плагина методы безопасно ничего не делают.
    /// </summary>
    public static class PlatformBridge
    {
        private static bool _gameReadySent;
        private static bool _gameplayActive;

        public static bool IsSdkReady
        {
            get
            {
#if PLUGIN_YG_2
                return YG2.isSDKEnabled;
#else
                return true;
#endif
            }
        }

        /// <summary>Вызывает action, когда SDK платформы инициализирован (сразу, если уже готов).</summary>
        public static void WhenSdkReady(Action action)
        {
#if PLUGIN_YG_2
            if (YG2.isSDKEnabled)
            {
                action?.Invoke();
                return;
            }

            void Handler()
            {
                YG2.onGetSDKData -= Handler;
                action?.Invoke();
            }
            YG2.onGetSDKData += Handler;
#else
            action?.Invoke();
#endif
        }

        /// <summary>Язык, предлагаемый платформой. Если модуль локализации YG2 не установлен — русский.</summary>
        public static string GetPlatformLanguage()
        {
#if PLUGIN_YG_2 && Localization_yg
            return YG2.lang;
#else
            return LocalizationManager.DefaultLanguage;
#endif
        }

        /// <summary>Сообщает платформе, что игра загружена и готова к взаимодействию (Game Ready API).</summary>
        public static void GameReady()
        {
            if (_gameReadySent) return;
            _gameReadySent = true;
#if PLUGIN_YG_2
            WhenSdkReady(YG2.GameReadyAPI);
#endif
        }

        public static void GameplayStart()
        {
            if (_gameplayActive) return;
            _gameplayActive = true;
#if PLUGIN_YG_2
            WhenSdkReady(() => YG2.GameplayStart());
#endif
        }

        public static void GameplayStop()
        {
            if (!_gameplayActive) return;
            _gameplayActive = false;
#if PLUGIN_YG_2
            WhenSdkReady(() => YG2.GameplayStop());
#endif
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _gameReadySent = false;
            _gameplayActive = false;
        }
    }
}
