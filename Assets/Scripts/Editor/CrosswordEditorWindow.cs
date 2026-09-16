using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CrosswordGame.EditorTools
{
    /// <summary>
    /// Простой редактор кроссвордов: загрузка JSON, сетка с номерами, список слов,
    /// проверка пересечений и сохранение. Меню: Tools → Crossword Editor.
    /// </summary>
    public class CrosswordEditorWindow : EditorWindow
    {
        private const float CellSize = 30f;

        private string _path;
        private CrosswordData _data;
        private CrosswordValidator.Result _validation;
        private Vector2 _wordsScroll;
        private Vector2 _gridScroll;
        private int _selectedWord = -1;
        private bool _dirty;

        private GUIStyle _letterStyle;
        private GUIStyle _numberStyle;

        [MenuItem("Tools/Crossword Editor", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<CrosswordEditorWindow>("Crossword Editor");
            window.minSize = new Vector2(900, 500);
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            if (_data == null)
            {
                EditorGUILayout.HelpBox("Загрузите JSON уровня (StreamingAssets/Crosswords/level_N.json) или создайте новый.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f));
            DrawHeader();
            DrawWords();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            DrawValidation();
            DrawGrid();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        #region Toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Load", EditorStyles.toolbarDropDown, GUILayout.Width(70)))
            {
                var menu = new GenericMenu();
                if (Directory.Exists(CrosswordLevelsValidator.LevelsFolder))
                {
                    foreach (var file in Directory.GetFiles(CrosswordLevelsValidator.LevelsFolder, "*.json").OrderBy(f => f.Length).ThenBy(f => f))
                    {
                        string path = file.Replace('\\', '/');
                        menu.AddItem(new GUIContent(Path.GetFileName(path)), path == _path, () => Load(path));
                    }
                }
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Other file..."), false, () =>
                {
                    string path = EditorUtility.OpenFilePanel("Load crossword", CrosswordLevelsValidator.LevelsFolder, "json");
                    if (!string.IsNullOrEmpty(path)) Load(path);
                });
                menu.ShowAsContext();
            }

            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(50)))
                CreateNew();

            GUI.enabled = _data != null;
            if (GUILayout.Button(_dirty ? "Save*" : "Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
                Save(false);
            if (GUILayout.Button("Save As...", EditorStyles.toolbarButton, GUILayout.Width(80)))
                Save(true);
            if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(70)))
                Validate(true);
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            GUILayout.Label(string.IsNullOrEmpty(_path) ? "(not saved)" : _path, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void Load(string path)
        {
            _path = path;
            _data = CrosswordLoader.Parse(File.ReadAllText(path)) ?? new CrosswordData();
            if (_data.words == null) _data.words = new List<CrosswordWord>();
            _selectedWord = -1;
            _dirty = false;
            Validate(false);
        }

        private void CreateNew()
        {
            int next = 1;
            while (File.Exists($"{CrosswordLevelsValidator.LevelsFolder}/{CrosswordLoader.GetLevelFileName(next)}")) next++;
            _data = new CrosswordData { id = next, difficulty = Mathf.Clamp(next, 1, 5) };
            _path = $"{CrosswordLevelsValidator.LevelsFolder}/{CrosswordLoader.GetLevelFileName(next)}";
            _selectedWord = -1;
            _dirty = true;
            Validate(false);
        }

        private void Save(bool saveAs)
        {
            string path = _path;
            if (saveAs || string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFilePanel("Save crossword", CrosswordLevelsValidator.LevelsFolder,
                    CrosswordLoader.GetLevelFileName(_data.id), "json");
                if (string.IsNullOrEmpty(path)) return;
            }

            Validate(true);
            if (!_validation.IsValid &&
                !EditorUtility.DisplayDialog("Crossword", "В уровне есть ошибки. Всё равно сохранить?", "Сохранить", "Отмена"))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(_data, true));
            _path = path;
            _dirty = false;
            AssetDatabase.Refresh();
        }

        private void Validate(bool log)
        {
            LocalizationManager.EnsureInitialized();
            string label = string.IsNullOrEmpty(_path) ? $"Level {_data.id}" : CrosswordLevelsValidator.GetLabel(_path);
            _validation = CrosswordValidator.Validate(_data, label, LocalizationManager.Alphabet);
            if (!log) return;
            foreach (var w in _validation.Warnings) Debug.LogWarning(w);
            foreach (var e in _validation.Errors) Debug.LogError(e);
            if (_validation.IsValid) Debug.Log($"{label}: OK ({_data.words.Count} words).");
        }

        #endregion

        #region Words

        private void DrawHeader()
        {
            EditorGUI.BeginChangeCheck();
            _data.id = EditorGUILayout.IntField("Id", _data.id);
            _data.difficulty = EditorGUILayout.IntSlider("Difficulty", _data.difficulty, 1, 5);
            if (EditorGUI.EndChangeCheck()) MarkChanged();
            EditorGUILayout.LabelField($"Words: {_data.words.Count} (expected {CrosswordValidator.ExpectedWordCount})", EditorStyles.boldLabel);
        }

        private void DrawWords()
        {
            _wordsScroll = EditorGUILayout.BeginScrollView(_wordsScroll);
            int removeIndex = -1;

            for (int i = 0; i < _data.words.Count; i++)
            {
                var word = _data.words[i];
                var style = i == _selectedWord ? "SelectionRect" : "HelpBox";
                EditorGUILayout.BeginVertical(style);
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(i == _selectedWord ? "●" : "○", GUILayout.Width(24))) _selectedWord = i == _selectedWord ? -1 : i;
                word.id = EditorGUILayout.TextField(word.id, GUILayout.Width(50));
                word.answer = EditorGUILayout.TextField(word.answer);
                bool across = word.direction != CrosswordWord.Down;
                across = GUILayout.Toggle(across, across ? "→ across" : "↓ down", "Button", GUILayout.Width(80));
                word.direction = across ? CrosswordWord.Across : CrosswordWord.Down;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 30;
                word.row = EditorGUILayout.IntField("row", word.row, GUILayout.Width(80));
                word.col = EditorGUILayout.IntField("col", word.col, GUILayout.Width(80));
                EditorGUIUtility.labelWidth = 0;
                word.question = EditorGUILayout.TextField(word.question);
                if (GUILayout.Button("✕", GUILayout.Width(24))) removeIndex = i;
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck()) MarkChanged();
                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
            {
                _data.words.RemoveAt(removeIndex);
                _selectedWord = -1;
                MarkChanged();
            }

            if (GUILayout.Button("+ Add word"))
            {
                int n = _data.words.Count + 1;
                while (_data.words.Any(w => w.id == $"w{n}")) n++;
                _data.words.Add(new CrosswordWord { id = $"w{n}", answer = "", question = "", direction = CrosswordWord.Across });
                _selectedWord = _data.words.Count - 1;
                MarkChanged();
            }
            EditorGUILayout.EndScrollView();
        }

        private void MarkChanged()
        {
            _dirty = true;
            Validate(false);
        }

        #endregion

        #region Grid

        private void DrawValidation()
        {
            if (_validation == null) return;
            if (_validation.IsValid)
                EditorGUILayout.HelpBox("Ошибок не найдено.", MessageType.Info);
            foreach (var e in _validation.Errors.Take(6)) EditorGUILayout.HelpBox(e, MessageType.Error);
            if (_validation.Errors.Count > 6) EditorGUILayout.HelpBox($"... и ещё {_validation.Errors.Count - 6}", MessageType.Error);
            foreach (var w in _validation.Warnings) EditorGUILayout.HelpBox(w, MessageType.Warning);
        }

        private void DrawGrid()
        {
            var letters = new Dictionary<GridPos, char>();
            var conflicts = new HashSet<GridPos>();
            var selected = new HashSet<GridPos>();
            var starts = new Dictionary<GridPos, int>();
            int maxRow = 0, maxCol = 0;

            for (int i = 0; i < _data.words.Count; i++)
            {
                var word = _data.words[i];
                string answer = CrosswordValidator.NormalizeAnswer(word.answer);
                for (int k = 0; k < answer.Length; k++)
                {
                    var pos = word.GetPos(k);
                    if (pos.row < 0 || pos.col < 0) continue;
                    maxRow = Mathf.Max(maxRow, pos.row);
                    maxCol = Mathf.Max(maxCol, pos.col);
                    if (letters.TryGetValue(pos, out char existing) && existing != answer[k]) conflicts.Add(pos);
                    else letters[pos] = answer[k];
                    if (i == _selectedWord) selected.Add(pos);
                }
            }

            // Нумерация так же, как в игре.
            if (_validation != null && _validation.IsValid && _data.words.Count > 0)
            {
                var board = new CrosswordBoard(_data);
                foreach (var cell in board.Cells)
                    if (cell.Number > 0) starts[cell.Pos] = cell.Number;
            }

            _gridScroll = EditorGUILayout.BeginScrollView(_gridScroll);
            Rect area = GUILayoutUtility.GetRect((maxCol + 2) * CellSize, (maxRow + 2) * CellSize);
            for (int r = 0; r <= maxRow; r++)
            for (int c = 0; c <= maxCol; c++)
            {
                var pos = new GridPos(r, c);
                var rect = new Rect(area.x + c * CellSize, area.y + r * CellSize, CellSize - 1, CellSize - 1);
                if (!letters.TryGetValue(pos, out char letter))
                {
                    EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.25f));
                    continue;
                }

                Color color = conflicts.Contains(pos) ? new Color(0.9f, 0.3f, 0.3f)
                    : selected.Contains(pos) ? new Color(0.6f, 0.8f, 1f) : Color.white;
                EditorGUI.DrawRect(rect, color);
                GUI.Label(rect, letter.ToString(), _letterStyle);
                if (starts.TryGetValue(pos, out int number))
                    GUI.Label(new Rect(rect.x + 1, rect.y, rect.width, 12), number.ToString(), _numberStyle);
            }
            EditorGUILayout.EndScrollView();
        }

        private void EnsureStyles()
        {
            if (_letterStyle != null) return;
            _letterStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            _letterStyle.normal.textColor = Color.black;
            _numberStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 8 };
            _numberStyle.normal.textColor = new Color(0.3f, 0.3f, 0.3f);
        }

        #endregion
    }
}
