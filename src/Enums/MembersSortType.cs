namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Evaluates available sorting methods for dimension members displayed in the
    ///     Grid.
    /// </summary>
    public enum MembersSortType
    {
        /// <summary>
        ///     Dimension members are sorted by their type defined by enumeration in HierarchyDataType.
        /// </summary>
        msTypeRelated,

        /// <summary>
        ///     Dimension members are sorted by alphabetical ascending order.
        /// </summary>
        msNameAsc,

        /// <summary>
        ///     Dimension members are sorted by alphabetical descending order.
        /// </summary>
        msNameDesc
    }
}