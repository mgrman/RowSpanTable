using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RowSpanTable.v2
{
    public class Row_v2 : LayoutGroup
    {
        private Table_v2 table;

        private int rowIndex;
        protected override void Awake()
        {
            base.Awake();

            Initialize();
        }

        private void Initialize()
        {
            table = transform.parent.GetComponent<Table_v2>();
            table.Initialize();
            rowIndex = 0;
            foreach (Transform rowTransform in transform.parent)
            {
                var row = rowTransform.GetComponent<Row_v2>();
                if (row == this)
                {
                    break;
                }

                rowIndex++;
            }
        }

        /// <summary>
        /// Called by the layout system to calculate the horizontal layout size.
        /// Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputHorizontal()
        {
            Initialize();
            base.CalculateLayoutInputHorizontal();

            var minSize = SumForMyRow(o => LayoutUtility.GetMinSize(o.rectTransform, 0));
            var totalPref = SumForMyRow(o => LayoutUtility.GetPreferredWidth(o.rectTransform));
            SetLayoutInputForAxis(minSize, totalPref, -1, 0);
        }

        /// <summary>
        /// Called by the layout system to calculate the vertical layout size.
        /// Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputVertical()
        {
            Initialize();
            var minSize = MaxForMyRow(o => LayoutUtility.GetMinSize(o.rectTransform, 1));
            var totalPref = MaxForMyRow(o => LayoutUtility.GetPreferredHeight(o.rectTransform));
            SetLayoutInputForAxis(minSize, totalPref, -1, 1);
        }

        private float MaxForMyRow(Func<Table_v2.CellData, float> cellFunc)
        {
            return table.GetCellsForRow(rowIndex)
                    .Select(cellFunc)
                    .Max();
        }

        private float SumForMyRow(Func<Table_v2.CellData, float> cellFunc)
        {
            return table.GetCellsForRow(rowIndex)
                .Select(cellFunc)
                .Sum();
        }

        /// <summary>
        /// Called by the layout system
        /// Also see ILayoutElement
        /// </summary>
        public override void SetLayoutHorizontal()
        {
            Initialize();
            foreach (var cell in table.GetCellsForRow(rowIndex))
            {
                if (cell.rectTransform.parent != transform)
                {
                    continue;
                }
                
                var x = Enumerable.Range(0, cell.col)
                    .Select(r => table.ColumnWidths[r])
                    .Sum();
                var colWidth = Enumerable.Range(cell.col, cell.colSpan)
                    .Select(r => table.ColumnWidths[r])
                    .Sum();

                SetChildAlongAxis(cell.rectTransform, 0, x , colWidth );
            }
        }

        /// <summary>
        /// Called by the layout system
        /// Also see ILayoutElement
        /// </summary>
        public override void SetLayoutVertical()
        {
            Initialize();
            
            foreach (var cell in table.GetCellsForRow(rowIndex))
            {
                if (cell.rectTransform.parent != transform)
                {
                    continue;
                }

                var y = 0;
                var rowHeight = Enumerable.Range(cell.row, cell.rowSpan)
                    .Select(r => table.RowHeights[r])
                    .Sum();
                
                SetChildAlongAxis(cell.rectTransform, 1, y, rowHeight);
            }
        }

    }
}