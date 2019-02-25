namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Enumerates the type of the context filter applied to the hierarchy level.
    /// </summary>
    public enum OlapFilterType
    {
        /// <summary>
        ///     The filter applies to the values of the appropriate measure.
        /// </summary>
        ftOnValue,

        /// <summary>
        ///     The filter applies to member captions.
        /// </summary>
        ftOnCaption,

        /// <summary>
        ///     The filter applies to Date hierarchies.
        /// </summary>
        ftOnDate
    }
}