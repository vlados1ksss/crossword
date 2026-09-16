using System.Collections.Generic;

namespace CrosswordGame
{
    /// <summary>Модель клетки поля. Хранит состояние, не зависит от UI.</summary>
    public class CellData
    {
        public readonly GridPos Pos;
        public readonly char Solution;
        public int Number;                 // 0 — клетка не является началом слова
        public CrosswordQuestion Across;   // горизонтальное слово, проходящее через клетку
        public CrosswordQuestion Down;     // вертикальное слово, проходящее через клетку
        public bool RevealedByHint;

        public CellData(GridPos pos, char solution)
        {
            Pos = pos;
            Solution = solution;
        }

        /// <summary>Буква известна игроку: открыта подсказкой или слово через клетку уже разгадано.</summary>
        public bool IsKnown => RevealedByHint || (Across != null && Across.IsSolved) || (Down != null && Down.IsSolved);

        /// <summary>Текущая отображаемая на поле буква.</summary>
        public char DisplayLetter => IsKnown ? Solution : '\0';

        public bool BelongsToTwoWords => Across != null && Down != null;

        public IEnumerable<CrosswordQuestion> Words
        {
            get
            {
                if (Across != null) yield return Across;
                if (Down != null) yield return Down;
            }
        }
    }

    /// <summary>Слово кроссворда во время игры: номер, клетки, состояние решения.</summary>
    public class CrosswordQuestion
    {
        public readonly CrosswordWord Data;
        public readonly string Answer;
        public readonly List<CellData> Cells = new List<CellData>();
        public int Number;
        public bool IsSolved;

        public CrosswordQuestion(CrosswordWord data)
        {
            Data = data;
            Answer = CrosswordValidator.NormalizeAnswer(data.answer);
        }

        public string Id => Data.id;
        public bool IsAcross => Data.IsAcross;
        public string Text => Data.question;
        public int Length => Answer.Length;

        public bool AllLettersKnown
        {
            get
            {
                foreach (var cell in Cells)
                    if (!cell.IsKnown) return false;
                return true;
            }
        }

        public int IndexOf(CellData cell) => Cells.IndexOf(cell);
    }

    /// <summary>
    /// Логическое поле кроссворда: строится из CrosswordData, вычисляет клетки слов,
    /// пересечения, границы сетки и нумерацию.
    /// </summary>
    public class CrosswordBoard
    {
        public readonly CrosswordData Data;
        public readonly List<CrosswordQuestion> Questions = new List<CrosswordQuestion>();
        public readonly List<CellData> Cells = new List<CellData>();
        public int MinRow { get; private set; }
        public int MinCol { get; private set; }
        public int Rows { get; private set; }
        public int Cols { get; private set; }

        private readonly Dictionary<GridPos, CellData> _cellsByPos = new Dictionary<GridPos, CellData>();

        public CrosswordBoard(CrosswordData data)
        {
            Data = data;
            Build();
        }

        public CellData GetCell(GridPos pos) => _cellsByPos.TryGetValue(pos, out var cell) ? cell : null;

        public CrosswordQuestion GetQuestion(string id) => Questions.Find(q => q.Id == id);

        public bool IsCompleted => Questions.TrueForAll(q => q.IsSolved);

        public int SolvedCount
        {
            get
            {
                int count = 0;
                foreach (var q in Questions) if (q.IsSolved) count++;
                return count;
            }
        }

        private void Build()
        {
            int minRow = int.MaxValue, minCol = int.MaxValue, maxRow = int.MinValue, maxCol = int.MinValue;

            foreach (var wordData in Data.words)
            {
                var question = new CrosswordQuestion(wordData);
                Questions.Add(question);

                for (int i = 0; i < question.Length; i++)
                {
                    GridPos pos = wordData.GetPos(i);
                    if (!_cellsByPos.TryGetValue(pos, out var cell))
                    {
                        cell = new CellData(pos, question.Answer[i]);
                        _cellsByPos.Add(pos, cell);
                        Cells.Add(cell);
                    }

                    if (question.IsAcross) cell.Across = question;
                    else cell.Down = question;
                    question.Cells.Add(cell);

                    if (pos.row < minRow) minRow = pos.row;
                    if (pos.col < minCol) minCol = pos.col;
                    if (pos.row > maxRow) maxRow = pos.row;
                    if (pos.col > maxCol) maxCol = pos.col;
                }
            }

            MinRow = minRow;
            MinCol = minCol;
            Rows = maxRow - minRow + 1;
            Cols = maxCol - minCol + 1;

            AssignNumbers();
        }

        /// <summary>Классическая нумерация: по строкам сверху вниз, слева направо. Общее начало — общий номер.</summary>
        private void AssignNumbers()
        {
            Cells.Sort((a, b) => a.Pos.row != b.Pos.row ? a.Pos.row.CompareTo(b.Pos.row) : a.Pos.col.CompareTo(b.Pos.col));
            int number = 0;
            foreach (var cell in Cells)
            {
                bool startsWord = false;
                foreach (var q in cell.Words)
                    if (q.Cells[0] == cell) startsWord = true;
                if (!startsWord) continue;

                cell.Number = ++number;
                foreach (var q in cell.Words)
                    if (q.Cells[0] == cell) q.Number = number;
            }
            Questions.Sort((a, b) => a.Number.CompareTo(b.Number));
        }
    }
}
