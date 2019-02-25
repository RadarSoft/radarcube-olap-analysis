namespace RadarSoft.RadarCube.Enums
{
    /// <summary>Enumerates the type of the current hierarchy.</summary>
    public enum HierarchyOrigin
    {
        /// <summary>
        ///     Object is not initialized.
        /// </summary>
        hoUnknown,

        /// <summary>
        ///     Represents a single-level hierarchy.
        /// </summary>
        hoAttribute,

        /// <summary>
        ///     Represents a hierarchy of the Parent-Child type.
        /// </summary>
        hoParentChild,

        /// <summary>
        ///     Represents a multilevel hierarchy.
        /// </summary>
        hoUserDefined,

        /// <summary>
        ///     Represents a MS AS Named Set.
        /// </summary>
        hoNamedSet
    }
}