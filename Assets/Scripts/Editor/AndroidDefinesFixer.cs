using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CrosswordGame.EditorTools
{
    /// <summary>
    /// PluginYourGames 2 добавляет символ YandexGamesPlatform_yg во все платформы, а под ним
    /// компилируется JS-мост (DllImport("__Internal") в .jslib), которого на Android нет —
    /// приложение падает при старте с EntryPointNotFoundException. Без символа ядро YG2
    /// компилируется безвредной заглушкой.
    ///
    /// YG2 проверяет наличие своего символа по подстроке, поэтому вместо простого удаления
    /// в Android-дефайны добавляется маркер ANDROID_APP_WITHOUT_YandexGamesPlatform_yg:
    /// подстрока найдена, символ повторно не добавляется, цикла перекомпиляций нет.
    /// WebGL и остальные платформы не трогаем.
    /// </summary>
    [InitializeOnLoad]
    public class AndroidDefinesFixer : IPreprocessBuildWithReport
    {
        public const string PlatformDefine = "YandexGamesPlatform_yg";
        public const string MarkerDefine = "ANDROID_APP_WITHOUT_YandexGamesPlatform_yg";

        static AndroidDefinesFixer()
        {
            EditorApplication.delayCall += () => EnsureAndroidDefines(false);
        }

        [MenuItem("Tools/Crossword/Fix Scripting Defines", priority = 200)]
        public static void FixMenu()
        {
            if (EnsureAndroidDefines(true))
                Debug.Log("Crossword: Android scripting defines fixed.");
            else
                Debug.Log($"Crossword: Android defines are already correct ({CurrentAndroidDefines()}).");
        }

        /// <summary>Возвращает true, если дефайны пришлось исправить.</summary>
        public static bool EnsureAndroidDefines(bool log)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android)
                .Split(';')
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d.Trim())
                .ToList();

            bool changed = false;
            if (defines.Remove(PlatformDefine)) changed = true;
            if (!defines.Contains(MarkerDefine))
            {
                defines.Add(MarkerDefine);
                changed = true;
            }
            if (!changed) return false;

            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, string.Join(";", defines));
            if (log) Debug.Log($"Crossword: Android defines set to {string.Join(";", defines)}");
            return true;
        }

        public static bool AndroidDefinesAreBroken() => CurrentAndroidDefines()
            .Split(';')
            .Any(d => d.Trim() == PlatformDefine);

        private static string CurrentAndroidDefines() => PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);

        public int callbackOrder => -2000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            if (!AndroidDefinesAreBroken()) return;

            EnsureAndroidDefines(true);
            throw new BuildFailedException(
                $"Android defines contained {PlatformDefine} (PluginYG2 JS bridge). " +
                "Defines are fixed now, scripts will be recompiled — start the build again.");
        }
    }
}
