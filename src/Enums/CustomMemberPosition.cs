namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Specifies the position of the calculated hierarchy member among its neighbors on
    ///     the same hierarchy level.
    /// </summary>
    public enum CustomMemberPosition
    {
        /// <summary>
        ///     Calculated member holds the first position.
        /// </summary>
        cmpFirst,

        /// <summary>
        ///     Calculated member holds the last position.
        /// </summary>
        cmpLast,

        /// <summary>
        ///     Calculated member holds the position according to the sorting rule which is being used at the moment.
        /// </summary>
        cmpGeneralOrder
    }
}