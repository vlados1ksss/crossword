using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CrosswordGame
{
    /// <summary>
    /// Сохранение прогресса в JSON-файл Application.persistentDataPath/progress.json.
    /// UI-компоненты не хранят состояние — они читают и изменяют его только через SaveManager.
    /// </summary>
    public class SaveManager
    {
        private const string FileName = "progress.json";

#if UNITY_WEBGL && !UNITY_EDITOR
        // Принудительная синхронизация IndexedDB после записи (см. Plugins/WebGL/CrosswordFileSync.jslib).
        [DllImport("__Internal")]
        private static extern void CrosswordSyncFiles();
#endif

        public SaveData Data { get; private set; }

        private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: failed to read save file, starting fresh. {e.Message}");
                Data = null;
            }

            if (Data == null) Data = new SaveData();
            if (Data.maxUnlockedLevel < 1) Data.maxUnlockedLevel = 1;
            if (Data.selectedLevel < 1) Data.selectedLevel = 1;
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(Data);
                string tempPath = FilePath + ".tmp";
                File.WriteAllText(tempPath, json);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tempPath, FilePath);
#if UNITY_WEBGL && !UNITY_EDITOR
                CrosswordSyncFiles();
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveManager: failed to write save file. {e.Message}");
            }
        }

        public LevelProgress GetLevel(int levelNumber)
        {
            var progress = Data.levels.Find(l => l.levelNumber == levelNumber);
            if (progress == null)
            {
                progress = new LevelProgress { levelNumber = levelNumber };
                Data.levels.Add(progress);
            }
            return progress;
        }

        public LevelProgress FindLevel(int levelNumber) => Data.levels.Find(l => l.levelNumber == levelNumber);

        public bool IsUnlocked(int levelNumber) => levelNumber <= Data.maxUnlockedLevel;

        public void UnlockLevel(int levelNumber)
        {
            if (levelNumber > Data.maxUnlockedLevel) Data.maxUnlockedLevel = levelNumber;
        }

        public void ResetLevel(int levelNumber)
        {
            Data.levels.RemoveAll(l => l.levelNumber == levelNumber);
            Save();
        }

        public void DeleteAll()
        {
            Data = new SaveData();
            Save();
        }
    }
}
