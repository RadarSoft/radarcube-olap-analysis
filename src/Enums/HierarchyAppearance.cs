namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Enumerates the display mode for the expanded Parent-Child hierarchy in the
    ///     Grid.
    /// </summary>
    /// <remarks>
    ///     <list type="table">
    ///         <item>
    ///             <term>
    ///                 <img src="images/hATypical.gif" />
    ///             </term>
    ///             <description>
    ///                 <img src="images/hNormal.gif" />
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <em>The atypical (tree-like) hierarchy appearance</em>
    ///             </term>
    ///             <description>
    ///                 <em>The normal hierarchy appearance</em>
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public enum HierarchyAppearance
    {
        /// <summary>
        ///     Each level of the expanded Parent-Child hierarchy is displayed as a separate column in the grid.
        /// </summary>
        haNormal,

        /// <summary>
        ///     All expanded Parent-Child levels of the hierarchy are displayed as a single column of the grid.
        /// </summary>
        haATypical
    }
}