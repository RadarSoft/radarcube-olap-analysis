using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Engine.Md;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Events;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Engine
{
    /// <summary>
    ///     This object serves as a layer between the Grid and the Cube - the data source, a
    ///     successor to the RadarCube component.
    /// </summary>
    /// <remarks>
    ///     Provides a set of low-level methods that allows direct access to the Cube
    ///     cells.
    /// </remarks>
    //[Serializable]
    public abstract class Engine : IStreamedObject
    {
        private int[] cache_indexes;
        private MetaLine cache_ml;

        [NonSerialized] internal Controls.Cube.RadarCube FCube;

        [NonSerialized] internal OlapControl FGrid;

        internal List<Level> FLevelsList = new List<Level>();
        internal SortedList<string, MetaLine> FMetaLines = new SortedList<string, MetaLine>();

        internal Engine()
        {
            DebugLogging.WriteLine("Engine.ctor()");
        }

        internal Engine(Controls.Cube.RadarCube ACube, OlapControl AGrid)
            : this()
        {
            FCube = ACube;
            FGrid = AGrid;
        }

        /// <summary>References to the OlapControl object containing the specified object.</summary>
        public OlapControl Grid => FGrid;

        private string CreateLevelString(IList<Member> Restriction, IList<Level> sources, out IList<int> LineIndexes)
        {
            // the <hierarchy list, index of requested levels in the "ll" list>
            var hi = new SortedList<string, int>(Restriction.Count + sources.Count);
            // <the unsorted list of levels> which belong to unique hierarchies
            var ll = new List<Level>(Restriction.Count + sources.Count);

            for (var i = 0; i < Restriction.Count; i++)
            {
                var l = Restriction[i].Level;
                hi.Add(l.Hierarchy.UniqueName, i);
                ll.Add(l);
            }

            foreach (var l in sources)
            {
                int i;
                if (hi.TryGetValue(l.Hierarchy.UniqueName, out i))
                {
                    ll[i] = l;
                }
                else
                {
                    hi.Add(l.Hierarchy.UniqueName, ll.Count);
                    ll.Add(l);
                }
            }
            // the sorted <list of level IDs, level unique names>
            var sl = new SortedList<int, string>(ll.Count);
            foreach (var l in ll)
                sl.Add(l.CubeLevel.fID, l.UniqueName);
            var sb = new StringBuilder();
            for (var i = 0; i < sl.Count; i++)
            {
                sb.Append(sl.Values[i]);
                sb.Append("|");
            }
            LineIndexes = sl.Keys;
            return sb.ToString();
        }

        internal void RestoreAfterSerialization(OlapControl grid)
        {
            var old = grid.Engine;
            FGrid = grid;
            FCube = grid.Cube;

            FCube.FEngineList.Remove(old);
            FCube.FEngineList.Add(this);
            if (FCube.fCallbackException != null)
                FGrid.callbackException = FCube.fCallbackException;

            FGrid.FEngine = this;
            foreach (var m in FMetaLines.Values)
                m.FGrid = grid;
        }

        //internal List<CubeDataNumeric> RetrieveCubeData(List<Member> restriction, List<Level> levels,
        //    List<int> depthes, MeasureShowMode mode, out List<Level> level_clue)
        //{
        //    Line l = GetLineForChart(restriction, levels, depthes, mode, false);


        //    level_clue = l.fM.fLevels;
        //    List<CubeDataNumeric> data = l.RetrieveCubeData(restriction);
        //    ProcessCalculatedMembers(l, data);
        //    return data;
        //}

        private Line GetLineForChart(List<Member> restriction, List<Level> levels, IList<int> depthes,
            MeasureShowMode mode, bool isRequest)
        {
            IList<int> il;
            var ls = CreateLevelString(restriction, levels, out il);

            var ml = GetMetaline(il);
            var l = ml.GetLine(ml.GetHierIDFromRestriction(restriction, levels, depthes), mode.Measure, mode);
            if (isRequest)
                l.AddRequest(restriction);
            return l;
        }

        private void ProcessCalculatedMeasures(Line l, List<CubeDataNumeric> data)
        {
            var hidx = new HashSet<long>();

            foreach (var m in l.Measure.AffectedMeasures())
            {
                var cl = l.MetaLine.GetLine(l.fHierId, m, m.ShowModes[0]);
                if (l == cl) continue;

                foreach (var d in cl.RetrieveCubeData(null))
                {
                    if (hidx.Contains(d.LineIdx)) continue;
                    hidx.Add(d.LineIdx);
                    var lm = new List<Member>(d.MemberIDs.Length);
                    for (var i = 0; i < d.MemberIDs.Length; i++)
                        lm.Add(l.Levels[i].GetMemberByID(d.MemberIDs[i]));
                    var a = new ICubeAddress(FGrid, lm);
                    a.Measure = l.Measure;
                    a.MeasureMode = l.fMode;
                    var eval = new Evaluator(FGrid, a);
                    var dn = new CubeDataNumeric();
                    var o = DoCalculateExpression(FGrid, a, eval);
                    if (o != null)
                    {
                        dn.Value = Convert.ToDouble(o);
                        dn.FormattedValue = l.Measure.FormatValue(o, l.Measure.DefaultFormat);
                        dn.LineIdx = d.LineIdx;
                        dn.MemberIDs = d.MemberIDs;
                        data.Add(dn);
                    }
                }
            }
        }


        private void ProcessCalculatedMembers(Line l, List<CubeDataNumeric> data)
        {
            var hasCalculatedMembers = false;
            foreach (var ll in l.Levels)
            foreach (var m in ll.FUniqueNamesArray.Values)
                if (m is CalculatedMember)
                {
                    hasCalculatedMembers = true;
                    break;
                }
            if (!hasCalculatedMembers)
                return;

            var data2 = new List<CubeDataNumeric>();
            foreach (var d in data)
            {
                var mm = l.fM.DecodeLineIdx(d.LineIdx);
                var lm = new List<Member>(mm.Length);
                for (var i = 0; i < mm.Length; i++)
                    lm.Add(l.Levels[i].GetMemberByID(mm[i]));
                var a = new ICubeAddress(FGrid, lm);
                a.Measure = l.Measure;
                a.MeasureMode = l.fMode;
                foreach (var ll in l.Levels)
                foreach (var m in ll.FUniqueNamesArray.Values)
                    if (m is CalculatedMember)
                    {
                        a.AddMember(m);
                        object v;
                        CellFormattingProperties f;
                        if (DoCalculate(FGrid, a, out v, out f))
                        {
                            var dn = new CubeDataNumeric();
                            dn.FormattedValue = f.FormattedValue;
                            dn.LineIdx = a.FLineIdx;
                            dn.MemberIDs = l.fM.DecodeLineIdx(dn.LineIdx);
                            dn.Value = Convert.ToDouble(v);
                            data2.Add(dn);
                        }
                    }
            }

            data.AddRange(data2);
        }

        internal List<CubeDataNumeric> RetrieveCubeData(Line l, out List<Level> level_clue)
        {
            level_clue = l.fM.fLevels;
            if (FGrid.DeferLayoutUpdate) return new List<CubeDataNumeric>();

            var data = l.RetrieveCubeData(null);
            ProcessCalculatedMembers(l, data);

            if (FCube.GetProductID() == RadarUtils.GetCurrentDesktopProductID() && l.Measure.Expression.IsFill())
                ProcessCalculatedMeasures(l, data);
            return data;
        }

        internal Line RetrieveRelatedLine(List<Level> levels, Measure measure)
        {
            return GetLineForChart(new List<Member>(), levels, new int[levels.Count],
                measure.ShowModes.FirstVisibleMode, true);
        }

        internal virtual void SetActive(bool Value)
        {
            DebugLogging.WriteLine("Engine.SetActive(Value={0})", Value);

            if (FGrid != null)
                FGrid.SetActive(Value);
            if (!Value)
            {
                foreach (var ml in FMetaLines.Values)
                    ml.Clear();
                FMetaLines.Clear();
                cache_indexes = null;
                cache_ml = null;
                FLevelsList.Clear();
            }
        }

        internal virtual bool IsNativeDataPresent(MeasureShowMode mode)
        {
            return mode.Mode == MeasureShowModeType.smNormal;
        }

        internal virtual bool CalculatedByServer(Measure m)
        {
            return m.IsCalculated == false;
        }

        internal abstract MetaLine CreateMetaline(OlapControl AGrid, IList<int> LevelIndexes);

        internal MetaLine GetMetaline(IList<int> LevelIndexes)
        {
            if (cache_indexes != null && cache_indexes.Length == LevelIndexes.Count
                && cache_ml != null)
            {
                var b = true;
                for (var i = 0; i < LevelIndexes.Count; i++)
                    if (LevelIndexes[i] != cache_indexes[i])
                    {
                        b = false;
                        break;
                    }
                if (b)
                    return cache_ml;
            }
            cache_indexes = new int[LevelIndexes.Count];
            LevelIndexes.CopyTo(cache_indexes, 0);

            var key = RadarUtils.Join('.', LevelIndexes);

            MetaLine M;
            FMetaLines.TryGetValue(key, out M);
            if (M != null)
            {
                cache_ml = M;
                return M;
            }
            M = CreateMetaline(FGrid, LevelIndexes);
            FMetaLines.Add(key, M);
            cache_ml = M;
            return M;
        }

        internal MetaLine GetMetaline(string ALineID)
        {
            MetaLine M;
            FMetaLines.TryGetValue(ALineID, out M);
            if (M != null) return M;
            List<int> LevelIndexes;
            if (ALineID.Length > 0)
            {
                var lines = ALineID.Split('.');
                LevelIndexes = new List<int>(lines.Length);
                foreach (var l in lines) LevelIndexes.Add(Convert.ToInt32(l));
                LevelIndexes.Sort();
            }
            else
            {
                LevelIndexes = new List<int>();
            }
            M = CreateMetaline(FGrid, LevelIndexes);
            FMetaLines.Add(ALineID, M);
            return M;
        }

        internal void ClearDependedMetalines(Level Level)
        {
            for (var i = FMetaLines.Count - 1; i >= 0; i--)
            {
                var IsDelete = true;
                foreach (var l in FMetaLines.Values[i].fLevels)
                    if (l == Level)
                    {
                        IsDelete = false;
                        break;
                    }
                if (IsDelete)
                {
                    var ml = FMetaLines.Values[i];
                    if (ml == cache_ml)
                    {
                        cache_ml = null;
                        cache_indexes = null;
                    }
                    FMetaLines.RemoveAt(i);
                }
            }
        }

        internal void ClearIncludedMetalines(Level Level)
        {
            for (var i = FMetaLines.Count - 1; i >= 0; i--)
            {
                var IsDelete = false;
                foreach (var l in FMetaLines.Values[i].fLevels)
                    if (l == Level)
                    {
                        IsDelete = true;
                        break;
                    }
                if (IsDelete)
                {
                    var ml = FMetaLines.Values[i];
                    if (ml == cache_ml)
                    {
                        cache_ml = null;
                        cache_indexes = null;
                    }
                    FMetaLines.RemoveAt(i);
                }
            }
        }

        internal void ClearMeasureData(Measure measure)
        {
            foreach (var m in FMetaLines.Values)
                for (var i = m.fLines.Count - 1; i >= 0; i--)
                    if (m.fLines.Values[i].Measure == measure)
                    {
                        m.fLines.Values[i].Unregister();
                        m.fLines.RemoveAt(i);
                        m.cache_line = null;
                    }
        }

        internal void ClearMeasureData(MeasureShowMode mode)
        {
            foreach (var m in FMetaLines.Values)
                for (var i = m.fLines.Count - 1; i >= 0; i--)
                    if (m.fLines.Values[i].fMode == mode)
                    {
                        m.fLines.Values[i].Unregister();
                        m.fLines.RemoveAt(i);
                        m.cache_line = null;
                    }
        }

        /// <summary>Clears all the already aggregated data</summary>
        public void Clear()
        {
            for (var i = FMetaLines.Count - 1; i >= 0; i--)
            {
                var ml = FMetaLines.Values[i];
                if (ml == cache_ml)
                {
                    cache_ml = null;
                    cache_indexes = null;
                }
                ml.Clear();
                ml.FGrid = null;
                FMetaLines.RemoveAt(i);
            }
        }

        internal void ClearIncludedHierarchy(Hierarchy H)
        {
            foreach (var l in H.FLevels) ClearIncludedMetalines(l);
        }

        internal bool GetCellValue(string LineID, int HierID, long LineIdx, string MeasureID, int ModeID,
            out object Value)
        {
            var A = new ICubeAddress(FGrid, LineID, HierID, LineIdx, MeasureID, ModeID);
            return GetCellValue(A, out Value);
        }

        internal bool HasCellValue(ICubeAddress address)
        {
            object V;
            if (address.Measure != null)
                return GetCellValue(address, out V);
            var a = address.Clone();
            if (FGrid.FCellSet.FVisibleMeasures.Count == 0) return true;
            foreach (var m in FGrid.FCellSet.FVisibleMeasures)
            {
                a.Measure = m;
                if (GetCellValue(a, out V)) return true;
            }
            return false;
        }

        internal bool HasCellValue(string LineID, int HierID, long LineIdx, string MeasureID, int ModeID)
        {
            var A = new ICubeAddress(FGrid, LineID, HierID, LineIdx, MeasureID, ModeID);
            return HasCellValue(A);
        }

        internal virtual object DoCalculateExpression(OlapControl Grid, ICubeAddress Address, Evaluator context)
        {
            return null;
        }

        private bool DoCalculate(OlapControl Grid, ICubeAddress Address, out object Value,
            out CellFormattingProperties Formatted)
        {
            var fEvent = new CalcMemberArgs();
            fEvent.fEvaluator = new Evaluator(Grid, Address);
            if (Address.IsCalculatedByExpression)
            {
                Value = DoCalculateExpression(Grid, Address, fEvent.fEvaluator);
                fEvent.ReturnValue = Address.Measure.FormatValue(Value, Address.Measure.DefaultFormat);

                Formatted = new CellFormattingProperties(fEvent.ReturnValue, fEvent.ReturnBackColor,
                    fEvent.ReturnForeColor,
                    fEvent.fFontStyle, fEvent.fFontName, fEvent.fFontSize);

                return Value != null;
            }

            fEvent.fValue = null;
            Grid.EventCalcMember(fEvent);

            Formatted = new CellFormattingProperties(fEvent.ReturnValue, fEvent.ReturnBackColor, fEvent.ReturnForeColor,
                fEvent.fFontStyle, fEvent.fFontName, fEvent.fFontSize);

            Value = fEvent.ReturnData;
            return Value != null;
        }

        internal bool GetCalculatedCellValue(ICubeAddress Address, out object Value,
            out CellFormattingProperties Formatted)
        {
            if (Address.Measure != null && Address.Measure.AggregateFunction == OlapFunction.stCalculated)
                return DoCalculate(FGrid, Address, out Value, out Formatted);

            if (Address.FLevelsAndMembers.Values.Any(m => m.MemberType == MemberType.mtCalculated))
                return DoCalculate(FGrid, Address, out Value, out Formatted);

            Value = null;
            Formatted = new CellFormattingProperties("");
            return false;
        }

        internal void RebuildGridMembers()
        {
            FGrid.FCellSet.Rebuild();
        }

        /// <summary>
        ///     Performs the Drillthrough operation against the Cube and returns a DataTable
        ///     filled with source records aggregated by the Cube cell with the specified
        ///     address.
        /// </summary>
        /// <param name="Address">The cube cell address.</param>
        /// <param name="dataTable">The DataTable to be filled with source records.</param>
        /// <param name="RowsToFetch">When more than 0, limits the max number of records in the resulting DataTable.</param>
        /// <param name="columns">When defined, limits the columns to include into the resulting DataTable.</param>
        public abstract void Drillthrough(ICubeAddress Address, DataTable dataTable, int RowsToFetch,
            ICollection<string> columns);

        /// <summary>
        ///     Performs the Drillthrough operation against the Cube and returns a DataTable
        ///     filled with source records aggregated by the Cube cell with the specified
        ///     address.
        /// </summary>
        /// <param name="Address">The cube cell address.</param>
        /// <param name="dataTable">The DataTable to be filled with source records.</param>
        /// <param name="RowsToFetch">When more than 0, limits the max number of records in the resulting DataTable.</param>
        /// <param name="columns">When defined, limits the columns to include into the resulting DataTable.</param>
        /// <param name="DrillThroughMethod">Defines the method to use when fetching records from the source data schema.</param>
        public abstract void Drillthrough(ICubeAddress Address, DataTable dataTable, int RowsToFetch,
            ICollection<string> columns, DrillThroughMethod DrillThroughMethod);

        public abstract void Drillthrough(IList<ICubeAddress> addresses, IList<Measure> measures, DataTable dataTable,
            int RowsToFetch, ICollection<string> columns, DrillThroughMethod DrillThroughMethod);

        /// <summary>
        ///     Performs the Drillthrough MDX-query against the OLAP-server.
        /// </summary>
        /// <param name="dataTable">The DataTable to be filled with source records.</param>
        /// <param name="mdx">The MDX-query to execute against the OLAP-server.</param>
        public abstract void Drillthrough(DataTable dataTable, string mdx);

        /// <summary>
        ///     Updates the fact table contents for the records aggregated in the Cube cell with
        ///     the specified address.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For the Grid with the MOlapCube component as the data source, the MDX
        ///         "Writeback" command with the appropriate parameters is fulfilled.
        ///     </para>
        ///     <para>
        ///         For the grid with the TOLAPCube component as the data source, the OnWriteback
        ///         event is called.
        ///     </para>
        /// </remarks>
        /// <param name="Address">The <see cref="ICubeAddress">address</see> of cube cell</param>
        /// <param name="NewValue">The new value for the cell</param>
        /// <param name="Method">The <see cref="TWritebackMethod">type of writeback distribution</see></param>
        public abstract void Writeback(ICubeAddress Address, object NewValue, WritebackMethod Method);

        /// <summary>
        ///     Creates an instance of the ICubeAddress interface.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If you specify no parameters for the method, the created instance will point
        ///         to the "Grand Total" cell.
        ///     </para>
        ///     <para>
        ///         If you need to create an instance pointing to any other cell, use the
        ///         overloaded version of the method.
        ///     </para>
        /// </remarks>
        public ICubeAddress CreateCubeAddress(List<Member> Members)
        {
            return new ICubeAddress(FGrid, Members);
        }

        /// <summary>
        ///     Creates an instance of the ICubeAddress interface.
        /// </summary>
        /// <returns></returns>
        public ICubeAddress CreateCubeAddress()
        {
            return new ICubeAddress(FGrid);
        }

        /// <summary>
        ///     Creates an instance of the ICubeAddress interface.
        /// </summary>
        /// <param name="Measure"></param>
        /// <param name="Members"></param>
        /// <returns></returns>
        public ICubeAddress CreateCubeAddress(Measure Measure, params Member[] Members)
        {
            var l = new List<Member>(Members.Length);
            foreach (var m in Members) l.Add(m);
            var Result = new ICubeAddress(FGrid, l);
            Result.Measure = Measure;
            return Result;
        }

        /// <summary>
        ///     Calculates a nonformatted value of the Cube cell specified by its
        ///     multidimensional address.
        /// </summary>
        public bool GetCellValue(ICubeAddress Address, out object Value)
        {
            Value = null;
            var M = GetMetaline(Address.FLevelsAndMembers.Keys);
            if (Address.Measure == null) return true;
            if (CalculatedByServer(Address.Measure))
            {
                var L = M.GetLine(Address.FHierID, Address.Measure,
                    IsNativeDataPresent(Address.MeasureMode) ? Address.MeasureMode : Address.Measure.ShowModes[0]);
                var Result = L.GetCell(Address, out Value);
                if (Result)
                    return true;
                CellFormattingProperties Formatted;
                return GetCalculatedCellValue(Address, out Value, out Formatted);
                //else return true;
            }
            {
                CellFormattingProperties Formatted;
                return GetCalculatedCellValue(Address, out Value, out Formatted);
            }
        }

        /// <summary>
        ///     Calculates a nonformatted value of the Cube cell specified by its
        ///     multidimensional address.
        /// </summary>
        /// <returns>True, if the cell contains a value</returns>
        /// <param name="Address">The multidimensional address of the cell</param>
        /// <param name="Value">Returned value of the cube cell</param>
        /// <param name="Formatted">Returned formatted value of the cube cell</param>
        public bool GetCellValue(ICubeAddress Address, out object Value, out CellFormattingProperties Formatted)
        {
            Value = null;
            Formatted = new CellFormattingProperties();
            var M = GetMetaline(Address.FLevelsAndMembers.Keys);
            if (Address.Measure == null) return true;
            if (CalculatedByServer(Address.Measure))
            {
                var L = M.GetLine(Address.FHierID, Address.Measure,
                    IsNativeDataPresent(Address.MeasureMode) ? Address.MeasureMode : Address.Measure.ShowModes[0]);
                var Result = L.GetCell(Address, out Value);
                if (!Result) return GetCalculatedCellValue(Address, out Value, out Formatted);
                return true;
            }
            return GetCalculatedCellValue(Address, out Value, out Formatted);
        }

        internal virtual bool GetValueForSort(CellsetMember x, ICubeAddress y,
            List<Member> fColMembers, List<Member> fRowMembers, out object Value)
        {
            var a = x.GetAddress();
            a.Merge(y);
            var b = GetCellValue(a, out Value);
            if (!b) return false;
            var sm = a.MeasureMode;
            if (sm == null || IsNativeDataPresent(sm)) return b;
            switch (sm.Mode)
            {
                case MeasureShowModeType.smPercentParentColItem:
                    x = x.FParent;
                    if (x == null) return true;
                    a = x.GetAddress();
                    a.Merge(y);
                    object V;
                    var b1 = GetCellValue(a, out V);
                    if (!b1) return b;
                    try
                    {
                        Value = Convert.ToDouble(Value) / Convert.ToDouble(V);
                    }
                    catch
                    {
                        ;
                    }
                    break;
                case MeasureShowModeType.smPercentColTotal:
                    a = x.GetAddress();
                    if (y.Measure != null)
                    {
                        a.Measure = y.Measure;
                        a.MeasureMode = y.MeasureMode;
                    }
                    b1 = GetCellValue(a, out V);
                    if (!b1) return b;
                    try
                    {
                        Value = Convert.ToDouble(Value) / Convert.ToDouble(V);
                    }
                    catch
                    {
                        ;
                    }
                    break;
                case MeasureShowModeType.smColumnRank:
                    if (!(Value is IComparable))
                        return true;

                    if (a.Measure == null) return true;
                    a.MeasureMode = a.Measure.ShowModes[0];

                    var Rank = 1;

                    var cmp = Value as IComparable;
                    foreach (var m in fColMembers)
                    {
                        if (m == null) continue;
                        try
                        {
                            a.AddMember(m);
                            object vv;
                            if (GetCellValue(a, out vv))
                                if (cmp.CompareTo(vv) < 0)
                                    Rank++;
                        }
                        catch
                        {
                            ;
                        }
                    }
                    Value = Rank;
                    break;
                case MeasureShowModeType.smRowRank:
                    try
                    {
                        Value = -Convert.ToDouble(Value);
                    }
                    catch
                    {
                        ;
                    }
                    break;
                case MeasureShowModeType.smSpecifiedByEvent:
                    if (!Grid.EventShowMeasureAssigned)
                        throw new Exception(string.Format(RadarUtils.GetResStr("rssmError"), a.Measure.DisplayName,
                            sm.Caption));
                    var E = new ShowMeasureArgs(Value, sm, null);
                    E.fRowSiblings = fRowMembers;
                    E.fColumnSiblings = fColMembers;
                    E.fEvaluator = new Evaluator(Grid, a);
                    Grid.EventShowMeasure(E);
                    Value = E.ReturnData;
                    break;
            }
            return b;
        }

        /// <summary>
        ///     Returns formatted and unformatted values of the cell from the current OLAP slice.
        /// </summary>
        /// <param name="CurrentCell">The data cell from the current OLAP slice</param>
        /// <param name="Value">An unformatted value of the cell</param>
        /// <param name="Formatted">A formatting properties of the cell</param>
        /// <returns>True, if the cell contains a value</returns>
        public virtual bool GetCellFormattedValue(IDataCell CurrentCell, out object Value,
            out CellFormattingProperties Formatted)
        {
            var Address = CurrentCell.Address;
            Formatted = new CellFormattingProperties();
            var Result = GetCellValue(Address, out Value, out Formatted);
            if (Result)
            {
                if (Formatted.FormattedValue == null || Address.Measure.FCubeMeasure != null
                    && Address.Measure.DefaultFormat != Address.Measure.FCubeMeasure.DefaultFormat)
                    if (Value != null)
                    {
                        object V;
                        Formatted.FormattedValue =
                            Address.Measure.DoFormatMode(CurrentCell, Value, Address.MeasureMode, out V);
                        Value = V;
                        //if (Address.MeasureMode.Mode == MeasureShowModeType.smSpecifiedByEvent)
                        //    Value = V;
                        return true;
                    }
            }
            else
            {
                Formatted.FormattedValue = "";
            }
            return Result;
        }

        internal void ClearRequestMap()
        {
            foreach (var m in FMetaLines.Values)
                m.ClearRequestMap();
        }

        internal void DoRetrieveData()
        {
            foreach (var m in FMetaLines.Values)
                m.DoRetrieveData();
        }

        internal virtual void RetrieveLine2(Dictionary<Level, HashSet<Member>> src, Line ALine)
        {
        }

        internal IEnumerable<Member> GetMembersList(ICubeAddress a, Members MembersList)
        {
            if (a.Measure == null ||
                !CalculatedByServer(a.Measure) ||
                MembersList[0].MemberType == MemberType.mtMeasure ||
                MembersList[0].MemberType == MemberType.mtMeasureMode)
                return MembersList.ToArray();

            if (a.GetMemberByHierarchy(MembersList[0].Level.Hierarchy) != null)
                return MembersList.ToArray();

            var aa = a.Clone();

            aa.AddMember(MembersList[0]);
            var M = GetMetaline(aa.FLevelsAndMembers.Keys);
            var L = M.GetLine(aa.FHierID, aa.Measure, aa.Measure.ShowModes[0]);
            return L.GetMembersList(aa, MembersList);
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgEngine);

            StreamUtils.WriteTag(writer, Tags.tgEngine_Levels);
            StreamUtils.WriteInt32(writer, FLevelsList.Count);
            foreach (var l in FLevelsList)
                if (l == null)
                    StreamUtils.WriteString(writer, "");
                else
                    StreamUtils.WriteString(writer, l.UniqueName);

            StreamUtils.WriteTag(writer, Tags.tgEngine_Metalines);
            StreamUtils.WriteInt32(writer, FMetaLines.Count);
            for (var i = 0; i < FMetaLines.Count; i++)
            {
                StreamUtils.WriteString(writer, FMetaLines.Keys[i]);
                StreamUtils.WriteTypedStreamedObject(writer, FMetaLines.Values[i], Tags.tgMetaLine);
            }

            StreamUtils.WriteTag(writer, Tags.tgEngine_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            FGrid = (OlapControl) options;
            StreamUtils.CheckTag(reader, Tags.tgEngine);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                int c;
                switch (tag)
                {
                    case Tags.tgEngine_Levels:
                        c = StreamUtils.ReadInt32(reader);
                        FLevelsList = new List<Level>(c);
                        for (var i = 0; i < c; i++)
                        {
                            var l = FGrid.Dimensions.FindLevel(StreamUtils.ReadString(reader));
                            FLevelsList.Add(l);
                        }
                        break;
                    case Tags.tgEngine_Metalines:
                        c = StreamUtils.ReadInt32(reader);
                        FMetaLines = new SortedList<string, MetaLine>(c);
                        for (var i = 0; i < c; i++)
                        {
                            var s = StreamUtils.ReadString(reader);
                            StreamUtils.ReadTag(reader); // skip Tags.tgMetaLine
                            var m = (MetaLine) StreamUtils.ReadTypedStreamedObject(reader, FGrid);
                            FMetaLines.Add(s, m);
                        }
                        break;
                    case Tags.tgEngine_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        #endregion
    }
}