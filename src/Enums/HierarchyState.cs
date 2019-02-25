using System;

namespace RadarSoft.RadarCube.Enums
{
    /// <summary>Enumerates the state and location of the specified hierarchy.</summary>
    [Flags]
    public enum HierarchyState
    {
        hsNone = 0,

        /// <summary>
        ///     The hierarchy is initialized and located in neither of both active areas of the grid.
        /// </summary>
        hsInitialized = 1,

        /// <summary>
        ///     The hierarchy is initialized and locates in its collapsed state, or in the row area or in the column area.
        /// </summary>
        hsActive = 2,

        /// <summary>
        ///     The hierarchy is initialized and locates in its expanded state, or in the row area or
        ///     in the column area (hierarchy members participate in creation of the current OLAP slice).
        /// </summary>
        hsActiveExpanded = 4
    }
}