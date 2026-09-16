using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrosswordGame
{
    /// <summary>
    /// Корневой объект игры (живёт между сценами). Создаёт сервисы и управляет переходами между сценами.
    /// Игровая логика кроссворда находится в CrosswordManager.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Game";

        private static GameManager _instance;

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                }
                return _instance;
            }
        }

        public SaveManager Save { get; private set; }
        public LevelManager Levels { get; private set; }
        public AdsManager Ads { get; private set; }
        public bool IsReady { get; private set; }

        private event Action ReadyCallbacks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;

            // До ответа SDK интерфейс работает на языке по умолчанию.
            LocalizationManager.EnsureInitialized();

            Save = new SaveManager();
            Save.Load();

            Ads = new AdsManager(AdsManager.CreateDefaultService());
            Ads.Initialize();

            Levels = new LevelManager();
            StartCoroutine(InitializeRoutine());
        }

        private const float SdkWaitTimeout = 5f;

        /// <summary>Ждёт инициализацию SDK, определяет язык через SDK и загружает уровни.</summary>
        private IEnumerator InitializeRoutine()
        {
            bool sdkReady = false;
            PlatformBridge.WhenSdkReady(() => sdkReady = true);

            float waited = 0f;
            while (!sdkReady && waited < SdkWaitTimeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (sdkReady)
            {
                LocalizationManager.Initialize(PlatformBridge.GetSdkLanguage());
            }
            else
            {
                // SDK запоздал: запускаемся на языке по умолчанию и применяем язык SDK, когда он станет доступен.
                Debug.LogWarning("[Loc] SDK is not initialized in time, default language is used until it is ready.");
                PlatformBridge.WhenSdkReady(() => LocalizationManager.Initialize(PlatformBridge.GetSdkLanguage()));
            }

            yield return Levels.LoadAll(OnLevelsLoaded);
        }

        private void OnLevelsLoaded()
        {
            IsReady = true;
            var callbacks = ReadyCallbacks;
            ReadyCallbacks = null;
            callbacks?.Invoke();
        }

        /// <summary>Выполняет действие после загрузки уровней (сразу, если уже загружены).</summary>
        public void WhenReady(Action action)
        {
            if (IsReady) action?.Invoke();
            else ReadyCallbacks += action;
        }

        public int SelectedLevel => Mathf.Clamp(Save.Data.selectedLevel, 1, Mathf.Max(1, Levels.Count));

        /// <summary>Уровень для кнопки «Продолжить»: последний выбранный, если он не пройден, иначе первый открытый непройденный.</summary>
        public int GetContinueLevel()
        {
            int selected = SelectedLevel;
            var selectedProgress = Save.FindLevel(selected);
            if (Save.IsUnlocked(selected) && (selectedProgress == null || !selectedProgress.completed))
                return selected;

            foreach (var level in Levels.Levels)
            {
                if (!Save.IsUnlocked(level.Number)) break;
                var progress = Save.FindLevel(level.Number);
                if (progress == null || !progress.completed) return level.Number;
            }
            return selected;
        }

        public bool HasAnyProgress()
        {
            foreach (var level in Save.Data.levels)
                if (level.HasProgress) return true;
            return false;
        }

        public void OpenLevel(int levelNumber)
        {
            if (!Save.IsUnlocked(levelNumber)) return;
            Save.Data.selectedLevel = levelNumber;
            Save.Save();
            SceneManager.LoadScene(GameScene);
        }

        public void OpenMainMenu()
        {
            PlatformBridge.GameplayStop();
            SceneManager.LoadScene(MainMenuScene);
        }
    }
}
