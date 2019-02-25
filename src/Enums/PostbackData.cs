using System;

namespace RadarSoft.RadarCube.Enums
{
    [Flags]
    public enum PostbackData
    {
        Nothing = 0,
        Toolbox = 1,
        CubeTree = 2,
        PivotArea = 4,
        Data = 8,
        Modificators = 16,
        Legends = 32,
        FilterGrid = 64,
        OlapGridContainer = 128,
        All = Toolbox | CubeTree | PivotArea | Data | Modificators | Legends | FilterGrid,
        ToolboxData = Toolbox | Data
    }
}