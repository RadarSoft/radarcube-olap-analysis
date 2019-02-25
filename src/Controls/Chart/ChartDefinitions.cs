using System.Collections.Generic;
using System.Drawing;
using RadarSoft.RadarCube.ClientAgents;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Controls.Chart
{
    internal class ChartDefinitions
    {
        internal bool ShowPointsOnLines = false;

        internal Dictionary<Measure, Color> MeasureColors = new Dictionary<Measure, Color>();

        internal bool IsEmpty => MeasureColors.Count == 0 &&
                                 MeasureTypes.Count == 0 &&
                                 TrendTypes.Count == 0;
#if DEBUG
        internal SerializableDictionary<Measure, SeriesType> MeasureTypes =
            new SerializableDictionary<Measure, SeriesType>();
#else
        internal Dictionary<Measure, SeriesType> MeasureTypes = new Dictionary<Measure, SeriesType>();
#endif
        internal Dictionary<Measure, TrendWrapper> TrendTypes = new Dictionary<Measure, TrendWrapper>();
    }
}