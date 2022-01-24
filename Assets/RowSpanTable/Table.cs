using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RowSpanTable
{
    public class Table : LayoutGroup
    {
        [SerializeField]
        private bool stretchWidth;

        [SerializeField]
        private float[] rowHeights;

        [SerializeField]
        private float[] columnWidths;

        private DrivenRectTransformTracker tracker;

        public int ColCount { get; private set; }

        public int RowCount { get; private set; }

        public CellData[,] Cells { get; private set; }

        public float[] RowHeights => rowHeights;

        public float[] ColumnWidths => columnWidths;

        protected override void Awake()
        {
            base.Awake();

            Initialize();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable()
        {
            tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }

        public void Initialize()
        {
            RowCount = 0;
            ColCount = 0;


            foreach (Transform rowTransform in transform)
            {
                var row = rowTransform.GetComponent<Row>();
                if (row == null)
                {
                    rowTransform.gameObject.SetActive(false);
                    continue;
                }

                RowCount++;
            }

            var rowIndex = 0;

            var perRowOffsets = new int[RowCount];
            foreach (Transform rowTransform in transform)
            {
                var row = rowTransform.GetComponent<Row>();
                if (row == null)
                {
                    rowTransform.gameObject.SetActive(false);
                    continue;
                }

                foreach (Transform cellTransform in rowTransform)
                {
                    var cell = cellTransform.GetComponent<Cell>();
                    if (cell == null)
                    {
                        cellTransform.gameObject.SetActive(false);
                        continue;
                    }

                    var columnIndex = perRowOffsets[rowIndex]; // TODO needs to be offseted by possible rowspan cells

                    RowCount = Math.Max(RowCount, rowIndex + cell.RowSpan);
                    ColCount = Math.Max(ColCount, columnIndex + cell.ColSpan);

                    for (var iy = 0; iy < cell.RowSpan; iy++)
                    {
                        perRowOffsets[rowIndex + iy] += cell.ColSpan;
                    }
                }

                rowIndex++;
            }

            Cells = new CellData[ColCount, RowCount];

            rowIndex = 0;
            perRowOffsets = new int[RowCount];
            foreach (Transform rowTransform in transform)
            {
                var row = rowTransform.GetComponent<Row>();
                if (row == null)
                {
                    continue;
                }

                foreach (Transform cellTransform in rowTransform)
                {
                    var cell = cellTransform.GetComponent<Cell>();
                    if (cell == null)
                    {
                        continue;
                    }

                    var columnIndex = perRowOffsets[rowIndex]; // TODO needs to be offseted by possible rowspan cells

                    var cellData = new CellData(columnIndex, rowIndex, cell.ColSpan, cell.RowSpan,
                        cell.GetComponent<RectTransform>());

                    for (var ix = 0; ix < cell.ColSpan; ix++)
                    {
                        for (var iy = 0; iy < cell.RowSpan; iy++)
                        {
                            Cells[columnIndex + ix, rowIndex + iy] = cellData;
                        }
                    }

                    for (var iy = 0; iy < cell.RowSpan; iy++)
                    {
                        perRowOffsets[rowIndex + iy] += cell.ColSpan;
                    }
                }

                rowIndex++;
            }
        }

        /// <summary>
        ///     Called by the layout system to calculate the horizontal layout size.
        ///     Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputHorizontal()
        {
            Initialize();
            base.CalculateLayoutInputHorizontal();

            var minSize = MaxForEachColumn(o => LayoutUtility.GetMinSize(o.rectTransform, 0));
            var totalPref = MaxForEachColumn(o => LayoutUtility.GetPreferredWidth(o.rectTransform));
            SetLayoutInputForAxis(minSize, totalPref, -1, 0);
        }

        /// <summary>
        ///     Called by the layout system to calculate the vertical layout size.
        ///     Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputVertical()
        {
            Initialize();
            var minSize = MaxForEachRow(o => LayoutUtility.GetMinSize(o.rectTransform, 1));
            var totalPref = MaxForEachRow(o => LayoutUtility.GetPreferredHeight(o.rectTransform));
            SetLayoutInputForAxis(minSize, totalPref, -1, 1);
        }

        public IEnumerable<CellData> GetCellsForRow(int row)
        {
            CellData previousCell = null;
            for (var ix = 0; ix < ColCount; ix++)
            {
                var cell = Cells[ix, row];
                if (cell == null || previousCell == cell)
                {
                    continue;
                }

                yield return cell;
                previousCell = cell;
            }
        }

        private IEnumerable<CellData> GetCellsForColumn(int col)
        {
            CellData previousCell = null;
            for (var iy = 0; iy < RowCount; iy++)
            {
                var cell = Cells[col, iy];
                if (cell == null || previousCell == cell)
                {
                    continue;
                }

                yield return cell;
                previousCell = cell;
            }
        }

        /// <summary>
        ///     Called by the layout system
        ///     Also see ILayoutElement
        /// </summary>
        public override void SetLayoutHorizontal()
        {
            m_Tracker.Clear();
            Initialize();
            UpdateColumnWidths();

            var widthsSum = ColumnWidths.Sum();
            foreach (Transform rowTransform in transform)
            {
                var row = rowTransform.GetComponent<Row>();
                if (row == null)
                {
                    continue;
                }

                var rowRectTransform = rowTransform.GetComponent<RectTransform>();
                SetChildAlongAxis(rowRectTransform, 0, 0, widthsSum);
            }
        }

        /// <summary>
        ///     Called by the layout system
        ///     Also see ILayoutElement
        /// </summary>
        public override void SetLayoutVertical()
        {
            Initialize();
            UpdateRowHeights();

            var rowIndex = 0;
            var y = 0f;
            foreach (Transform rowTransform in transform)
            {
                var row = rowTransform.GetComponent<Row>();
                if (row == null)
                {
                    continue;
                }

                var rowHeight = RowHeights[rowIndex];

                var rowRectTransform = rowTransform.GetComponent<RectTransform>();

                SetChildAlongAxis(rowRectTransform, 1, y, rowHeight);

                y += rowHeight;
                rowIndex++;
            }

            m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, RowHeights.Sum());
        }

        private void UpdateColumnWidths()
        {
            columnWidths = new float[ColCount];
            for (var ix = 0; ix < ColCount; ix++)
            {
                var colWidth = 0f;
                var column = GetCellsForColumn(ix);
                foreach (var cell in column)
                {
                    var width = LayoutUtility.GetPreferredWidth(cell.rectTransform) / cell.colSpan;
                    colWidth = Math.Max(colWidth, width);
                }

                ColumnWidths[ix] = colWidth;
            }

            if (stretchWidth)
            {
                var scaleFactor = rectTransform.rect.width / ColumnWidths.Sum();

                for (var ix = 0; ix < ColCount; ix++)
                {
                    ColumnWidths[ix] *= scaleFactor;
                }
            }
        }

        private void UpdateRowHeights()
        {
            rowHeights = new float[RowCount];
            for (var iy = 0; iy < RowCount; iy++)
            {
                var rowHeight = 0f;
                var row = GetCellsForRow(iy);
                foreach (var cell in row)
                {
                    var height = LayoutUtility.GetPreferredHeight(cell.rectTransform) / cell.rowSpan;
                    rowHeight = Math.Max(rowHeight, height);
                }

                RowHeights[iy] = rowHeight;
            }
        }

        private float MaxForEachRow(Func<CellData, float> cellFunc)
        {
            return Enumerable.Range(0, RowCount)
                .Select(r => GetCellsForRow(r)
                    .Select(cellFunc)
                    .Sum())
                .Max();
        }

        private float MaxForEachColumn(Func<CellData, float> cellFunc)
        {
            return Enumerable.Range(0, ColCount)
                .Select(r => GetCellsForColumn(r)
                    .Select(cellFunc)
                    .Sum())
                .Max();
        }

        public class CellData
        {
            public CellData(int col, int row, int colSpan, int rowSpan, RectTransform rectTransform)
            {
                this.col = col;
                this.row = row;
                this.colSpan = colSpan;
                this.rowSpan = rowSpan;
                this.rectTransform = rectTransform;
            }

            public RectTransform rectTransform { get; }
            public int col { get; }
            public int row { get; }
            public int colSpan { get; }
            public int rowSpan { get; }
        }
    }
}