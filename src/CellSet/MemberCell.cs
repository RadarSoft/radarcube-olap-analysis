using System;
using System.Collections.Generic;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.CellSet
{
    internal class MemberCell : IMemberCell
    {
        private List<CubeAction> fActions;
        internal IMemberCell ParentCell;
        internal IEnumerable<Member> ParentCells;

        internal MemberCell(CellSet ACellSet, CellsetMember M)
        {
            CellSet = ACellSet;
            Model = M;
#if DEBUG
            if (M != null && M.DisplayName != null && M.DisplayName.Contains("Ken"))
            {
                var cc = M.GetHashCode();
            }
#endif
        }

        internal CellsetMember Model { get; set; }

        internal CellsetMember RealMember
        {
            get
            {
                if (Model.FAtypicalBehavior && Model.FIsTotal)
                    return Model.FParent;
                var cm = Model;
                if (cm == null) return null;
                while (cm.FChildren.Count == 1 && cm.FChildren[0].Attribute != null)
                    cm = cm.FChildren[0];
                return cm;
            }
        }

        internal object Data
        {
            get
            {
                if (Model.FIsPager)
                    return null;
                return Model.Data;
            }
        }

        public bool IsGroup
        {
            get
            {
                if (Model.FMember == null)
                    return false;
                return Model.FMember.MemberType == MemberType.mtGroup;
            }
        }

        public bool IsInFrame => Model.IsInFrame;

        public int CurrentPage => Model.CurrentPage;

        public void PageTo(int page)
        {
            Model.PageTo(page);
        }

        public PossibleDrillActions PossibleDrillActions
        {
            get
            {
                if (CellSet.FGrid.Mode == OlapGridMode.gmQueryResult)
                    return PossibleDrillActions.esNone;
                if (CellSet.FGrid.CellsetMode == CellsetMode.cmChart)
                    return PossibleDrillActions.esNone;
                if (Model.FMember == null || Model.FMember.FMemberType == MemberType.mtMeasure)
                    return PossibleDrillActions.esNone;
                if (Model.FMember == null || Model.FMember.FMemberType == MemberType.mtMeasureMode)
                    return PossibleDrillActions.esNone;
                if (Model.FMember == null || Model.FMember.FMemberType == MemberType.mtCalculated)
                    return PossibleDrillActions.esNone;
                if (IsTotal)
                    return PossibleDrillActions.esNone;
                if (IsPager)
                    return PossibleDrillActions.esNone;
                if (Attribute != null)
                    return PossibleDrillActions.esNone;

                if (RealMember.FChildren.Count > 0)
                    if (RealMember.FChildren[0].FMember == null ||
                        RealMember.FChildren[0].FMember.MemberType != MemberType.mtMeasure)
                        return CellSet.FGrid.OnAllowDrillAction(this, PossibleDrillActions.esCollapsed);

                var Result = PossibleDrillActions.esNone;
                if (RealMember.FMember.FLevel.FHierarchy != null &&
                    CellSet.FGrid.Cube.HasMemberChildren(RealMember.FMember))
                    Result = PossibleDrillActions.esParentChild;

#if DEBUG
                if (Model.FMember.DisplayName == "111")
                {
                }

                if (Model.FMember.DisplayName == "Household")
                {
                }
                if (Model.FMember.DisplayName == "USA")
                {
                }
#endif

                var addition = PossibleDrillActions.esNextHierarchy;

                var H = RealMember.FMember.FLevel.FHierarchy;
                if (Model.FMember.MemberType != MemberType.mtGroup)
                {
                    if (Model.FMember.FLevel.Index + 1 < H.Levels.Count
                        && Model.FMember.MemberType != MemberType.mtGroup
                        && (Model.FMember.FCubeMember == null || Model.FMember.FCubeMember.fIsLeaf != true)
                    )
                        Result |= PossibleDrillActions.esNextLevel;
                }
                else
                {
                    addition = PossibleDrillActions.esNone;
                }

                var Axis = RealMember.FIsRow
                    ? H.Dimension.Grid.FLayout.fRowAxis
                    : H.Dimension.Grid.FLayout.fColumnAxis;

                for (var i = 0; i <= Axis.Count - 2; i++)
                    if (Axis[i] == H) return CellSet.FGrid.OnAllowDrillAction(this, Result | addition);

                if (Result == PossibleDrillActions.esNone)
                    return Result;
                return CellSet.FGrid.OnAllowDrillAction(this, Result);
            }
        }

        public InfoAttribute Attribute => Model == null ? null : Model.Attribute;


        public ILevelCell Level
        {
            get
            {
                if (Model.FLevel == null) return null;
                return new LevelCell(CellSet, Model.FLevel);
            }
        }

        public bool IsPager => Model.FIsPager;

        public IMemberCell HierarchyMemberCell
        {
            get
            {
                IMemberCell m = this;
                while (m != null)
                {
                    var mm = m.Member;
                    if (mm == null) return null;
                    if (mm.FMemberType == MemberType.mtMeasureMode ||
                        mm.FMemberType == MemberType.mtMeasure)
                        m = m.Parent;
                    else
                        return m;
                }
                return null;
            }
        }

        public string Comment
        {
            get
            {
                var s = string.Empty;
                if (Address != null)
                    CellSet.fComments.TryGetValue(Address, out s);
                return s;
            }
            set
            {
                if (Address != null)
                {
                    CellSet.fComments.Remove(Address);
                    CellSet.fComments.Add(Address, value);
                }
#if DEBUG
#endif
            }
        }

        public void DrillAction(PossibleDrillActions Mode)
        {
            DebugLogging.WriteLine("MemberCell.DrillAction({0})", Mode);

            if ((Mode & PossibleDrillActions) != Mode)
            {
                var S = "";
                if (Mode == PossibleDrillActions.esCollapsed)
                    S = RadarUtils.GetResStr("rsDrillAction0");
                if (Mode == PossibleDrillActions.esParentChild)
                    S = RadarUtils.GetResStr("rsDrillAction1");
                if (Mode == PossibleDrillActions.esNextLevel)
                    S = RadarUtils.GetResStr("rsDrillAction2");
                if (Mode == PossibleDrillActions.esNextHierarchy)
                    S = RadarUtils.GetResStr("rsDrillAction3");

                throw new Exception(
                    string.Format(RadarUtils.GetResStr("rsInadmissibleDrillAction"), S, Value));
            }

            CellSet.DrillAction_Inner(Mode, RealMember);
        }

        public void DrillUp()
        {
            DrillAction(PossibleDrillActions.esCollapsed);
        }

        public Member Member => Model.FMember;

        public byte Indent => Model.FIndent;

        public bool IsTotal
        {
            get
            {
                var mc = RealMember;
                while (mc != null)
                {
                    if (mc.FIsTotal)
                        return true;
                    mc = mc.FParent;
                }
                return false;
            }
        }

        public IMemberCell Parent
        {
            get
            {
                if (Model.FParent == null)
                    return null;
                if (Model.FIndent == 0)
                    return new MemberCell(CellSet, Model.FParent);
                var i = Model.FStartRow - 1;
                while (((IMemberCell) CellSet.Cells(Model.FStartColumn, i)).Indent == Model.FIndent)
                    i--;
                return (IMemberCell) CellSet.Cells(Model.FStartColumn, i);
            }
        }

        public IMemberCell Children(int Index)
        {
            return new MemberCell(CellSet, Model.FChildren[Index]);
        }

        public int ChildrenCount => Model.FChildren.Count;

        public IMemberCell Siblings(int Index)
        {
            if (Model == null) return this;
            var L = Model.GetList();
            if (L.FSiblingsCount <= Index || Index < 0) throw new ArgumentOutOfRangeException();
            if (L[0].FIsTotal)
            {
                if (Index + 2 > L.Count) throw new ArgumentOutOfRangeException();
                return new MemberCell(CellSet, L[Index + 1]);
            }
            var CS = L[Index];
            if (CS.FIsTotal) throw new ArgumentOutOfRangeException();
            return new MemberCell(CellSet, CS);
        }

        public int SiblingsCount
        {
            get
            {
                if (Model == null)
                    return 1;
                return Model.GetList().FSiblingsCount;
            }
        }

        public int SiblingsOrder
        {
            get
            {
                if (Model == null)
                    return -1;
                return Model.FSiblingsOrder;
            }
        }

        public IMemberCell NextMember
        {
            get
            {
                var L = Model.GetList();
                var i = L.IndexOf(Model);
                if (i < 0 || i >= L.FSiblingsCount - 1) return null;
                var CS = L[i + 1];
                if (CS.FMember == null) return null;
                if (CS.FMember.FParent == Model.FMember.FParent) return new MemberCell(CellSet, CS);
                return null;
            }
        }

        public IMemberCell PrevMember
        {
            get
            {
                List<CellsetMember> L = Model.GetList();
                var i = L.IndexOf(Model);
                if (i < 1)
                    return null;
                var CS = L[i - 1];
                if (CS.FMember == null || Model.FMember == null)
                    return null;
                if (CS.FMember.FParent == Model.FMember.FParent)
                    return new MemberCell(CellSet, CS);
                return null;
            }
        }

        public bool IsLeaf => Model.FChildren.Count == 0;

        public ICubeAddress Address
        {
            get
            {
                if (CellSet.FGrid.Mode == OlapGridMode.gmQueryResult)
                    return null;
                return Model.FLevel == null || Model.FLineID == null ? null : Model.GetAddress();
            }
        }

        public LayoutArea Area => Model.FStartRow >= CellSet.FFixedRows ? LayoutArea.laRow : LayoutArea.laColumn;

        public string Description
        {
            get
            {
                var memberDescription = Model.FMember == null ? null : Model.FMember.Description;
                var s = memberDescription;
                if (
                    //(fCellSet.Grid.HierarchiesDisplayMode == HierarchiesDisplayMode.TreeLike) &&
                    Model.FMember != null)
                    s += Model.FMember.ExtractAttributesAsTooltip(memberDescription, false);
                return s;
            }
        }

        public int StartRow => Model.FStartRow;

        public int StartColumn => Model.FStartColumn;

        public int PagedStartColumn
        {
            get
            {
                int i;
                if (CellSet.AdjustedColsHelper.TryGetValue(Model.FStartColumn, out i))
                    return i;
                throw new Exception("Invalid paging index conversion");
            }
        }

        public int PagedStartRow
        {
            get
            {
                int i;
                if (CellSet.AdjustedRowsHelper.TryGetValue(Model.FStartRow, out i))
                    return i;
                throw new Exception("Invalid paging index conversion");
            }
        }

        public int RowSpan => Model.GetRowSpan();

        public int ColSpan => Model.GetColSpan();

        public IEnumerable<ICellValue> Values
        {
            get
            {
                if (CellSet.Grid.HierarchiesDisplayMode == HierarchiesDisplayMode.TreeLike && Model.FMember != null)
                    return Model.FMember.GetAttributesAsObjects();
                return null;
            }
        }

        public IEnumerable<ICellValue> Descriptions
        {
            get
            {
                if (Model.FMember != null)
                    return Model.FMember.GetToolTipAttributesAsObjects();
                return null;
            }
        }

        public string Value
        {
            // from winforms for infoattributer in treelike grid mode
            get
            {
                if (Model.FIsPager)
                    return "...";
                var s = Model.DisplayName;
                if (CellSet.Grid.HierarchiesDisplayMode == HierarchiesDisplayMode.TreeLike && Model.FMember != null)
                    s += Model.FMember.GetAttributesAsColumn();
                return s;
            }
        }

        public CellType CellType => CellType.ctMember;

        public CellSet CellSet { get; }

        public List<CubeAction> CubeActions
        {
            get
            {
                if (fActions != null)
                    return fActions;
                fActions = CellSet.FGrid.Cube.RetrieveActions(this);
                return fActions;
            }
        }

        internal void ExpandNodesAnywhere(PossibleDrillActions Mode, Level toLevel)
        {
            CellSet.DrillAnywhereAction_Inner(Mode, RealMember, toLevel);
        }
    }
}