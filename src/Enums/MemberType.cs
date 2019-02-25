namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     The hierarchy member type.
    /// </summary>
    public enum MemberType
    {
        /// <summary>
        ///     The member type is not determined.
        /// </summary>
        mtUnknown,

        /// <summary>
        ///     A common hierarchy member.
        /// </summary>
        mtCommon,

        /// <summary>
        ///     A calculated hierarchy member.
        /// </summary>
        mtCalculated,

        /// <summary>
        ///     The member - a measure.
        /// </summary>
        mtMeasure,

        /// <summary>
        ///     The member - a group.
        /// </summary>
        mtGroup,
        mtMeasureMode
    }
}