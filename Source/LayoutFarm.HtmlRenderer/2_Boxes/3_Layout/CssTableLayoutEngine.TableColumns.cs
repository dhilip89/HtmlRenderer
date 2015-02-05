﻿// 2015,2014 ,BSD, WinterDev
//ArthurHub 


namespace LayoutFarm.HtmlBoxes
{
    partial class CssTableLayoutEngine
    {
        enum ColumnSpecificWidthLevel : byte
        {
            None,
            StartAtMin,
            Adjust,

            ExpandToMax,
            FromItsContent,
            FromCellConstraint
        }

        class TableColumn
        {
            int index;
            ColumnSpecificWidthLevel spWidthLevel;

            float actualWidth;
            float minWidth;
            float maxWidth;

            public TableColumn(int index)
            {
                this.index = index;
            }
            public float Width
            {
                get { return this.actualWidth; }
            }
            public void SetWidth(float width, ColumnSpecificWidthLevel specificWidthLevel)
            {
                this.SpecificWidthLevel = specificWidthLevel;
                this.actualWidth = width;
            }
            public float MinWidth
            {
                get { return this.minWidth; }
            }
            public float MaxWidth
            {
                get { return this.maxWidth; }
            }
            public bool HasSpecificWidth
            {
                get { return this.spWidthLevel == ColumnSpecificWidthLevel.FromCellConstraint; }
            }
            public ColumnSpecificWidthLevel SpecificWidthLevel
            {
                get { return this.spWidthLevel; }
                private set
                {
                    if (this.spWidthLevel == ColumnSpecificWidthLevel.FromCellConstraint)
                    {
                    }
                    else
                    {
                        this.spWidthLevel = value;
                    }
                }
            }
            //-----------------------------------------------------
            public void S3_UpdateIfWider(float newWidth, ColumnSpecificWidthLevel level)
            {
                //called at state3 only
                if (newWidth > this.actualWidth)
                {
                    this.actualWidth = this.minWidth;
                    this.SpecificWidthLevel = level;
                }
            }
            public void S5_SetMinWidth(float minWidth)
            {
                this.minWidth = minWidth;
            }
            public void UpdateMinMaxWidthIfWider(float newMinWidth, float newMaxWidth)
            {
                if (newMinWidth > this.minWidth)
                {
                    this.minWidth = newMinWidth;
                }

                if (newMaxWidth > this.maxWidth)
                {
                    this.maxWidth = newMaxWidth;
                }
            }
            public bool TouchLowerLimit
            {
                get { return this.actualWidth <= this.minWidth; }
            }
            public bool TouchUpperLimit
            {
                get { return this.actualWidth >= this.maxWidth; }
            }

            public void AddMoreWidthValue(float offset, ColumnSpecificWidthLevel level)
            {
                this.actualWidth += offset;
                this.SpecificWidthLevel = level;
            }
            public void UseMinWidth()
            {
                this.actualWidth = this.minWidth;
                this.SpecificWidthLevel = ColumnSpecificWidthLevel.StartAtMin;
            }
            public void UseMaxWidth()
            {
                this.actualWidth = this.MaxWidth;
                this.SpecificWidthLevel = ColumnSpecificWidthLevel.ExpandToMax;
            }
            public void AdjustDecrByOne()
            {
                this.actualWidth--;
                this.SpecificWidthLevel = ColumnSpecificWidthLevel.Adjust;
            }
        }
        class TableColumnCollection
        {
            TableColumn[] columns;
            public TableColumnCollection(int columnCount)
            {
                this.columns = new TableColumn[columnCount];
                for (int i = columnCount - 1; i >= 0; --i)
                {
                    this.columns[i] = new TableColumn(i);
                }
            }
            public void SetColumnWidth(int colIndex, float width)
            {
                columns[colIndex].SetWidth(width, ColumnSpecificWidthLevel.FromCellConstraint);
            }
            public TableColumn this[int index]
            {
                get
                {
                    return this.columns[index];
                }
            }
            public void CountUnspecificWidthColumnAndOccupiedSpace(out int numOfUnspecificColWidth, out float occupiedSpace)
            {
                numOfUnspecificColWidth = 0;
                occupiedSpace = 0f;
                for (int i = columns.Length - 1; i >= 0; --i)
                {
                    var col = columns[i];
                    if (!col.HasSpecificWidth)
                    {
                        numOfUnspecificColWidth++;
                    }
                    else
                    {
                        occupiedSpace += col.Width;
                    }
                }
            }

            public bool FindFirstReducibleColumnWidth(int startAtIndex, out int foundAtIndex)
            {

                int col_count = columns.Length;
                for (int i = startAtIndex; i < col_count; ++i)
                {
                    if (!columns[i].TouchLowerLimit)
                    {
                        foundAtIndex = i;
                        return true;
                    }

                }
                foundAtIndex = -1;
                return false;
            }
            public int Count
            {
                get
                {
                    return this.columns.Length;
                }
            }
            public float CalculateTotalWidth()
            {
                float total = 0;
                for (int i = columns.Length - 1; i >= 0; --i)
                {
                    total += columns[i].Width;
                    //float t = columns[i];
                    //if (float.IsNaN(t))
                    //{
                    //    throw new Exception("CssTable Algorithm error: There's a NaN in column widths");
                    //}
                    //else
                    //{
                    //    f += t;
                    //}
                }
                return total;

            }

            public void S4_AddMoreWidthToColumns(bool onlyNonspeicificWidth, float value)
            {
                if (onlyNonspeicificWidth)
                {
                    for (int i = columns.Length - 1; i >= 0; --i)
                    {
                        var col = columns[i];

                        if (!col.HasSpecificWidth)
                        {
                            col.AddMoreWidthValue(value, ColumnSpecificWidthLevel.Adjust);
                        }
                    }
                }
                else
                {
                    for (int i = columns.Length - 1; i >= 0; --i)
                    {
                        columns[i].AddMoreWidthValue(value, ColumnSpecificWidthLevel.Adjust);
                    }
                }
            }
            public void LowerAllColumnToMinWidth()
            {
                for (int i = columns.Length - 1; i >= 0; --i)
                {
                    columns[i].UseMinWidth();
                }
            }
            /// <summary>
            /// Gets the spanned width of a cell (With of all columns it spans minus one).
            /// </summary>
            public float GetSpannedMinWidth(int rowCellCount, int realcolindex, int colspan)
            {
                float w = 0f;
                int min_widths_len = this.columns.Length;
                for (int i = realcolindex; (i < min_widths_len) && (i < rowCellCount || i < realcolindex + colspan - 1); ++i)
                {
                    w += this.columns[i].MinWidth;
                }
                return w;
            }
            /// <summary>
            /// Gets the cells width, taking colspan and being in the specified column
            /// </summary>
            /// <param name="cIndex"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            public float GetCellWidth(int cIndex, int colspan, float horizontal_spacing)
            {
                if (colspan == 1)
                {
                    return this.columns[cIndex].Width;
                }
                else
                {
                    float sum = 0f;
                    int col_count = this.columns.Length;
                    for (int i = cIndex; (i < cIndex + colspan) && (i < col_count); ++i)
                    {
                        sum += this.columns[i].Width;
                    }
                    sum += (colspan - 1) * horizontal_spacing;
                    return sum;
                }
            }

        }
    }
}