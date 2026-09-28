using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CrosswordGame.EditorTools
{
    /// <summary>
    /// Сборка версии для Яндекс Игр (WebGL, реклама через PluginYourGames 2).
    /// Меню: Tools → Crossword → WebGL. Версию игры сборщик не меняет.
    /// </summary>
    public static class WebGLBuilder
    {
        private const string OutputFolder = "Builds/WebGL";
        private const string PendingBuildKey = "Crossword.PendingWebGLBuild";
        private const string YandexTemplate = "PROJECT:YandexGames";

        [MenuItem("Tools/Crossword/WebGL/Build (release)", priority = 400)]
        private static void BuildMenu()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.Log("Crossword: switching active build target to WebGL, the build will continue automatically.");
                SessionState.SetBool(PendingBuildKey, true);
                EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.WebGL, BuildTarget.WebGL);
                return;
            }
            BuildNow();
        }

        [MenuItem("Tools/Crossword/WebGL/Apply Player Settings", priority = 401)]
        private static void ApplyPlayerSettingsMenu()
        {
            ApplyPlayerSettings();
            Debug.Log("Crossword: WebGL player settings applied.");
        }

        [MenuItem("Tools/Crossword/WebGL/Switch Platform To WebGL", priority = 402)]
        private static void SwitchPlatformMenu()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
            {
                Debug.Log("Crossword: WebGL is already the active build target.");
                return;
            }
            EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.WebGL, BuildTarget.WebGL);
        }

        [InitializeOnLoadMethod]
        private static void ContinuePendingBuild()
        {
            if (!SessionState.GetBool(PendingBuildKey, false)) return;
            SessionState.EraseBool(PendingBuildKey);
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL) return;
            EditorApplication.delayCall += BuildNow;
        }

        private static void BuildNow()
        {
            ApplyPlayerSettings();
            SplashScreenSetup.Apply(false);

            if (PlayerSettings.WebGL.template != YandexTemplate)
                Debug.LogWarning($"Crossword: WebGL template is '{PlayerSettings.WebGL.template}', " +
                                 $"expected '{YandexTemplate}' (PluginYG2 template for Yandex Games).");

            Directory.CreateDirectory(OutputFolder);
            string path = $"{OutputFolder}/crossword-{PlayerSettings.bundleVersion}";

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = path,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            Debug.Log($"Crossword: building WebGL -> {path}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"Crossword: WebGL build {summary.result}. Version: {PlayerSettings.bundleVersion}, " +
                      $"size: {summary.totalSize / (1024 * 1024)} MB, errors: {summary.totalErrors}, " +
                      $"warnings: {summary.totalWarnings}, output: {path}");
        }

        /// <summary>Настройки, которых ждут Яндекс Игры: без потоков, со сжатием и кэшированием данных.</summary>
        private static void ApplyPlayerSettings()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);

            // Хостинг Яндекс Игр отдаёт .br с нужными заголовками, поэтому Brotli без JS-фолбэка:
            // самый маленький размер загрузки. Эти же значения выставляет сборочный скрипт PluginYG2.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;                       // повторные запуски грузятся из кэша
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.threadsSupport = false;                    // потоки Яндекс Игры не поддерживают
            PlayerSettings.runInBackground = false;                         // игра должна замирать в неактивной вкладке

            if (Directory.Exists("Assets/WebGLTemplates/YandexGames"))
                PlayerSettings.WebGL.template = YandexTemplate;
        }
    }
}
