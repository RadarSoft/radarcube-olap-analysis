using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Controls.Cube.Md;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Tools;
using RadarSoft.XmlaClient.Metadata;
using Hierarchy = RadarSoft.RadarCube.Layout.Hierarchy;
using HierarchyOrigin = RadarSoft.RadarCube.Enums.HierarchyOrigin;
using Level = RadarSoft.RadarCube.Layout.Level;
using Measure = RadarSoft.RadarCube.Layout.Measure;
using Member = RadarSoft.RadarCube.Layout.Member;

namespace RadarSoft.RadarCube.Engine.Md
{
    /// <exclude />
    [Serializable]
    public class MdEngine : Engine
    {
        internal MdEngine(Controls.Cube.RadarCube ACube, OlapControl AGrid)
            : base(ACube, AGrid)
        {
        }

        public MdEngine()
        {
        }

        private void RetrieveLine2_MSAS(
            Dictionary<Level, HashSet<Member>> src, Line ALine, Hierarchy intelligenceh, StringBuilder WHERE,
            List<long> idxmul, List<Level> idxlevels, MOlapCube cube,
            ref StringBuilder WITH, List<Hierarchy> sources,
            string MeasureName, ref StringBuilder SELECT, ref long idx, ref StringBuilder SUBCUBE)
        {
            //DebugLogging.WriteLine("MdEngine.RetrieveLine2_MSAS(aline={0})", ALine.ToString());

            var single = true;
            var subcubeCounter = 0;
            var lh = new List<Hierarchy>(2);

            if (cube.Is2000)
            {
                WHERE.Append(MeasureName);

                foreach (var h in sources) // visual totals
                    if (h.Filtered)
                    {
                        cube.MakeVisualTotals(h, WITH);
                        lh.Add(h);
                    }
            }
            else
            {
                if (ALine.Measure.IsKPI)
                {
                    switch (ALine.fMode.Mode)
                    {
                        case MeasureShowModeType.smKPIValue:
                        case MeasureShowModeType.smSpecifiedByEvent:
                            WHERE.Append("KPIValue");
                            break;
                        case MeasureShowModeType.smKPIGoal:
                            WHERE.Append("KPIGoal");
                            break;
                        case MeasureShowModeType.smKPIStatus:
                            WHERE.Append("KPIStatus");
                            break;
                        case MeasureShowModeType.smKPITrend:
                            WHERE.Append("KPITrend");
                            break;
                        case MeasureShowModeType.smKPIWeight:
                            WHERE.Append("KPIWeight");
                            break;
                    }
                    WHERE.Append("(\"");
                    WHERE.Append(ALine.Measure.UniqueName);
                    WHERE.Append("\")");
                }
                else
                {
                    WHERE.Append(MeasureName);
                }
            }

            var lh2 = new List<Hierarchy>();
            var singles = new SortedList<string, Member>();
            foreach (var h in FGrid.FFilteredHierarchies) // subcube filtered
            {
                Member m = null;
                var _done = false;

                if (cube.Is2000)
                {
                    if (lh.Contains(h))
                        continue; // already processed (visualtotals)

                    foreach (var probe in src)
                    {
                        bool ss;
                        if (probe.Key.Hierarchy == h)
                        {
                            _done = true;
                            cube.MakeWhere(h, WITH, WHERE, probe.Value, out ss);
                            single = single && ss;
                            if (single && probe.Value.Count == 1)
                                if (m != null)
                                    single = false;
                                else
                                    m = probe.Value.First();
                        }
                    }
                }
                else
                {
                    if (h.RetrieveFilteredMember() != null && !IsHierarchyInLevels(h, ALine.Levels))
                        foreach (var probe in src)
                            if (probe.Key.Hierarchy == h)
                            {
                                cube.MakeWhere(h, WITH, WHERE, probe.Value, out single);
                                if (single)
                                {
                                    var m1 = probe.Value.First();
                                    singles.Add(h.UniqueName, m1);
                                    if (h == intelligenceh && WITH.ToString().Contains("{0}"))
                                        // sets restriction member to the intelligence script
                                        WITH = new StringBuilder(WITH.ToString().Replace("{0}", m.UniqueName));
                                }
                                lh2.Add(h);
                                break;
                            }
                }

                if (cube.Is2000)
                {
                    if (!_done)
                        cube.MakeWhere(h, WITH, WHERE, null, out single);

                    if (single)
                    {
                        singles.Add(h.UniqueName, m);
                        if (h == intelligenceh && WITH.ToString().Contains("{0}"))
                            WITH = new StringBuilder(WITH.ToString().Replace("{0}", m.UniqueName));
                    }
                    lh2.Add(h);
                }
                else
                {
                    if (lh2.Contains(h))
                        continue;

                    if (SUBCUBE == null)
                        SUBCUBE = new StringBuilder("SELECT ");
                    if (subcubeCounter > 0)
                    {
                        SUBCUBE.Append(",");
                        SUBCUBE.AppendLine();
                    }
                    SUBCUBE.Append("{");
                    //SUBCUBE.Append(cube.DoSubcubeFilter(h, m));
                    SUBCUBE.Append(cube.DoSubcubeFilter(h, null));
                    SUBCUBE.Append("} ON ");
                    SUBCUBE.Append(subcubeCounter++);
                    lh2.Add(h);
                }
            }

            string contextsubcube = null;

            if (cube.Is2000)
            {
                foreach (var probe in src) // apply other restrictions
                {
                    if (probe.Value.Count > 1 || probe.Key.Hierarchy == null)
                        continue;

                    if (!lh2.Contains(probe.Key.Hierarchy))
                    {
                        if (sources.Contains(probe.Key.Hierarchy) &&
                            (!ALine.Levels.Contains(probe.Key) ||
                             probe.Key.Hierarchy.Origin == HierarchyOrigin.hoParentChild))
                            continue;

                        if (probe.Key.Hierarchy.Origin == HierarchyOrigin.hoParentChild)
                            if (probe.Value.Count == 1 && ALine.Levels.Contains(probe.Key))
                                if (probe.Value.First().Depth == ALine.fDepthes[ALine.Levels.IndexOf(probe.Key)] - 1)
                                    continue;

                        if (src.Keys.Any(item => item.Hierarchy == probe.Key.Hierarchy && item.Index > probe.Key.Index))
                            continue;

                        cube.MakeWhere(probe.Key.Hierarchy, WITH, WHERE, probe.Value, out single);
                        if (!single)
                            continue;
                    }
                    if (!singles.ContainsKey(probe.Key.Hierarchy.UniqueName) && probe.Value.Count == 1)
                    {
                        singles.Add(probe.Key.Hierarchy.UniqueName, probe.Value.First());
                        if (probe.Key.Hierarchy == intelligenceh && WITH.ToString().Contains("{0}"))
                            WITH = new StringBuilder(WITH.ToString().Replace("{0}", probe.Value.First().UniqueName));
                    }
                    idx += probe.Value.First().ID * ALine.Multipliers[ALine.Levels.IndexOf(probe.Key)];
                }
            }
            else
            {
                contextsubcube = cube.GetContextFilterSubcube(FGrid);

                if (SUBCUBE != null)
                {
                    SUBCUBE.AppendLine();
                    SUBCUBE.Append("FROM ");
                    SUBCUBE.Append(contextsubcube);
                }
            }

            // make crossjoin
            var b = false;
            for (var i = 0; i < ALine.Levels.Count; i++)
            {
                var l = ALine.Levels[i];

                if (singles.ContainsKey(l.Hierarchy.UniqueName))
                    continue;
                idxlevels.Add(l);
                idxmul.Add(ALine.Multipliers[ALine.Levels.IndexOf(l)]);
                if (b)
                    SELECT.Append("*");

                var me = new HashSet<Member>();
                foreach (var probe in src)
                    if (probe.Key.Hierarchy == l.Hierarchy)
                    {
                        me = probe.Value;
                        break;
                    }
                SELECT.Append(cube.MakeCrossjoin(l, WITH, me, ALine.fDepthes[i]));
                if (l.Hierarchy == intelligenceh && WITH.ToString().Contains("{0}"))
                    // sets "currentmember" for the crossjoin hierarchy to the intelligence script
                    WITH = new StringBuilder(WITH.ToString().Replace("{0}", l.Hierarchy.UniqueName + ".CURRENTMEMBER"));
                b = true;
            }

            if (idxlevels.Count == 0)
            {
                SELECT = new StringBuilder("SELECT ");
            }
            else
            {
                if (cube.MDXCellsetThreshold > 0 && cube.Is2000 == false)
                    SELECT.Append("}), "
                                  + cube.MDXCellsetThreshold
                                  + ")} DIMENSION PROPERTIES MEMBER_TYPE ON 0 ");
                else
                    SELECT.Append("} DIMENSION PROPERTIES MEMBER_TYPE ON 0 ");
            }

            if (cube.Is2000)
            {
                SELECT.AppendLine();
                SELECT.Append("FROM ");
                SELECT.Append(cube.ApplySubcubeFilter());
                SELECT.AppendLine();

                WHERE.Append(")");
            }
            else
            {
                if (SUBCUBE == null)
                {
                    SELECT.Append("FROM ");
                    SELECT.Append(contextsubcube);
                }
                else
                {
                    SELECT.Append("FROM (");
                    SELECT.Append(SUBCUBE);
                    SELECT.Append(")");
                }
                SELECT.AppendLine();

                WHERE.Append(")");
            }
        }


        internal override bool IsNativeDataPresent(MeasureShowMode mode)
        {
            switch (mode.Mode)
            {
                case MeasureShowModeType.smKPIGoal:
                    return true;
                case MeasureShowModeType.smKPIStatus:
                    return true;
                case MeasureShowModeType.smKPITrend:
                    return true;
                case MeasureShowModeType.smKPIValue:
                    return true;
                case MeasureShowModeType.smKPIWeight:
                    return true;
                case MeasureShowModeType.smNormal:
                    return true;
            }
            if (mode.LinkedIntelligence != null) return true;
            return false;
        }

        internal override bool CalculatedByServer(Measure m)
        {
            return m.AggregateFunction != OlapFunction.stCalculated || !string.IsNullOrEmpty(m.Expression);
        }

        /// <summary>
        ///     Returns formatted and unformatted values of the cells from the current OLAP
        ///     slice.
        /// </summary>
        /// <returns>True, if the cell contains a value</returns>
        /// <param name="CurrentCell">The data cell from the current OLAP slice</param>
        /// <param name="Value">An unformatted value of the cell</param>
        /// <param name="Formatted">Formatting properties of the cell</param>
        public override bool GetCellFormattedValue(IDataCell CurrentCell, out object Value,
            out CellFormattingProperties Formatted)
        {
            var Address = CurrentCell.Address;

            Value = null;
            var M = GetMetaline(Address.FLevelsAndMembers.Keys);
            Formatted = new CellFormattingProperties("");
            if (Address.Measure == null) return true;
            if (CalculatedByServer(Address.Measure))
                if (IsNativeDataPresent(Address.MeasureMode))
                {
                    var L = M.GetLine(Address.FHierID, Address.Measure, Address.MeasureMode) as MdLine;
                    var Result = L.GetCellFormatted(Address, out Value, out Formatted);
                    if (!Result)
                        return GetCalculatedCellValue(Address, out Value, out Formatted);
                    return true;
                }
                else
                {
                    var L = M.GetLine(Address.FHierID, Address.Measure,
                        IsNativeDataPresent(Address.MeasureMode)
                            ? Address.MeasureMode
                            : Address.Measure.ShowModes[0]) as MdLine;
                    object V;
                    var Result = L.GetCell(Address, out Value);
                    if (Value != null)
                    {
                        Formatted.FormattedValue =
                            Address.Measure.DoFormatMode(CurrentCell, Value, Address.MeasureMode, out V);
                        Value = V;
                        return true;
                    }
                    return false;
                }
            return GetCalculatedCellValue(Address, out Value, out Formatted);
        }

        internal override MetaLine CreateMetaline(OlapControl AGrid, IList<int> LevelIndexes)
        {
            return new MdMetaLine(FGrid, LevelIndexes);
        }

        /// <summary>
        ///     Performs the Drillthrough operation against the cube and returns a DataTable
        ///     filled with source records aggregated by the cube cell with the specified
        ///     address.
        /// </summary>
        /// <param name="Address">The cube cell address.</param>
        /// <param name="dataTable">The DataTable to be filled with source records.</param>
        /// <param name="RowsToFetch">When more than 0, limits the max number of records in the resulting DataTable.</param>
        /// <param name="columns">When defined, limits the columns to include into the resulting DataTable.</param>
        /// <param name="DrillThroughMethod">Defines the method to use when fetching records from the source data schema.</param>
        public override void Drillthrough(ICubeAddress Address, DataTable dataTable, int RowsToFetch,
            ICollection<string> columns, DrillThroughMethod DrillThroughMethod)
        {
            Drillthrough(Address, dataTable, RowsToFetch, columns);
        }


        public override void Drillthrough(IList<ICubeAddress> addresses, IList<Measure> measures, DataTable dataTable,
            int RowsToFetch, ICollection<string> columns, DrillThroughMethod DrillThroughMethod)
        {
        }

        /// <summary>
        ///     Retrieves the source data that was used to create a specified cell in a cube
        /// </summary>
        /// <param name="Address"></param>
        /// <param name="AdataSet"></param>
        /// <param name="RowsToFetch"></param>
        public override void Drillthrough(ICubeAddress Address, DataTable AdataSet, int RowsToFetch,
            ICollection<string> columns)
        {
            ((MOlapCube) FCube).EnsureConnected();
            var sb = new StringBuilder("DRILLTHROUGH ");
            if (RowsToFetch > 0)
            {
                sb.Append("MAXROWS ");
                sb.Append(RowsToFetch);
            }
            sb.Append(" SELECT {");
            sb.Append(Address.Measure.UniqueName);
            sb.Append("} on 0");
            for (var i = 0; i < Address.LevelsCount; i++)
            {
                sb.Append(", {");
                var m = Address.Members(i);
                if (m.Filtered)
                {
                    bool single;
                    string set;
                    ((MOlapCube) FCube).CreateVisibleSet(m.FLevel.FHierarchy,
                        out single, out set, m, false);
                    if (!single)
                        throw new Exception(string.Format(RadarUtils.GetResStr("rsMuitifilterDrillthroughError"),
                            m.FLevel.FHierarchy.DisplayName));
                    sb.Append(set);
                }
                else
                {
                    sb.Append(Address.Members(i).UniqueName);
                }
                sb.Append("} on ");
                sb.Append(i + 1);
            }
            var j = Address.LevelsCount;
            for (var i = 0; i < FGrid.FFilteredHierarchies.Count; i++)
            {
                if (Address.GetMemberByHierarchy(FGrid.FFilteredHierarchies[i]) != null) continue;
                bool single;
                string set;
                ((MOlapCube) FCube).CreateVisibleSet(FGrid.FFilteredHierarchies[i],
                    out single, out set, new HashSet<Member>(), false);
                if (!single)
                    throw new Exception(
                        string.Format(RadarUtils.GetResStr("rsMuitifilterDrillthroughError"),
                            FGrid.FFilteredHierarchies[i].DisplayName));
                sb.Append(", {");
                sb.Append(set);
                sb.Append("} on ");
                sb.Append(++j);
            }

            sb.Append(" FROM ");
            sb.Append(((MOlapCube) FCube).ApplySubcubeFilter());

            if (columns != null && columns.Count > 0)
            {
                sb.Append(" RETURN ");
                var b = false;
                foreach (var s in columns)
                {
                    if (b)
                        sb.Append(", ");
                    else
                        b = true;
                    sb.Append(s);
                }
            }

            Drillthrough(AdataSet, sb.ToString());
        }

        public override void Drillthrough(DataTable AdataSet, string mdx)
        {
            //AdomdDataReader CS = ((MOlapCube)FCube).ExecuteMDXReader(mdx);

            //AdataSet.Clear();
            //AdataSet.Columns.Clear();

            //for (int i = 0; i < CS.FieldCount; i++)
            //{
            //    DataColumn dc = new DataColumn(CS.GetName(i), CS.GetFieldType(i));
            //    AdataSet.Columns.Add(dc);
            //}
            //while (CS.Read())
            //{
            //    DataRow dr = AdataSet.NewRow();
            //    for (int i = 0; i < CS.FieldCount; i++)
            //    {
            //        object o = CS[i];
            //        dr[i] = o ?? DBNull.Value;
            //    }
            //    AdataSet.Rows.Add(dr);
            //}
            //CS.Close();
        }

        /// <summary>
        ///     Updates the fact table contents for the records aggregated in the Cube cell with
        ///     a specified address
        /// </summary>
        public override void Writeback(ICubeAddress Address, object NewValue, WritebackMethod Method)
        {
            ((MOlapCube) FCube).EnsureConnected();
            var sb = new StringBuilder("UPDATE CUBE [");
            sb.Append(((MOlapCube) FCube).CubeName);
            sb.Append("] SET (");
            sb.Append(Address.Measure.UniqueName);
            for (var i = 0; i < Address.LevelsCount; i++)
            {
                sb.Append(",");
                sb.Append(Address.Members(i).UniqueName);
            }
            sb.Append(") = ");
            var nv = Convert.ToDouble(NewValue);
            sb.Append(nv.ToString(CultureInfo.InvariantCulture.NumberFormat));
            sb.Append(" ");

            switch (Method)
            {
                case WritebackMethod.wmEqualAllocation:
                    sb.Append(" USE_EQUAL_ALLOCATION");
                    break;
                case WritebackMethod.wmEqualIncrement:
                    sb.Append(" USE_EQUAL_INCREMENT");
                    break;
                case WritebackMethod.wmWeightedAllocation:
                    sb.Append(" USE_WEIGHTED_ALLOCATION");
                    break;
                case WritebackMethod.wmWeightedIncrement:
                    sb.Append(" USE_WEIGHTED_INCREMENT");
                    break;
            }

            ((MOlapCube) FCube).ExecuteMDXCommand(sb.ToString());
            Clear();
            RebuildGridMembers();
        }

        private long GetLineIdx(MemberCollection ms, Line ALine)
        {
            long Result = 0;
            for (var i = 0; i < ms.Count; i++)
            {
                var L = ALine.Levels[i];
                var M = L.FindMember(ms[i].UniqueName);
                if (M == null) return -1;
                Result += M.ID * ALine.Multipliers[i];
            }
            return Result;
        }

        private bool IsHierarchyInLevels(Hierarchy h, List<Level> levels)
        {
            foreach (var l in levels)
                if (l.Hierarchy == h) return true;
            return false;
        }

        internal override void RetrieveLine2(Dictionary<Level, HashSet<Member>> src, Line ALine)
        {
            DebugLogging.WriteLine("MdEngine.RetrieveLine2(line={0})", ALine.ToString());

            var line = ALine as MdLine;
            //ICubeAddress Restriction = new ICubeAddress(ALine.MetaLine.Grid, restriction);

            var sources = new List<Hierarchy>();
            foreach (var l1 in ALine.Levels)
                if (!src.ContainsKey(l1)) sources.Add(l1.Hierarchy);

            var cube = (MOlapCube) FGrid.Cube;
            cube.EnsureConnected();

            var QUERY = new StringBuilder();
            var WITH = new StringBuilder();
            var WHERE = new StringBuilder("WHERE (");
            StringBuilder SUBCUBE = null;
            StringBuilder SELECT;
            var intelligenceh = ALine.fMode.LinkedIntelligence == null ? null : ALine.fMode.LinkedIntelligence.fParent;
            var MeasureName = ALine.fMode.LinkedIntelligence == null
                ? ALine.Measure.UniqueName
                : cube.MakeIntelligenceMember(ALine, WITH);

            if (ALine.fMode.Mode == MeasureShowModeType.smNormal && ALine.Measure.Filter != null)
                cube.MakeMeasureFilter(ALine.Measure, WITH, out MeasureName);

            if (ALine.Measure.AggregateFunction == OlapFunction.stCalculated &&
                !string.IsNullOrEmpty(ALine.Measure.Expression))
                cube.MakeCalculatedMeasure(ALine.Measure, WITH);

            if (cube.MDXCellsetThreshold > 0 && !cube.Is2000)
                SELECT = new StringBuilder("SELECT {HEAD(NONEMPTY({");
            else
                SELECT = new StringBuilder("SELECT NON EMPTY {");
            // GetLineIDX info
            var idxlevels = new List<Level>(src.Count + sources.Count);
            var idxmul = new List<long>(src.Count + sources.Count);
            long idx = 0;

            //if (!cube.Is2000) //MSAS 2000
            //{
            //    #region MSAS 2000
            //    WITH = RetrieveLine2_MSAS2000(
            //        src, ALine, intelligenceh, WHERE, idxmul, idxlevels,
            //        cube, WITH, sources, MeasureName, ref SELECT, ref idx);

            //    #endregion
            //}
            //else //MSAS 2005
            //{
            //    #region MSAS 2005

            //    RetrieveLine2_MSAS2005(
            //        src, ALine, cube, ref WITH, WHERE, ref SUBCUBE, ref SELECT,
            //        intelligenceh, MeasureName, idxlevels, idxmul);
            //    #endregion
            //}

            RetrieveLine2_MSAS(src, ALine, intelligenceh, WHERE, idxmul, idxlevels, cube, ref WITH, sources,
                MeasureName, ref SELECT, ref idx, ref SUBCUBE);

            QUERY.Append(WITH);
            QUERY.Append(SELECT);
            QUERY.Append(WHERE);
            QUERY.Append(cube.ApplyCellProperties());

            var css = cube.ExecuteMDXCellset(QUERY.ToString(), true);

            if (css.Cells.Count == cube.MDXCellsetThreshold && cube.MDXCellsetThreshold > 0)
                cube.MDXCellsetThresholdReached = true;

            if (css.Cells.Count == 0)
                return;

            if (css.Axes.Count == 0)
            {
                var c = new WrappedCell(css.Cells[0]);
                CellColorSettings mcs = null;
                CellFontSettings mfs = null;

                if (c == null || c.Value == null)
                    return;

                ALine.StartMergeSeries(1);

                line.AddData(idx, new LineData(c.Value, c.FormattedValue, mcs, mfs));

                ALine.EndMergeSeries();
                return;
            }
            var cc = css.Cells;
            var tc = css.Axes[0].Set.Tuples;
            ALine.StartMergeSeries(cc.Count);
            for (var i = 0; i < cc.Count; i++)
            {
                var c = new WrappedCell(css.Cells[i]);
                if (c == null || c.Value == null)
                    continue;

                var mc = tc[i].Members;
                var Idx = idx;
                for (var j = 0; j < mc.Count; j++)
                {
                    var L = idxlevels[j].CubeLevel;
                    var m = idxlevels[j].FindMember(mc[j].UniqueName);
                    CubeMember M = null;
                    if (m == null)
                    {
                        var m_ = mc[j];
                        //                        if ((m_.Type == MemberTypeEnum.All) || (m_.UniqueName.EndsWith(".UNKNOWNMEMBER")))
                        if (m_.Type == MemberTypeEnum.All)
                        {
                            Idx = -1;
                            break;
                        }
                        if (m_.LevelName != L.UniqueName && L.Hierarchy.Origin == HierarchyOrigin.hoUserDefined)
                        {
                            L = L.Hierarchy.Levels.Find(m_.LevelName);
                            M = L.FindMemberByUniqueName(m_.UniqueName);
                        }
                        if (M == null)
                        {
                            M = new CubeMember(L.Hierarchy, L, m_.Caption, m_.Description, m_.UniqueName, m_.Name,
                                false, m_.LevelName);
                            var parent = m_.Parent == null
                                ? null
                                : L.Hierarchy.FindMemberByUniqueName(m_.Parent.UniqueName);
                            if (m_.Type == MemberTypeEnum.Formula)
                                M.IsMDXCalculated = true;
                            //if ((parent == null) && (idxlevels[j].Index > 0))
                            //{
                            //    object pname = m_.Properties["PARENT_MEMBER_TYPE"].Value;
                            //    if (pname != null)
                            //        parent = L.Hierarchy.FindMemberByUniqueName(pname.ToString());
                            //}
                            if (parent == null)
                            {
                                L.Members.Add(M);
                            }
                            else
                            {
                                if (L == parent.ParentLevel)
                                {
                                    cube.SetCubeMemberParent(M, parent);
                                }
                                else
                                {
                                    var H = L.Hierarchy;
                                    cube.SetCubeMemberParent(L.Hierarchy, H.Levels.IndexOf(M.ParentLevel),
                                        H.Levels.IndexOf(parent.ParentLevel), M.UniqueName, parent.UniqueName);
                                    L.Members.Add(M);
                                }
                            }
                        }
                    }
                    if (m != null)
                        Idx += m.ID * idxmul[j];
                    else
                        Idx += M.ID * idxmul[j];
                }
                CellColorSettings mcs = null;
                CellFontSettings mfs = null;
                if (Idx >= 0)
                    line.AddData(Idx, new LineData(c.Value, c.FormattedValue, mcs, mfs));
                for (var k = 0; k < ALine.Levels.Count; k++)
                    ALine.Levels[k].CreateNewMembersLight(false);
            }
            for (var k = 0; k < ALine.Levels.Count; k++)
                ALine.Levels[k].CreateNewMembersLight(true);
            ALine.EndMergeSeries();
        }
    }
}