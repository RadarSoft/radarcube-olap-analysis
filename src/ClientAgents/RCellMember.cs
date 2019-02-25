using System.ComponentModel;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class RCellMember
    {
        [DefaultValue(OlapFilterCondition.fcGreater)]
        public OlapFilterCondition Condition = OlapFilterCondition.fcGreater;

        [DefaultValue(0)] public int CurrentPage;

        [DefaultValue("0")] public string FirstValue = "0";

        [DefaultValue(true)] public bool IsAggregatesRestricted = true;

        [DefaultValue(0)] public int ItemsInPage;

        [DefaultValue("")] public string LevelUniqueName = "";

        [DefaultValue("0")] public string SecondValue = "0";

        [DefaultValue(0)] public int SiblingsCount;

        [DefaultValue("")] public string SortFlag = "";

        public RCellMember()
        {
        }

        public RCellMember(IMemberCell mc)
        {
            if (mc.IsPager)
            {
                var MC = mc as MemberCell;
                CurrentPage = mc.CurrentPage;
                ItemsInPage = MC.Level.Level.PagerSettings.LinesInPage;
                SiblingsCount = mc.SiblingsCount;
            }

            if (mc.Member != null && mc.Member.MemberType == MemberType.mtMeasure)
            {
                var m = mc.CellSet.Grid.Measures.Find(mc.Member.UniqueName);
                if (m.Filter != null)
                {
                    Condition = m.Filter.FilterCondition;
                    FirstValue = m.Filter.FirstValue;
                    SecondValue = m.Filter.SecondValue;
                    IsAggregatesRestricted = m.Filter.RestrictsTo == MeasureFilterRestriction.mfrAggregatedValues;
                }
            }
            if (mc.Member != null && mc.Member.MemberType != MemberType.mtMeasureMode &&
                mc.Member.MemberType != MemberType.mtMeasure)
            {
                var m = mc.Member;
                if (m.Level != null && m.Level.Hierarchy != null)
                    if (m.Level.Hierarchy.Origin == HierarchyOrigin.hoParentChild)
                        if (m.CubeMember != null && m.Level.Hierarchy.CubeHierarchy.FMDXLevelNames.Count > 0)
                            LevelUniqueName =
                                m.Level.Hierarchy.CubeHierarchy.FMDXLevelNames[m.CubeMember.FMDXLevelIndex];
                        else
                            LevelUniqueName = m.Level.UniqueName;
                    else
                        LevelUniqueName = m.Level.UniqueName;
            }

            if (mc.Area == LayoutArea.laColumn && mc.IsLeaf && mc.CellSet.Grid.CellsetMode == CellsetMode.cmGrid)
            {
                SortFlag = mc.StartColumn.ToString();
                if (mc.CellSet.ValueSortedColumn == mc.StartColumn)
                    if (mc.CellSet.ValueSortingDirection == ValueSortingDirection.sdAscending)
                        SortFlag += "|1";
                    else
                        SortFlag += "|-1";
            }
        }
    }
}