using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Controls.Analysis;

namespace RadarSoft.RadarCube.ClientAgents
{
    public class MvcJsonSettings : JsonSettings
    {
        public string callbackAction;
        public string callbackController;
        public string exportAction;
        public string exportController;
        public string postbackAction;
        public string postbackController;

        public MvcJsonSettings(OlapControl grid)
            : base(grid)
        {
            var olapAnalysis = grid as OlapAnalysis;
            callbackController = olapAnalysis.CallbackController;
            callbackAction = olapAnalysis.CallbackAction;
            exportController = olapAnalysis.ExportController;
            exportAction = olapAnalysis.ExportAction;
            filterClientId = olapAnalysis.Filter.ClientID;
        }
    }
}