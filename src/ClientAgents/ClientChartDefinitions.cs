using System.ComponentModel;
using System.Linq;
using RadarSoft.RadarCube.Controls.Chart;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.ClientAgents
{
    public class ClientChartDefinitions
    {
        [DefaultValue(null)] public string[] ColorMeasureColors;

        [DefaultValue(null)] public string[] ColorMeasures;

        [DefaultValue(false)] public bool ShowPointsOnLines;

        [DefaultValue(null)] public string[] TrendMeasures;

        [DefaultValue(null)] public string[] TrendMeasureTypes;

        [DefaultValue(null)] public string[] TypeMeasures;

        [DefaultValue(null)] public string[] TypeMeasureTypes;

        public ClientChartDefinitions()
        {
        }

        internal ClientChartDefinitions(ChartDefinitions d)
        {
            InitClientChartDefinitions(d, d.ShowPointsOnLines);
        }

        private void InitClientChartDefinitions(ChartDefinitions d, bool showPointsOnLines = false)
        {
            ShowPointsOnLines = showPointsOnLines;

            if (d.MeasureColors.Count > 0)
            {
                ColorMeasures = d.MeasureColors.Keys.Select(item => item.UniqueName).ToArray();
                ColorMeasureColors = d.MeasureColors.Values.Select(item => item.ColorToString()).ToArray();
            }

            if (d.MeasureTypes.Count > 0)
            {
                TypeMeasures = d.MeasureTypes.Keys.Select(item => item.UniqueName).ToArray();
                TypeMeasureTypes = d.MeasureTypes.Values.Select(item => item.ToString()).ToArray();
            }

            if (d.TrendTypes.Count > 0)
            {
                TrendMeasures = d.TrendTypes.Keys.Select(item => item.UniqueName).ToArray();
                TrendMeasureTypes = d.TrendTypes.Values.Select(item => item.WrapperToString()).ToArray();
            }
        }
    }
}