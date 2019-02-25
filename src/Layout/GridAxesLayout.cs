using System.Collections.Generic;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>
    ///     <para>
    ///         Describes the current layout for the OlapGrid control. As a AxesLayout
    ///         heir contains additional descriptopn of cell Background and Foreground.
    ///     </para>
    /// </summary>
    public class GridAxesLayout : AxesLayout
    {
        internal GridAxesLayout(OlapControl AGrid)
            : base(AGrid)
        {
        }

        /// <summary>
        ///     <para>
        ///         The list of Y axis' levels (rows). Expands the RowAxis property that contains
        ///         the list of the specified axis' hierarchies.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         To relocate hierarchies between the axis use the OlapGrid.Pivoting... methods,
        ///         to expand/hide levels within hierarchies - OlapGrid.Cellset.ExpandAllNodes and
        ///         Level.Visible.
        ///     </para>
        /// </remarks>
        public IList<Level> RowLevels => fRowLevels.AsReadOnly();

        /// <summary>
        ///     <para>
        ///         The list of the X axis' levels (columns). Expands the ColumnAxis property
        ///         that contains the list of the specified axis' levels.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         To relocate hierarchies between the axis use the OlapGrid.Pivoting... methods,
        ///         to expand/hide levels within hierarchies - OlapGrid.Cellset.ExpandAllNodes and
        ///         Level.Visible.
        ///     </para>
        /// </remarks>
        public IList<Level> ColumnLevels => fColumnLevels.AsReadOnly();

        ///// <summary>Gets (if any) or sets the measure describing the X axis.</summary>
        //public Measure XAxis
        //{
        //    get { return fXAxisMeasure; }
        //    set
        //    {
        //        if (fXAxisMeasure != value)
        //        {
        //            fXAxisMeasure = value;
        //            if (Grid.Active) Grid.CellSet.Rebuild();
        //        }
        //    }
        //}

        ///// <summary><para>Returns the list of measure groups, describing the Y axis.</para></summary>
        ///// <remarks>To place measures into groups or remove them, use the appropriate 
        ///// OlapGrid.Pivoting... methods. </remarks>
        //public List<MeasureGroup> YAxis
        //{
        //    get { return fYAxisMeasures; }
        //}

        /// <summary>
        ///     <para>
        ///         Gets an object (a measure or a hierarchy level), describing the Background color axis or
        ///         null, if the Background color axis is not used.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     To add/remove measures (or hierarchy levels) to the axis use the appropriate
        ///     OlapAnalysis.Pivoting... methods.
        /// </remarks>
        public IDescriptionable BackgroundAxis => fColorAxisItem;

        /// <summary>
        ///     <para>
        ///         Gets an object (a measure or a hierarchy level), describing the Foreground color axis or
        ///         null, if the Foreground color axis is not used.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     To add/remove measures (or hierarchy levels) to the axis use the appropriate
        ///     OlapGrid.Pivoting... methods.
        /// </remarks>
        public IDescriptionable ForegroundAxis => fColorForeAxisItem;
    }
}