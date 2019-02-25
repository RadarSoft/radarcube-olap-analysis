namespace RadarSoft.RadarCube.Enums
{
    /// <summary>Enumerates the restriction type for measure filters.</summary>
    public enum MeasureFilterRestriction
    {
        /// <summary>
        ///     Restricts the fact table values (the Desktop or Direct versions only).
        /// </summary>
        mfrFactTable,

        /// <summary>
        ///     Restricts the aggregated values.
        /// </summary>
        mfrAggregatedValues
    }
}