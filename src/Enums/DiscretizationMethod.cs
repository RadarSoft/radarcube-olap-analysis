namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Enumerates the discretization mode to apply to the hierarchy when building
    ///     dimension members.
    /// </summary>
    public enum DiscretizationMethod
    {
        /// <summary>
        ///     No discretization is applied to the hierarchy.
        /// </summary>
        dmNone,

        /// <summary>
        ///     Groups hierarchy members into ranges with the same number of members.
        /// </summary>
        dmEqualRanges,

        /// <summary>
        ///     Creates ranges so that the total population is distributed equally across the ranges.
        /// </summary>
        dmEqualAreas
    }
}