using System;
using System.Runtime.InteropServices;
using UnityEngine;
#if !UNITY_ANDROID && PLUGIN_YG_2
using YG;
#endif

namespace CrosswordGame
{
    /// <summary>
    /// Единственное место, где игра знает о платформе.
    /// WebGL (Яндекс Игры) — PluginYourGames 2: Game Ready, Gameplay API, язык из SDK, реклама YG2.
    /// Android — приложение с рекламой Yandex Mobile Ads (РСЯ): разметки геймплея нет, язык берётся из системы.
    /// </summary>
    public static class PlatformBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string CrosswordGetSdkLanguage();
#endif

        private static bool _gameReadySent;
        private static bool _gameplayActive;

        /// <summary>Создаёт рекламный бэкенд текущей платформы.</summary>
        public static IAdsBackend CreateAdsBackend(MonoBehaviour host)
        {
#if UNITY_ANDROID
            return new YandexMobileAdsBackend(host, MobileAdsConfig.Load());
#elif PLUGIN_YG_2
            return new YandexGamesAdsBackend();
#else
            return new NoAdsBackend();
#endif
        }

        public static bool IsSdkReady
        {
            get
            {
#if !UNITY_ANDROID && PLUGIN_YG_2
                return YG2.isSDKEnabled;
#else
                return true;
#endif
            }
        }

        /// <summary>Вызывает action, когда SDK платформы инициализирован (сразу, если уже готов).</summary>
        public static void WhenSdkReady(Action action)
        {
#if !UNITY_ANDROID && PLUGIN_YG_2
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

        /// <summary>
        /// Язык, определённый платформой: на Яндекс Играх — environment.i18n.lang из SDK,
        /// на Android — язык системы, в редакторе — язык симуляции YG2.
        /// Пустая строка или null означают «язык неизвестен».
        /// </summary>
        public static string GetSdkLanguage()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return SystemLanguageCode();
#elif UNITY_WEBGL && !UNITY_EDITOR
            return CrosswordGetSdkLanguage();
#elif !UNITY_ANDROID && PLUGIN_YG_2 && UNITY_EDITOR
            return YG2.infoYG.Simulation.language;
#else
            return SystemLanguageCode();
#endif
        }

        private static string SystemLanguageCode()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Russian: return "ru";
                case SystemLanguage.Belarusian: return "be";
                case SystemLanguage.Ukrainian: return "uk";
                case SystemLanguage.English: return "en";
                case SystemLanguage.Turkish: return "tr";
                case SystemLanguage.German: return "de";
                case SystemLanguage.French: return "fr";
                case SystemLanguage.Spanish: return "es";
                case SystemLanguage.Portuguese: return "pt";
                case SystemLanguage.Italian: return "it";
                default: return null; // язык по умолчанию
            }
        }

        /// <summary>Сообщает платформе, что игра загружена и готова к взаимодействию (Game Ready API).</summary>
        public static void GameReady()
        {
            if (_gameReadySent) return;
            _gameReadySent = true;
#if !UNITY_ANDROID && PLUGIN_YG_2
            WhenSdkReady(YG2.GameReadyAPI);
#endif
        }

        /// <summary>Начало игрового процесса. На Android разметки геймплея нет — вызов ничего не делает.</summary>
        public static void GameplayStart()
        {
            if (_gameplayActive) return;
            _gameplayActive = true;
#if !UNITY_ANDROID && PLUGIN_YG_2
            WhenSdkReady(() => YG2.GameplayStart());
#endif
        }

        public static void GameplayStop()
        {
            if (!_gameplayActive) return;
            _gameplayActive = false;
#if !UNITY_ANDROID && PLUGIN_YG_2
            WhenSdkReady(() => YG2.GameplayStop());
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _gameReadySent = false;
            _gameplayActive = false;
        }
    }
}
