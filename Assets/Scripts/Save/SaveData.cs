using System;
using System.Collections.Generic;

namespace CrosswordGame
{
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int selectedLevel = 1;          // номер последнего выбранного уровня
        public int maxUnlockedLevel = 1;       // уровни 1..maxUnlockedLevel открыты
        public List<LevelProgress> levels = new List<LevelProgress>();
    }

    [Serializable]
    public class LevelProgress
    {
        public int levelNumber;
        public bool completed;
        public List<string> solvedWords = new List<string>();
        public List<GridPos> hintCells = new List<GridPos>();   // клетки, открытые подсказкой
        public int hintsUsed;
        public float playTimeSeconds;

        public bool HasProgress => completed || solvedWords.Count > 0 || hintCells.Count > 0;
    }
}
