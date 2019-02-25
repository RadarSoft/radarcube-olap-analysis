using System;

namespace RadarSoft.RadarCube.Enums
{
    [Flags]
    internal enum ChartGridZone
    {
        gzNone = 0,

        /// <summary>
        ///     region for all level cells
        /// </summary>
        gzCellLevel = 1,
        gzCellColumn = 2,
        gzCellRow = 4,
        gzCells = gzCellLevel | gzCellColumn | gzCellRow,
        gzAxisX = 8,
        gzAxisY = 16,
        gzCharts = 32,
        gzEmptyCellUnderLeftMembers = 64,

        /// <summary>
        ///     pivot column level cells
        /// </summary>
        gzCellLevelCol = gzCellLevel | gzCellColumn,

        /// <summary>
        ///     pivot row level cells
        /// </summary>
        gzCellLevelRow = gzCellLevel | gzCellRow
    }
}