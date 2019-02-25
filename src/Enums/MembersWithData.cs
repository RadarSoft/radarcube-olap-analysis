namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Enumerates the Cube's actions when it meets a value in the fact table for a
    ///     hierarchy member with Child members og its own, i.e. it is not leaf.
    /// </summary>
    public enum MembersWithData
    {
        /// <summary>
        ///     Ready only those records corresponding to leaves in the hierarchy. Ignore the entire row of the fact table with
        ///     data for the non-leaf members.
        /// </summary>
        dmLeafMembersOnly,

        /// <summary>
        ///     Raises an exception if the fact table contains data for non-leaf member in the hierarchy.
        /// </summary>
        dmNonLeafDataException,

        /// <summary>
        ///     Creates an additional member with the same name in the hierarchy and associate the row with this member,
        ///     but not show the empty member in the grid. In this case the values of totals may differ from the sum of members.
        /// </summary>
        dmNonLeafDataHidden,

        /// <summary>
        ///     Creates an additional member with the same name in the hierarchy and associate the row with this member,
        ///     and show the empty member as an additional child of the non-leaf member along with other siblings.
        /// </summary>
        dmNonLeafDataVisible
    }
}