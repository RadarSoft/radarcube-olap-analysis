using System.Collections.Generic;

namespace RadarSoft.RadarCube.Controls.Chart
{
    /// <summary>
    ///     <para>
    ///         Describes the Cube's Y axis. Contains the list of objects, describing the
    ///         list of this axis' charts.
    ///     </para>
    /// </summary>
    public class ChartAxesDescriptor
    {
        /// <summary>
        ///     <para>
        ///         The list of the Y axis' descriptors. Each cell of the Y axis will contain one
        ///         or a few charts, whose descriptors are in this list.
        ///     </para>
        /// </summary>
        public List<ChartAxisDescriptor> ChartAreas { get; } = new List<ChartAxisDescriptor>();
    }
}