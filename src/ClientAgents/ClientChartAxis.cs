using System.ComponentModel;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class ClientChartAxis
    {
        public ChartAxisFormat Format;

        [DefaultValue(0)] public double Max = 0;

        [DefaultValue(0)] public double Min = 0;
    }
}