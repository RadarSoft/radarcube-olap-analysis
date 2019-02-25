using System.ComponentModel;

namespace RadarSoft.RadarCube.CellSet
{
    /// <summary>
    ///     Enumerates the export Cellset contents modes.
    /// </summary>
    public enum CellsetTableMode
    {
        /// <summary>
        ///     The Cellset content is not exported.
        /// </summary>
        [Description("None")] ctmNone,

        /// <summary>
        ///     Cellset is exported completely.
        /// </summary>
        [Description("Whole cellset")] ctmWholeCellset,

        /// <summary>
        ///     Cellset is exported completely except totals.
        /// </summary>
        [Description("Whole cellset except totals")] ctmWholeCellsetExceptTotals,

        /// <summary>
        ///     Only a selected part in Grid is exported.
        /// </summary>
        [Description("Selection only")] ctmSelectionOnly,

        /// <summary>
        ///     Neighbors of a selected cell are exported vertically.
        /// </summary>
        [Description("Selected cell neighbors vertical")] ctmSelectedCellNeighborsVertical,

        /// <summary>
        ///     Neighbors of a selected cell are exported horizontally.
        /// </summary>
        [Description("Selected cell neighbors horizontal")] ctmSelectedCellNeighborsHorizontal,

        /// <summary>
        ///     A cellset row containing a selected cell is exported.
        /// </summary>
        [Description("Selected cell row")] ctmSelectedCellRow,

        /// <summary>
        ///     A cellset column containing a selected cell is exported.
        /// </summary>
        [Description("Selected cell column")] ctmSelectedCellColumn
    }
}