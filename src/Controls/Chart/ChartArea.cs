using System.Collections.Generic;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.Controls.Chart
{
    /// <summary>
    ///     <para>
    ///         Returns the Chart and the data for building it (references
    ///         to the axes and a list of data series)
    ///     </para>
    /// </summary>
    public class ChartArea
    {
        internal Dictionary<string, OlapChartSeries> _series = new Dictionary<string, OlapChartSeries>();
        private ChartCell fChartCell;

        internal ChartArea(ChartAxis axis)
        {
            AxisY = axis;
        }

        /// <summary>
        ///     The list of data series displayed in the Chart.
        /// </summary>
        public List<OlapChartSeries> SeriesList { get; } = new List<OlapChartSeries>();

        /// <summary>
        ///     References to the Y axis of the Chart.
        /// </summary>
        public ChartAxis AxisY { get; }

        /// <summary>
        ///     References to the Grid cell, with the specified Chart.
        /// </summary>
        public IChartCell Cell => fChartCell;

        internal void ConvertSeries(ChartCell cell)
        {
            fChartCell = cell;
            foreach (var c in _series.Values)
                SeriesList.Add(c);
            _series = null;
        }
    }
}