using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrosswordGame.EditorTools
{
    /// <summary>
    /// External Dependency Manager запускает gradle отдельным процессом, а тот требует JAVA_HOME.
    /// На Unity 6 EDM4U не всегда находит JDK, поставляемый с редактором, и резолв падает с
    /// «JAVA_HOME is not set». Здесь переменная подставляется для процесса редактора
    /// (дочерние процессы её наследуют). Системные переменные окружения не меняются.
    /// </summary>
    [InitializeOnLoad]
    public static class AndroidJdkEnvironment
    {
        static AndroidJdkEnvironment()
        {
            Apply(false);
        }

        [MenuItem("Tools/Crossword/Android/Set JAVA_HOME For Editor", priority = 323)]
        private static void ApplyMenu() => Apply(true);

        public static bool Apply(bool log)
        {
            string current = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(current) && File.Exists(Path.Combine(current, "bin", "java.exe")))
            {
                if (log) Debug.Log($"Crossword: JAVA_HOME already set to {current}");
                return true;
            }

            string jdk = FindEditorJdk();
            if (jdk == null)
            {
                if (log) Debug.LogWarning("Crossword: JDK bundled with the editor not found, set JAVA_HOME manually.");
                return false;
            }

            Environment.SetEnvironmentVariable("JAVA_HOME", jdk);
            string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            string binFolder = Path.Combine(jdk, "bin");
            if (path.IndexOf(binFolder, StringComparison.OrdinalIgnoreCase) < 0)
                Environment.SetEnvironmentVariable("PATH", binFolder + Path.PathSeparator + path);

            if (log) Debug.Log($"Crossword: JAVA_HOME set to {jdk} for this editor session.");
            return true;
        }

        private static string FindEditorJdk()
        {
            string engine = BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None);
            if (!string.IsNullOrEmpty(engine))
            {
                string bundled = Path.Combine(engine, "OpenJDK");
                if (File.Exists(Path.Combine(bundled, "bin", "java.exe"))) return bundled;
            }

            // Путь, заданный пользователем в Preferences → External Tools.
            string preference = EditorPrefs.GetString("JdkPath", string.Empty);
            if (!string.IsNullOrEmpty(preference) && File.Exists(Path.Combine(preference, "bin", "java.exe")))
                return preference;

            return null;
        }
    }
}
