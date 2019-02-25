using RadarSoft.RadarCube.Controls.Analysis;
using RadarSoft.RadarCube.Controls.Chart;

namespace RadarSoft.RadarCube.ClientAgents
{
    public class MvcChartJsonSettings : JsonChartSettings
    {
        public string callbackAction;
        public string callbackController;
        public string exportAction;
        public string exportController;
        public string postbackAction;
        public string postbackController;

        public MvcChartJsonSettings(OlapChart chart)
            : base(chart)
        {
            var mvcChart = chart as OlapAnalysis;
            callbackController = mvcChart.CallbackController;
            callbackAction = mvcChart.CallbackAction;
            exportController = mvcChart.ExportController;
            exportAction = mvcChart.ExportAction;
            filterClientId = mvcChart.Filter.ClientID;
        }
    }
}