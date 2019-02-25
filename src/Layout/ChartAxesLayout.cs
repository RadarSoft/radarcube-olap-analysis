using System.Collections.Generic;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>
    ///     <para>
    ///         Describes the current layout for the OlapAnalysis control. As a AxesLayout
    ///         heir contains additional descriptopn of the X, Y, Color, Size and Details axes that
    ///         are used for building diagrams.
    ///     </para>
    /// </summary>
    public class ChartAxesLayout : AxesLayout
    {
        internal ChartAxesLayout(OlapControl AGrid)
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

        /// <summary>Gets (if any) or sets the measure describing the X axis.</summary>
        public Measure XAxis
        {
            get => fXAxisMeasure;
            set
            {
                if (fXAxisMeasure != value)
                {
                    fXAxisMeasure = value;
                    if (Grid.Active) Grid.CellSet.Rebuild();
                }
            }
        }

        /// <summary>
        ///     <para>Returns the list of measure groups, describing the Y axis.</para>
        /// </summary>
        /// <remarks>
        ///     To place measures into groups or remove them, use the appropriate
        ///     OlapGrid.Pivoting... methods.
        /// </remarks>
        public List<MeasureGroup> YAxis => fYAxisMeasures;

        /// <summary>
        ///     <para>
        ///         Gets an object (a measure or a hierarchy level), describing the Color axis or
        ///         null, if the Color axis is not used.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     To add/remove measures (or hierarchy levels) to the axis use the appropriate
        ///     OlapGrid.Pivoting... methods.
        /// </remarks>
        public IDescriptionable ColorAxis => fColorAxisItem;

        /// <summary>
        ///     <para>
        ///         Gets an object (a measure or a hierarchy level), describing the Size axis or
        ///         null, if the Size axis is not used.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     To add/remove measures (or hierarchy levels) to the axis use the appropriate
        ///     OlapGrid.Pivoting... methods.
        /// </remarks>
        public IDescriptionable SizeAxis => fSizeAxisItem;

        /// <summary>
        ///     <para>
        ///         Gets an object (a measure or a hierarchy level), describing the Shape axis or
        ///         null, if the Shape axis is not used.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     To add/remove measures (or hierarchy levels) to the axis use the appropriate
        ///     OlapGrid.Pivoting... methods.
        /// </remarks>
        public IDescriptionable ShapeAxis => fShapeAxisItem;
    }
}