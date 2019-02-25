using System.Collections.Generic;
using RadarSoft.RadarCube.Controls.Chart;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Interfaces
{
    /// <summary>
    ///     Interface, describing the Grid cell with Charts.
    /// </summary>
    /// <remarks>Is returned by the CellSet.Cells. method.</remarks>
    public interface IChartCell : ICell
    {
        /// <summary>
        ///     Identifies the position of the specified cell within a multidimensional
        ///     cube.
        /// </summary>
        ICubeAddress Address { get; }

        /// <summary>
        ///     Returns the IMemberCell interface to the nearest Grid cell in the area of rows
        ///     linked to the hierarchy members names.
        /// </summary>
        IMemberCell RowMember { get; }

        /// <summary>
        ///     Returns, as the IMemberCell interface, a reference to the closest Grid cell,
        ///     associated with the specified hierarchy member, from the columns area.
        /// </summary>
        IMemberCell ColumnMember { get; }

        /// <summary>
        ///     The list of Charts in the Grid cell.
        /// </summary>
        /// <remarks>
        ///     In case when the Y axis is measure-based, one cell may contain one or a few
        ///     Charts, each of which has its own Y axis, while X axis is common for all of them.
        ///     In the Axes Layout context, the list of cell's Charts is defined by the list
        ///     of measure groups. (see the ChartAxesLayout.YAxis property).
        /// </remarks>
        ChartArea[] Charts { get; }

        /// <summary>
        ///     References to the X axis.
        /// </summary>
        ChartAxis AxisX { get; }

        /// <summary>
        ///     For a discrete X axis returnes the list of members, that form the X axis for the Chart.
        /// </summary>
        IList<Member> XMembers { get; }

        /// <summary>
        ///     For a discrete Y axis returnes the list of members, that form the Y axis for the Chart.
        /// </summary>
        IList<Member> YMembers { get; }

        /// <summary>
        ///     Returnes the possible list of MSAS Actions for the Chart point.
        /// </summary>
        /// <param name="details"></param>
        /// <returns></returns>
        IList<CubeAction> Actions(ChartCellDetails details);
    }
}