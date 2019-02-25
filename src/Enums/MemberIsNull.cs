namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Enumerates the Cube's actions when it meets null in the fact table instead of a
    ///     hierarchy member.
    /// </summary>
    public enum MemberIsNull
    {
        /// <summary>
        ///     Ignores the entire row of the fact table.
        /// </summary>
        nmIgnoreRecord,

        /// <summary>
        ///     Raises an exception.
        /// </summary>
        nmRaiseException,

        /// <summary>
        ///     Creates an empty (virtual) member in the hierarchy and associate the row with this member,
        ///     but not show the empty member in the grid. In this case the values of totals may differ from the sum of members.
        /// </summary>
        nmUnknownMemberHidden,

        /// <summary>
        ///     Creates an empty (virtual) member in the hierarchy and associate the row with this member,
        ///     and show the empty member in the grid along with others.
        /// </summary>
        nmUnknownMemberVisible
    }
}