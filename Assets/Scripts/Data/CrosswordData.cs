using System;
using System.Collections.Generic;

namespace CrosswordGame
{
    /// <summary>Описание одного кроссворда, загружаемое из JSON (StreamingAssets/Crosswords/level_N.json).</summary>
    [Serializable]
    public class CrosswordData
    {
        public int id;
        public int difficulty;
        public List<CrosswordWord> words = new List<CrosswordWord>();
    }

    /// <summary>Слово кроссворда в формате JSON. row/col — первая клетка слова.</summary>
    [Serializable]
    public class CrosswordWord
    {
        public const string Across = "across";
        public const string Down = "down";

        public string id;
        public string answer;
        public string question;
        public int row;
        public int col;
        public string direction;

        public bool IsAcross => direction == Across;
        public int Length => string.IsNullOrEmpty(answer) ? 0 : answer.Length;

        /// <summary>Координаты i-й буквы слова.</summary>
        public GridPos GetPos(int index)
        {
            return IsAcross ? new GridPos(row, col + index) : new GridPos(row + index, col);
        }
    }

    [Serializable]
    public struct GridPos : IEquatable<GridPos>
    {
        public int row;
        public int col;

        public GridPos(int row, int col)
        {
            this.row = row;
            this.col = col;
        }

        public bool Equals(GridPos other) => row == other.row && col == other.col;
        public override bool Equals(object obj) => obj is GridPos other && Equals(other);
        public override int GetHashCode() => row * 1000 + col;
        public override string ToString() => $"row {row}, col {col}";
    }
}
