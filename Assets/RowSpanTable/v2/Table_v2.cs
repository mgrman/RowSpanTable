using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RowSpanTable.v2
{
    public class Table_v2 : LayoutGroup
    {
        private int colCount;

        private int rowCount;

        public int ColCount => colCount;

        public int RowCount => rowCount;

        private CellData[,] cells;
        public CellData[,] Cells => cells;

        [SerializeField]
        private bool stretchWidth;

        [SerializeField]
        private float[] rowHeights;

        [SerializeField]
        private float[] columnWidths;

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

        public float[] RowHeights => rowHeights;

        public float[] ColumnWidths => columnWidths;

        protected override void Awake()
        {
            base.Awake();

            Initialize();
        }

        public void Initialize()
        {
            rowCount = 0;
            colCount = 0;


            foreach (Transform rowTransform in transform)
            {
                var row = rowTransform.GetComponent<Row_v2>();
                if (row == null)
                {
                    rowTransform.gameObject.SetActive(false);
                    continue;
                }

                rowCount++;
            }

            int rowIndex = 0;

            var perRowOffsets = new int[rowCount];
            foreach (Transform rowTransform in transform)
            {
                var row = rowTransform.GetComponent<Row_v2>();
                if (row == null)
                {
                    rowTransform.gameObject.SetActive(false);
                    continue;
                }

                foreach (Transform cellTransform in rowTransform)
                {
                    var cell = cellTransform.GetComponent<Cell_v2>();
                    if (cell == null)
                    {
                        cellTransform.gameObject.SetActive(false);
                        continue;
                    }

                    var columnIndex = perRowOffsets[rowIndex]; // TODO needs to be offseted by possible rowspan cells

                    rowCount = Math.Max(rowCount, rowIndex + cell.RowSpan);
                    colCount = Math.Max(colCount, columnIndex + cell.ColSpan);

                    for (int iy = 0; iy < cell.RowSpan; iy++)
                    {
                        perRowOffsets[rowIndex + iy] += cell.ColSpan;
                    }
                }

                rowIndex++;
            }

            cells = new CellData[colCount, rowCount];

            rowIndex = 0;
             perRowOffsets = new int[rowCount];
            foreach (Transform rowTransform in transform)
            {
                var row = rowTransform.GetComponent<Row_v2>();
                if (row == null)
                {
                    continue;
                }

                foreach (Transform cellTransform in rowTransform)
                {
                    var cell = cellTransform.GetComponent<Cell_v2>();
                    if (cell == null)
                    {
                        continue;
                    }

                    var columnIndex = perRowOffsets[rowIndex]; // TODO needs to be offseted by possible rowspan cells

                    var cellData = new CellData(columnIndex, rowIndex, cell.ColSpan, cell.RowSpan, cell.GetComponent<RectTransform>());

                    for (int ix = 0; ix < cell.ColSpan; ix++)
                    {
                        for (int iy = 0; iy < cell.RowSpan; iy++)
                        {
                            cells[columnIndex + ix, rowIndex + iy] = cellData;
                        }
                    }

                    for (int iy = 0; iy < cell.RowSpan; iy++)
                    {
                        perRowOffsets[rowIndex + iy] += cell.ColSpan;
                    }

                    columnIndex++;
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
            for (int ix = 0; ix < colCount; ix++)
            {
                var cell = cells[ix, row];
                if (cell == null || previousCell == cell)
                {
                    continue;
                }

                yield return cell;
                previousCell = cell;
            }
        }

        public IEnumerable<CellData> GetCellsForColumn(int col)
        {
            CellData previousCell = null;
            for (int iy = 0; iy < rowCount; iy++)
            {
                var cell = cells[col, iy];
                if (cell == null || previousCell == cell)
                {
                    continue;
                }

                yield return cell;
                previousCell = cell;
            }
        }

        /// <summary>
        /// Called by the layout system
        /// Also see ILayoutElement
        /// </summary>
        public override void SetLayoutHorizontal()
        {
            Initialize();
            UpdateColumnWidths();

            var widthsSum = ColumnWidths.Sum();
            foreach (Transform rowTransform in transform)
            {
                var row = rowTransform.GetComponent<Row_v2>();
                if (row == null)
                {
                    continue;
                }

                var rowRectTransform = rowTransform.GetComponent<RectTransform>();
                SetChildAlongAxis(rowRectTransform, 0, 0, widthsSum);
            }
        }

        /// <summary>
        /// Called by the layout system
        /// Also see ILayoutElement
        /// </summary>
        public override void SetLayoutVertical()
        {
            Initialize();
            UpdateRowHeights();

            int rowIndex = 0;
            var y = 0f;
            foreach (Transform rowTransform in transform)
            {
                var row = rowTransform.GetComponent<Row_v2>();
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

        }

        private void UpdateColumnWidths()
        {
            columnWidths = new float[colCount];
            for (int ix = 0; ix < colCount; ix++)
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
              var  scaleFactor = rectTransform.rect.width / ColumnWidths.Sum();

                for (int ix = 0; ix < colCount; ix++)
                {
                    ColumnWidths[ix] *= scaleFactor;
                }
            }
        }

        private void UpdateRowHeights()
        {
            rowHeights = new float[rowCount];
            for (int iy = 0; iy < rowCount; iy++)
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
            return Enumerable.Range(0, rowCount)
                .Select(r => GetCellsForRow(r)
                    .Select(cellFunc)
                    .Sum())
                .Max();
        }

        private float MaxForEachColumn(Func<CellData, float> cellFunc)
        {
            return Enumerable.Range(0, colCount)
                .Select(r => GetCellsForColumn(r)
                    .Select(cellFunc)
                    .Sum())
                .Max();
        }

    }
}