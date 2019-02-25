using System;
using System.Collections.Generic;
using System.Text;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Controls.Chart;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Engine;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.CellSet
{
    /// <summary>
    ///     <para>
    ///         Represents Cellset for the chart-control. As the heir to the TCellset
    ///         contains additional description of diagram's axes.
    ///     </para>
    /// </summary>
    public class ChartCellSet : CellSet
    {
        [NonSerialized] private ChartAxisDescriptor fSizeAxisDescriptor;
        [NonSerialized] private ChartAxisDescriptor fShapeAxisDescriptor;
        [NonSerialized] private ChartAxisDescriptor fColorAxisDescriptor;
        [NonSerialized] private ChartAxisDescriptor fXAxisDescriptor;
        [NonSerialized] private ChartAxesDescriptor fYAxesDescriptor;
        [NonSerialized] internal List<List<Member>> fRowChartMembers;
        [NonSerialized] internal List<List<Member>> fColumnChartMembers;
        [NonSerialized] internal IChartCell[,] IChartsArray;
        [NonSerialized] internal List<Member> fColorChartMembers;
        [NonSerialized] internal List<Member> fSizeChartMembers;
        [NonSerialized] internal List<Member> fShapeChartMembers;

        internal ChartCellSet(OlapControl AGrid)
            : base(AGrid)
        {
        }

#if SL /// <summary>References to the Grid Chart-control, the specified Cellset belongs to.</summary>
        public new RiaOLAPChart Grid
        {
            get { return (RiaOLAPChart)base.Grid; }
        }
#endif
        /// <summary>
        ///     <para>
        ///         Descriptor of the "Size" axis. If the "Size" axis is not defined, the
        ///         DesctiptorObject property of this descriptor is null.
        ///     </para>
        /// </summary>
        public ChartAxisDescriptor SizeAxisDescriptor => fSizeAxisDescriptor;

        /// <summary>
        ///     <para>
        ///         Descriptor of the "Shape" axis. If the "Shape" axis is not defined, the
        ///         DesctiptorObject property of this descriptor is null.
        ///     </para>
        /// </summary>
        public ChartAxisDescriptor ShapeAxisDescriptor => fShapeAxisDescriptor;

        /// <summary>
        ///     Descriptor of the "Color" axis. If the "Color" axis is not defined, the
        ///     DesctiptorObject property of this descriptor is null.
        /// </summary>
        public ChartAxisDescriptor ColorAxisDescriptor => fColorAxisDescriptor;

        /// <summary>
        ///     <para>
        ///         Descriptor of the "X" axis (column axis). The column axis always contains
        ///         either the last of the open levels of the column area, or a measure, placed in that
        ///         area.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For example, if the column area (TGridChart.AxesLayout.ColumnLevels) contains
        ///         "Product Categories", "Product Models" and "Products" categories, but no measures,
        ///         then the X axis will be represented by the "Products" level, and the two upper levels
        ///         will form the cells.
        ///     </para>
        ///     <para>
        ///         If the column area contains a measure, the X axis also may contain a measure
        ///         that, in this case, will be the X axis descriptor, no matter how many numbers there are in the
        ///         column area. Unlike the Y axis, the X axis can contain no more that one measure.
        ///     </para>
        /// </remarks>
        public ChartAxisDescriptor XAxisDescriptor => fXAxisDescriptor;

        /// <summary>
        ///     <para>
        ///         Descriptor of the "Y" axis (row axis). The row axis always contains either
        ///         the last of the open levels of the row area, or a collection of measures, placed in
        ///         that area.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For example, if the row area (TGridChart.AxesLayout.ColumnLevels) contains
        ///         "Product Categories", "Product Models" and "Products" categories, but no measures,
        ///         then the Y axis will be represented by the "Products" level, and the two upper levels
        ///         will form the cells.
        ///     </para>
        ///     <para>
        ///         The Y axis can contain one or more measures, grouprd in a set of collections
        ///         of the MeasureGroup type. Each of these collections is a separate Chart that displays
        ///         the values of the measures in it.
        ///     </para>
        /// </remarks>
        public ChartAxesDescriptor YAxesDescriptor => fYAxesDescriptor;

        /// <summary>
        ///     <para>
        ///         Returns the list of members - the elements of the Y axis for the Cube cells
        ///         with a row index, passed as the perameter, or null, if the descriptor of the Y axis
        ///         is NOT an hierarchy level.
        ///     </para>
        /// </summary>
        /// <param name="rowIndex">Row index</param>
        public IList<Member> RowChartMembers(int rowIndex)
        {
            if (fRowChartMembers == null) return null;
            return fRowChartMembers[rowIndex - FFixedRows].AsReadOnly();
        }

        /// <summary>
        ///     <para></para>
        ///     <para>
        ///         Returns the list of members - the elements of the X axis - for the Cube cells
        ///         with a column index, passed as the parameter, or null, if the descriptor of the X
        ///         axis is NOT an hierarchy level.
        ///     </para>
        /// </summary>
        /// <param name="columnIndex">Column index</param>
        public IList<Member> ColumnChartMembers(int columnIndex)
        {
            if (fColumnChartMembers == null) return null;
            return fColumnChartMembers[columnIndex - FFixedColumns].AsReadOnly();
        }

        /// <summary>
        ///     <para>
        ///         Returns the list of members - elements of the color modification axis or null
        ///         if the color modificator is NOT an hierarchy level.
        ///     </para>
        /// </summary>
        public IList<Member> ColorChartMembers => fColorChartMembers == null ? null : fColorChartMembers.AsReadOnly();

        /// <summary>
        ///     <para>
        ///         Returns the list of members - elements of the size modificator axis or null,
        ///         if the size modificator is NOT an hierarchy level.
        ///     </para>
        /// </summary>
        public IList<Member> SizeChartMembers => fSizeChartMembers == null ? null : fSizeChartMembers.AsReadOnly();

        /// <summary>
        ///     <para>
        ///         Returns the list of members - elements of the shape modificator axis or null,
        ///         if the shape modificator is NOT an hierarchy level.
        ///     </para>
        /// </summary>
        public IList<Member> ShapeChartMembers => fShapeChartMembers == null ? null : fShapeChartMembers.AsReadOnly();

        internal override ICell DataCells(int Column, int Row)
        {
            return IChartsArray[Column - FFixedColumns, Row - FFixedRows];
        }

        internal override void ClearMembers()
        {
            base.ClearMembers();
            fRowChartMembers = null;
            fColumnChartMembers = null;
            fSizeAxisDescriptor = null;
            fShapeAxisDescriptor = null;
            fColorAxisDescriptor = null;
            fXAxisDescriptor = null;
            fYAxesDescriptor = null;
            IChartsArray = null;
        }

        internal override void RebuildChart()
        {
            FGrid.fMeasures.InitMeasures();
            var LA = FGrid.FLayout;
            foreach (var H in FGrid.FLayout.fDetailsAxis)
                H.DefaultInit();

            // create the axis descriptors (without any series)
            fXAxisDescriptor = new ChartAxisDescriptor();
            if (LA.fXAxisMeasure != null)
            {
                fXAxisDescriptor.fDescriptor = LA.fXAxisMeasure;
            }
            else
            {
                if (LA.fColumnLevels.Count > 0)
                    fXAxisDescriptor.fDescriptor = LA.fColumnLevels[LA.fColumnLevels.Count - 1];
            }

            fYAxesDescriptor = new ChartAxesDescriptor();
            if (LA.fYAxisMeasures.Count > 0)
            {
                foreach (var gm in LA.fYAxisMeasures)
                {
                    var cad = new ChartAxisDescriptor();
                    fYAxesDescriptor.ChartAreas.Add(cad);
                    if (gm.Count == 1)
                        cad.fDescriptor = gm[0];
                    else
                        cad.fDescriptor = new MeasuresListDescriptor(gm);
                }
            }
            else
            {
                var cad = new ChartAxisDescriptor();
                fYAxesDescriptor.ChartAreas.Add(cad);
                if (LA.fRowLevels.Count > 0)
                    cad.fDescriptor = LA.fRowLevels[LA.fRowLevels.Count - 1];
            }

            fColorAxisDescriptor = new ChartAxisDescriptor();
            fColorChartMembers = null;
            SortedDictionary<int, Member>
                f_ColorChartMembers = null; // <ID, index in the corresponding Members collection>
            if (LA.fColorAxisItem != null)
            {
                if (LA.fColorAxisItem is Measure)
                    fColorAxisDescriptor.fDescriptor = LA.fColorAxisItem;
                if (LA.fColorAxisItem is Hierarchy)
                {
                    var H = (Hierarchy) LA.fColorAxisItem;
                    H.DefaultInit(1);
                    fColorAxisDescriptor.fDescriptor = ((Hierarchy) LA.fColorAxisItem).FirstVisibleLevel();
                    f_ColorChartMembers = new SortedDictionary<int, Member>();
                }
            }

            fSizeAxisDescriptor = new ChartAxisDescriptor();
            fSizeChartMembers = null;
            SortedDictionary<int, Member>
                f_SizeChartMembers = null; // <ID, index in the corresponding Members collection>
            if (LA.fSizeAxisItem != null)
            {
                if (LA.fSizeAxisItem is Measure)
                    fSizeAxisDescriptor.fDescriptor = LA.fSizeAxisItem;
                if (LA.fSizeAxisItem is Hierarchy)
                {
                    var H = (Hierarchy) LA.fSizeAxisItem;
                    H.DefaultInit(1);
                    fSizeAxisDescriptor.fDescriptor = ((Hierarchy) LA.fSizeAxisItem).FirstVisibleLevel();
                    f_SizeChartMembers = new SortedDictionary<int, Member>();
                }
            }

            fShapeAxisDescriptor = new ChartAxisDescriptor();
            fShapeChartMembers = null;
            SortedDictionary<int, Member>
                f_ShapeChartMembers = null; // <ID, index in the corresponding Members collection>
            if (LA.fShapeAxisItem != null)
            {
                if (LA.fShapeAxisItem is Measure)
                    fShapeAxisDescriptor.fDescriptor = LA.fShapeAxisItem;
                if (LA.fShapeAxisItem is Hierarchy)
                {
                    var H = (Hierarchy) LA.fShapeAxisItem;
                    H.DefaultInit(1);
                    fShapeAxisDescriptor.fDescriptor = ((Hierarchy) LA.fShapeAxisItem).FirstVisibleLevel();
                    f_ShapeChartMembers = new SortedDictionary<int, Member>();
                }
            }

            // making the cartesian product of used levels

            var YLimit = LA.fYAxisMeasures.Count == 0 ? LA.fRowLevels.Count - 2 : LA.fRowLevels.Count - 1;
            var XLimit = LA.fXAxisMeasure == null ? LA.fColumnLevels.Count - 2 : LA.fColumnLevels.Count - 1;

            var ldecart = new List<Level>();
            var lc_keys = new List<Level>(); // list of levels to build the column part of the cellset
            var l_keys = new SortedList<string, Level>(); // list of levels to hold the IChartCell array 
            for (var i = 0; i < LA.fColumnLevels.Count; i++)
            {
                var l = LA.fColumnLevels[i];
                lc_keys.Add(l);
                if (i <= XLimit)
                {
                    l_keys.Remove(l.Hierarchy.UniqueName);
                    l_keys.Add(l.Hierarchy.UniqueName, l);
                }
                if (i < LA.fColumnLevels.Count - 1 &&
                    LA.fColumnLevels[i + 1].Hierarchy == l.Hierarchy) continue;
                ldecart.Add(l);
            }

            var lr_keys = new List<Level>(); // list of levels to build the row part of the cellset
            for (var i = 0; i < LA.fRowLevels.Count; i++)
            {
                var l = LA.fRowLevels[i];
                lr_keys.Add(l);
                if (i <= YLimit)
                {
                    l_keys.Remove(l.Hierarchy.UniqueName);
                    l_keys.Add(l.Hierarchy.UniqueName, l);
                }
                if (i < LA.fRowLevels.Count - 1 &&
                    LA.fRowLevels[i + 1].Hierarchy == l.Hierarchy) continue;
                ldecart.Add(l);
            }

            foreach (var h in LA.fDetailsAxis)
            {
                var l = h.FirstVisibleLevel();
                ldecart.Add(l);
                CheckLevelForFetchedParents(l);
            }

            if (LA.fColorAxisItem is Hierarchy)
            {
                var l = ((Hierarchy) LA.fColorAxisItem).FirstVisibleLevel();
                ldecart.Add(l);
                CheckLevelForFetchedParents(l);
            }

            if (LA.fSizeAxisItem is Hierarchy)
            {
                var l = ((Hierarchy) LA.fSizeAxisItem).FirstVisibleLevel();
                ldecart.Add(l);
                CheckLevelForFetchedParents(l);
            }

            if (LA.fShapeAxisItem is Hierarchy)
            {
                var l = ((Hierarchy) LA.fShapeAxisItem).FirstVisibleLevel();
                ldecart.Add(l);
                CheckLevelForFetchedParents(l);
            }

            // fetching ascendants of members of used levels if it is necessary (it should be done BEFORE fetching the lines)

            foreach (var l in LA.fRowLevels)
                CheckLevelForFetchedParents(l);

            foreach (var l in LA.fColumnLevels)
                CheckLevelForFetchedParents(l);


            // fetching the data for size and color modifiers
            Line sizeLine = null;
            ChartAxis sizeAxis = null;
            if (LA.fSizeAxisItem is Measure)
            {
                sizeLine = FGrid.FEngine.RetrieveRelatedLine(ldecart, (Measure) LA.fSizeAxisItem);
                sizeAxis = new ChartAxis(this, ChartAxisFormat.Continuous, fSizeAxisDescriptor);
            }
            else
            {
                if (LA.fSizeAxisItem is Hierarchy)
                    sizeAxis = new ChartAxis(this, ChartAxisFormat.Discrete, fSizeAxisDescriptor);
            }

            Line shapeLine = null;
            ChartAxis shapeAxis = null;
            if (LA.fShapeAxisItem is Measure)
            {
                shapeLine = FGrid.FEngine.RetrieveRelatedLine(ldecart, (Measure) LA.fShapeAxisItem);
                shapeAxis = new ChartAxis(this, ChartAxisFormat.Continuous, fShapeAxisDescriptor);
            }
            else
            {
                if (LA.fShapeAxisItem is Hierarchy)
                    shapeAxis = new ChartAxis(this, ChartAxisFormat.Discrete, fShapeAxisDescriptor);
            }

            Line colorLine = null;
            ChartAxis colorAxis = null;
            if (LA.fColorAxisItem is Measure)
            {
                colorLine = FGrid.FEngine.RetrieveRelatedLine(ldecart, (Measure) LA.fColorAxisItem);
                colorAxis = new ChartAxis(this, ChartAxisFormat.Continuous, fColorAxisDescriptor);
            }
            else
            {
                if (LA.fColorAxisItem is Hierarchy)
                    colorAxis = new ChartAxis(this, ChartAxisFormat.Discrete, fColorAxisDescriptor);
            }

            var clue = new List<Level>();

            // fetching the data
            var datalist = new SortedList<string, List<CubeDataNumeric>>();
            var datalist2 = new SortedList<string, Line>();
            Line xLine = null;
            if (LA.fXAxisMeasure != null)
            {
                if (LA.fYAxisMeasures.Count == 0)
                {
                    datalist2.Add(LA.fXAxisMeasure.UniqueName,
                        FGrid.FEngine.RetrieveRelatedLine(ldecart, LA.fXAxisMeasure));
                }
                else
                    xLine = FGrid.FEngine.RetrieveRelatedLine(ldecart, LA.fXAxisMeasure);
            }
            foreach (var mg in LA.fYAxisMeasures)
            {
                foreach (var m in mg)
                {
                    if (datalist2.ContainsKey(m.UniqueName))
                        datalist2.Remove(m.UniqueName);

                    datalist2.Add(m.UniqueName, FGrid.FEngine.RetrieveRelatedLine(ldecart, m));
                }
            }
            // all the data has been prepared for fetching

            if (!FGrid.DeferLayoutUpdate)
                FGrid.FEngine.DoRetrieveData();

            foreach (var dl2 in datalist2)
                datalist.Add(dl2.Key, FGrid.FEngine.RetrieveCubeData(dl2.Value, out clue));

            // making the multipliers arrays
            var lc_mul = new long[lc_keys.Count];
            var lc_clue = new int[lc_keys.Count];
            var lc_clue_key = new bool[lc_keys.Count];
            var k = 1;
            for (var i = lc_mul.Length - 1; i >= 0; i--)
            {
                lc_mul[i] = k;
                k *= lc_keys[i].CompleteMembersCount;
                var p = clue.IndexOf(lc_keys[i]);
                if (p >= 0)
                {
                    lc_clue_key[i] = true;
                    lc_clue[i] = p;
                }
                else
                {
                    lc_clue_key[i] = false;
                    var h = lc_keys[i].Hierarchy;
                    for (var j = 0; j < clue.Count; j++)
                        if (clue[j].Hierarchy == h)
                        {
                            lc_clue[i] = j;
                            break;
                        }
                }
            }

            var lr_mul = new long[lr_keys.Count];
            var lr_clue = new int[lr_keys.Count];
            var lr_clue_key = new bool[lr_keys.Count];
            k = 1;
            for (var i = lr_mul.Length - 1; i >= 0; i--)
            {
                lr_mul[i] = k;
                k *= lr_keys[i].CompleteMembersCount;
                var p = clue.IndexOf(lr_keys[i]);
                if (p >= 0)
                {
                    lr_clue_key[i] = true;
                    lr_clue[i] = p;
                }
                else
                {
                    lr_clue_key[i] = false;
                    var h = lr_keys[i].Hierarchy;
                    for (var j = 0; j < clue.Count; j++)
                        if (clue[j].Hierarchy == h)
                        {
                            lr_clue[i] = j;
                            break;
                        }
                }
            }

            var _tmp = new int[l_keys.Count];
            for (var i = 0; i < l_keys.Count; i++)
                _tmp[i] = l_keys.Values[i].ID;
            Array.Sort(_tmp);

            var l_ml = FGrid.Engine.GetMetaline(_tmp);
            var l_mul = new long[clue.Count];
            var l_mul_key = new bool[clue.Count];
            var l_mul_idx = new Level[clue.Count];
            for (var i = 0; i < clue.Count; i++)
            {
                var p = l_ml.fLevels.IndexOf(clue[i]);
                if (p >= 0)
                {
                    l_mul[i] = l_ml.fIdxArray[p];
                    l_mul_key[i] = true;
                }
                else
                {
                    var h = clue[i].Hierarchy;
                    var b = false;
                    for (var j = 0; j < l_ml.fLevels.Count; j++)
                        if (l_ml.fLevels[j].Hierarchy == h)
                        {
                            l_mul[i] = l_ml.fIdxArray[j];
                            l_mul_key[i] = false;
                            l_mul_idx[i] = l_ml.fLevels[j];
                            b = true;
                            break;
                        }
                    if (!b)
                        l_mul[i] = 0;
                }
            }
            // the multipliers are filled

            // setting the sort position of active members
            foreach (var l in LA.fRowLevels)
                l.SetSortPosition();
            foreach (var l in LA.fColumnLevels)
                l.SetSortPosition();

            // making the details key for the series identication
            var ls_key = new List<int>();
            if (LA.fSizeAxisItem is Hierarchy)
            {
                if (!LA.fRowAxis.Contains((Hierarchy) LA.fSizeAxisItem) &&
                    !LA.fColumnAxis.Contains((Hierarchy) LA.fSizeAxisItem)
#if !SL
                    || Grid is OlapChart
#endif
                )
                    ls_key.Add(clue.IndexOf(((Hierarchy) LA.fSizeAxisItem).FirstVisibleLevel()));
                ((Hierarchy) LA.fSizeAxisItem).FirstVisibleLevel().SetSortPosition();
            }

            if (LA.fShapeAxisItem is Hierarchy)
            {
                if (!LA.fRowAxis.Contains((Hierarchy) LA.fShapeAxisItem) &&
                    !LA.fColumnAxis.Contains((Hierarchy) LA.fShapeAxisItem)
#if !SL
                    || Grid is OlapChart
#endif
                )
                    ls_key.Add(clue.IndexOf(((Hierarchy) LA.fShapeAxisItem).FirstVisibleLevel()));
                ((Hierarchy) LA.fShapeAxisItem).FirstVisibleLevel().SetSortPosition();
            }

            if (LA.fColorAxisItem is Hierarchy)
            {
                if (!LA.fRowAxis.Contains((Hierarchy) LA.fColorAxisItem) &&
                    !LA.fColumnAxis.Contains((Hierarchy) LA.fColorAxisItem)
#if !SL
                    || Grid is OlapChart
#endif
                )
                    ls_key.Add(clue.IndexOf(((Hierarchy) LA.fColorAxisItem).FirstVisibleLevel()));
                ((Hierarchy) LA.fColorAxisItem).FirstVisibleLevel().SetSortPosition();
            }
            foreach (var h in LA.fDetailsAxis)
                ls_key.Add(clue.IndexOf(h.FirstVisibleLevel()));

            // 
            var cell_details = new Dictionary<long, Dictionary<ChartAxis, ChartArea>>();
            var xcells = new SortedDictionary<long, Member[]>();
            var ycells = new SortedDictionary<long, Member[]>();

            ChartAxis xaxis = null;
            var xaxis_idx = -1;
            if (LA.fXAxisMeasure != null)
            {
                xaxis = new ChartAxis(this, ChartAxisFormat.Continuous, fXAxisDescriptor);
            }
            else
            {
                xaxis = new ChartAxis(this, ChartAxisFormat.Discrete, fXAxisDescriptor);
                if (lc_keys.Count > 0)
                    xaxis_idx = clue.IndexOf(lc_keys[lc_keys.Count - 1]);
            }

            var coloraxis_idx = -1;
            if (LA.fColorAxisItem is Hierarchy)
                coloraxis_idx = clue.IndexOf(((Hierarchy) LA.fColorAxisItem).FirstVisibleLevel());

            var shapeaxis_idx = -1;
            if (LA.fShapeAxisItem is Hierarchy)
                shapeaxis_idx = clue.IndexOf(((Hierarchy) LA.fShapeAxisItem).FirstVisibleLevel());

            var sizeaxis_idx = -1;
            if (LA.fSizeAxisItem is Hierarchy)
                sizeaxis_idx = clue.IndexOf(((Hierarchy) LA.fSizeAxisItem).FirstVisibleLevel());

            if (LA.fYAxisMeasures.Count > 0)
            {
                for (var ii = 0; ii < LA.fYAxisMeasures.Count; ii++)
                {
                    var gm = LA.fYAxisMeasures[ii];
                    var ax = new ChartAxis(this, ChartAxisFormat.Continuous, fYAxesDescriptor.ChartAreas[ii]);
                    foreach (var m in gm)
                    {
                        List<CubeDataNumeric> data;
                        datalist.TryGetValue(m.UniqueName, out data);

                        foreach (var dn in data)
                        {
                            var serieskey = new StringBuilder(m.UniqueName + "|");
                            foreach (var i in ls_key)
                            {
                                serieskey.Append(dn.MemberIDs[i]);
                                serieskey.Append('|');
                            }

                            var CD = new ChartCellDetails(FGrid, clue, dn.MemberIDs);
                            if (xLine != null)
                            {
                                double sd;
                                if (!xLine.GetNumericData(dn.LineIdx, out sd, out CD._XValueFormatted))
                                    continue;
                                CD._XValue = sd;
                                xaxis.fMax = Math.Max(xaxis.fMax, sd);
                                xaxis.fMin = Math.Min(xaxis.fMin, sd);
                            }
                            else
                            {
                                if (xaxis_idx >= 0)
                                {
                                    var mx = clue[xaxis_idx].GetMemberByID(dn.MemberIDs[xaxis_idx]);
                                    CD._XValue = mx;
                                    CD._XValueFormatted = mx.DisplayName;
                                }
                            }

                            CD._YValue = dn.Value;
                            CD._YValueFormatted = dn.FormattedValue;
                            ax.fMax = Math.Max(ax.fMax, dn.Value);
                            ax.fMin = Math.Min(ax.fMin, dn.Value);

                            Member colorMember = null;
                            Member sizeMember = null;
                            Member shapeMember = null;

                            if (sizeLine != null)
                            {
                                double sd;
                                if (sizeLine.GetNumericData(dn.LineIdx, out sd, out CD._SizeValueFormatted))
                                {
                                    CD._SizeValue = sd;
                                    sizeAxis.fMax = Math.Max(sizeAxis.fMax, sd);
                                    sizeAxis.fMin = Math.Min(sizeAxis.fMin, sd);
                                }
                            }
                            else
                            {
                                if (LA.fSizeAxisItem is Hierarchy)
                                {
                                    var mc = clue[sizeaxis_idx].GetMemberByID(dn.MemberIDs[sizeaxis_idx]);
                                    sizeMember = mc;
                                    if (!f_SizeChartMembers.ContainsKey(mc.FSortPosition))
                                        f_SizeChartMembers.Add(mc.FSortPosition, mc);
                                }
                            }
                            if (shapeLine != null)
                            {
                                double sd;
                                if (shapeLine.GetNumericData(dn.LineIdx, out sd, out CD._ShapeValueFormatted))
                                {
                                    CD._ShapeValue = sd;
                                    shapeAxis.fMax = Math.Max(shapeAxis.fMax, sd);
                                    shapeAxis.fMin = Math.Min(shapeAxis.fMin, sd);
                                }
                            }
                            else
                            {
                                if (LA.fShapeAxisItem is Hierarchy)
                                {
                                    var mc = clue[shapeaxis_idx].GetMemberByID(dn.MemberIDs[shapeaxis_idx]);
                                    shapeMember = mc;
                                    if (!f_ShapeChartMembers.ContainsKey(mc.FSortPosition))
                                        f_ShapeChartMembers.Add(mc.FSortPosition, mc);
                                }
                            }
                            if (colorLine != null)
                            {
                                double sd;
                                if (colorLine.GetNumericData(dn.LineIdx, out sd, out CD._ColorValueFormatted))
                                {
                                    CD._ColorValue = sd;
                                    colorAxis.fMax = Math.Max(colorAxis.fMax, sd);
                                    colorAxis.fMin = Math.Min(colorAxis.fMin, sd);
                                }
                            }
                            else
                            {
                                if (LA.fColorAxisItem is Hierarchy)
                                {
                                    var mc = clue[coloraxis_idx].GetMemberByID(dn.MemberIDs[coloraxis_idx]);
                                    colorMember = mc;
                                    if (!f_ColorChartMembers.ContainsKey(mc.FSortPosition))
                                        f_ColorChartMembers.Add(mc.FSortPosition, mc);
                                }
                            }

                            // xaxis_cells
                            long key = 0;
                            var tm = new Member[lc_clue.Length];
                            for (var i = 0; i < lc_clue.Length; i++)
                            {
                                var me = clue[lc_clue[i]].GetMemberByID(dn.MemberIDs[lc_clue[i]]);
                                if (!lc_clue_key[i])
                                    me = me.GetParentMember(lc_keys[i]);
                                key += me.FSortPosition * lc_mul[i];
                                tm[i] = me;
                            }
                            if (!xcells.ContainsKey(key))
                                xcells.Add(key, tm);

                            // yaxis_cells
                            key = 0;
                            tm = new Member[lr_clue.Length];
                            for (var i = 0; i < lr_clue.Length; i++)
                            {
                                var me = clue[lr_clue[i]].GetMemberByID(dn.MemberIDs[lr_clue[i]]);
                                if (!lr_clue_key[i])
                                    me = me.GetParentMember(lr_keys[i]);
                                key += me.FSortPosition * lr_mul[i];
                                tm[i] = me;
                            }
                            if (!ycells.ContainsKey(key)) ycells.Add(key, tm);

                            //main
                            key = 0;
                            for (var i = 0; i < clue.Count; i++)
                            {
                                var _mul = l_mul[i];
                                if (_mul > 0)
                                {
                                    var me = clue[i].GetMemberByID(dn.MemberIDs[i]);
                                    if (l_mul_key[i])
                                    {
                                        key += _mul * me.ID;
                                    }
                                    else
                                    {
                                        me = me.GetParentMember(l_mul_idx[i]);
                                        key += _mul * me.ID;
                                    }
                                }
                            }

                            Dictionary<ChartAxis, ChartArea> cdd;
                            if (!cell_details.TryGetValue(key, out cdd))
                            {
                                cdd = new Dictionary<ChartAxis, ChartArea>();
                                cell_details.Add(key, cdd);
                            }

                            ChartArea ca;
                            if (!cdd.TryGetValue(ax, out ca))
                            {
                                ca = new ChartArea(ax);
                                cdd.Add(ax, ca);
                            }

                            OlapChartSeries ccs;
                            if (!ca._series.TryGetValue(serieskey.ToString(), out ccs))
                            {
                                ccs = new OlapChartSeries(colorMember, sizeMember, shapeMember, m, ca);
                                ca._series.Add(serieskey.ToString(), ccs);
                            }
                            CD.fSeries = ccs;
                            ccs.Data.Add(CD);
                        }
                    }
                }
            }
            else
            {
                var yaxis_idx = -1;
                ChartAxis ay = null;
                if (fYAxesDescriptor != null && fYAxesDescriptor.ChartAreas.Count > 0)
                    ay = new ChartAxis(this, ChartAxisFormat.Discrete, fYAxesDescriptor.ChartAreas[0]);
                if (lr_keys.Count > 0)
                    yaxis_idx = clue.IndexOf(lr_keys[lr_keys.Count - 1]);
                if (datalist.Count > 0)
                {
                    var data = datalist.Values[0];

                    foreach (var dn in data)
                    {
                        var serieskey = new StringBuilder();
                        foreach (var i in ls_key)
                        {
                            serieskey.Append(dn.MemberIDs[i]);
                            serieskey.Append('|');
                        }

                        var CD = new ChartCellDetails(FGrid, clue, dn.MemberIDs);

                        if (yaxis_idx >= 0)
                        {
                            var mx = clue[yaxis_idx].GetMemberByID(dn.MemberIDs[yaxis_idx]);
                            CD._YValue = mx;
                            CD._YValueFormatted = mx.DisplayName;
                        }

                        CD._XValue = dn.Value;
                        CD._XValueFormatted = dn.FormattedValue;
                        xaxis.fMax = Math.Max(xaxis.fMax, dn.Value);
                        xaxis.fMin = Math.Min(xaxis.fMin, dn.Value);

                        Member colorMember = null;
                        Member sizeMember = null;
                        Member shapeMember = null;

                        if (sizeLine != null)
                        {
                            double sd;
                            if (sizeLine.GetNumericData(dn.LineIdx, out sd, out CD._SizeValueFormatted))
                            {
                                CD._SizeValue = sd;
                                sizeAxis.fMax = Math.Max(sizeAxis.fMax, sd);
                                sizeAxis.fMin = Math.Min(sizeAxis.fMin, sd);
                            }
                        }
                        else
                        {
                            if (LA.fSizeAxisItem is Hierarchy)
                            {
                                var mc = clue[sizeaxis_idx].GetMemberByID(dn.MemberIDs[sizeaxis_idx]);
                                sizeMember = mc;
                                if (!f_SizeChartMembers.ContainsKey(mc.FSortPosition))
                                    f_SizeChartMembers.Add(mc.FSortPosition, mc);
                            }
                        }
                        if (shapeLine != null)
                        {
                            double sd;
                            if (shapeLine.GetNumericData(dn.LineIdx, out sd, out CD._ShapeValueFormatted))
                            {
                                CD._ShapeValue = sd;
                                shapeAxis.fMax = Math.Max(shapeAxis.fMax, sd);
                                shapeAxis.fMin = Math.Min(shapeAxis.fMin, sd);
                            }
                        }
                        else
                        {
                            if (LA.fShapeAxisItem is Hierarchy)
                            {
                                var mc = clue[shapeaxis_idx].GetMemberByID(dn.MemberIDs[shapeaxis_idx]);
                                shapeMember = mc;
                                if (!f_ShapeChartMembers.ContainsKey(mc.FSortPosition))
                                    f_ShapeChartMembers.Add(mc.FSortPosition, mc);
                            }
                        }
                        if (colorLine != null)
                        {
                            double sd;
                            if (colorLine.GetNumericData(dn.LineIdx, out sd, out CD._ColorValueFormatted))
                            {
                                CD._ColorValue = sd;
                                colorAxis.fMax = Math.Max(colorAxis.fMax, sd);
                                colorAxis.fMin = Math.Min(colorAxis.fMin, sd);
                            }
                        }
                        else
                        {
                            if (LA.fColorAxisItem is Hierarchy)
                            {
                                var mc = clue[coloraxis_idx].GetMemberByID(dn.MemberIDs[coloraxis_idx]);
                                colorMember = mc;
                                if (!f_ColorChartMembers.ContainsKey(mc.FSortPosition))
                                    f_ColorChartMembers.Add(mc.FSortPosition, mc);
                            }
                        }

                        // xaxis_cells
                        long key = 0;
                        var tm = new Member[lc_clue.Length];
                        for (var i = 0; i < lc_clue.Length; i++)
                        {
                            var me = clue[lc_clue[i]].GetMemberByID(dn.MemberIDs[lc_clue[i]]);
                            if (!lc_clue_key[i])
                                me = me.GetParentMember(lc_keys[i]);
                            key += me.FSortPosition * lc_mul[i];
                            tm[i] = me;
                        }
                        if (!xcells.ContainsKey(key)) xcells.Add(key, tm);

                        // yaxis_cells
                        key = 0;
                        tm = new Member[lr_clue.Length];
                        for (var i = 0; i < lr_clue.Length; i++)
                        {
                            var me = clue[lr_clue[i]].GetMemberByID(dn.MemberIDs[lr_clue[i]]);
                            if (!lr_clue_key[i])
                                me = me.GetParentMember(lr_keys[i]);
                            key += me.FSortPosition * lr_mul[i];
                            tm[i] = me;
                        }
                        if (!ycells.ContainsKey(key)) ycells.Add(key, tm);

                        //main
                        key = 0;
                        for (var i = 0; i < clue.Count; i++)
                        {
                            var _mul = l_mul[i];
                            if (_mul > 0)
                            {
                                var me = clue[i].GetMemberByID(dn.MemberIDs[i]);
                                if (l_mul_key[i])
                                {
                                    key += _mul * me.ID;
                                }
                                else
                                {
                                    me = me.GetParentMember(l_mul_idx[i]);
                                    key += _mul * me.ID;
                                }
                            }
                        }

                        Dictionary<ChartAxis, ChartArea> cdd;
                        if (!cell_details.TryGetValue(key, out cdd))
                        {
                            cdd = new Dictionary<ChartAxis, ChartArea>();
                            cell_details.Add(key, cdd);
                        }

                        ChartArea ca;
                        if (!cdd.TryGetValue(xaxis, out ca))
                        {
                            ca = new ChartArea(ay);
                            cdd.Add(xaxis, ca);
                        }

                        OlapChartSeries ccs;
                        if (!ca._series.TryGetValue(serieskey.ToString(), out ccs))
                        {
                            ccs = new OlapChartSeries(colorMember, sizeMember, shapeMember, null, ca);
                            ca._series.Add(serieskey.ToString(), ccs);
                        }
                        CD.fSeries = ccs;
                        ccs.Data.Add(CD);
                    }
                }
            }


            var index = 0;
            fColumnChartMembers = null;
            List<Member> c_members = null;
            var xlimit = lc_clue.Length;
            if (LA.fXAxisMeasure == null)
            {
                fColumnChartMembers = new List<List<Member>>();
                xlimit--;
            }

            for (var i = 0; i < xlimit; i++)
                FColumnLevels.Add(new CellsetLevel(LA.fColumnLevels[i]));

            Member[] tmm;
            CellsetMember[] tcm;
            if (xlimit >= 0)
            {
                tmm = new Member[xlimit];
                tcm = new CellsetMember[xlimit];
                foreach (var cm in xcells.Values)
                {
                    CellsetMember parent = null;
                    for (var i = 0; i < xlimit; i++)
                    {
                        if (cm[i] != tmm[i])
                        {
                            for (var j = i; j < xlimit; j++)
                            {
                                tmm[j] = cm[j];
                                //TODO
                                tcm[j] = new CellsetMember(tmm[j], parent, FColumnLevels[j], false);
                                if (parent == null)
                                    FColumnMembers.Add(tcm[j]);
                                else
                                    parent.FChildren.Add(tcm[j]);
                                parent = tcm[j];
                            }
                            index++;
                            if (fColumnChartMembers != null)
                            {
                                c_members = new List<Member>();
                                fColumnChartMembers.Add(c_members);
                            }
                            break;
                        }
                        parent = tcm[i];
                    }
                    if (c_members != null)
                        c_members.Add(cm[cm.Length - 1]);
                }
                if (xlimit == 0)
                {
                    if (LA.fColumnLevels.Count == 1)
                    {
                        var l = new CellsetLevel(LA.fColumnLevels[0]);
                        FColumnLevels.Add(l);
                        //TODO
                        FColumnMembers.Add(new CellsetMember(null, null, l, false));
                        c_members = new List<Member>();
                        foreach (var mm in xcells.Values)
                            c_members.Add(mm[0]);
                        fColumnChartMembers.Add(c_members);
                    }
                    if (LA.fXAxisMeasure != null)
                    {
                        var l = new CellsetLevel(FGrid.Measures.Level);
#if !SL
                        if (Grid is OlapChart == false)
                            FColumnLevels.Add(l);
#endif
                        //TODO
                        FColumnMembers.Add(new CellsetMember(null, null, l, false));
                    }
                }
            }
            else
            {
                //TODO
                FColumnMembers.Add(new CellsetMember(null, null, null, false));
            }


            index = 0;
            fRowChartMembers = null;
            c_members = null;
            var ylimit = lr_clue.Length;
            if (LA.fYAxisMeasures.Count == 0)
            {
                fRowChartMembers = new List<List<Member>>();
                ylimit--;
            }

            for (var i = 0; i < ylimit; i++)
                FRowLevels.Add(new CellsetLevel(LA.fRowLevels[i]));

            if (ylimit >= 0)
            {
                tmm = new Member[ylimit];
                tcm = new CellsetMember[ylimit];
                foreach (var cm in ycells.Values)
                {
                    CellsetMember parent = null;
                    for (var i = 0; i < ylimit; i++)
                    {
                        if (cm[i] != tmm[i])
                        {
                            for (var j = i; j < ylimit; j++)
                            {
                                tmm[j] = cm[j];
                                //TODO
                                tcm[j] = new CellsetMember(tmm[j], parent, FRowLevels[j], true);
                                if (parent == null)
                                    FRowMembers.Add(tcm[j]);
                                else
                                    parent.FChildren.Add(tcm[j]);
                                parent = tcm[j];
                            }
                            index++;
                            if (fRowChartMembers != null)
                            {
                                c_members = new List<Member>();
                                fRowChartMembers.Add(c_members);
                            }
                            break;
                        }
                        parent = tcm[i];
                    }
                    if (c_members != null)
                        c_members.Add(cm[cm.Length - 1]);
                }
                if (ylimit == 0)
                {
                    if (LA.fRowLevels.Count == 1)
                    {
                        var l = new CellsetLevel(LA.fRowLevels[0]);
                        FRowLevels.Add(l);
                        //TODO
                        FRowMembers.Add(new CellsetMember(null, null, l, true));
                        c_members = new List<Member>();
                        foreach (var mm in ycells.Values)
                            c_members.Add(mm[0]);
                        fRowChartMembers.Add(c_members);
                    }
                    if (LA.fYAxisMeasures.Count > 0)
                    {
                        var l = new CellsetLevel(FGrid.Measures.Level);
#if !SL
                        if (Grid is OlapChart == false)
                            FRowLevels.Add(l);
#endif
                        //TODO
                        FRowMembers.Add(new CellsetMember(null, null, l, true));
                    }
                }
            }
            else
            {
                //TODO
                FRowMembers.Add(new CellsetMember(null, null, null, true));
            }

            if (fColumnChartMembers != null && fColumnChartMembers.Count == 0)
                fColumnChartMembers = null;
            if (fRowChartMembers != null && fRowChartMembers.Count == 0)
                fRowChartMembers = null;

            CreateSpans();

            if (f_ColorChartMembers != null)
                fColorChartMembers = new List<Member>(f_ColorChartMembers.Values);

            if (f_SizeChartMembers != null)
                fSizeChartMembers = new List<Member>(f_SizeChartMembers.Values);

            if (f_ShapeChartMembers != null)
                fShapeChartMembers = new List<Member>(f_ShapeChartMembers.Values);

            IChartsArray = new IChartCell[ColumnCount - FFixedColumns, RowCount - FFixedRows];
            for (var c = FFixedColumns; c < ColumnCount; c++)
            for (var r = FFixedRows; r < RowCount; r++)
                IChartsArray[c - FFixedColumns, r - FFixedRows] = new ChartCell(this, r, c, cell_details);

            if (FGrid.IsUpdating)
                FGrid.DecrementUpdateCounter();
            else
                FGrid.EndChange(GridEventType.geRebuildNodes);
        }
    }
}