using System;
using System.Collections.Generic;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.CellSet
{
    internal class LevelCell : ILevelCell
    {
        internal CellSet fCellSet;
        internal CellsetLevel fCSL;

        internal LevelCell(CellSet ACellSet, CellsetLevel CSL)
        {
            fCSL = CSL;
            fCellSet = ACellSet;
        }

        public string Description => fCSL.FLevel == null ? "" : fCSL.FLevel.Description;

        public int StartRow => fCSL.FStartRow;

        public int StartColumn => fCSL.FStartCol;

        public int PagedStartColumn
        {
            get
            {
                int i;
                if (fCellSet.AdjustedColsHelper.TryGetValue(fCSL.FStartCol, out i))
                    return i;
                throw new Exception("Invalid paging index conversion");
            }
        }

        public int PagedStartRow
        {
            get
            {
                int i;
                if (fCellSet.AdjustedRowsHelper.TryGetValue(fCSL.FStartRow, out i))
                    return i;
                throw new Exception("Invalid paging index conversion");
            }
        }

        public int RowSpan => fCSL.FRowSpan;

        public int ColSpan => fCSL.FColSpan;

        public string Value
        {
            get
            {
                if (fCSL.FLevel == null) return "";
                return fCSL.Attribute == null ? fCSL.FLevel.DisplayName : fCSL.Attribute.DisplayName;
            }
        }

        public CellType CellType => CellType.ctLevel;

        public CellSet CellSet => fCellSet;

        public List<CubeAction> CubeActions => new List<CubeAction>();

        #region ILevelCell Members

        public byte Indent => fCSL.FIndent;

        public InfoAttribute Attribute => fCSL == null ? null : fCSL.Attribute;

        public Level Level => fCSL.FLevel;

        public void ExpandAllNodes(PossibleDrillActions Mode)
        {
            fCellSet.ExpandAllNodes(Mode, fCSL.FLevel);
        }

        public void ExpandNodesAnywhere(PossibleDrillActions Mode, Level toLevel)
        {
            fCellSet.ExpandNodesAnywhere(Mode, Level, toLevel);
        }

        public void CollapseAllNodes()
        {
            fCellSet.CollapseAllNodes(fCSL.FLevel);
        }

        public PossibleDrillActions PossibleDrillActions
        {
            get
            {
                var Result = PossibleDrillActions.esNone;
                if (fCellSet.Grid.CellsetMode == CellsetMode.cmGrid)
                    return Result;
                if (Level == null || Level.Hierarchy == null)
                    return Result;
                var h = Level.Hierarchy;

                if (Level.Index == h.Levels.Count - 1)
                    return Result;

                for (var i = Level.Index + 1; i < h.Levels.Count; i++)
                    if (h.Levels[i].Visible)
                    {
                        Result = PossibleDrillActions.esCollapsed;
                        break;
                    }

                if (!h.Levels[Level.Index + 1].Visible)
                {
                    Result |= PossibleDrillActions.esNextLevel;
                }
                else
                {
                    if (Level.Index < h.Levels.Count - 2 && !h.Levels[Level.Index + 2].Visible)
                    {
                        var ll = h.Levels[Level.Index + 1];
                        if (fCellSet.FGrid.FLayout.fRowLevels.Count > 0 &&
                            fCellSet.FGrid.FLayout.fYAxisMeasures.Count == 0
                            && fCellSet.FGrid.FLayout.fRowLevels[fCellSet.FGrid.FLayout.fRowLevels.Count - 1] == ll)
                            Result |= PossibleDrillActions.esNextLevel;
                        if (fCellSet.FGrid.FLayout.fColumnLevels.Count > 0 &&
                            fCellSet.FGrid.FLayout.fXAxisMeasure == null
                            && fCellSet.FGrid.FLayout.fColumnLevels[fCellSet.FGrid.FLayout.fColumnLevels.Count - 1] ==
                            ll)
                            Result |= PossibleDrillActions.esNextLevel;
                    }
                }
                return Result;
            }
        }

        #endregion
    }
}