namespace RadarSoft.RadarCube.Enums
{
    /// <summary>Enumerates the display mode of the current measure.</summary>
    public enum MeasureShowModeType
    {
        /// <summary>
        ///     Algorithm is specified in the OlapGrid.OnShowMeasure event handler.
        /// </summary>
        smSpecifiedByEvent,

        /// <summary>
        ///     Normal display mode. The aggregated value of the cube cell is displayed in the grid.
        /// </summary>
        smNormal,

        /// <summary>
        ///     In percents from total value in the row.
        /// </summary>
        smPercentRowTotal,

        /// <summary>
        ///     In percents from total value in the column.
        /// </summary>
        smPercentColTotal,

        /// <summary>
        ///     In percents from the parent element of the row.
        /// </summary>
        smPercentParentRowItem,

        /// <summary>
        ///     In percents from the parent element of the column.
        /// </summary>
        smPercentParentColItem,

        /// <summary>
        ///     The row rank of the cell.
        /// </summary>
        smRowRank,

        /// <summary>
        ///     The column rank of the cell.
        /// </summary>
        smColumnRank,

        /// <summary>
        ///     In percents from total grand.
        /// </summary>
        smPercentGrandTotal,
        smKPIValue,
        smKPIGoal,
        smKPIStatus,
        smKPITrend,
        smKPIWeight
    }
}