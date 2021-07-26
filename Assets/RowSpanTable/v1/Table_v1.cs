using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RowSpanTable.v1
{

    public class Table_v1 : LayoutGroup
    {
        [SerializeField]
        private bool stretchWidth;

        private CellData[,] cells;
        private int colCount;

        private int rowCount;

        private List<CellData> uniqueCells;

        private void Initialize()
        {
            rowCount = 0;
            colCount = 0;
            foreach (Transform child in transform)
            {
                var cell = child.GetComponent<Cell_v1>();

                rowCount = Math.Max(rowCount, cell.row + cell.rowspan);

                colCount = Math.Max(colCount, cell.coll + cell.colspan);
            }

            cells = new CellData[colCount, rowCount];
            uniqueCells = new List<CellData>();
            foreach (Transform child in transform)
            {
                var cell = child.GetComponent<Cell_v1>();
                var cellData = new CellData(cell.coll, cell.row, cell.colspan, cell.rowspan, cell.GetComponent<RectTransform>());
                uniqueCells.Add(cellData);
                for (var ix = 0; ix < cell.colspan; ix++)
                {
                    for (var iy = 0; iy < cell.rowspan; iy++)
                    {
                        cells[cell.coll + ix, cell.row + iy] = cellData;
                    }
                }
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

        private IEnumerable<CellData> GetCellsForRow(int row)
        {
            CellData previousCell = null;
            for (var ix = 0; ix < colCount; ix++)
            {
                var cell = cells[ix, row];
                if (cell == null || previousCell == cell)
                    continue;

                yield return cell;
                previousCell = cell;
            }
        }

        private IEnumerable<CellData> GetCellsForColumn(int col)
        {
            CellData previousCell = null;
            for (var iy = 0; iy < rowCount; iy++)
            {
                var cell = cells[col, iy];
                if (cell == null || previousCell == cell)
                    continue;

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
            Initialize();
            var colWidths = new float[colCount];
            for (var ix = 0; ix < colCount; ix++)
            {
                var colWidth = 0f;
                var column = GetCellsForColumn(ix);
                foreach (var cell in column)
                {
                    var width = LayoutUtility.GetPreferredWidth(cell.rectTransform) / cell.colSpan;
                    colWidth = Math.Max(colWidth, width);
                }

                colWidths[ix] = colWidth;
            }

            float scaleFactor = 1;
            if (stretchWidth)
                scaleFactor = rectTransform.rect.width / colWidths.Sum();

            foreach (var cell in uniqueCells)
            {
                var x = Enumerable.Range(0, cell.col)
                    .Select(r => colWidths[r])
                    .Sum();
                var colWidth = Enumerable.Range(cell.col, cell.colSpan)
                    .Select(r => colWidths[r])
                    .Sum();

                SetChildAlongAxis(cell.rectTransform, 0, x * scaleFactor, colWidth * scaleFactor);
            }
        }

        /// <summary>
        ///     Called by the layout system
        ///     Also see ILayoutElement
        /// </summary>
        public override void SetLayoutVertical()
        {
            Initialize();
            var rowHeights = new float[rowCount];
            for (var iy = 0; iy < rowCount; iy++)
            {
                var rowHeight = 0f;
                var row = GetCellsForRow(iy);
                foreach (var cell in row)
                {
                    var height = LayoutUtility.GetPreferredHeight(cell.rectTransform) / cell.rowSpan;
                    rowHeight = Math.Max(rowHeight, height);
                }

                rowHeights[iy] = rowHeight;
            }

            foreach (var cell in uniqueCells)
            {
                var y = Enumerable.Range(0, cell.row)
                    .Select(r => rowHeights[r])
                    .Sum();
                var rowHeight = Enumerable.Range(cell.row, cell.rowSpan)
                    .Select(r => rowHeights[r])
                    .Sum();

                SetChildAlongAxis(cell.rectTransform, 1, y, rowHeight);
            }
        }

        private class CellData
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