using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CrosswordGame.EditorTools
{
    /// <summary>
    /// Проверка JSON-файлов кроссвордов в редакторе: вручную через меню и автоматически при изменении файлов.
    /// </summary>
    public class CrosswordLevelsValidator : AssetPostprocessor
    {
        public const string LevelsFolder = "Assets/StreamingAssets/Crosswords";

        [MenuItem("Tools/Crossword/Validate All Levels", priority = 1)]
        public static void ValidateAllMenu()
        {
            int errors = ValidateAll(out int files);
            if (errors == 0)
                Debug.Log($"Crossword: all {files} level files are valid.");
            else
                Debug.LogError($"Crossword: found {errors} error(s) in {files} level files. See messages above.");
        }

        public static int ValidateAll(out int files)
        {
            files = 0;
            int errors = 0;
            if (!Directory.Exists(LevelsFolder)) return 0;
            foreach (var path in Directory.GetFiles(LevelsFolder, "*.json"))
            {
                files++;
                errors += ValidateFile(path);
            }
            return errors;
        }

        public static int ValidateFile(string path)
        {
            string label = GetLabel(path);
            CrosswordData data = CrosswordLoader.Parse(File.ReadAllText(path));
            LocalizationManager.EnsureInitialized();
            var result = CrosswordValidator.Validate(data, label, LocalizationManager.Alphabet);
            foreach (var warning in result.Warnings) Debug.LogWarning(warning);
            foreach (var error in result.Errors) Debug.LogError(error);
            return result.Errors.Count;
        }

        public static string GetLabel(string path)
        {
            var match = Regex.Match(Path.GetFileNameWithoutExtension(path), @"level_(\d+)");
            return match.Success ? $"Level {match.Groups[1].Value}" : Path.GetFileName(path);
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
            {
                if (path.StartsWith(LevelsFolder) && path.EndsWith(".json"))
                    ValidateFile(path);
            }
        }
    }
}
