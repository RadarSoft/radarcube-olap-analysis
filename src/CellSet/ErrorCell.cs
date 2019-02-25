using System;
using System.Collections.Generic;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.CellSet
{
    internal class ErrorCell : ICell
    {
        internal ErrorCell(CellSet ACellSet, int col, int row)
        {
            CellSet = ACellSet;
            StartRow = row;
            StartColumn = col;
        }

        public string Description => null;

        public int StartRow { get; }

        public int StartColumn { get; }

        public int PagedStartColumn
        {
            get
            {
                int i;
                if (CellSet.AdjustedColsHelper.TryGetValue(StartColumn, out i))
                    return i;
                throw new Exception("Invalid paging index conversion");
            }
        }

        public int PagedStartRow
        {
            get
            {
                int i;
                if (CellSet.AdjustedRowsHelper.TryGetValue(StartRow, out i))
                    return i;
                throw new Exception("Invalid paging index conversion");
            }
        }

        public int RowSpan => 1;

        public int ColSpan => 1;

        public string Value => CellSet.FErrorString;

        public CellType CellType => CellType.ctNone;

        public CellSet CellSet { get; }

        public List<CubeAction> CubeActions => new List<CubeAction>();
    }
}