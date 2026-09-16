using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrosswordGame
{
    /// <summary>
    /// Строит поле кроссворда по CrosswordBoard: создаёт клетки (из пула), расставляет их,
    /// отображает номера и подсветку. Клетки не хранятся в сцене — всё строится из JSON.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CrosswordGridManager : MonoBehaviour
    {
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private CrosswordCell cellPrefab;
        [SerializeField] private float maxCellSize = 96f;
        [SerializeField, Range(0f, 0.2f)] private float spacingRatio = 0.06f;
        [SerializeField] private float areaPadding = 12f;

        public event Action<CrosswordCell> CellClicked;
        public event Action LayoutChanged;

        public CrosswordBoard Board { get; private set; }
        public float CellSize { get; private set; }

        private readonly List<CrosswordCell> _pool = new List<CrosswordCell>();
        private readonly Dictionary<CellData, CrosswordCell> _views = new Dictionary<CellData, CrosswordCell>();
        private Vector2 _lastAreaSize;

        public IEnumerable<CrosswordCell> Views => _views.Values;

        public void Build(CrosswordBoard board)
        {
            Board = board;
            _views.Clear();

            // Клетки создаются только при загрузке уровня; лишние из пула скрываются.
            while (_pool.Count < board.Cells.Count)
            {
                var cell = Instantiate(cellPrefab, gridRoot);
                cell.Clicked += OnCellClicked;
                _pool.Add(cell);
            }

            for (int i = 0; i < _pool.Count; i++)
            {
                bool used = i < board.Cells.Count;
                _pool[i].gameObject.SetActive(used);
                if (!used) continue;

                var data = board.Cells[i];
                _pool[i].name = $"Cell_{data.Pos.row}_{data.Pos.col}";
                _pool[i].Bind(data);
                _views[data] = _pool[i];
            }

            _lastAreaSize = Vector2.zero;
            Relayout();
        }

        public CrosswordCell GetView(CellData data) => data != null && _views.TryGetValue(data, out var view) ? view : null;

        public void RefreshAll()
        {
            foreach (var view in _views.Values) view.Refresh(false);
        }

        public void SetHighlight(CrosswordQuestion activeWord, CellData activeCell)
        {
            foreach (var pair in _views)
            {
                bool inWord = activeWord != null && (pair.Key.Across == activeWord || pair.Key.Down == activeWord);
                pair.Value.SetHighlight(inWord, pair.Key == activeCell);
            }
        }

        private void OnRectTransformDimensionsChange() => Relayout();

        private void Relayout()
        {
            if (Board == null || gridRoot == null) return;

            var area = (RectTransform)transform;
            Vector2 areaSize = area.rect.size;
            if (areaSize.x <= 0 || areaSize.y <= 0) return;
            if ((areaSize - _lastAreaSize).sqrMagnitude < 0.01f) return;
            _lastAreaSize = areaSize;

            float width = areaSize.x - areaPadding * 2f;
            float height = areaSize.y - areaPadding * 2f;
            // step = size * (1 + spacing); общая ширина = cols * step - spacing * size
            float stepFactor = 1f + spacingRatio;
            float sizeByWidth = width / (Board.Cols * stepFactor - spacingRatio);
            float sizeByHeight = height / (Board.Rows * stepFactor - spacingRatio);
            CellSize = Mathf.Floor(Mathf.Min(maxCellSize, sizeByWidth, sizeByHeight));
            float step = CellSize * stepFactor;

            float totalWidth = Board.Cols * step - CellSize * spacingRatio;
            float totalHeight = Board.Rows * step - CellSize * spacingRatio;
            gridRoot.sizeDelta = new Vector2(totalWidth, totalHeight);

            foreach (var pair in _views)
            {
                var rect = pair.Value.RectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0.5f, 0.5f);
                int col = pair.Key.Pos.col - Board.MinCol;
                int row = pair.Key.Pos.row - Board.MinRow;
                rect.anchoredPosition = new Vector2(col * step + CellSize * 0.5f, -(row * step + CellSize * 0.5f));
                pair.Value.SetLayout(CellSize);
            }

            LayoutChanged?.Invoke();
        }

        private void OnCellClicked(CrosswordCell cell) => CellClicked?.Invoke(cell);
    }
}
