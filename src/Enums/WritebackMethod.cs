namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Represents methods of allocating a value for a specified non-leaf cell across dependent leaf cells during the
    ///     write-back operation.
    /// </summary>
    public enum WritebackMethod
    {
        /// <summary>
        ///     <leaf cell value> =
        ///         <value> / <count of leaf cells>
        /// </summary>
        wmEqualAllocation,

        /// <summary>
        ///     <leaf cell value> =
        ///         <leaf cell value> + (
        ///             <value> - <existing value>) / Count of leaf cells
        /// </summary>
        wmEqualIncrement,

        /// <summary>
        ///     <leaf cell value> =
        ///         <value> * <leaf cell value> / <existing value>
        /// </summary>
        wmWeightedAllocation,

        /// <summary>
        ///     <leaf cell value> =
        ///         <leaf cell value> + (
        ///             <value> - <existing value>) * <leaf cell value> / <existing value>
        /// </summary>
        wmWeightedIncrement
    }
}