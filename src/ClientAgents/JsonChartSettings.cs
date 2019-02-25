using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Controls.Chart;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.ClientAgents
{
    public class JsonChartSettings : JsonSettings
    {
        public string background;

        //public SeriesType[] chartsType = null;
        public ClientChartDefinitions chartDefinitions;

        public string foreground;
        public int maxTextLength = 30;
        public string rsClose;
        public string rsShowUnderlying;
        public double scaleX = 1D;
        public double scaleY = 1D;
        public string txt_trendsMenu;
        public string url_areasBtn;
        public string url_areasMenu;
        public string url_barsBtn;
        public string url_barsMenu;
        public string url_cubicTrendsBtn;
        public string url_deltaAreasBtn;
        public string url_deltaBarsBtn;
        public string url_deltaLinesBtn;
        public string url_holesBtn;
        public string url_linesBtn;
        public string url_linesMenu;
        public string url_linTrendsBtn;
        public string url_noTrendsBtn;
        public string url_percentAreasBtn;
        public string url_percentBarsBtn;
        public string url_percentLinesBtn;
        public string url_piesBtn;
        public string url_piesMenu;
        public string url_pointsBtn;

        public string url_pointsMenu;
        public string url_polylinesBtn;
        public string url_quadTrendsBtn;
        public string url_stepLinesBtn;
        public string url_trendsMenu;

        public FontProperties fontProperties;


        internal JsonChartSettings(OlapChart chart)
            : base(chart)
        {
            url_pointsMenu = chart.ImageUrl("pointsMenu.png");
            url_pointsBtn = chart.ImageUrl("points.png");
            url_holesBtn = chart.ImageUrl("holes.png");
            url_linesMenu = chart.ImageUrl("linesMenu.png");
            url_linesBtn = chart.ImageUrl("lines.png");
            url_polylinesBtn = chart.ImageUrl("polylines.png");
            url_stepLinesBtn = chart.ImageUrl("stepLines.png");
            url_deltaLinesBtn = chart.ImageUrl("deltaLines.png");
            url_percentLinesBtn = chart.ImageUrl("percentLines.png");
            url_barsMenu = chart.ImageUrl("barsMenu.png");
            url_barsBtn = chart.ImageUrl("bars.png");
            url_deltaBarsBtn = chart.ImageUrl("deltaBars.png");
            url_percentBarsBtn = chart.ImageUrl("percentBars.png");
            url_areasMenu = chart.ImageUrl("areasMenu.png");
            url_areasBtn = chart.ImageUrl("areas.png");
            url_deltaAreasBtn = chart.ImageUrl("deltaAreas.png");
            url_percentAreasBtn = chart.ImageUrl("percentAreas.png");
            url_piesMenu = chart.ImageUrl("piesMenu.png");
            url_piesBtn = chart.ImageUrl("pies.png");
            url_trendsMenu = chart.ImageUrl("trendsMenu.png");
            url_noTrendsBtn = chart.ImageUrl("noTrends.png");
            url_linTrendsBtn = chart.ImageUrl("linTrends.png");
            url_quadTrendsBtn = chart.ImageUrl("quadTrends.png");
            url_cubicTrendsBtn = chart.ImageUrl("cubicTrends.png");
            rsClose = RadarUtils.GetResStr("rsClose");
            rsShowUnderlying = RadarUtils.GetResStr("mnShowUnderlying");

            //url_nextlevel = chart.ImageUrl("Expand.gif");
            //url_collapsedl = chart.ImageUrl("Collapse.gif");
            //url_nextlevel = chart.ImageUrl("Expand_Level.gif");

            url_nextlevel = chart.ImageUrl("plus.png");
            url_collapsedl = chart.ImageUrl("minus.png");
        }


        public override void InitControlData(CellSet.CellSet cs, OlapControl grid)
        {
            if (grid.callbackException != null)
            {
                exception = SessionTimeoutDialog.RenderMassage(grid, grid.callbackException);
                return;
            }

            if (grid.Cube == null)
                return;

            Cellset = new RCellset(cs, grid.MaxTextLength);
            Layout = new ClientLayout(grid.AxesLayout);

            var chart = grid as OlapChart;
            if (!chart.chartDefinitions.IsEmpty)
                chartDefinitions = new ClientChartDefinitions(chart.chartDefinitions);

            if (!double.IsNaN(chart.Scale.Item1) && !double.IsNaN(chart.Scale.Item2))
            {
                scaleX = chart.Scale.Item1;
                scaleY = chart.Scale.Item2;
            }

            //grid.ChartsType = new SeriesType[]{SeriesType.Bar};
            chartsType = grid.ChartsType;
            maxTextLength = grid.MaxTextLength;
            analysisType = "chart";

            fontProperties = new FontProperties();
        }
    }
}