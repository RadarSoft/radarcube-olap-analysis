namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Enumerates the possible types of hierarchy members.
    /// </summary>
    public enum HierarchyDataType
    {
        /// <summary>
        ///     The type of the hierarchy members isn't defined
        ///     (the members will be sorted regarding their database order)
        /// </summary>
        htCommon,

        /// <summary>
        ///     The hierarchy members are numeric values
        /// </summary>
        htNumbers,

        /// <summary>
        ///     The hierarchy members are strings
        /// </summary>
        htStrings,

        /// <summary>
        ///     The hierarchy members are bytes
        /// </summary>
        htBytes,

        /// <summary>
        ///     The hierarchy members are dates
        /// </summary>
        htDates,

        /// <summary>
        ///     The members are measure captions
        /// </summary>
        htMeasures
    }
}