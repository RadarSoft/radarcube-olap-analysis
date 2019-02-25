using System.Collections.Generic;

namespace RadarSoft.RadarCube.ClientAgents
{
    internal class ChartExportData
    {
        internal List<ExportChartCellJsonData> xAxisData { get; set; }
        internal List<ExportChartCellJsonData> yAxisData { get; set; }
        internal ExportChartCellJsonData[,] chartData { get; set; }
        internal ExportChartCellJsonData legendData { get; set; }
    }
}