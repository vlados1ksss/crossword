using System.Collections.Generic;
using System.Text;

namespace CrosswordGame
{
    /// <summary>
    /// Проверка корректности данных кроссворда до построения поля.
    /// Не зависит от Unity-сцены, поэтому используется и в игре, и в редакторе кроссвордов.
    /// </summary>
    public static class CrosswordValidator
    {
        public const int ExpectedWordCount = 12;

        public class Result
        {
            public readonly List<string> Errors = new List<string>();
            public readonly List<string> Warnings = new List<string>();
            public bool IsValid => Errors.Count == 0;
        }

        /// <summary>Приводит ответ к виду, в котором он хранится в игре: верхний регистр, Ё → Е, без пробелов по краям.</summary>
        public static string NormalizeAnswer(string answer)
        {
            if (string.IsNullOrEmpty(answer)) return string.Empty;
            return answer.Trim().ToUpperInvariant().Replace('Ё', 'Е');
        }

        public static Result Validate(CrosswordData data, string label, string allowedLetters)
        {
            var result = new Result();
            if (data == null)
            {
                result.Errors.Add($"{label}:\nJSON is empty or has invalid format.");
                return result;
            }
            if (data.words == null || data.words.Count == 0)
            {
                result.Errors.Add($"{label}:\nLevel has no words.");
                return result;
            }
            if (data.words.Count != ExpectedWordCount)
            {
                result.Warnings.Add($"{label}:\nLevel contains {data.words.Count} words, expected {ExpectedWordCount}.");
            }

            var ids = new HashSet<string>();
            // Клетка -> (буква, id слова, направление)
            var letters = new Dictionary<GridPos, char>();
            var owners = new Dictionary<GridPos, List<CrosswordWord>>();
            var validWords = new List<CrosswordWord>();

            foreach (var word in data.words)
            {
                string wordLabel = $"{label}, word {(string.IsNullOrEmpty(word.id) ? "<no id>" : word.id)}";
                int errorsBefore = result.Errors.Count;

                if (string.IsNullOrWhiteSpace(word.id))
                    result.Errors.Add($"{wordLabel}:\nWord id is empty.");
                else if (!ids.Add(word.id))
                    result.Errors.Add($"{wordLabel}:\nDuplicate word id.");

                string answer = NormalizeAnswer(word.answer);
                if (answer.Length == 0)
                    result.Errors.Add($"{wordLabel}:\nAnswer is empty.");
                else if (answer.Length < 2)
                    result.Errors.Add($"{wordLabel}:\nAnswer must contain at least 2 letters.");

                for (int i = 0; i < answer.Length; i++)
                {
                    if (allowedLetters.IndexOf(answer[i]) < 0)
                    {
                        result.Errors.Add($"{wordLabel}:\nAnswer \"{word.answer}\" contains forbidden character '{answer[i]}' at position {i + 1}.");
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(word.question))
                    result.Errors.Add($"{wordLabel}:\nQuestion is empty.");

                if (word.row < 0 || word.col < 0)
                    result.Errors.Add($"{wordLabel}:\nInvalid coordinates (row {word.row}, col {word.col}). Coordinates must be >= 0.");

                if (word.direction != CrosswordWord.Across && word.direction != CrosswordWord.Down)
                    result.Errors.Add($"{wordLabel}:\nInvalid direction \"{word.direction}\". Use \"across\" or \"down\".");

                if (result.Errors.Count == errorsBefore)
                    validWords.Add(word);
            }

            // Проверка пересечений выполняется только для слов без базовых ошибок.
            foreach (var word in validWords)
            {
                string wordLabel = $"{label}, word {word.id}";
                string answer = NormalizeAnswer(word.answer);
                for (int i = 0; i < answer.Length; i++)
                {
                    GridPos pos = word.GetPos(i);
                    if (letters.TryGetValue(pos, out char existing))
                    {
                        if (existing != answer[i])
                        {
                            result.Errors.Add($"{wordLabel}:\nConflict at row {pos.row}, col {pos.col}.\nExpected: {existing}\nFound: {answer[i]}");
                        }
                        foreach (var other in owners[pos])
                        {
                            if (other.direction == word.direction)
                                result.Errors.Add($"{wordLabel}:\nOverlaps word {other.id} in the same direction at row {pos.row}, col {pos.col}.");
                        }
                        owners[pos].Add(word);
                    }
                    else
                    {
                        letters[pos] = answer[i];
                        owners[pos] = new List<CrosswordWord> { word };
                    }
                }
            }

            // Клетки непосредственно перед началом и после конца слова должны быть пустыми,
            // иначе на поле образуется слово длиннее заданного.
            foreach (var word in validWords)
            {
                string wordLabel = $"{label}, word {word.id}";
                GridPos before = word.IsAcross ? new GridPos(word.row, word.col - 1) : new GridPos(word.row - 1, word.col);
                GridPos after = word.GetPos(word.Length);
                if (letters.ContainsKey(before))
                    result.Errors.Add($"{wordLabel}:\nCell before the word (row {before.row}, col {before.col}) is occupied by another word.");
                if (letters.ContainsKey(after))
                    result.Errors.Add($"{wordLabel}:\nCell after the word (row {after.row}, col {after.col}) is occupied by another word.");
            }

            if (result.IsValid && !IsConnected(validWords))
                result.Warnings.Add($"{label}:\nCrossword consists of several unconnected parts.");

            return result;
        }

        private static bool IsConnected(List<CrosswordWord> words)
        {
            if (words.Count <= 1) return true;
            var visited = new HashSet<CrosswordWord> { words[0] };
            var queue = new Queue<CrosswordWord>();
            queue.Enqueue(words[0]);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var other in words)
                {
                    if (visited.Contains(other) || !Intersects(current, other)) continue;
                    visited.Add(other);
                    queue.Enqueue(other);
                }
            }
            return visited.Count == words.Count;
        }

        private static bool Intersects(CrosswordWord a, CrosswordWord b)
        {
            for (int i = 0; i < a.Length; i++)
            for (int j = 0; j < b.Length; j++)
                if (a.GetPos(i).Equals(b.GetPos(j)))
                    return true;
            return false;
        }

        public static string Format(Result result)
        {
            var sb = new StringBuilder();
            foreach (var e in result.Errors) sb.AppendLine(e).AppendLine();
            return sb.ToString();
        }
    }
}
