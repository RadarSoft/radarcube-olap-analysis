using System;
using System.Collections.Generic;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Controls.Chart;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.CellSet
{
    /// <summary>
    ///     <para>
    ///         Represents Cellset for the chart-control. As the heir to the TCellset
    ///         contains additional description of diagram's axes.
    ///     </para>
    /// </summary>
    public class GridCellSet : CellSet
    {
        [NonSerialized] private ChartAxisDescriptor fColorAxisDescriptor;

        [NonSerialized] internal List<Member> fColorGridMembers;

        [NonSerialized] private ChartAxisDescriptor fForeColorAxisDescriptor;

        [NonSerialized] internal List<Member> fForeColorGridMembers;

        internal GridCellSet(OlapControl AGrid)
            : base(AGrid)
        {
        }

        /// <summary>References to the Grid-control, the specified Cellset belongs to.</summary>
        public new OlapChart Grid => (OlapChart) base.Grid;

        /// <summary>
        ///     Descriptor of the "Color" axis. If the "Color" axis is not defined, the
        ///     DesctiptorObject property of this descriptor is null.
        /// </summary>
        public ChartAxisDescriptor ColorAxisDescriptor => fColorAxisDescriptor;

        /// <summary>
        ///     Descriptor of the "ForeColor" axis. If the "ForeColor" axis is not defined, the
        ///     DesctiptorObject property of this descriptor is null.
        /// </summary>
        public ChartAxisDescriptor ForeColorAxisDescriptor => fForeColorAxisDescriptor;

        /// <summary>
        ///     <para>
        ///         Returns the list of members - elements of the color modification axis or null
        ///         if the color modificator is NOT an hierarchy level.
        ///     </para>
        /// </summary>
        public IList<Member> ColorGridMembers => fColorGridMembers == null ? null : fColorGridMembers.AsReadOnly();

        /// <summary>
        ///     <para>
        ///         Returns the list of members - elements of the forecolor modification axis or null
        ///         if the forecolor modificator is NOT an hierarchy level.
        ///     </para>
        /// </summary>
        public IList<Member> ForeColorGridMembers =>
            fForeColorGridMembers == null ? null : fForeColorGridMembers.AsReadOnly();

        internal override void ClearMembers()
        {
            base.ClearMembers();
            fColorAxisDescriptor = null;
            fForeColorAxisDescriptor = null;
        }

        internal override void RebuildGrid()
        {
            FGrid.fMeasures.InitMeasures();
            var LA = FGrid.FLayout;
            ChartAxis colorAxis = null;

            fColorAxisDescriptor = new ChartAxisDescriptor();
            fColorGridMembers = null;

            SortedDictionary<int, Member>
                f_ColorGridMembers = null; // <ID, index in the corresponding Members collection>
            if (LA.fColorAxisItem != null)
            {
                if (LA.fColorAxisItem is Measure)
                {
                    fColorAxisDescriptor.fDescriptor = LA.fColorAxisItem;
                    colorAxis = new ChartAxis(this, ChartAxisFormat.Continuous, fColorAxisDescriptor);
                }
                if (LA.fColorAxisItem is Hierarchy)
                {
                    var H = (Hierarchy) LA.fColorAxisItem;
                    H.DefaultInit(1);
                    fColorAxisDescriptor.fDescriptor = ((Hierarchy) LA.fColorAxisItem).FirstVisibleLevel();
                    if (fColorAxisDescriptor.fDescriptor != null)
                    {
                        fColorGridMembers = new List<Member>(((Level) fColorAxisDescriptor.fDescriptor).Members);
                        colorAxis = new ChartAxis(this, ChartAxisFormat.Discrete, fColorAxisDescriptor);
                    }
                }
            }


            fForeColorAxisDescriptor = new ChartAxisDescriptor();
            fForeColorGridMembers = null;
            SortedDictionary<int, Member>
                f_ForeColorChartMembers = null; // <ID, index in the corresponding Members collection>
            if (LA.fColorForeAxisItem != null)
            {
                if (LA.fColorForeAxisItem is Measure)
                {
                    fForeColorAxisDescriptor.fDescriptor = LA.fColorForeAxisItem;
                    colorAxis = new ChartAxis(this, ChartAxisFormat.Continuous, fForeColorAxisDescriptor);
                }

                if (LA.fColorForeAxisItem is Hierarchy)
                {
                    var H = (Hierarchy) LA.fColorForeAxisItem;
                    H.DefaultInit(1);
                    fForeColorAxisDescriptor.fDescriptor = ((Hierarchy) LA.fColorForeAxisItem).FirstVisibleLevel();
                    if (fForeColorAxisDescriptor.fDescriptor != null)
                    {
                        fForeColorGridMembers =
                            new List<Member>(((Level) fForeColorAxisDescriptor.fDescriptor).Members);
                        colorAxis = new ChartAxis(this, ChartAxisFormat.Discrete, fForeColorAxisDescriptor);
                    }
                }
            }

            //// making the cartesian product of used levels

            //List<Level> ldecart = new List<Level>();
            //SortedList<string, Level> l_keys = new SortedList<string, Level>(); // list of levels to hold the IChartCell array 
            //for (int i = 0; i < LA.fColumnLevels.Count; i++)
            //{
            //    Level l = LA.fColumnLevels[i];
            //    //if ((i < LA.fColumnLevels.Count - 1) &&
            //    //    (LA.fColumnLevels[i + 1].Hierarchy == l.Hierarchy)) continue;
            //    ldecart.Add(l);
            //}

            //for (int i = 0; i < LA.fRowLevels.Count; i++)
            //{
            //    Level l = LA.fRowLevels[i];

            //    //if ((i < LA.fRowLevels.Count - 1) &&
            //    //    (LA.fRowLevels[i + 1].Hierarchy == l.Hierarchy)) continue;
            //    ldecart.Add(l);
            //}


            //if (LA.fColorAxisItem is Hierarchy)
            //{
            //    Level l = ((Hierarchy)LA.fColorAxisItem).FirstVisibleLevel();
            //    ldecart.Add(l);
            //    CheckLevelForFetchedParents(l);
            //}


            //// fetching ascendants of members of used levels if it is necessary (it should be done BEFORE fetching the lines)

            //foreach (Level l in LA.fRowLevels)
            //{
            //    CheckLevelForFetchedParents(l);
            //}

            //foreach (Level l in LA.fColumnLevels)
            //{
            //    CheckLevelForFetchedParents(l);
            //}


            //// fetching the data for size and color modifiers

            //Line colorLine = null;
            //ChartAxis colorAxis = null;
            //if (LA.fColorAxisItem is Measure)
            //{
            //    colorLine = FGrid.FEngine.RetrieveRelatedLine(ldecart, (Measure)LA.fColorAxisItem);
            //    colorAxis = new ChartAxis(this, ChartAxisFormat.Continuous, fColorAxisDescriptor);
            //}
            //else
            //{
            //    if (LA.fColorAxisItem is Hierarchy)
            //    {
            //        colorAxis = new ChartAxis(this, ChartAxisFormat.Discrete, fColorAxisDescriptor);
            //    }
            //}

            //List<Level> clue = new List<Level>();

            //// fetching the data
            //SortedList<string, List<CubeDataNumeric>> datalist = new SortedList<string, List<CubeDataNumeric>>();
            //SortedList<string, Line> datalist2 = new SortedList<string, Line>();
            //Line xLine = null;

            //foreach (Measure m in FGrid.fMeasures)
            //{
            //    if (m.Visible == false)
            //        continue;
            //    datalist2.Add(m.UniqueName, FGrid.FEngine.RetrieveRelatedLine(ldecart, m));
            //}
            //// all the data has been prepared for fetching

            //if (!FGrid.DeferLayoutUpdate)
            //    FGrid.FEngine.DoRetrieveData();


            //foreach (KeyValuePair<string, Line> dl2 in datalist2)
            //    datalist.Add(dl2.Key, FGrid.FEngine.RetrieveCubeData(dl2.Value, out clue));


            //int coloraxis_idx = -1;
            //if (LA.fColorAxisItem is Hierarchy)
            //{
            //    coloraxis_idx = clue.IndexOf(((Hierarchy)LA.fColorAxisItem).FirstVisibleLevel());
            //    ((Hierarchy)LA.fColorAxisItem).FirstVisibleLevel().SetSortPosition();
            //}

            //if (FGrid.fMeasures.Count > 0)
            //{
            //    foreach (Measure m in FGrid.fMeasures)
            //    {
            //        if (m.Visible == false)
            //            continue;

            //        List<CubeDataNumeric> data;
            //        datalist.TryGetValue(m.UniqueName, out data);

            //        foreach (CubeDataNumeric dn in data)
            //        {

            //            ChartCellDetails CD = new ChartCellDetails(FGrid, clue, dn.MemberIDs);
            //            Member colorMember = null;

            //            if (colorLine != null)
            //            {
            //                double sd;
            //                if (colorLine.GetNumericData(dn.LineIdx, out sd, out CD._ColorValueFormatted))
            //                {
            //                    CD._ColorValue = sd;
            //                    colorAxis.fMax = Math.Max(colorAxis.fMax, sd);
            //                    colorAxis.fMin = Math.Min(colorAxis.fMin, sd);
            //                }
            //            }
            //            else
            //            {
            //                if (LA.fColorAxisItem is Hierarchy)
            //                {
            //                    Member mc = clue[coloraxis_idx].GetMemberByID(dn.MemberIDs[coloraxis_idx]);
            //                    colorMember = mc;
            //                    if (!f_ColorGridMembers.ContainsKey(mc.FSortPosition))
            //                        f_ColorGridMembers.Add(mc.FSortPosition, mc);
            //                }
            //            }
            //        }
            //    }
            //}
            //else
            //{
            //    if (datalist.Count > 0)
            //    {
            //        List<CubeDataNumeric> data = datalist.Values[0];

            //        foreach (CubeDataNumeric dn in data)
            //        {
            //            Member colorMember = null;
            //            ChartCellDetails CD = new ChartCellDetails(FGrid, clue, dn.MemberIDs);

            //            if (colorLine != null)
            //            {
            //                double sd;
            //                if (colorLine.GetNumericData(dn.LineIdx, out sd, out CD._ColorValueFormatted))
            //                {
            //                    CD._ColorValue = sd;
            //                    colorAxis.fMax = Math.Max(colorAxis.fMax, sd);
            //                    colorAxis.fMin = Math.Min(colorAxis.fMin, sd);
            //                }
            //            }
            //            else
            //            {
            //                if (LA.fColorAxisItem is Hierarchy)
            //                {
            //                    Member mc = clue[0].GetMemberByID(dn.MemberIDs[0]);
            //                    colorMember = mc;
            //                    if (!f_ColorGridMembers.ContainsKey(mc.FSortPosition))
            //                        f_ColorGridMembers.Add(mc.FSortPosition, mc);
            //                }
            //            }
            //        }
            //    }
            //}


            //if (f_ColorGridMembers != null)
            //{
            //    fColorGridMembers = new List<Member>(f_ColorGridMembers.Values);
            //}
        }
    }
}