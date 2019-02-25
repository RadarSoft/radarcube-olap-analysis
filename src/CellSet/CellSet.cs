using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using RadarSoft.RadarCube.CellSet.Md;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Controls.Chart;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Engine;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.CellSet
{
    /// <summary>
    ///     Represents the current OLAP slice displayed in the Grid and contains all
    ///     necessary properties and methods to manipulate the current slice.
    /// </summary>
    /// <remarks>
    ///     This object deals with the data only and is made invisible on purpose. Thus, it
    ///     does not have properties like the width of rows and columns in pixels, the scrolling
    ///     position, the current cell and so on. All those properties attributed to the data of
    ///     the current class belong to the OlapGrid object
    /// </remarks>
    ////[Serializable]
    public class CellSet : IStreamedObject, ICellSet
    {
        internal virtual void InitLine(List<Level> ldecart, LayoutArea tLayoutArea,
            out Line line, out ChartAxis axis)
        {
            line = null;
            axis = null;
        }

        internal bool IsDisposed { get; private set; }

        protected virtual void Close()
        {
            FGrid = null;
            if (_ICells != null)
                _ICells.Clear();
        }

        internal void Dispose()
        {
            Close();
            IsDisposed = true;
        }

        internal Dictionary<string, int> ScrolledNodes { get; } = new Dictionary<string, int>();

        [NonSerialized]
#if DEBUG
        private CellSetMembers _FRowMembers = new CellSetMembers();

        internal CellSetMembers FRowMembers
        {
            get => _FRowMembers;
            set => _FRowMembers = value;
        }
#else
        internal CellSetMembers FRowMembers = new CellSetMembers();
#endif
        [NonSerialized] internal CellSetMembers FColumnMembers = new CellSetMembers();
        [NonSerialized] internal List<CellsetLevel> FRowLevels = new List<CellsetLevel>();
        [NonSerialized] internal List<CellsetLevel> FColumnLevels = new List<CellsetLevel>();
        [NonSerialized] private int FColumnCount;
        [NonSerialized] internal OlapControl FGrid;

#if DEBUG
        internal DebugHashSet<DrillAction> FDrillActions = new DebugHashSet<DrillAction>();
#else
        internal HashSet<DrillAction> FDrillActions = new HashSet<DrillAction>();
#endif
        [NonSerialized] internal object[] FRowMembersArray;
        [NonSerialized] internal object[] FColMembersArray;
        [NonSerialized] internal int FFixedRows;
        [NonSerialized] internal int FFixedColumns;

        [NonSerialized] internal List<Measure> FVisibleMeasures = new List<Measure>();
        [NonSerialized] internal Measure FDefaultMeasure;
        [NonSerialized] internal bool FHideMeasureFlag;
        [NonSerialized] internal bool FHideMeasureModeFlag;
        [NonSerialized] internal string FErrorString = "";
        [NonSerialized] internal ICubeAddress FSortingAddress;
        internal Dictionary<ICubeAddress, string> fComments = new Dictionary<ICubeAddress, string>();
        internal int FValueSortedColumn = -1;
        internal ValueSortingDirection FSortingDirection = ValueSortingDirection.sdDescending;

        internal LayoutArea MeasureLayout => FGrid.FLayout.fMeasureLayout;

        internal MeasurePosition MeasurePosition => FGrid.FLayout.fMeasurePosition;

        internal void SetRowCount(int rowCount)
        {
            //DebugLogging.WriteLine("CellSet.SetRowCount({0})", rowCount);

            RowCount = rowCount;
            _IsRowVisible_Array = new bool?[RowCount];
        }

        internal void SetColumnCount(int columnCount)
        {
            //DebugLogging.WriteLine("CellSet.SetColumnCount({0})", columnCount);

            FColumnCount = columnCount;
            _IsColumnVisible_Array = new bool?[ColumnCount];
        }

        /// <summary>
        ///     Defines whether the specified column is visible in the hierarchy view page
        ///     mode.
        /// </summary>
        /// <returns>True if a given column is visible</returns>
        /// <param name="column">The grid column index</param>
        public bool IsColumnVisible(int column)
        {
            var res = _IsColumnVisible_Array[column];
            if (res.HasValue == false)
            {
                res = IsColumnVisible_Inner(column);
                _IsColumnVisible_Array[column] = res;
            }
#if DEBUG
#endif
            return res.Value;
        }

        private bool?[] _IsColumnVisible_Array;

        private bool IsColumnVisible_Inner(int column)
        {
            if (FFixedRows == 0 || column < FFixedColumns)
                return true;
            var o = (CellsetMember) FColMembersArray[column + (FFixedRows - 1) * FColumnCount];
            if (o == null)
                for (var i = FFixedRows - 2; i >= 0; i--)
                {
                    o = (CellsetMember) FRowMembersArray[column + i * FColumnCount];
                    if (o != null) break;
                }
            while (o != null)
            {
                if (!o.IsInFrame) return false;
                o = o.FParent;
            }
            return true;

            //
            // old code below !!!
            //

            //if ((FFixedRows == 0) || (column < FFixedColumns)) return true;
            //CellsetMember o = (CellsetMember)FColMembersArray[column + (FFixedRows - 1) * FColumnCount];
            //if (o == null)
            //{
            //    for (int i = FFixedRows - 2; i >= 0; i--)
            //    {
            //        o = (CellsetMember)FRowMembersArray[column + i * FColumnCount];
            //        if (o != null) break;
            //    }
            //}
            //while (o != null)
            //{
            //    if (!o.IsInFrame) return false;
            //    o = o.FParent;
            //}
            //return true;
        }

        /// <summary>
        ///     Defines whether the specified row is visible in the hierarchy view page
        ///     mode.
        /// </summary>
        /// <returns>True if a given row is visible</returns>
        /// <param name="row">The grid row index</param>
        public bool IsRowVisible(int row)
        {
            var res = _IsRowVisible_Array[row];
            if (res.HasValue == false)
            {
                res = IsRowVisibleInner(row);
                _IsRowVisible_Array[row] = res;
            }
#if DEBUG
#endif
            return res.Value;
        }

        private bool?[] _IsRowVisible_Array;

        private bool IsRowVisibleInner(int row)
        {
            if (_IsRowVisible_Array == null)
                return true;
            if (FFixedColumns == 0 || row < FFixedRows)
                return true;
            var o = (CellsetMember) FRowMembersArray[FFixedColumns - 1 + row * FFixedColumns];
            if (o == null)
                for (var i = FFixedColumns - 2; i >= 0; i--)
                {
                    o = (CellsetMember) FRowMembersArray[i + row * FFixedColumns];
                    if (o != null) break;
                }
            while (o != null)
            {
                if (!o.IsInFrame)
                    return false;
                o = o.FParent;
            }
            return true;
        }

        private void DoSetSiblingsOrder(CellSetMembers members)
        {
            var i = 0;
            foreach (var m in members)
            {
                if (m.FIsTotal || m.FIsPager)
                {
                    m.FSiblingsOrder = -1;
                }
                else
                {
                    m.FSiblingsOrder = i++;
                    DoSetSiblingsOrder(m.FChildren);
                }
                foreach (var a in m.FAttributes)
                    a.FSiblingsOrder = m.FSiblingsOrder;
            }
            members.FSiblingsCount = i;
        }

        internal CellSet(OlapControl aGrid)
        {
            DebugLogging.WriteLine("CellSet.ctor()");
            FGrid = aGrid;
        }

        private void FindSortedColumn()
        {
            FValueSortedColumn = -1;
            if (FGrid.FLayout.fMeasureLayout == LayoutArea.laRow &&
                FGrid.FLayout.fMeasurePosition == MeasurePosition.mpLast &&
                FDefaultMeasure == null)
                FSortingAddress = null;
            if (FSortingAddress == null) return;
            for (var i = FFixedColumns; i < FColumnCount; i++)
                if (((IMemberCell) Cells(i, FFixedRows - 1)).Address == FSortingAddress)
                {
                    FValueSortedColumn = i;
                    return;
                }
            FSortingAddress = null;
        }

        private void CreateEmptyCellSet(string AMessage)
        {
            FErrorString = AMessage;
            FFixedRows = 1;
            FFixedColumns = 1;
            SetColumnCount(1);
            SetRowCount(1);
        }

        public ICell PagedCells(int Column, int Row)
        {
            //if (FGrid.AllowPaging)
            //{
            AdjustPaging();
            return Cells(_adjustedCols[Column], _adjustedRows[Row]);
            //}
            //return Cells(Column, Row);
        }

        /// <summary>
        ///     Returns the cell at the intersection of a specified row and column of the OLAP
        ///     slice.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This function never returns the ICell interface instance directly but rather
        ///         one of its successors: either IDataCell, IMemberCell or ILevelCell interface. You
        ///         can learn the actual type of the returned interface by checking the ICell.CellType
        ///         property.
        ///     </para>
        ///     <para>
        ///         You can find out the total size of CellSet by checking the RowCount and
        ///         ColumnCount properties.
        ///     </para>
        /// </remarks>
        /// <param name="Column">The grid column index</param>
        /// <param name="Row">The grid row index</param>
        public ICell Cells(int Column, int Row)
        {
            var location = new Point(Column, Row);
            //var res = Cells_Creator(Column, Row); // old
            var res1 = Cells_StoreHouse(location);
            return res1;
        }

        private ICell Cells_StoreHouse(Point aLocation)
        {
            ICell res;
            if (_ICells.TryGetValue(aLocation, out res) == false)
            {
                res = Cells_Creator(aLocation.X, aLocation.Y);
                _ICells.Add(aLocation, res);
            }
            return res;
        }

        private readonly Dictionary<Point, ICell> _ICells = new Dictionary<Point, ICell>();

        internal ICell Cells_Creator(int Column, int Row)
        {
            if (Column == 0 && Row == 0 && FErrorString != "") return new ErrorCell(this, Column, Row);

            if (Column < 0 || Row < 0 || Column >= FColumnCount || Row >= RowCount)
                throw new ArgumentOutOfRangeException(
                    string.Format(RadarUtils.GetResStr("rsCellOutOfBounds"), Row, Column));

            if (Column < FFixedColumns && Row < FFixedRows)
            {
                var o = FColMembersArray[Column + Row * FColumnCount];
                if (o == null) return new ErrorCell(this, Column, Row);
                return new LevelCell(this, (CellsetLevel) o);
            }

            if (Column < FFixedColumns)
            {
                var o = FRowMembersArray[Column + Row * FFixedColumns];
                if (o == null) return new ErrorCell(this, Column, Row);
                return new MemberCell(this, (CellsetMember) o);
            }

            if (Row < FFixedRows)
            {
                var o = FColMembersArray[Column + Row * FColumnCount];
                if (o == null) return new ErrorCell(this, Column, Row);
                return new MemberCell(this, (CellsetMember) o);
            }

            // data members
            return DataCells(Column, Row);
        }

        internal virtual ICell DataCells(int Column, int Row)
        {
            return new DataCell(this, Row, Column);
        }

        /// <summary>
        ///     Returns the cell at the intersection of a specified row and column of the OLAP
        ///     slice.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This property never returns the ICell interface instance directly but rather
        ///         one of its successors: either IDataCell, IMemberCell or ILevelCell interface. You
        ///         can learn the actual type of the returned interface by checking the ICell.CellType
        ///         property.
        ///     </para>
        ///     <para>
        ///         You can find out the total size of CellSet by checking the RowCount and
        ///         ColumnCount properties.
        ///     </para>
        /// </remarks>
        public ICell this[int Column, int Row] => Cells(Column, Row);

        internal void RestoreAfterSerialization(OlapControl fGrid)
        {
            FGrid = fGrid;
            FRowMembers = new CellSetMembers();
            FColumnMembers = new CellSetMembers();
            FRowLevels = new List<CellsetLevel>();
            FColumnLevels = new List<CellsetLevel>();
            FVisibleMeasures = new List<Measure>();
        }

        internal virtual void ClearMembers()
        {
            DebugLogging.WriteLine("CellSet.ClearMembers()");

            FErrorString = "";
            FRowLevels.Clear();
            FColumnLevels.Clear();
            FRowMembers.Clear();
            FColumnMembers.Clear();

            currentColCellIndex = -1;
            currentRowCellIndex = -1;

            _adjustedCols = null;
            _adjustedRows = null;
            _adjustedColsHelper = null;
            _adjustedRowsHelper = null;
        }

        private CellsetLevel GetCellsetLevel(bool IsRowArea, Level level, InfoAttribute attr)
        {
            var sl = new SortedList<int, CellsetLevel>();

            IList<Hierarchy> lh = IsRowArea ? FGrid.FLayout.fRowAxis : FGrid.FLayout.fColumnAxis;

            if (IsRowArea)
                foreach (var ll in FRowLevels)
                {
                    if (ll.FLevel == level && ll.Attribute == attr)
                        return ll;
                    sl.Add(ll.GetInternalIndex(lh), ll);
                }
            else
                foreach (var ll in FColumnLevels)
                {
                    if (ll.FLevel == level && ll.Attribute == attr)
                        return ll;
                    sl.Add(ll.GetInternalIndex(lh), ll);
                }

            var r = new CellsetLevel(level);
            r.Attribute = attr;
            sl.Add(r.GetInternalIndex(lh), r);

            if (IsRowArea)
            {
                FRowLevels.Insert(sl.Values.IndexOf(r), r);
                RebuildLevelsIDs(FRowLevels);
            }
            else
            {
                FColumnLevels.Insert(sl.Values.IndexOf(r), r);
                RebuildLevelsIDs(FColumnLevels);
            }

            return r;
        }

        private int currentRowCellIndex;
        private int currentColCellIndex;

        private void CreateMembers(bool IsRowArea, CellsetMember ParentMember, CellsetLevel SourceLevel,
            Members MembersList)
        {
#if DEBUG
            if (SourceLevel.FLevel.DisplayName == "Tipo Produto")
            {
            }
#endif

            var InitHierID = 0;
            var a = new ICubeAddress(Grid);

            var ShowTotals = SourceLevel.FLevel.FHierarchy != null &&
                             SourceLevel.FLevel.FHierarchy.FTotalAppearance != TotalAppearance.taInvisible;
            if (ShowTotals && ParentMember != null) ShowTotals = !ParentMember.FIsTotal;

            if (ShowTotals && ParentMember != null && ParentMember.FMember != null)
                ShowTotals = !ParentMember.FMember.IsRaggedVirtual;

            var aTypicalBehavior = IsRowArea && Grid.HierarchiesDisplayMode == HierarchiesDisplayMode.TreeLike;

            var showEmpty = SourceLevel.FLevel.FHierarchy == null || SourceLevel.FLevel.FHierarchy.ShowEmptyLines;

            var InitLineID = string.Empty;
            long InitLineIdx = 0;
            var MeasureID = string.Empty;
            var ModeID = 0;
            Member P = null;
            CellSetMembers C;
            if (ParentMember == null)
            {
                if (FDefaultMeasure != null) MeasureID = FDefaultMeasure.UniqueName;
                C = IsRowArea ? FRowMembers : FColumnMembers;
            }
            else
            {
                InitLineID = ParentMember.FLineID;
                InitHierID = ParentMember.FHierID;
                InitLineIdx = ParentMember.FLineIdx;
                MeasureID = ParentMember.FMeasureID;
                ModeID = ParentMember.FModeID;
                C = ParentMember.FChildren;
                P = ParentMember.FMember;
            }
            var FirstTotalAdded = false;
            if (ShowTotals && SourceLevel.FLevel.FHierarchy.FTotalAppearance == TotalAppearance.taFirst
                || aTypicalBehavior && ParentMember != null)
                if (Grid.FEngine.HasCellValue(InitLineID, InitHierID, InitLineIdx, MeasureID, 0))
                    if (FGrid.IsAllowMember(a, P, true))
                    {
#if DEBUG
                        //Debug.Assert(ParentMember != null, "ParentMember != null");
                        if (ParentMember != null)
                        {
                            var gm = ParentMember.FMember as GroupMember;
                            if (gm != null)
                                if (gm.Children.Count > 0)
                                {
                                }
                        }

#endif
                        var csm = new CellsetMember(P, ParentMember, SourceLevel, IsRowArea)
                                  {
                                      FLineID = InitLineID,
                                      FHierID = InitHierID,
                                      FLineIdx = InitLineIdx,
                                      FMeasureID = MeasureID,
                                      FModeID = ModeID,
                                      FIsTotal = true,
                                      FAtypicalBehavior = aTypicalBehavior
                                  };
                        if (csm.FAtypicalBehavior && P != null)
                            csm.FIndent = Convert.ToByte(P.FDepth);

                        C.Add(csm);

                        CreateAttribures(IsRowArea, SourceLevel, a, P, csm);

                        if (SourceLevel.FLevel != Grid.Measures.Level)
                            InsertLastMeasure(csm);
                        if (csm.FChildren.Count == 0)
                            IncreaseIndex(IsRowArea);
                    }

            Member lastMember = null;
            if (MembersList.Count > 0)
            {
                a.FLineID = InitLineID;
                a.FHierID = InitHierID;
                a.FLineIdx = InitLineIdx;
                a.FMeasureID = MeasureID;
                a.FModeID = ModeID;
                a.SilentInit();
                var allowedMembers = showEmpty ? MembersList.ToArray() : Grid.FEngine.GetMembersList(a, MembersList);
                foreach (var M in allowedMembers)
                {
                    if (IsRowArea && currentRowCellIndex > FGrid.MaxRowsInGrid && FGrid.MaxRowsInGrid > 0)
                        break;
                    if (!IsRowArea && currentColCellIndex > FGrid.MaxColumnsInGrid && FGrid.MaxColumnsInGrid > 0)
                        break;
                    if (M.Parent != null && M.Parent != P)
                        continue;
                    if (!M.Visible)
                        continue;
                    if (M.FCubeMember != null && M.FCubeMember.FHideSystemGeneratedMember)
                        continue;
                    a.FLineID = InitLineID;
                    a.FHierID = InitHierID;
                    a.FLineIdx = InitLineIdx;
                    a.FMeasureID = MeasureID;
                    a.FModeID = ModeID;
                    a.SilentInit();
                    a.AddMember(M);
                    if (!showEmpty && !Grid.FEngine.HasCellValue(a))
                        continue;
                    if (!FGrid.IsAllowMember(a, M, false))
                        continue;
                    var csm = new CellsetMember(M, ParentMember, SourceLevel, IsRowArea)
                              {
                                  FLineID = a.FLineID,
                                  FHierID = a.FHierID,
                                  FLineIdx = a.FLineIdx,
                                  FMeasureID = a.FMeasureID,
                                  FModeID = a.FModeID,
                                  FAtypicalBehavior = IsRowArea && SourceLevel.FLevel.FDepth >= 0 &&
                                                      Grid.HierarchiesDisplayMode == HierarchiesDisplayMode.TreeLike
                              };

                    if (csm.FAtypicalBehavior)
                        csm.FIndent = Convert.ToByte(M.FDepth);

                    C.Add(csm);

                    var expandMeasure = M.FMemberType == MemberType.mtMeasure &&
                                        MeasurePosition == MeasurePosition.mpFirst;
                    if (M.FMemberType == MemberType.mtMeasure && !expandMeasure)
                    {
                        var mm = Grid.Measures[M.UniqueName];
                        expandMeasure = !(mm.ShowModes.CountVisible == 0 ||
                                          mm.ShowModes.CountVisible == 1 && mm.ShowModes[0].Visible);
                    }

                    CreateAttribures(IsRowArea, SourceLevel, a, M, csm);
#if DEBUG
                    if (csm.DisplayName == "1996")
                    {
                    }
#endif

                    var expandStatus = DrillAction.GetDrilledAction(csm.FMember, FDrillActions, csm);
                    if (M.MemberType == MemberType.mtMeasure)
                    {
                        if (!FHideMeasureModeFlag)
                        {
                            expandStatus = PossibleDrillActions.esParentChild;
                        }
                        else
                        {
                            if (MeasurePosition == MeasurePosition.mpFirst)
                                expandStatus = PossibleDrillActions.esNextHierarchy;
                        }
                    }
                    else
                    {
                        if (M.MemberType == MemberType.mtMeasureMode && MeasurePosition == MeasurePosition.mpFirst)
                            expandStatus = PossibleDrillActions.esNextHierarchy;
                    }
                    if (expandStatus == PossibleDrillActions.esParentChild)
                    {
                        if (!csm.FAtypicalBehavior) SourceLevel.FDepth = Math.Max(SourceLevel.FDepth, M.FDepth + 2);
                        CreateMembers(IsRowArea, csm, SourceLevel, M.Children);
                        csm.FExpandStatus = PossibleDrillActions.esParentChild;
                        //if (M.MemberType != MemberType.mtMeasure)
                        //    FNewlyOpenNodes.Add(CreateOpenNodesString(CM), PossibleDrillActions.esParentChild);
                    }
                    Level NewLevel = null;
#if DEBUG
                    if (SourceLevel.FLevel.DisplayName == "Year")
                    {
                    }
#endif

                    if (expandStatus == PossibleDrillActions.esNextLevel)
                    {
                        if (SourceLevel.FLevel.FHierarchy.FLevels.Count <= SourceLevel.FLevel.fIndex + 1)
                            continue;

                        NewLevel = SourceLevel.FLevel.FHierarchy.FLevels[SourceLevel.FLevel.fIndex + 1];
                        //?NewLevel.Initialize();
                        //if (NewLevel.Members.Count == 0) continue;
                        var newCellSetLevel = GetCellsetLevel(IsRowArea,
                            NewLevel, null);

#if DEBUG
                        if (csm != null && csm.DisplayName == "1996")
                        {
                        }
#endif

                        CreateMembers(IsRowArea, csm, newCellSetLevel, NewLevel.Members);
                        csm.FExpandStatus = PossibleDrillActions.esNextLevel;
                        //FNewlyOpenNodes.Add(CreateOpenNodesString(CM), PossibleDrillActions.esNextLevel);
                    }

                    if (expandStatus == PossibleDrillActions.esNextHierarchy)
                    {
                        if (IsRowArea)
                        {
                            var k = SourceLevel.FLevel.FHierarchy == null
                                ? -1
                                : Grid.FLayout.fRowAxis.IndexOf(SourceLevel.FLevel.FHierarchy);
                            if (Grid.FLayout.fRowAxis.Count == k + 1) continue;
                            NewLevel = Grid.FLayout.fRowAxis[k + 1].FLevels[0];
                        }
                        else
                        {
                            var k = SourceLevel.FLevel.FHierarchy == null
                                ? -1
                                : Grid.FLayout.fColumnAxis.IndexOf(SourceLevel.FLevel.FHierarchy);
                            if (Grid.FLayout.fColumnAxis.Count == k + 1) continue;
                            NewLevel = Grid.FLayout.fColumnAxis[k + 1].FLevels[0];
                        }
                        var newCellSetLevel = GetCellsetLevel(IsRowArea, NewLevel, null);
                        var members = NewLevel.Members;
                        var gm = csm.FMember as GroupMember;
                        if (gm != null)
                            members = gm.Children;

                        CreateMembers(IsRowArea, csm, newCellSetLevel, members);
                        csm.FExpandStatus = PossibleDrillActions.esNextHierarchy;
                        //if ((M.MemberType != MemberType.mtMeasure) && (M.MemberType != MemberType.mtMeasureMode))
                        //    FNewlyOpenNodes.Add(CreateOpenNodesString(CM), PossibleDrillActions.esNextHierarchy);
                    }
                    //                }

                    if (csm.FMember.FMemberType != MemberType.mtMeasure &&
                        csm.FMember.FMemberType != MemberType.mtMeasureMode)
                        InsertLastMeasure(csm);

                    if (csm.FChildren.Count == 0)
                        IncreaseIndex(IsRowArea);
                }
#if DEBUG
                if (ParentMember != null && ParentMember.DisplayName == "Laura")
                {
                }
#endif
            }

            if (SourceLevel.FLevel.PagerSettings.AllowPaging)
            {
                var count = C.Count;
                if (FirstTotalAdded)
                    count--;
                if (count > SourceLevel.FLevel.PagerSettings.LinesInPage)
                {
                    var cm = new CellsetMember(P, ParentMember, SourceLevel, IsRowArea)
                             {
                                 FIsPager = true
                             };
                    C.Add(cm);
                    IncreaseIndex(IsRowArea);

                    cm.FAtypicalBehavior = IsRowArea
                                           && SourceLevel.FLevel.FDepth >= 0
                                           && Grid.HierarchiesDisplayMode == HierarchiesDisplayMode.TreeLike;
                    if (lastMember != null && cm.FAtypicalBehavior)
                        cm.FIndent = Convert.ToByte(lastMember.FDepth);
                }
            }

            if (ShowTotals
                && SourceLevel.FLevel.FHierarchy.FTotalAppearance == TotalAppearance.taLast
                && (!aTypicalBehavior || ParentMember == null))
                if (Grid.FEngine.HasCellValue(InitLineID, InitHierID, InitLineIdx, MeasureID, 0))
                    if (Grid.IsAllowMember(a, P, true))
                    {
                        var cm = new CellsetMember(P, ParentMember, SourceLevel, IsRowArea)
                                 {
                                     FLineID = InitLineID,
                                     FHierID = InitHierID,
                                     FLineIdx = InitLineIdx,
                                     FMeasureID = MeasureID,
                                     FModeID = ModeID,
                                     FIsTotal = true,
                                     FAtypicalBehavior = aTypicalBehavior
                                 };
                        C.Add(cm);

                        CreateAttribures(IsRowArea, SourceLevel, a, P, cm);
                        InsertLastMeasure(cm);

                        if (cm.FChildren.Count == 0)
                            IncreaseIndex(IsRowArea);

                        //
                        //
                        //

                        //cm = new CellsetMember(P, ParentMember, SourceLevel, IsRowArea)
                        //{
                        //    FLineID = InitLineID,
                        //    FHierID = InitHierID,
                        //    FLineIdx = InitLineIdx,
                        //    FMeasureID = MeasureID,
                        //    FModeID = ModeID,
                        //    FIsTotal = true,
                        //    FAtypicalBehavior = aTypicalBehavior
                        //};
                        //C.Add(cm);

                        //CreateAttribures(IsRowArea, SourceLevel, a, P, cm);
                        //IncreaseIndex(IsRowArea);

                        //
                        //
                        //
                    }
        }

        private void IncreaseIndex(bool isRowArea)
        {
            if (isRowArea)
                currentRowCellIndex++;
            else
                currentColCellIndex++;
        }

        private void CreateAttribures(bool IsRowArea, CellsetLevel SourceLevel, ICubeAddress a, Member M,
            CellsetMember CM)
        {
            var cm2 = CM;
            var NewCellSetLevel2 = SourceLevel;

            if (SourceLevel.FLevel.CubeLevel != null && Grid.HierarchiesDisplayMode == HierarchiesDisplayMode.TableLike)
                foreach (var ti in SourceLevel.FLevel.CubeLevel.InfoAttributes
                    .Where(item => item.IsDisplayModeAsColumn))
                {
                    var newCellSetLevel = GetCellsetLevel(IsRowArea, SourceLevel.FLevel, ti);
                    NewCellSetLevel2 = newCellSetLevel;
                    var cm21 = new CellsetMember(M, CM, newCellSetLevel, IsRowArea);
                    CM.FAttributes.Add(cm21);
                    cm2 = cm21;
                    cm2.FLineID = a.FLineID;
                    cm2.FHierID = a.FHierID;
                    cm2.FLineIdx = a.FLineIdx;
                    cm2.FMeasureID = a.FMeasureID;
                    cm2.FModeID = a.FModeID;
                    cm2.Attribute = ti;
                    cm2.FAtypicalBehavior = SourceLevel.FLevel.FHierarchy != null && IsRowArea &&
                                            SourceLevel.FLevel.FDepth >= 0 &&
                                            Grid.HierarchiesDisplayMode == HierarchiesDisplayMode.TreeLike;
                    cm2.FIsTotal = false;
                }
        }

        private void InsertLastMeasure(CellsetMember CM)
        {
            if (MeasurePosition == MeasurePosition.mpLast &&
                (!FHideMeasureFlag || FVisibleMeasures.Count > 1)
                && (CM.FMember == null || CM.FMember.FMemberType != MemberType.mtMeasure)
                && CM.FChildren.Count == 0)
            {
                if (CM.FIsRow && MeasureLayout == LayoutArea.laRow)
                {
                    var NewCellSetLevel = FRowLevels[FRowLevels.Count - 1];
                    if (NewCellSetLevel.FLevel != FGrid.fMeasures.FLevel)
                    {
                        NewCellSetLevel = new CellsetLevel(FGrid.fMeasures.FLevel);
                        FRowLevels.Add(NewCellSetLevel);
                    }
                    CreateMembers(CM.FIsRow, CM, NewCellSetLevel,
                        FGrid.fMeasures.FLevel.Members);
                }
                if (!CM.FIsRow && MeasureLayout == LayoutArea.laColumn)
                {
                    var NewCellSetLevel = FColumnLevels[FColumnLevels.Count - 1];
                    if (NewCellSetLevel.FLevel != FGrid.fMeasures.FLevel)
                    {
                        NewCellSetLevel = new CellsetLevel(FGrid.fMeasures.FLevel);
                        FColumnLevels.Add(NewCellSetLevel);
                    }
                    CreateMembers(CM.FIsRow, CM, NewCellSetLevel,
                        FGrid.fMeasures.FLevel.Members);
                }
            }
        }

        private void RebuildLevelsIDs(List<CellsetLevel> Levels)
        {
            for (var i = 0; i < Levels.Count; i++) Levels[i].fID = i;
        }

        private void FillSpanInRows(CellsetMember N)
        {
            // FRowSpan, FColSpan, FStartColumn
            if (N.FLevel != null)
            {
                N.FIndent += N.FLevel.FIndent;
                if (N.FIsPager && N.FMember != null && N.FIsRow) N.FIndent++;
            }
            if (N.FChildren.Count == 0)
            {
                N.FRowSpan = 1;
                N.FColSpan = N.FLevel == null ? FFixedColumns : FFixedColumns - N.FLevel.FDivingLevel;
                if (N.FLevel != null && N.FLevel.FDepth > 1)
                    if (N.FMember == null)
                    {
                        if (!(N.FParent == null || N.FParent.FLevel != N.FLevel))
                            N.FColSpan -= N.FMember.FParent.FDepth + 1;
                    }
                    else
                    {
                        N.FColSpan -= N.FMember.FDepth;
                        if (N.FIsTotal && N.FLevel.FLevel == N.FMember.FLevel) N.FColSpan--;
                    }
                N.FStartColumn = FFixedColumns - N.FColSpan;
                if (N.FAttributes.Count > 0)
                    N.FColSpan = 1;
            }
            else
            {
                N.FRowSpan = 0;
                foreach (var n in N.FChildren)
                {
                    FillSpanInRows(n);
                    N.FRowSpan += n.FRowSpan;
                }
                N.FColSpan = FFixedColumns - N.FLevel.FDivingLevel;
                if (N.FLevel.FDepth > 1 && N.FMember != null) N.FColSpan -= N.FMember.FDepth;
                N.FStartColumn = FFixedColumns - N.FColSpan;
                if (N.FChildren[0].FIsTotal && N.FChildren.Count > 1)
                    N.FColSpan = N.FChildren[1].FStartColumn - N.FStartColumn;
                else
                    N.FColSpan = N.FChildren[0].FStartColumn - N.FStartColumn;
                N.FColSpan -= N.FAttributes.Count;
                foreach (var n in N.FChildren)
                    if ((n.FIsPager || n.FIsTotal) && !n.FAtypicalBehavior)
                    {
                        var ii = N.FStartColumn + N.FColSpan - n.FStartColumn;
                        n.FColSpan -= ii;
                        n.FStartColumn += ii;
                    }
            }
            var i = N.FStartColumn;
            foreach (var n in N.FAttributes)
            {
                n.FStartColumn = ++i;
                n.FColSpan = Math.Min(1, N.FColSpan);
                n.FRowSpan = N.FRowSpan;
                if (N.FChildren.Count == 0 && N.FAttributes.Last() == n)
                    n.FColSpan = FFixedColumns - n.FStartColumn;
            }
        }

        private void FillSpanInColumns(CellsetMember N)
        {
            if (N.FChildren.Count == 0)
            {
                N.FColSpan = 1;
                N.FRowSpan = N.FLevel == null ? FFixedRows : FFixedRows - N.FLevel.FDivingLevel;
                if (N.FLevel != null && N.FLevel.FDepth > 1)
                    if (N.FMember == null)
                    {
                        if (!(N.FParent == null || N.FParent.FLevel != N.FLevel))
                            N.FRowSpan -= N.FMember.FParent.FDepth + 1;
                    }
                    else
                    {
                        N.FRowSpan -= N.FMember.FDepth;
                        if (N.FIsTotal && N.FLevel.FLevel == N.FMember.FLevel)
                            N.FRowSpan--;
                    }
                N.FStartRow = FFixedRows - N.FRowSpan;
                if (N.FAttributes.Count > 0)
                    N.FRowSpan = 1;
            }
            else
            {
                N.FColSpan = 0;
                foreach (var n in N.FChildren)
                {
                    FillSpanInColumns(n);
                    N.FColSpan += n.FColSpan;
                }
                N.FRowSpan = FFixedRows - N.FLevel.FDivingLevel;
                if (N.FLevel.FDepth > 1 && N.FMember != null) N.FRowSpan -= N.FMember.FDepth;
                N.FStartRow = FFixedRows - N.FRowSpan;
                if (N.FChildren[0].FIsTotal && N.FChildren.Count > 1)
                    N.FRowSpan = N.FChildren[1].FStartRow - N.FStartRow;
                else
                    N.FRowSpan = N.FChildren[0].FStartRow - N.FStartRow;
                N.FRowSpan -= N.FAttributes.Count;
                foreach (var n in N.FChildren)
                    if (n.FIsPager || n.FIsTotal)
                    {
                        var ii = N.FStartRow + N.FRowSpan - n.FStartRow;
                        n.FRowSpan -= ii;
                        n.FStartRow += ii;
                    }
            }
            var i = N.FStartRow;
            foreach (var n in N.FAttributes)
            {
                n.FStartRow = ++i;
                n.FRowSpan = 1;
                n.FColSpan = N.FColSpan;
                if (N.FChildren.Count == 0 && N.FAttributes.Last() == n)
                    n.FRowSpan = FFixedRows - n.FStartRow;
            }
        }

        private void SetStartsInRows(CellsetMember N, int Index)
        {
            N.FStartRow = Index;
            var j = Index;
            foreach (var m in N.FChildren)
            {
                SetStartsInRows(m, j);
                j += m.FRowSpan;
            }
            foreach (var m in N.FAttributes)
                m.FStartRow = N.FStartRow;
        }

        private void SetStartsInColumns(CellsetMember N, int Index)
        {
            N.FStartColumn = Index;
            var j = Index;
            foreach (var m in N.FChildren)
            {
                SetStartsInColumns(m, j);
                j += m.FColSpan;
            }
            foreach (var m in N.FAttributes)
                m.FStartColumn = N.FStartColumn;
        }

        private void SetNodeInRows(CellsetMember N)
        {
            for (var j = 0; j < N.FRowSpan; j++)
            for (var k = 0; k < N.FColSpan; k++)
                FRowMembersArray[N.FStartColumn + k + (N.FStartRow + j) * FFixedColumns] = N;
            foreach (var m in N.FChildren) SetNodeInRows(m);
            foreach (var m in N.FAttributes) SetNodeInRows(m);
        }

        private void SetNodeInColumns(CellsetMember N)
        {
            for (var j = 0; j < N.FRowSpan; j++)
            for (var k = 0; k < N.FColSpan; k++)
                FColMembersArray[N.FStartColumn + k + (N.FStartRow + j) * FColumnCount] = N;
            foreach (var m in N.FChildren) SetNodeInColumns(m);
            foreach (var m in N.FAttributes) SetNodeInColumns(m);
        }

        internal void CreateSpans()
        {
            _adjustedCols = null;
            _adjustedRows = null;
            _adjustedRowsHelper = null;
            _adjustedColsHelper = null;

            FFixedRows = 0;
            foreach (var l in FColumnLevels)
            {
                l.FDivingLevel = FFixedRows;
                FFixedRows += l.FDepth;
            }

            var s = 0;
            switch (Grid.HierarchiesDisplayMode)
            {
                case HierarchiesDisplayMode.TreeLike:

                    FFixedColumns = 1;
                    foreach (var l in FRowLevels)
                    {
                        l.FDivingLevel = s;
                        l.FIndent = Convert.ToByte(l.FDivingLevel);
                        var mm = l.AllChildren().Where(item => item.FMember != null).ToList();
                        if (mm.Count > 0)
                            s += mm.Max(item => item.FMember.FDepth) + 1;
                        else
                            s++;
                        l.FDivingLevel = 0;
                        l.FRowSpan = 1;
                        l.FColSpan = 1;
                        l.FStartRow = FFixedRows++;
                        l.FStartCol = 0;
                    }
                    break;
                case HierarchiesDisplayMode.TableLike:
                    FFixedColumns = 0;
                    if (FRowLevels.Count > 0)
                        FFixedRows++;
                    foreach (var l in FRowLevels)
                    {
                        l.FDivingLevel = FFixedColumns;
                        FFixedColumns += l.FDepth;
                        l.FRowSpan = 1;
                        l.FColSpan = l.FDepth;
                        l.FStartRow = FFixedRows - 1;
                        l.FStartCol = s;
                        s += l.FDepth;
                    }
                    break;
            }

            foreach (var m in FColumnMembers)
                FillSpanInColumns(m);

            foreach (var m in FRowMembers)
                FillSpanInRows(m);

            if (FFixedColumns == 0) FFixedColumns = 1;
            s = 0;
            foreach (var l in FColumnLevels)
            {
                l.FRowSpan = l.FDepth;
                l.FColSpan = FFixedColumns;
                l.FStartRow = s;
                l.FStartCol = 0;
                s += l.FDepth;
            }

            s = FFixedRows;
            foreach (var m in FRowMembers)
            {
                SetStartsInRows(m, s);
                s += m.FRowSpan;
            }
            SetRowCount(s);
            if (RowCount == 1
                && FRowLevels.Count == 0
                && FColumnLevels.Count == 1
                && FColumnLevels[0].FLevel.LevelType == HierarchyDataType.htMeasures)
                SetRowCount(2);

            s = FFixedColumns;
            foreach (var m in FColumnMembers)
            {
                SetStartsInColumns(m, s);
                s += m.FColSpan;
            }
            SetColumnCount(s);

            FRowMembersArray = new object[FFixedColumns * RowCount];
            foreach (var CSL in FColumnLevels)
                for (var j = 0; j < CSL.FRowSpan; j++)
                for (var k = 0; k < CSL.FColSpan; k++)
                    FRowMembersArray[CSL.FStartCol + k + (CSL.FStartRow + j) * FFixedColumns] = CSL;
            foreach (var CSL in FRowLevels)
                for (var j = 0; j < CSL.FRowSpan; j++)
                for (var k = 0; k < CSL.FColSpan; k++)
                    FRowMembersArray[CSL.FStartCol + k + (CSL.FStartRow + j) * FFixedColumns] = CSL;
            foreach (var m in FRowMembers) SetNodeInRows(m);

            FColMembersArray = new object[FFixedRows * FColumnCount];
            foreach (var CSL in FColumnLevels)
                for (var j = 0; j < CSL.FRowSpan; j++)
                for (var k = 0; k < CSL.FColSpan; k++)
                    FColMembersArray[CSL.FStartCol + k + (CSL.FStartRow + j) * FColumnCount] = CSL;
            foreach (var CSL in FRowLevels)
                for (var j = 0; j < CSL.FRowSpan; j++)
                for (var k = 0; k < CSL.FColSpan; k++)
                    FColMembersArray[CSL.FStartCol + k + (CSL.FStartRow + j) * FColumnCount] = CSL;
            foreach (var m in FColumnMembers) SetNodeInColumns(m);

            if (FRowMembers.Count == 0 && FColumnMembers.Count == 0 && FGrid.CellsetMode == CellsetMode.cmChart)
            {
                FFixedColumns = 0;
                FFixedRows = 0;
                SetRowCount(1);
                SetColumnCount(1);
            }

            try
            {
                if (FSortingAddress == null) return;
                FindSortedColumn();
                if (FSortingAddress == null)
                {
                    Rebuild();
                    return;
                }
                if (!DoSortByValue()) return;

                s = FFixedRows;
                foreach (var m in FRowMembers)
                {
                    SetStartsInRows(m, s);
                    s += m.FRowSpan;
                }
                SetRowCount(s);

                FRowMembersArray = new object[FFixedColumns * RowCount];
                foreach (var CSL in FColumnLevels)
                    for (var j = 0; j < CSL.FRowSpan; j++)
                    for (var k = 0; k < CSL.FColSpan; k++)
                        FRowMembersArray[CSL.FStartCol + k + (CSL.FStartRow + j) * FFixedColumns] = CSL;
                foreach (var CSL in FRowLevels)
                    for (var j = 0; j < CSL.FRowSpan; j++)
                    for (var k = 0; k < CSL.FColSpan; k++)
                        FRowMembersArray[CSL.FStartCol + k + (CSL.FStartRow + j) * FFixedColumns] = CSL;
                foreach (var m in FRowMembers) SetNodeInRows(m);
            }
            finally
            {
                DoSetSiblingsOrder(FRowMembers);
                DoSetSiblingsOrder(FColumnMembers);
            }
        }

        private class ValueComparer : IComparer<CellsetMember>
        {
            private readonly bool fAsc;
            private readonly CellSet fCellset;
            private readonly List<Member> fColMembers = new List<Member>();
            private readonly ICubeAddress mx;
            private readonly Dictionary<CellsetMember, object> values = new Dictionary<CellsetMember, object>();
            private List<Member> fRowMembers;

            internal ValueComparer(CellSet ACellset, bool IsAscendent)
            {
                fCellset = ACellset;
                fAsc = IsAscendent;
                var mc = (IMemberCell) fCellset.Cells(fCellset.FValueSortedColumn, fCellset.FFixedRows - 1);
                var mc1 = mc.HierarchyMemberCell;
                if (mc1 != null)
                    for (var i = 0; i < mc1.SiblingsCount; i++)
                    {
                        var member = mc1.Siblings(i);
                        var M = member.Member;
                        if (M != null) fColMembers.Add(M);
                    }
                mx = mc.Address;
            }

            #region IComparer<TCellsetMember> Members

            private object RetrieveValue(CellsetMember x)
            {
                object Result;
                if (fRowMembers == null)
                {
                    fRowMembers = new List<Member>();
                    IMemberCell mc = new MemberCell(fCellset, x);
                    var mc1 = mc.HierarchyMemberCell;
                    if (mc1 != null)
                        for (var i = 0; i < mc1.SiblingsCount; i++)
                        {
                            var member = mc1.Siblings(i);
                            var M = member.Member;
                            if (M != null) fRowMembers.Add(M);
                        }
                }
                if (values.TryGetValue(x, out Result)) return Result;
                fCellset.FGrid.FEngine.GetValueForSort(x, mx, fColMembers, fRowMembers, out Result);
                values.Add(x, Result);
                return Result;
            }

            public int Compare(CellsetMember x, CellsetMember y)
            {
                if (x == y) return 0;
                if (x.FMember.MemberType != MemberType.mtMeasure && x.FMember.FMemberType != MemberType.mtMeasureMode)
                {
                    var dx = RetrieveValue(x);
                    var dy = RetrieveValue(y);
                    if (dx == null && dy != null) return 1;
                    if (dx != null && dy == null) return -1;
                    if (dx is IComparable)
                        try
                        {
                            var c = dx as IComparable;
                            if (fAsc)
                            {
                                var i = c.CompareTo(dy);
                                if (i != 0)
                                    return i;
                                return ((IComparable) x.DisplayName).CompareTo(y.DisplayName);
                            }
                            else
                            {
                                var i = c.CompareTo(dy);
                                if (i != 0)
                                    return -i;
                                return -((IComparable) x.DisplayName).CompareTo(y.DisplayName);
                            }
                        }
                        catch
                        {
                            ;
                        }
                }
                var ix = x.FMember.SiblingsList.IndexOf(x.FMember);
                var iy = y.FMember.SiblingsList.IndexOf(y.FMember);
                return ix - iy;
            }

            #endregion
        }

        private void SortByValueIterator(List<CellsetMember> ms, bool IsAsc)
        {
            if (ms.Count == 0) return;
            var vc = new ValueComparer(this, IsAsc);
            if (ms[0].FIsTotal)
            {
                if (ms[ms.Count - 1].FIsPager)
                    ms.Sort(1, ms.Count - 2, vc);
                else
                    ms.Sort(1, ms.Count - 1, vc);
            }
            else
            {
                if (ms[ms.Count - 1].FIsTotal)
                {
                    if (ms[ms.Count - 2].FIsPager)
                        ms.Sort(0, ms.Count - 2, vc);
                    else
                        ms.Sort(0, ms.Count - 1, vc);
                }
                else
                {
                    if (ms[ms.Count - 1].FIsPager)
                        ms.Sort(0, ms.Count - 1, vc);
                    else
                        ms.Sort(vc);
                }
            }
            foreach (var m in ms) SortByValueIterator(m.FChildren, IsAsc);
        }

        private bool DoSortByValue()
        {
            if (FValueSortedColumn < 0) return false;
            DoSetSiblingsOrder(FRowMembers);
            DoSetSiblingsOrder(FColumnMembers);
            SortByValueIterator(FRowMembers, FSortingDirection == ValueSortingDirection.sdAscending);
            return true;
        }

        private string CreateOpenNodesString(CellsetMember M)
        {
            while (M.Attribute != null)
                M = M.FParent;
            StringBuilder sb;
            if (FVisibleMeasures.Count == 1)
                sb = new StringBuilder("-1");
            else
                sb = new StringBuilder(M.FMeasureID);
            sb.Append(':');
            while (M != null)
            {
                if (M.FMember == null)
                {
                    sb.Append("null.");
                }
                else if (M.FMember.MemberType != MemberType.mtMeasure)
                {
                    sb.Append(M.FMember.UniqueName);
                    sb.Append('.');
                }
                M = M.FParent;
            }
            return sb.ToString();
        }

        /// <summary>
        ///     <para>
        ///         Rebuilds the contents of the current Cellset. As a rule, it's not required to
        ///         call this method from the application code. It is called by using the RadarCube
        ///         core.
        ///     </para>
        /// </summary>
        public void Rebuild()
        {
            if (FGrid == null || FGrid.IsDisposed2)
                return;
            DebugLogging.WriteLine("CellSet.Rebuild() // FGrid={0}", FGrid.GetType().Name);

            _ICells.Clear();
            if (FGrid.DelayPivoting)
                return;

            try
            {
                if (FGrid.Mode == OlapGridMode.gmQueryResult)
                {
                    FGrid.Cube.RestoreQueryResult(FGrid);
                    return;
                }
                FGrid.IncrementUpdateCounter();
                FGrid.FLayout.CheckExpandedLevels();
                DoRetrieveMembers();

                OptimizeDrills();

                ClearMembers();
                FGrid.MakeFiltered2List();
                foreach (var H in FGrid.FLayout.fRowAxis)
                    H.DefaultInit();
                foreach (var H in FGrid.FLayout.fColumnAxis)
                    H.DefaultInit();
                foreach (var H in FGrid.FLayout.fPageAxis)
                    H.DefaultInit();

                if (FGrid.CellsetMode == CellsetMode.cmGrid)
                {
                    foreach (var l in FGrid.FLayout.fColumnLevels)
                        CheckLevelForFetchedParents(l);

                    foreach (var l in FGrid.FLayout.fRowLevels)
                        CheckLevelForFetchedParents(l);

                    foreach (var h in FGrid.FLayout.fPageAxis)
                    foreach (var l in h.Levels)
                        CheckLevelForFetchedParents(l);
                }

                FGrid.FEngine.ClearRequestMap();

                CreateRequestMap();

                if (!FGrid.DeferLayoutUpdate)
                    FGrid.FEngine.DoRetrieveData();

                if (FGrid.CellsetMode == CellsetMode.cmChart)
                {
                    RebuildChart();
                    latestState = FGrid.Serializer.XMLString;
                    return;
                }
                RebuildGrid();
                
                FHideMeasureFlag = FGrid.FLayout.fHideMeasureIfPossible;
                FHideMeasureModeFlag = FGrid.FLayout.fHideMeasureModesIfPossible;
                if (FHideMeasureModeFlag)
                    for (var i = 0; i < Grid.Measures.Count; i++)
                    {
                        var m = Grid.Measures[i];
                        if (m.Visible && (m.ShowModes.CountVisible > 1 || !m.ShowModes[0].Visible))
                        {
                            FHideMeasureModeFlag = false;
                            break;
                        }
                    }

                if (!FHideMeasureModeFlag)
                    FHideMeasureFlag = false;

                if (FGrid.FLayout.fRowAxis.Count == 0)
                    FHideMeasureFlag = false;
                if (Grid.FLayout.fColumnAxis.Count == 0)
                    FHideMeasureFlag = false;

                FGrid.fMeasures.InitMeasures();
                FGrid.fMeasures.GetVisibleMeasures(FVisibleMeasures);

                if (FVisibleMeasures.Count != 1)
                    FDefaultMeasure = null;
                else
                    foreach (var m in FGrid.fMeasures)
                        if (m.Visible)
                        {
                            FDefaultMeasure = m;
                            break;
                        }

                currentRowCellIndex = 0;
                currentColCellIndex = 0;
                if (MeasurePosition == MeasurePosition.mpFirst &&
                    (FVisibleMeasures.Count > 1 || !FHideMeasureFlag) && FVisibleMeasures.Count > 0)
                {
                    var L = new CellsetLevel(FGrid.fMeasures.FLevel);
                    if (MeasureLayout == LayoutArea.laRow)
                    {
                        FRowLevels.Add(L);
                        CreateMembers(true, null, L, L.FLevel.Members);
                    }
                    else
                    {
                        FColumnLevels.Add(L);
                        CreateMembers(false, null, L, L.FLevel.Members);
                    }
                }

                if (Grid.FLayout.fRowAxis.Count > 0 && FRowLevels.Count == 0)
                {
                    var L = new CellsetLevel(Grid.FLayout.fRowAxis[0].FLevels[0]);
                    FRowLevels.Add(L);
                    CreateMembers(true, null, L, L.FLevel.Members);
                }
                if (Grid.FLayout.fColumnAxis.Count > 0 && FColumnLevels.Count == 0)
                {
                    var L = new CellsetLevel(Grid.FLayout.fColumnAxis[0].FLevels[0]);
                    FColumnLevels.Add(L);
                    CreateMembers(false, null, L, L.FLevel.Members);
                }

                if (MeasurePosition == MeasurePosition.mpLast
                    && (FVisibleMeasures.Count > 1 || !FHideMeasureFlag) && FVisibleMeasures.Count > 0)
                    switch (MeasureLayout)
                    {
                        case LayoutArea.laRow:
                            if (Grid.FLayout.fRowAxis.Count == 0)
                            {
                                var L = new CellsetLevel(FGrid.fMeasures.FLevel);
                                FRowLevels.Add(L);
                                CreateMembers(true, null, L, L.FLevel.Members);
                            }
                            break;
                        case LayoutArea.laColumn:
                            if (Grid.FLayout.fColumnAxis.Count == 0)
                            {
                                var L = new CellsetLevel(FGrid.fMeasures.FLevel);
                                FColumnLevels.Add(L);
                                CreateMembers(false, null, L, L.FLevel.Members);
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                if (FColumnMembers.Count == 0 && FRowMembers.Count > 0)
                {
                    var dummy = new CellsetMember(null, null, null, false);
                    FColumnMembers.Add(dummy);
                }

                if (FRowMembers.Count == 0 && FColumnMembers.Count > 0)
                {
                    var dummy = new CellsetMember(null, null, null, true);
                    FRowMembers.Add(dummy);
                }
                CreateSpans();
                FGrid.DecrementUpdateCounter();
                if (FGrid.IsUpdating == false)
                    FGrid.EndChange(GridEventType.geRebuildNodes);

                RefreshLastState();
            }
            catch (Exception ex)
            {
                FGrid.ErrorCatchHandler(ex, latestState);
            }
        }

        private void RefreshLastState()
        {
            //if (!Grid.IsUpdating && Grid.Active)
            //    latestState = FGrid.Serializer.XMLString;
        }

        //
        protected virtual void CheckLevelForFetchedParents(Level l)
        {
            //Debug.Assert(l != null, "l != null");
            //Debug.Assert(l.CubeLevel != null, "l.CubeLevel != null");

            //if (!l.CubeLevel.IsFullyFetched)
            //    for (var i = 0; i <= l.Index; i++)
            //    {
            //        var ppl = l.Hierarchy.Levels[i];
            //        if (!ppl.CubeLevel.IsFullyFetched)
            //        {
            //            //FGrid.Cube.RetrieveMembers(FGrid, ppl);

            //            ////bool dummy;
            //            ////FGrid.Cube.RetrieveMembersPartial(FGrid, ppl, -1, -1, null, out dummy);
            //        }
            //    }
        }

        private Updater _UpdaterOptimizeDrills;

        internal Updater UpdaterOptimizeDrills
        {
            get
            {
                if (_UpdaterOptimizeDrills == null)
                {
                    _UpdaterOptimizeDrills = new Updater(this);
                    _UpdaterOptimizeDrills.UpdateEnd += UpdaterOptimizeDrills_UpdateEnd;
                }
                return _UpdaterOptimizeDrills;
            }
        }

        private void UpdaterOptimizeDrills_UpdateEnd(object sender, EventArgs e)
        {
            OptimizeDrills();
        }

        internal void OptimizeDrills()
        {
            //DebugLogging.WriteLine("CellSet.OptimizeDrills_START() FDrillActions={0}", FDrillActions.Count);
            //
            //remove invalide and UnActive DA
            //

            foreach (var v in FDrillActions.ToArray())
            {
                if ((v.ParentLevel.Hierarchy.State & v.Level.Hierarchy.State).HasFlag(HierarchyState.hsActive) == false)
                    FDrillActions.Remove(v);

                if (v.Members.Any(item => item == null))
                    FDrillActions.Remove(v);

                if (v.DrillUps != null)
                    v.DrillUps.RemoveWhere(item => item == null);
            }

            if (UpdaterOptimizeDrills.IsBusy)
                return;

            var hashhashDrillAction = Get_BrothersDrillActions();

            // convert drills to drillAll
            // walking to brothers members
            foreach (var pair in hashhashDrillAction)
            {
                if (TryRemoveDrillAllAction(pair.Value))
                    continue;

                var hashtable = pair.Value.First();

                //if (hashtable.ParentLevel.CompleteMembersCount * 0.7 <= v.Value.Count)
                if (enable_ConvertDrillsToDrillAll(hashtable, pair.Value))
                    OptimizeDrills_ConvertToDrillUps(pair.Value);
            }

            // Convert drillAll to drills
            ConvertDrillAll2Drills();

            //DebugLogging.WriteLine("CellSet.OptimizeDrills_END()");
        }


        private Dictionary<string, HashSet<DrillAction>> Get_BrothersDrillActions()
        {
            var res = new Dictionary<string, HashSet<DrillAction>>();

            // get brothers DA at hastable
            foreach (var itemDrillAction in FDrillActions)
            {
                var uniquename = itemDrillAction.UniqueName;
                HashSet<DrillAction> h;
                if (!res.TryGetValue(uniquename, out h))
                {
                    h = new HashSet<DrillAction>();
                    res.Add(uniquename, h);
                }
                h.Add(itemDrillAction);
            }
            return res;
        }

        private bool TryRemoveDrillAllAction(HashSet<DrillAction> hashSet)
        {
            var ad = hashSet.FirstOrDefault(item => item.IsAllDrilled);
            if (ad != null)
            {
                foreach (var v1 in hashSet.ToArray())
                {
                    if (v1 == ad)
                        continue;

                    ad.DrillUps.Remove(v1.ParentMember);
                    hashSet.Remove(v1);
                    FDrillActions.Remove(v1);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Convert drillAll to drills
        /// </summary>
        private void ConvertDrillAll2Drills()
        {
            //DebugLogging.Write("CellSet.OptimizeDrills_ConvertDrillAll2Drills");

            var alll = FDrillActions
                .Where(item => item.IsAllDrilled
                               //&& (item.ParentLevel.CompleteMembersCount <= item.DrillUps.Count * 1.8)
                               && enable_CompleteMembersCount_Little_DrillUps(item)
                    //&& (item.ParentLevel.CompleteMembersCount <= item.DrillUps.Count * __ITEM_DRILLUP_COUNT)
                )
                .ToList();

            //DebugLogging.WriteLine("CellSet. (alll.Count={0})", alll.Count);

            foreach (var v in alll)
            {
                foreach (var m in v.ParentLevel.Members)
                {
                    if (v.DrillUps.Contains(m))
                        continue;

                    var da = new DrillAction();
                    da.Level = v.Level;
                    da.ParentLevel = v.ParentLevel;
                    da.Members.AddLast(m);
                    FDrillActions.Add(da);
                }
                FDrillActions.Remove(v);
            }

            //DebugLogging.WriteLine("Drill.");
        }

        private bool enable_ConvertDrillsToDrillAll(DrillAction hashtable, HashSet<DrillAction> pair)
        {
            return hashtable.ParentLevel.CompleteMembersCount * __V_VALUE_COUNT <= pair.Count;
        }

        private bool enable_CompleteMembersCount_Little_DrillUps(DrillAction item)
        {
            return item.ParentLevel.CompleteMembersCount <= item.DrillUps.Count * __ITEM_DRILLUP_COUNT;
        }

        internal static bool enable_CompleteMembersCount_Little_Count(KeyValuePair<Level, HashSet<Member>> item)
        {
            return item.Key.CompleteMembersCount <= item.Value.Count * __V_VALUE_COUNT2;
        }

        /// <summary>
        ///     ~0.7
        /// </summary>
        internal const double __V_VALUE_COUNT = 0.9999999; // 0.7

        /// <summary>
        ///     ~1.8
        /// </summary>
        internal const double __ITEM_DRILLUP_COUNT = 1.9999999; // 1.8

        /// <summary>
        ///     ~1.1
        /// </summary>
        internal const double __V_VALUE_COUNT2 = 1.000001; // 1.1

        private void OptimizeDrills_ConvertToDrillUps(HashSet<DrillAction> hashsetDrillActions)
        {
            var ad = new DrillAction
                     {
                         Level = hashsetDrillActions.First().Level,
                         ParentLevel = hashsetDrillActions.First().ParentLevel
                     };
            ad.DrillUps = new HashSet<Member>();

            FDrillActions.Add(ad);

            DebugLogging.WriteLine("Drill.OptimizeDrills_ConvertToDrillUps_START(DrillsCount={0} Add DrillAll={1})",
                FDrillActions.Count, ad.ToString());

            var memberdDrillActions = new Dictionary<Member, DrillAction>();
#if DEBUG
            //DrillActionEQ deq = new DrillActionEQ();
            //MemberEQ meq = new MemberEQ();

            //IEnumerable<Member> en = hashsetDrillActions.Select(x => x.ParentMember);
            //IEnumerable<Member> end = en.Distinct();
            //IEnumerable<Member> end1 = en.Distinct(meq);
            //

            //var f1 = hashsetDrillActions.Where(x => x.ParentMember != null).ToList();
            //var f2 = f1.Distinct().ToList();
            //var f3 = f2.Distinct().Where(x => memberdDrillActions.ContainsKey(x.ParentMember) == false).ToList();

            //if (en.Count() != end.Count())
            //{
            //    //    throw new ArithmeticException("en.Count() != end.Count()");
            //}
            //if (en.Count() != end1.Count())
            //{
            //    //    throw new ArithmeticException("en.Count() != end.Count()");
            //}
            //if (end.Count() != end1.Count())
            //{
            //    //    throw new ArithmeticException("end.Count() != end.Count()");
            //}
#endif
            var brothers_dict = new Dictionary<Member, List<DrillAction>>();

            try
            {
                memberdDrillActions = new Dictionary<Member, DrillAction>();

                foreach (var da_item in hashsetDrillActions
                    .Where(x => x.ParentMember != null)
                    .Distinct()
                    .ToList())
                {
                    var pm = da_item.ParentMember;
                    if (memberdDrillActions.ContainsKey(pm) == false)
                        memberdDrillActions.Add(pm, da_item);

                    if (brothers_dict.ContainsKey(pm) == false)
                        brothers_dict.Add(pm, new List<DrillAction>());

                    brothers_dict[pm].Add(da_item);
                }
            }
            catch (Exception e1)
            {
#if DEBUG
                throw e1;
#endif
            }

            foreach (var m1 in ad.ParentLevel.Members)
            {
                if (brothers_dict.ContainsKey(m1))
                {
                    var brothers = brothers_dict[m1];

                    foreach (var brother in brothers)
                        if (memberdDrillActions != null && memberdDrillActions.ContainsKey(m1))
                            FDrillActions.Remove(brother);
                        else
                            ad.DrillUps.Add(m1);
                }

                //DrillAction v1;
                //if (memberdDrillActions != null && memberdDrillActions.TryGetValue(m1, out v1))
                //{
                //    DebugLogging.WriteLine("Drill.OptimizeDrills_ConvertToDrillUps(Remove DrillAct={0})", v1.ToString());
                //    FDrillActions.Remove(v1);
                //}
                //else
                //{
                //    DebugLogging.WriteLine("Drill.OptimizeDrills_ConvertToDrillUps(Add DrillUps to DAAll={0} member({1}))", ad, m1);
                //    ad.DrillUps.Add(m1);
                //}

                DebugLogging.WriteLine("Drill.OptimizeDrills_ConvertToDrillUps_END(DrillsCount={0})", FDrillActions);
            }
        }

        private class DrillActionEQ : IEqualityComparer<DrillAction>
        {
            public bool Equals(DrillAction x, DrillAction y)
            {
                if (x.ParentMember != y.ParentMember)
                    return false;

                if (x.DrillUps != y.DrillUps)
                    return false;

                if (x.Level != y.Level)
                    return false;

                if (x.Method != y.Method)
                    return false;

                if (x.ParentLevel != y.ParentLevel)
                    return false;

                return true;
            }

            public int GetHashCode(DrillAction obj)
            {
                var da = obj;
                return da.IsAllDrilled.GetHashCode() ^ da.Method.GetHashCode() ^ da.ParentMember.GetHashCode();
            }
        }

        private class MemberEQ : IEqualityComparer<Member>
        {
            public bool Equals(Member x, Member y)
            {
                if (x.FChildren != y.FChildren)
                    return false;

                if (x.FCubeMember != y.FCubeMember)
                    return false;

                if (x.FDepth != y.FDepth)
                    return false;

                if (x.FDescription != y.FDescription)
                    return false;

                if (x.DisplayName != y.DisplayName)
                    return false;

                if (x.FLevel != y.FLevel)
                    return false;

                if (x.FMemberType != y.FMemberType)
                    return false;

                if (x.FNextLevelChildren != y.FNextLevelChildren)
                    return false;

                if (x.FParent != y.FParent)
                    return false;

                if (x.FRank != y.FRank)
                    return false;

                if (x.FShortName != y.FShortName)
                    return false;

                if (x.UniqueName != y.UniqueName)
                    return false;

                return true;
            }

            public int GetHashCode(Member obj)
            {
                var da = obj;
                return da.UniqueName.GetHashCode();
            }
        }

        internal string latestState;

        private void DoAddRequest(ICubeAddress ca, DrillAction action, DrillAction action2)
        {
            var ml = FGrid.FEngine.GetMetaline(ca.FLineID);

            var mml = new List<MeasureShowMode>();

            foreach (var ms in FGrid.Measures)
            {
                if (!ms.Visible)
                    continue;
                foreach (var mm in ms.ShowModes)
                {
                    if (!mm.Visible && mm != ms.ShowModes[0]) continue;

                    if (Grid.Cube.GetProductID() == RadarUtils.GetCurrentDesktopProductID() && mm.Measure.Expression.IsFill())
                        mml.Add(mm);
                    var l = ml.GetLine(ca.FHierID, ms, mm);
                    l.AddRequest(action, action2);
                }
            }
            foreach (var mm in mml.ToArray())
            foreach (var m in mm.Measure.AffectedMeasures())
            {
                var mm1 = m.ShowModes[0];
                if (!mml.Contains(mm1))
                {
                    var l = ml.GetLine(ca.FHierID, m, mm1);
                    l.AddRequest(action, action2);
                    mml.Add(mm1);
                }
            }
        }

        private int GetHierarchyPosition(Level level)
        {
            if (Grid.AxesLayout.RowAxis.Contains(level.Hierarchy))
                return Grid.AxesLayout.RowAxis.IndexOf(level.Hierarchy);
            return Grid.AxesLayout.ColumnAxis.IndexOf(level.Hierarchy);
        }

        private void CreateRequestMap()
        {
            var rowLevels = new HashSet<Level>();
            var colLevels = new HashSet<Level>();

            var ca = new ICubeAddress(FGrid);
            DoAddRequest(ca, null, null);

            Member colm = null;
            Member rowm = null;
            if (FGrid.AxesLayout.fColumnAxis.Count > 0)
            {
                var flevelcol = FGrid.AxesLayout.fColumnAxis[0].Levels[0];
                colLevels.Add(flevelcol);

                // *** TODO: need more check (error on report with empty period filter) ***
                if (flevelcol.Members.Count > 0)
                {
                    colm = flevelcol.Members[0];
                    ca.AddMember(colm);
                    DoAddRequest(ca, null, null);
                }
                ca = new ICubeAddress(FGrid);
                ca.AddMember(flevelcol.Members[0]);
                DoAddRequest(ca, null, null);
            }

            if (FGrid.AxesLayout.fRowAxis.Count > 0)
            {
                var flevelrow = FGrid.AxesLayout.fRowAxis[0].Levels[0];
                rowLevels.Add(flevelrow);

                // *** TODO: need more check (error on report with empty period filter) ***
                if (flevelrow.Members.Count > 0)
                    if (ca.LevelsCount > 0)
                    {
                        rowm = flevelrow.Members[0];
                        ca.AddMember(rowm);
                        DoAddRequest(ca, null, null);
                    }
                ca = new ICubeAddress(FGrid);
                ca.AddMember(flevelrow.Members[0]);
                DoAddRequest(ca, null, null);
            }

            var lvls = new Dictionary<string, Tuple<Level, Level>>();
            var lvlms = new Dictionary<Level, HashSet<Member>>();

            foreach (var v in FDrillActions)
            {
                var tmp_key = v.ParentLevel + "|%|" + v.Level;
                if (lvls.ContainsKey(tmp_key) == false)
                    lvls.Add(tmp_key, new Tuple<Level, Level>(v.ParentLevel, v.Level));
                Member pm = null;
                if (v.Method == PossibleDrillActions.esParentChild)
                {
                    if (v.ParentMember == null)
                    {
                        pm = v.ParentLevel.Members.FirstOrDefault(item => item.Children.Count > 0);
                        if (pm != null)
                            pm = pm.Children[0];
                    }
                    else
                    {
                        if (v.ParentMember.Children.Count > 0)
                            pm = v.ParentMember.Children[0];
                    }
                }
                else
                {
                    if (v.ParentMember != null && v.ParentMember.Depth > 0)
                        pm = v.ParentMember;
                }

                if (pm != null)
                {
                    HashSet<Member> mm;
                    if (lvlms.TryGetValue(v.Level, out mm) == false)
                    {
                        mm = new HashSet<Member>();
                        lvlms.Add(v.Level, mm);
                    }
                    if (mm.All(item => item.Depth != pm.Depth))
                        mm.Add(pm);
                }
            }

            if (rowLevels.Count > 0)
            {
                var b = false;
                do
                {
                    b = false;
                    foreach (var v in lvls.Values)
                        if (rowLevels.Contains(v.Item1))
                            b |= rowLevels.Add(v.Item2);
                } while (b);
            }

            if (colLevels.Count > 0)
            {
                var b = false;
                do
                {
                    b = false;
                    foreach (var v in lvls.Values)
                        if (colLevels.Contains(v.Item1))
                            b |= colLevels.Add(v.Item2);
                } while (b);
            }
            var rowll = rowLevels.ToArray();
            var colll = colLevels.ToArray();

            foreach (var v in FDrillActions)
            {
                ca = new ICubeAddress(FGrid);
                ca.AddMembersIfDeepMembersFirst(v.Members.ToList());

                var cur = v.ParentMember;
                if (v.Method == PossibleDrillActions.esParentChild)
                    if (cur == null)
                    {
                        cur = v.ParentLevel.Members.FirstOrDefault(item => item.Children.Count > 0);
                        if (cur != null)
                            cur = cur.Children[0];
                    }
                    else
                    {
                        if (cur.Children.Count == 0)
                            continue;
                        cur = cur.Children[0];
                    }
                else
                    cur = v.Level.Members[0];

                // ticket [#555]
                if (cur != null)
                {
                    ca.AddMember(cur);
                }
                else
                {
#if DEBUG
                    DebugLogging.WriteLine("CellSet.CreateRequestMap: cur == null!");
#endif
                }

                if (v.IsAllDrilled)
                    if (v.ParentLevel.Members.Count > 0)
                    {
                        ca.AddMember(v.ParentLevel.Members[0]);
                    }
                    else
                    {
#if DEBUG
                        throw new ArgumentOutOfRangeException("v.ParentLevel.Members.Count == 0");
#endif
                    }

                Member oppm;
                Level[] curl;
                if (rowLevels.Contains(v.Level))
                {
                    curl = rowll;
                    oppm = colm;
                }
                else
                {
                    curl = colll;
                    oppm = rowm;
                }

                var vi1 = GetHierarchyPosition(v.ParentLevel);
                foreach (var v2 in FDrillActions)
                    if (curl.Contains(v2.ParentLevel))
                    {
                        //if (v2.IsAllDrilled == false)
                        //    continue;
                        if (vi1 <= GetHierarchyPosition(v2.ParentLevel))
                            continue;
                        ca.AddMember(v2.ParentLevel.Members[0]);
                    }

                DoAddRequest(ca, v, null);

                if (oppm != null)
                {
                    ca.AddMember(oppm);
                    DoAddRequest(ca, v, null);
                }

                //#if DEBUG
                //                var res1 = FDrillActions
                //                    .Where(x => curl.Contains(x.ParentLevel) == false)
                //                    .ToList();

                //                res1 = GetList_DEBUG(FDrillActions, curl);

                //                foreach (var v2 in res1)
                //                {
                //                    //if (curl.Contains(v2.ParentLevel))
                //                    //    continue;
                //#else
                foreach (var v2 in FDrillActions)
                {
                    if (curl.Contains(v2.ParentLevel))
                        continue;
                    //#endif          

                    var ca2 = ca.Clone();

                    var cur2 = v2.ParentMember;
                    foreach (var m2 in v2.Members)
                        ca2.AddMember(m2);

                    if (v2.Method == PossibleDrillActions.esParentChild)
                        if (cur2 == null)
                        {
                            cur2 = v2.ParentLevel.Members.FirstOrDefault(item => item.Children.Count > 0);
                            if (cur2 != null)
                                cur2 = cur2.Children[0];
                        }
                        else
                        {
                            if (cur2.Children.Count == 0)
                                continue;
                            cur2 = cur2.Children[0];
                        }
                    else
                        cur2 = v2.Level.Members[0];
                    ca2.AddMember(cur2);

                    DoAddRequest(ca2, v, v2);
                }
            }
        }

        private void DoRetrieveMembers()
        {
            //if (FGrid.Cube.GetProductID() != RadarUtils.GetCurrentMSASProductID())
            //    return;

            var list = new HashSet<CubeMember>();
            //
            // fill from level by first member if action
            //
            foreach (var a in FDrillActions)
            {
                var m = a.Members.FirstOrDefault();
                if (m == null) continue;

                if (m.MemberType == MemberType.mtCommon && m.Level == a.Level &&
                    m.CubeMember.FChildrenCount < 0)
                    list.Add(m.CubeMember);
            }

            if (list.Count > 0)
                FGrid.Cube.RetrieveMembersCount3(FGrid, list);

            //
            // get levels from column and row
            //

            var pivottedlevels = new HashSet<CubeLevel>();

            if (FGrid.AxesLayout.RowAxis.Count > 0 && !FGrid.AxesLayout.RowAxis[0].Levels[0].CubeLevel.IsFullyFetched)
                pivottedlevels.Add(FGrid.AxesLayout.RowAxis[0].Levels[0].CubeLevel);

            if (FGrid.AxesLayout.ColumnAxis.Count > 0 &&
                !FGrid.AxesLayout.ColumnAxis[0].Levels[0].CubeLevel.IsFullyFetched)
                pivottedlevels.Add(FGrid.AxesLayout.ColumnAxis[0].Levels[0].CubeLevel);

            if (FGrid.AxesLayout.PageAxis.Count > 0)
            {
                foreach (var h in FGrid.AxesLayout.PageAxis)
                {
                    if(!h.Levels[0].CubeLevel.IsFullyFetched)
                        pivottedlevels.Add(h.Levels[0].CubeLevel);
                }
            }


            //
            //
            //

            var cubemembersbyLevel = new Dictionary<CubeLevel, List<CubeMember>>();

            var cubelevels_dict = new Dictionary<CubeLevel, List<MDXLevel>>();

            foreach (var a in FDrillActions)
            {
                var cubelevel = a.Level.CubeLevel;
                if (a.Method == PossibleDrillActions.esNextHierarchy || a.IsAllDrilled)
                {
                    var firstLevelFetched = cubelevel.IsFullyFetched ||
                                            cubelevel.Hierarchy.Origin == HierarchyOrigin.hoParentChild &&
                                            cubelevel.FFirstLevelMembersCount == cubelevel.Members.Count;

                    if (!firstLevelFetched)
                        pivottedlevels.Add(cubelevel);
                }
                else
                {
                    if (a.ParentMember != null && a.ParentMember.MemberType == MemberType.mtCommon)
                        if (!a.ParentMember.CubeMember.IsFullyFetched && !pivottedlevels.Contains(cubelevel))
                        {
                            List<CubeMember> lm;
                            if (!cubemembersbyLevel.TryGetValue(cubelevel, out lm))
                            {
                                lm = new List<CubeMember>();
                                cubemembersbyLevel.Add(cubelevel, lm);
                            }
                            lm.Add(a.ParentMember.CubeMember);
                        }

                    if (a.ParentMember != null && a.ParentMember.MemberType == MemberType.mtCommon &&
                        a.ParentLevel.Hierarchy.Origin == HierarchyOrigin.hoParentChild)
                    {
                        if (!a.ParentMember.CubeMember.IsFullyFetched && !pivottedlevels.Contains(cubelevel))
                        {
                        }
                        var mdxlevel = a.ParentMember.CubeMember.MDXLevel;
                        if (mdxlevel != null && mdxlevel.Isfullfetched)
                        {
                            MDXLevel nextmdxlevel = null;

                            if (a.ParentMember.CubeMember.FMDXLevelIndex + 1 <
                                a.ParentMember.CubeMember.ParentLevel._MDXLevels.Count)
                                nextmdxlevel =
                                    a.ParentMember.CubeMember.ParentLevel._MDXLevels[
                                        a.ParentMember.CubeMember.FMDXLevelIndex + 1];

                            List<MDXLevel> levels;
                            if (cubelevels_dict.TryGetValue(cubelevel, out levels) == false)
                                cubelevels_dict.Add(cubelevel, new List<MDXLevel>());
                            levels = cubelevels_dict[cubelevel];
                            if (levels.Contains(nextmdxlevel) == false)
                                levels.Add(nextmdxlevel);
                        }
                    }
                }

                if (a.Level.FStaticMembers.Count < a.Level.CubeLevel.Members.Count)
                    a.Level.CreateNewMembers();
            }

            foreach (var pair in cubelevels_dict)
            {
                var all = pair.Value.Any(x => !x.Isfullfetched);
                if (!all) // all next mdslevel are fetched
                    pivottedlevels.Remove(pair.Key);
            }

            foreach (var cl in pivottedlevels)
                Grid.Cube.DoRetrieveMembers3(FGrid, cl);

            foreach (var lm in cubemembersbyLevel)
            {
                if (lm.Key.IsFullyFetched)
                    continue;

                var mml = lm.Value.GroupBy(item => item.ParentLevel);
                var b = false;
                foreach (var mmm in mml)
                    if (mmm.Key.FMembersCount == mmm.Count())
                    {
                        Grid.Cube.DoRetrieveMembers3(FGrid, lm.Key);
                        b = true;
                        break;
                    }
                if (b)
                    continue;

                var lmv = lm.Value.Where(item => !item.IsFullyFetched).ToList();
                if (lmv.Count > 0)
                    Grid.Cube.RetrieveDescendants(Grid, lmv, lm.Key);
            }
        }

        internal virtual void RebuildChart()
        {
        }

        internal virtual void RebuildGrid()
        {
        }


        private bool DrillupsProcessed(CellsetMember M, PossibleDrillActions act)
        {
            foreach (var v in FDrillActions)
            {
                if (v.DrillUps == null || M.FMember.FLevel != v.ParentLevel || v.Method != act)
                    continue;

                if (v.DrillUps.Contains(M.FMember))
                {
                    v.DrillUps.Remove(M.FMember);
                    Rebuild();
                    FGrid.EndChange(GridEventType.geDrillAction, M.FMember, PossibleDrillActions.esNextLevel);

                    return true;
                }
            }

            return false;
        }

        internal void OpenNextLevel(CellsetMember M)
        {
            if (DrillupsProcessed(M, PossibleDrillActions.esNextLevel))
                return;

            var h = M.FMember.Level.Hierarchy;
            Level l = null;
            for (var i = M.FMember.Level.Index + 1; i < h.Levels.Count; i++)
                if (h.Levels[i].Visible)
                {
                    l = h.Levels[i];
                    break;
                }

            if (l == null)
            {
                l = h.Levels[M.FMember.Level.Index + 1];
                l.FVisible = true;
            }

            var da = new DrillAction(l, M);
            FDrillActions.Add(da);

            Rebuild();
            FGrid.EndChange(GridEventType.geDrillAction, M.FMember, PossibleDrillActions.esNextLevel);
        }

        internal void OpenParentChild(CellsetMember M)
        {
            if (DrillupsProcessed(M, PossibleDrillActions.esParentChild)) return;

            var da = new DrillAction(M.FMember.Level, M);
            FDrillActions.Add(da);

            Rebuild();
            FGrid.EndChange(GridEventType.geDrillAction, M.FMember, PossibleDrillActions.esParentChild);
        }

        internal void OpenNextHierarchy(CellsetMember M)
        {
            if (DrillupsProcessed(M, PossibleDrillActions.esNextHierarchy)) return;

            if (M.FMember.MemberType != MemberType.mtMeasure)
            {
                IList<Hierarchy> hh = M.FIsRow ? FGrid.FLayout.fRowAxis : FGrid.FLayout.fColumnAxis;


                var h = hh[hh.IndexOf(M.FMember.FLevel.FHierarchy) + 1];
                Level l = null;
                for (var i = 0; i < h.Levels.Count; i++)
                    if (h.Levels[i].Visible)
                    {
                        l = h.Levels[i];
                        break;
                    }

                if (l == null)
                {
                    l = h.Levels[0];
                    l.FVisible = true;
                }

                var da = new DrillAction(l, M);
                FDrillActions.Add(da);
            }

            Rebuild();
            FGrid.EndChange(GridEventType.geDrillAction, M.FMember, PossibleDrillActions.esNextHierarchy);
        }

        internal void Collapse(CellsetMember M)
        {
            foreach (var v in FDrillActions.ToArray())
            {
                if (M.IsThisDrillAction(v))
                    FDrillActions.Remove(v);

                if (v.ParentLevel == M.FMember.Level && v.IsAllDrilled)
                    v.DrillUps.Add(M.FMember);
            }
            Rebuild();
        }

        private Level GetNextLevel(Level Level, PossibleDrillActions action)
        {
            switch (action)
            {
                case PossibleDrillActions.esParentChild:
                    return Level;
                case PossibleDrillActions.esNextLevel:
                    var p = Level.Index + 1;
                    if (p >= Level.Hierarchy.Levels.Count) return null;
                    return Level.Hierarchy.Levels[p];

                case PossibleDrillActions.esNextHierarchy:
                    Hierarchy h = null;
                    p = Grid.FLayout.fColumnAxis.IndexOf(Level.Hierarchy) + 1;
                    if (p > 0 && p < Grid.FLayout.fColumnAxis.Count)
                    {
                        h = Grid.FLayout.fColumnAxis[p];
                    }
                    else
                    {
                        p = Grid.FLayout.fRowAxis.IndexOf(Level.Hierarchy) + 1;
                        if (p > 0 && p < Grid.FLayout.fRowAxis.Count)
                            h = Grid.FLayout.fRowAxis[p];
                    }
                    if (h == null) return null;

                    Level nl = null;
                    foreach (var nli in h.Levels)
                        if (nli.Visible)
                        {
                            nl = nli;
                            break;
                        }
                    if (nl == null) nl = h.Levels[0];

                    return nl;
                default:
                    return null;
            }
        }

        private IEnumerable<CellsetMember> GetMembersForLevel(Level l)
        {
            return GetMembersForLevel(l, null);
        }

        private IEnumerable<CellsetMember> GetMembersForLevel(Level l, CellsetMember m)
        {
            if (m == null)
            {
                if (FColumnLevels.Any(item => item.FLevel == l))
                    foreach (var mm in FColumnMembers)
                    foreach (var mmm in GetMembersForLevel(l, mm))
                        if (mmm.FMember != null)
                            yield return mmm;
                if (FRowLevels.Any(item => item.FLevel == l))
                    foreach (var mm in FRowMembers)
                    foreach (var mmm in GetMembersForLevel(l, mm))
                        if (mmm.FMember != null)
                            yield return mmm;
            }
            else
            {
                if (m.FLevel.FLevel == l) yield return m;
                foreach (var mm in m.FChildren)
                foreach (var mmm in GetMembersForLevel(l, mm))
                    if (mmm.FMember != null)
                        yield return mmm;
            }
        }

        /// <summary>
        ///     Drills all nodes of the specified level in the way defined by the Mode
        ///     parameter.
        /// </summary>
        public void ExpandAllNodes(PossibleDrillActions Mode, Level Level)
        {
            DebugLogging.WriteLine("Drill.ExpandAllNodes(Mode={0} Level={1})", Mode, Level);

            if (FGrid.CellsetMode == CellsetMode.cmGrid)
            {
                if (Mode == PossibleDrillActions.esParentChild)
                {
                    var cl = FRowLevels.FirstOrDefault(item => item.FLevel == Level);
                    if (cl == null)
                        cl = FColumnLevels.FirstOrDefault(item => item.FLevel == Level);
                    if (cl == null) return;

                    var cubelevel = cl.FLevel.CubeLevel;
                    Grid.Cube.DoRetrieveMembers3(FGrid, cubelevel);

                    var needRebuild = false;
                    foreach (var v in cl.AllChildren())
                    {
                        if (v.FMember == null || v.FMember.IsLeaf || v.FChildren.Count > 0)
                            continue;
                        if (v.FMember.Children.Count == 0)
                            continue;
                        var da = new DrillAction(Level, v);
                        FDrillActions.Add(da);
                        needRebuild = true;
                    }
                    if (!needRebuild)
                        return;
                }
                else
                {
                    if (Level.Hierarchy.Origin == HierarchyOrigin.hoParentChild)
                        return;

                    var nl = GetNextLevel(Level, Mode);
                    if (nl == null && Mode != PossibleDrillActions.esCollapsed)
                        return;

                    foreach (var v in FDrillActions.ToArray())
                        if (v.ParentLevel == Level)
                            FDrillActions.Remove(v);
                    if (Mode != PossibleDrillActions.esCollapsed)
                        if (FDrillActions.Any(item => item.IsAllDrilled))
                        {
                            DebugLogging.WriteLine("Drill.ExpandAllNodes(Any(item => item.IsAllDrilled = true!)");

                            foreach (var mm in GetMembersForLevel(Level))
                                FDrillActions.Add(new DrillAction(nl, mm));
                        }
                        else
                        {
                            var da = new DrillAction
                                     {
                                         ParentLevel = Level,
                                         Level = nl
                                     };
                            da.DrillUps = new HashSet<Member>();

                            FDrillActions.Add(da);

                            DebugLogging.WriteLine("Drill.ExpandAllNodes(Add {0})", da);
//#if DEBUG
//                            switch (Level.Hierarchy.Origin)
//                            {
//                                //case HierarchyOrigin.hoParentChild:
//                                //case HierarchyOrigin.hoUserDefined:
//                                //    var level_members = FColMembersArray.OfType<CellsetMember>()
//                                //        .Where(x => x.FMember != null && x.FMember.Level == Level);

//                                //    level_members = level_members.Union(FRowMembersArray.OfType<CellsetMember>()
//                                //        .Where(x => x.FMember != null && x.FMember.Level == Level));

//                                //    FGrid.BeginUpdate();

//                                //    UpdaterOptimizeDrills.BeginUpdate();

//                                //    foreach (var un in level_members.Select(x => x.FMember.UniqueName))
//                                //    {
//                                //        level_members = FColMembersArray.OfType<CellsetMember>()
//                                //        .Where(x => x.FMember != null && x.FMember.Level == Level)
//                                //        .Union(FRowMembersArray.OfType<CellsetMember>()
//                                //        .Where(x => x.FMember != null && x.FMember.Level == Level));

//                                //        var cs = level_members.FirstOrDefault(x => x.FMember.UniqueName == un);

//                                //        if (cs == null)
//                                //            continue;

//                                //        this.DrillAction_Inner(Mode, cs);
//                                //        //Rebuild();

//                                //    }

//                                //    UpdaterOptimizeDrills.EndUpdate();

//                                //        //ForEach(x => this.DrillAction_Inner(Mode, x));
//                                //    FGrid.EndUpdate();
//                                //    break;
//                                default:


//                                    break;
//                            }

//#else
//                            DrillAction da = new DrillAction();
//                            da.ParentLevel = Level;
//                            da.Level = nl;
//                            TCommon.ResetDrillUps(da);
//                            FDrillActions.Add(da);
//#endif
                        }

                    switch (Mode)
                    {
                        case PossibleDrillActions.esNextLevel:
                        case PossibleDrillActions.esNextHierarchy:
                            nl.FVisible = true;
                            break;
                    }
                }
                Rebuild();
            }
            else
            {
                switch (Mode)
                {
                    case PossibleDrillActions.esNextLevel:
                        var h = Level.Hierarchy;
                        if (Level.Index >= h.Levels.Count - 1)
                            return;

                        for (var i = Level.Index + 1; i < h.Levels.Count; i++)
                            if (!h.Levels[i].Visible)
                            {
                                h.Levels[i].Visible = true;
                                break;
                            }
                        break;
                    case PossibleDrillActions.esCollapsed:
                        h = Level.Hierarchy;
                        var needRebuild = false;
                        for (var i = Level.Index + 1; i < h.Levels.Count; i++)
                            if (h.Levels[i].Visible)
                            {
                                h.Levels[i].FVisible = false;
                                needRebuild = true;
                            }
                        if (needRebuild)
                        {
                            Grid.FLayout.CheckExpandedLevels();
                            if (!FGrid.IsUpdating)
                                Rebuild();
                            Grid.EndChange(GridEventType.gePivotAction, Level);
                        }
                        break;
                }
            }
        }

        /// <summary>
        ///     Expands all the hierarchies in the active cellset areas.
        /// </summary>
        /// <param name="preferredMode">The prefererrable mode to expand.</param>
        /// <param name="expandColumns">Indicates whether the column area should be expanded.</param>
        /// <param name="expandRows">Indicates whether the row area should be expanded.</param>
        public void ExpandAllHierarchies(PossibleDrillActions preferredMode, bool expandColumns, bool expandRows)
        {
            if (expandColumns)
                for (var i = 0; i < Grid.AxesLayout.ColumnAxis.Count; i++)
                {
                    if (preferredMode == PossibleDrillActions.esNextHierarchy &&
                        i == Grid.AxesLayout.ColumnAxis.Count - 1)
                        break;
                    var h = Grid.AxesLayout.ColumnAxis[i];
                    if (preferredMode == PossibleDrillActions.esNextLevel)
                    {
                        for (var j = 0; j < h.Levels.Count - 1; j++)
                            if (Grid.AxesLayout.fColumnLevels.Contains(h.Levels[j]))
                                ExpandAllNodes(PossibleDrillActions.esNextLevel, h.Levels[j]);
                    }
                    else
                    {
                        for (var j = h.Levels.Count - 1; j >= 0; j--)
                            if (Grid.AxesLayout.fColumnLevels.Contains(h.Levels[j]))
                                ExpandAllNodes(preferredMode, h.Levels[j]);
                    }
                }
            if (expandRows)
                for (var i = 0; i < Grid.AxesLayout.RowAxis.Count; i++)
                {
                    if (preferredMode == PossibleDrillActions.esNextHierarchy && i == Grid.AxesLayout.RowAxis.Count - 1)
                        break;
                    var h = Grid.AxesLayout.RowAxis[i];
                    if (preferredMode == PossibleDrillActions.esNextLevel)
                    {
                        for (var j = 0; j < h.Levels.Count - 1; j++)
                            if (Grid.AxesLayout.fRowLevels.Contains(h.Levels[j]))
                                ExpandAllNodes(PossibleDrillActions.esNextLevel, h.Levels[j]);
                    }
                    else
                    {
                        for (var j = h.Levels.Count - 1; j >= 0; j--)
                            if (Grid.AxesLayout.fRowLevels.Contains(h.Levels[j]))
                                ExpandAllNodes(preferredMode, h.Levels[j]);
                    }
                }
        }

        public void ExpandNodesAnywhere(PossibleDrillActions Mode, Level FromLevel, Level ToLevel)
        {
            if (FGrid.CellsetMode == CellsetMode.cmGrid)
            {
                var nextLevel = GetNextLevel(FromLevel, Mode);
                if (Mode == PossibleDrillActions.esNextLevel && nextLevel != null && nextLevel.Index <= ToLevel.Index)
                {
                    ExpandAllNodes(Mode, FromLevel);
                    if (nextLevel.Index < ToLevel.Index)
                        ExpandNodesAnywhere(Mode, nextLevel, ToLevel);
                }

                if (Mode == PossibleDrillActions.esNextHierarchy && nextLevel != null)
                {
                    ExpandAllNodes(Mode, FromLevel);
                    if (nextLevel != ToLevel)
                        ExpandNodesAnywhere(Mode, nextLevel, ToLevel);
                }
            }
        }

        /// <summary>Collapses all nodes of the specified level.</summary>
        public void CollapseAllNodes(Level Level)
        {
            ExpandAllNodes(PossibleDrillActions.esCollapsed, Level);
        }

        private CellsetMember DoFindMember(List<CellsetMember> CMs, string MemberCaption)
        {
            if (CMs.Count == 0) return null;
            foreach (var CM in CMs)
            {
                if (CM.DisplayName == MemberCaption) return CM;
                var Result = DoFindMember(CM.FChildren, MemberCaption);
                if (Result != null) return Result;
            }
            return null;
        }

        private CellsetMember DoFindMember(List<CellsetMember> CMs, Member Member)
        {
            if (CMs.Count == 0)
                return null;

            foreach (var CM in CMs)
            {
                if (CM.FMember == Member)
                    return CM;

                var Result = DoFindMember(CM.FChildren, Member);
                if (Result != null)
                    return Result;
            }
            return null;
        }

        /// <summary>
        ///     Searches the current OLAP slice for the cellset member that represents the member
        ///     passed as the parameter
        /// </summary>
        public IMemberCell FindMember(Member Member)
        {
            var C = DoFindMember(FRowMembers, Member);
            if (C != null)
                return new MemberCell(this, C);

            C = DoFindMember(FColumnMembers, Member);
            if (C != null)
                return new MemberCell(this, C);

            return null;
        }

        /// <summary>
        ///     Returns the corresponding ILevelCell instance for the specified level.
        /// </summary>
        /// <param name="level">The level to find the ILevelCell instance</param>
        /// <returns>The ILevelCell instance</returns>
        public ILevelCell FindLevel(Level level)
        {
            foreach (var cl in FRowLevels)
                if (cl.FLevel == level) return new LevelCell(this, cl);

            foreach (var cl in FColumnLevels)
                if (cl.FLevel == level) return new LevelCell(this, cl);

            return null;
        }

        /// <summary>
        ///     Searches the current OLAP slice for the cellset member that represents the member
        ///     passed as the parameter.
        /// </summary>
        public IMemberCell FindMember(string MemberCaption)
        {
            var C = DoFindMember(FRowMembers, MemberCaption);
            if (C != null) return new MemberCell(this, C);

            C = DoFindMember(FColumnMembers, MemberCaption);
            if (C != null) return new MemberCell(this, C);

            return null;
        }

        /// <summary>The total number of columns in the current CellSet.</summary>
        /// <remarks>
        ///     The sizes of both the data area and the names of the hierarchy members area
        ///     matter for this property. You can find out the size of the names of the hierarchy
        ///     members area through the FixedColumns property.
        /// </remarks>
        public int ColumnCount => FColumnCount;

        /// <summary>The total number of columns in the current CellSet.</summary>
        public int PagedColumnCount
        {
            get
            {
                AdjustPaging();
                return FPagedColumnCount;
            }
        }

        /// <summary>
        ///     The total number of rows in the current CellSet with the singularities of paging
        ///     taken into account.
        /// </summary>
        public int PagedRowCount
        {
            get
            {
                AdjustPaging();
                return FPagedRowCount;
            }
        }

        [NonSerialized] private int[] _adjustedRows;
        [NonSerialized] private int[] _adjustedCols;
        [NonSerialized] internal Dictionary<int, int> _adjustedRowsHelper;
        [NonSerialized] internal Dictionary<int, int> _adjustedColsHelper;

        internal Dictionary<int, int> AdjustedColsHelper
        {
            get
            {
                if (_adjustedColsHelper == null)
                    try
                    {
                        AdjustPaging();
                    }
                    catch
                    {
                        throw;
                    }

                return _adjustedColsHelper;
            }
        }

        internal Dictionary<int, int> AdjustedRowsHelper
        {
            get
            {
                if (_adjustedRowsHelper == null)
                    try
                    {
                        AdjustPaging();
                    }
                    catch
                    {
                        throw;
                    }

                return _adjustedRowsHelper;
            }
        }

        [NonSerialized] private int FPagedColumnCount = -1;

        [NonSerialized] private int FPagedRowCount = -1;
        //private AxisDescriptor _ColorAxisDescriptor;

        internal void AdjustPaging()
        {
            if (_adjustedRows != null)
                return;

            _adjustedRowsHelper = new Dictionary<int, int>();
            _adjustedColsHelper = new Dictionary<int, int>();

            //if (FGrid.AllowPaging == false)
            //{
            //    FPagedRowCount = RowCount;
            //    FPagedColumnCount = ColumnCount;

            //    var list = new List<int>();

            //    for (int i = 0; i < RowCount; i++)
            //    {
            //        _adjustedRowsHelper.Add(i, list.Count);
            //        list.Add(i);
            //    }
            //    _adjustedRows = list.ToArray();
            //    FPagedRowCount = list.Count;

            //    list = new List<int>();
            //    for (int i = 0; i < FColumnCount; i++)
            //    {
            //        _adjustedColsHelper.Add(i, list.Count);
            //         list.Add(i);
            //    }
            //    _adjustedCols = list.ToArray();
            //    FPagedColumnCount = list.Count;
            //}
            //else
            {
                var list = new List<int>();

                for (var i = 0; i < RowCount; i++)
                {
                    _adjustedRowsHelper.Add(i, list.Count);
                    if (IsRowVisible(i))
                        list.Add(i);
                }
                _adjustedRows = list.ToArray();
                FPagedRowCount = list.Count;

                list = new List<int>();
                for (var i = 0; i < FColumnCount; i++)
                {
                    _adjustedColsHelper.Add(i, list.Count);
                    if (IsColumnVisible(i))
                        list.Add(i);
                }
                _adjustedCols = list.ToArray();
                FPagedColumnCount = list.Count;
            }
        }

        /// <summary>The total number of rows in the current CellSet.</summary>
        /// <remarks>
        ///     The sizes of both the data area and the names of the hierarchy members area
        ///     matter for this property. You can find out the size of the names of the hierarchy
        ///     members area through the FixedRows property.
        /// </remarks>
        public int RowCount { get; private set; }

        /// <summary>
        ///     References to the OlapControl object that the current class belongs
        ///     to.
        /// </summary>
        public OlapControl Grid
        {
            [DebuggerStepThrough] get { return FGrid; }
        }

        /// <summary>
        ///     The number of the rows, where the levels and the hierarchy members names are
        ///     displayed.
        /// </summary>
        /// <remarks>
        ///     You can find out the aggregate size of the CellSet in rows by applying the
        ///     RowCount property
        /// </remarks>
        public int FixedRows
        {
            [DebuggerStepThrough] get { return FFixedRows; }
        }

        /// <summary>
        ///     The number of the columns in the area, where the levels and the hierarchy members
        ///     names are displayed (at the very left part of the window).
        /// </summary>
        public int FixedColumns
        {
            [DebuggerStepThrough] get { return FFixedColumns; }
        }

        /// <summary>
        ///     Gets a measure which values are displayed in the data area, if measures names in
        ///     the current CellSet are not visible.
        /// </summary>
        /// <remarks>
        ///     If among all Cube measures only one must be displayed at the current point and
        ///     the AxesLayout.HideMeasureIfPossible property is set to True, then for the purpose of
        ///     saving screen space, the cell with the measure name in the CellSet is not depicted. In
        ///     this case, this property allows you to find out what measure exactly is depicted in the
        ///     CellSet.
        /// </remarks>
        public Measure DefaultMeasure => FDefaultMeasure;

        /// <summary>
        ///     <para>
        ///         The index of the column, where the Grid data is sorted by ascending or
        ///         descending order.
        ///     </para>
        /// </summary>
        /// <remarks>Returns -1 if the Grid hasn't been sorted by column values</remarks>
        public int ValueSortedColumn
        {
            get => FValueSortedColumn;
            set
            {
                if (value < FFixedColumns || value >= FColumnCount)
                {
                    FSortingAddress = null;
                    if (FValueSortedColumn >= 0)
                    {
                        FValueSortedColumn = -1;
                        if (Grid.IsUpdating == false) Rebuild();
                    }
                    return;
                }
                FValueSortedColumn = value;
                FSortingAddress = ((IMemberCell) Cells(value, FFixedRows - 1)).Address;
                if (Grid.IsUpdating == false) Rebuild();
            }
        }

        /// <summary>
        ///     The sorting direction (ascending or descending) for the column, where the Grid
        ///     data is sorted.
        /// </summary>
        /// <remarks>
        ///     If the Grid hasn't been sorted by column values (ValueSortedColumn = -1), this
        ///     property is not applicable
        /// </remarks>
        public ValueSortingDirection ValueSortingDirection
        {
            get => FSortingDirection;
            set
            {
                if (FSortingDirection == value)
                    return;
                FSortingDirection = value;
                if (FValueSortedColumn > 0)
                    Rebuild();
            }
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgCellset);

            if (ScrolledNodes.Count > 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgCellset_ScrolledNodes);
                StreamUtils.WriteInt32(writer, ScrolledNodes.Count);
                foreach (var o in ScrolledNodes)
                {
                    StreamUtils.WriteString(writer, o.Key);
                    StreamUtils.WriteInt32(writer, o.Value);
                }
            }

            if (fComments.Count > 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgCellset_Comments);
                StreamUtils.WriteInt32(writer, fComments.Count);
                foreach (var o in fComments)
                {
                    StreamUtils.WriteString(writer, o.Key.ToString());
                    StreamUtils.WriteString(writer, o.Value);
                }
            }

            if (FDrillActions.Count > 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgCellset_DrillActions);
                StreamUtils.WriteInt32(writer, FDrillActions.Count);
                foreach (var v in FDrillActions)
                    StreamUtils.WriteString(writer, v.ToString());
            }
            //if (FRowMetaMember != null)
            //{
            //    StreamUtils.WriteStreamedObject(writer, FRowMetaMember, Tags.tgCellset_RowMetaMember);
            //}

            //if (FColMetaMember != null)
            //{
            //    StreamUtils.WriteStreamedObject(writer, FColMetaMember, Tags.tgCellset_ColMetaMember);
            //}

            if (FSortingDirection != ValueSortingDirection.sdDescending)
            {
                StreamUtils.WriteTag(writer, Tags.tgCellset_SortingDirection);
                StreamUtils.WriteInt32(writer, (int) FSortingDirection);
            }

            if (FValueSortedColumn != -1)
            {
                StreamUtils.WriteTag(writer, Tags.tgCellset_SortedColumnIndex);
                StreamUtils.WriteInt32(writer, FValueSortedColumn);
            }

            StreamUtils.WriteTag(writer, Tags.tgCellset_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgCellset);
            //          FRowMetaMember = null;
            //          FColMetaMember = null;
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                int c;
                switch (tag)
                {
                    case Tags.tgCellset_ScrolledNodes:
                        c = StreamUtils.ReadInt32(reader);
                        for (var i = 0; i < c; i++)
                            ScrolledNodes.Add(
                                StreamUtils.ReadString(reader),
                                StreamUtils.ReadInt32(reader));
                        break;
                    case Tags.tgCellset_Comments:
                        c = StreamUtils.ReadInt32(reader);
                        for (var i = 0; i < c; i++)
                            fComments.Add(ICubeAddress.FromString(FGrid, StreamUtils.ReadString(reader)),
                                StreamUtils.ReadString(reader));
                        break;
                    case Tags.tgCellset_DrillActions:
                        c = StreamUtils.ReadInt32(reader);
                        for (var i = 0; i < c; i++)
                        {
                            var v = DrillAction.FromString(FGrid, StreamUtils.ReadString(reader));
                            if (v != null)
                                FDrillActions.Add(v);
                        }
                        break;
                    //case Tags.tgCellset_RowMetaMember:
                    //    FRowMetaMember = new MetaMember(null);
                    //    StreamUtils.ReadStreamedObject(reader, FRowMetaMember, FGrid);
                    //    break;
                    //case Tags.tgCellset_ColMetaMember:
                    //    FColMetaMember = new MetaMember(null);
                    //    StreamUtils.ReadStreamedObject(reader, FColMetaMember, FGrid);
                    //    break;
                    case Tags.tgCellset_SortingDirection:
                        FSortingDirection = (ValueSortingDirection) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCellset_SortedColumnIndex:
                        FValueSortedColumn = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCellset_ExpandAllActions:
                        c = StreamUtils.ReadInt32(reader);
                        for (var i = 0; i < c; i++)
                        {
                            StreamUtils.ReadString(reader);
                            StreamUtils.ReadString(reader);
                            //Level lf = Grid.Dimensions.FindLevel(StreamUtils.ReadString(reader));
                            //Level lt = Grid.Dimensions.FindLevel(StreamUtils.ReadString(reader));
                            //if ((lf != null) && (lt != null))
                            //    AddExpandAllRequest(lf, lt);
                        }
                        break;
                    case Tags.tgCellset_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        #endregion

        internal void ApplyOpenedNodes(Dictionary<string, PossibleDrillActions> on)
        {
            FDrillActions.Clear();
            FGrid.FLayout.CheckExpandedLevels();

            //Rebuild();

            var on1 = new Dictionary<string, PossibleDrillActions>(on.Count);
            foreach (var o in on)
                if (o.Key.StartsWith("-1:"))
                    on1.Add(o.Key, o.Value);
                else
                    on1.Add("-1" + o.Key.Substring(o.Key.IndexOf(':')), o.Value);

            if (FGrid.FLayout.fColumnLevels.Count > 0)
            {
                foreach (var l in FGrid.FLayout.fColumnLevels)
                    CheckLevelForFetchedParents(l);

                DoApplyOpenedNodes(on1, null, null, FGrid.FLayout.fColumnLevels[0], false);
            }

            if (FGrid.FLayout.fRowLevels.Count > 0)
            {
                foreach (var l in FGrid.FLayout.fRowLevels)
                    CheckLevelForFetchedParents(l);

                DoApplyOpenedNodes(on1, null, null, FGrid.FLayout.fRowLevels[0], true);
            }
        }

        private void DoApplyOpenedNodes(Dictionary<string, PossibleDrillActions> on, Member parentMember,
            DrillAction parentAction, Level currentLevel, bool isRow)
        {
            var mm = currentLevel.Members;

            if (parentMember != null)
                if (parentMember.Level.Hierarchy.Origin == HierarchyOrigin.hoParentChild)
                {
                    mm = parentMember.Children;
                }
                else
                {
                    if (parentMember.Level.Hierarchy == currentLevel.Hierarchy &&
                        parentMember.Level.Index + 1 == currentLevel.Index)
                        mm = parentMember.NextLevelChildren;
                }

            foreach (var m in mm)
            {
                var s = CreateNonMeasuresOpenNodesString(m, parentAction);
                PossibleDrillActions a;
                if (on.TryGetValue(s, out a))
                {
                    var nl = GetNextLevel(m.Level, a);
                    var da = new DrillAction {ParentLevel = m.Level, Level = nl};
                    if (parentAction != null)
                        foreach (var m1 in parentAction.Members)
                            da.Members.AddLast(m1);
                    da.Members.AddFirst(m);
                    DoApplyOpenedNodes(on, m, da, nl, isRow);
                    FDrillActions.Add(da);
                }
            }
        }

        private string CreateNonMeasuresOpenNodesString(Member probe, DrillAction parent)
        {
            var sb = new StringBuilder("-1:" + probe + ".");
            if (parent != null && parent.Members.Count > 0)
                sb.Append(string.Join(".", parent.Members.Select(item => item.UniqueName).ToArray()) + ".");
            return sb.ToString();
        }


        internal void DrillAction_Inner(PossibleDrillActions Mode, CellsetMember RealMember)
        {
            //DebugLogging.WriteLine("CellSet.DrillAction_Inner(Mode={0} Member={1})", Mode, RealMember);

            if (Mode == PossibleDrillActions.esCollapsed)
                Collapse(RealMember);
            if (Mode == PossibleDrillActions.esParentChild)
                OpenParentChild(RealMember);
            if (Mode == PossibleDrillActions.esNextLevel)
                OpenNextLevel(RealMember);
            if (Mode == PossibleDrillActions.esNextHierarchy)
                OpenNextHierarchy(RealMember);
        }

        internal void DrillAnywhereAction_Inner(PossibleDrillActions Mode, CellsetMember RealMember, Level toLevel)
        {
            DrillAction_Inner(Mode, RealMember);
            if (Mode == PossibleDrillActions.esNextLevel)
            {
                var h = RealMember.FMember.Level.Hierarchy;
                Level l = null;
                for (var i = RealMember.FMember.Level.Index + 1; i < h.Levels.Count; i++)
                    if (h.Levels[i].Visible)
                    {
                        l = h.Levels[i];
                        break;
                    }

                //if (l == null)
                //{
                //    l = h.Levels[RealMember.FMember.Level.Index + 1];
                //}

                if (l == null || l == toLevel)
                    return;

                foreach (var nextMember in l.Members)
                {
                    var cellsetMember = GetCellByMember(nextMember);
                    if (cellsetMember != null)
                        DrillAnywhereAction_Inner(Mode, cellsetMember, toLevel);
                }
            }

            if (Mode == PossibleDrillActions.esNextHierarchy && RealMember.FMember.MemberType != MemberType.mtMeasure)
            {
                IList<Hierarchy> hh = RealMember.FIsRow ? FGrid.FLayout.fRowAxis : FGrid.FLayout.fColumnAxis;

                var h = hh[hh.IndexOf(RealMember.FMember.FLevel.FHierarchy) + 1];
                Level l = null;
                for (var i = 0; i < h.Levels.Count; i++)
                    if (h.Levels[i].Visible)
                    {
                        l = h.Levels[i];
                        break;
                    }

                if (l == null || l == toLevel)
                    return;

                for (var i = 0; i < l.Members.Count; i++)
                {
                    var nextMember = l.Members[i];
                    var cellsetMember = GetCellByMember(nextMember);
                    if (cellsetMember != null)
                        DrillAnywhereAction_Inner(Mode, cellsetMember, toLevel);
                }
            }
        }

        private CellsetMember GetCellByMember(Member member)
        {
            for (var i = 0; i < RowCount; i++)
            for (var j = 0; j < ColumnCount; j++)
            {
                var cell = Cells(j, i) as MemberCell;
                if (cell != null && cell.Member == member)
                    return cell.RealMember;
            }

            return null;
        }
    }
}