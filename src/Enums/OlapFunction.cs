namespace RadarSoft.RadarCube.Enums
{
    /// <summary>Enumerates the function to aggregate the fact data into a Cube cell.</summary>
    public enum OlapFunction
    {
        /// <summary>
        ///     For MSAS versions only. Indicates that the aggregation type of the measure cannot be aggregated to any other value
        ///     of this enumeration.
        /// </summary>
        stInherited,

        /// <summary>
        ///     Indicates the calculated measure of the second type.
        ///     It means the values of this measure are aggregated on the values calculated for each row of the fact table in
        ///     TOLAPCube.OnAggregate event handler.
        /// </summary>
        stCustomAggregated,

        /// <summary>
        ///     Indicates the calculated measure of the third type.
        ///     It means the values of ths measure are cal�ulated on peviously aggregated cube cells in the OlapGrid.OnCalcMember
        ///     event handler.
        /// </summary>
        stCalculated,

        /// <summary>
        ///     The sum of measure values for all fact table rows aggreagated into the current cube cell.
        /// </summary>
        stSum,

        /// <summary>
        ///     A number of fact table rows aggregated into the current cube cell.
        /// </summary>
        stCount,

        /// <summary>
        ///     Mean, average of distribution.
        /// </summary>
        stAverage,

        /// <summary>
        ///     Minimum measure values for all fact table rows aggregated into the current cube cell.
        /// </summary>
        stMin,

        /// <summary>
        ///     A maximum measure value for all fact table rows aggregated into the current cube cell.
        /// </summary>
        stMax,

        /// <summary>
        ///     Unbiased variance of measure values for all fact table rows aggregated into the current cube cell.
        /// </summary>
        stVariance,

        /// <summary>
        ///     Biased variance of measure values for all fact table rows, aggregated into the current cube cell.
        /// </summary>
        stVarianceB,

        /// <summary>
        ///     Unbiased standard deviation of measure values for all fact table rows aggregated into the current cube cell.
        /// </summary>
        stStdDev,

        /// <summary>
        ///     Biased standard deviation of measure values for all fact table rows aggregated into the current cube cell.
        /// </summary>
        stStdDevB,

        /// <summary>
        ///     The median of measure values for all fact table rows aggregated into the current cube cell.
        /// </summary>
        stMedian,

        /// <summary>
        ///     A number of unique measure values for all fact table rows aggregated into the current cube cell.
        /// </summary>
        stDistinctCount
    }
}