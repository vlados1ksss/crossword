using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrosswordGame
{
    public class LevelInfo
    {
        public int Number;
        public CrosswordData Data;
        public CrosswordValidator.Result Validation;
        public bool IsValid => Data != null && Validation != null && Validation.IsValid;
        public int Difficulty => Data != null ? Data.difficulty : 0;
        public int WordCount => Data != null && Data.words != null ? Data.words.Count : 0;
    }

    /// <summary>
    /// Загружает все уровни из StreamingAssets/Crosswords/level_1.json, level_2.json, ...
    /// Файлы перебираются по порядку до первого отсутствующего, поэтому новый уровень
    /// добавляется простым созданием следующего JSON-файла, без изменения кода.
    /// </summary>
    public class LevelManager
    {
        private const int MaxLevels = 200;

        public readonly List<LevelInfo> Levels = new List<LevelInfo>();

        public int Count => Levels.Count;

        public LevelInfo GetLevel(int number) => Levels.Find(l => l.Number == number);

        public IEnumerator LoadAll(Action onComplete)
        {
            Levels.Clear();
            string alphabet = LocalizationManager.Alphabet;

            for (int number = 1; number <= MaxLevels; number++)
            {
                string text = null;
                bool finished = false;
                yield return CrosswordLoader.LoadText(CrosswordLoader.GetLevelPath(number),
                    t => { text = t; finished = true; },
                    _ => finished = true);

                if (!finished || text == null) break;

                var info = new LevelInfo { Number = number, Data = CrosswordLoader.Parse(text) };
                info.Validation = CrosswordValidator.Validate(info.Data, $"Level {number}", alphabet);
                NormalizeAnswers(info.Data);

                foreach (var warning in info.Validation.Warnings) Debug.LogWarning(warning);
                foreach (var error in info.Validation.Errors) Debug.LogError(error);

                Levels.Add(info);
            }

            if (Levels.Count == 0)
                Debug.LogError($"No crossword levels found in StreamingAssets/{CrosswordLoader.Folder}.");

            onComplete?.Invoke();
        }

        private static void NormalizeAnswers(CrosswordData data)
        {
            if (data?.words == null) return;
            foreach (var word in data.words)
                word.answer = CrosswordValidator.NormalizeAnswer(word.answer);
        }
    }
}
