using System;

namespace RadarSoft.RadarCube.Enums
{
    [Flags]
    internal enum DockPanelArea
    {
        laNone = 0,
        laRow = 1,
        laColumn = 2,
        laColor = 4,
        laSize = 8,
        laShape = 16,
        laDetails = 32,
        laTree = 64,

        laValues = 128,
        laColorFore = 256,
        laPage = 512,


        laQFilter = 1024,
        laToolBox = 2048,
        laHintLine = 4096,

        laLegends = laHintLine * 2,
        //laDocumentPane = laLegends * 2,

        //
        // special sets
        //

        la_OLAPGridPivotAreas = laQFilter | laTree | laRow | laValues
                                | laPage | laColumn
                                | laColor | laColorFore,

        la_OLAPGridData = laToolBox | laHintLine,
        la_OLAPGridAll = la_OLAPGridPivotAreas | la_OLAPGridData,

        la_OLAPChartPivotAreas = laQFilter | laTree | laRow | laValues
                                 | laPage | laColumn
                                 | laColor | laSize | laShape | laDetails | laLegends,

        la_OLAPChartData = laToolBox | laHintLine,
        la_OLAPChartAll = la_OLAPChartPivotAreas | la_OLAPChartData
    }
}