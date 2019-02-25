using System.Drawing;
using RadarSoft.RadarCube.ClientAgents;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Controls.Chart
{
    public static class OlapChartExtensions
    {
        public static void DefineChartColor(this Measure m, Color c)
        {
            var ch2 = m.Grid as OlapChart;
            if (ch2 != null)
            {
                ch2.chartDefinitions.MeasureColors.Remove(m);
                if (c != Color.Empty)
                    ch2.chartDefinitions.MeasureColors.Add(m, c);
            }
        }

        public static void DefineChartMeasureType(this Measure m, SeriesType type)
        {
            var ch2 = m.Grid as OlapChart;
            if (ch2 != null)
            {
                ch2.chartDefinitions.MeasureTypes.Remove(m);
                ch2.chartDefinitions.MeasureTypes.Add(m, type);
            }
        }

        public static void DefineChartMeasureType(this Measure m, SeriesType type, bool showPointOnLines)
        {
            DefineChartMeasureType(m, type);
            var ch2 = m.Grid as OlapChart;
            if (ch2 != null)
                ch2.chartDefinitions.ShowPointsOnLines = showPointOnLines;
        }


        public static void DefineChartTrendType(this Measure m, TrendType trendtype, Color c)
        {
            var ch2 = m.Grid as OlapChart;
            if (ch2 != null)
            {
                ch2.chartDefinitions.TrendTypes.Remove(m);

                var tw = new TrendWrapper(c, trendtype);
                ch2.chartDefinitions.TrendTypes.Add(m, tw);
            }
        }
    }
}