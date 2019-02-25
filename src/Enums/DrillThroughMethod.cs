namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Represents the method applied when TOLAPCube.Drillthrough method is called
    ///     on.
    /// </summary>
    public enum DrillThroughMethod
    {
        /// <summary>
        ///     No Drillthrough method is applied. If this is the case, Drillthrough method doesn't run at all.
        /// </summary>
        Unknown,

        /// <summary>
        ///     The records are fetched as they are from the fact table. This means no conversion is performed over the hierarchy
        ///     members.
        /// </summary>
        PureFactTable,

        /// <summary>
        ///     All hierarchy members are fecthed from many tables ralated to the fact table.
        ///     The resulting hierarchy members are converted into their names when possible.
        /// </summary>
        EntireSchemaMembers
    }
}