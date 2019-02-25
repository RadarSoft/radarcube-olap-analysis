using System;

namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Enumerates possible drilling actions for hierarchy levels and members in the
    ///     Grid.
    /// </summary>
    [Flags]
    public enum PossibleDrillActions
    {
        /// <summary>
        ///     No drilling actions are possible.
        /// </summary>
        esNone = 0,

        /// <summary>
        ///     Collapse all expanded members.
        /// </summary>
        esCollapsed = 1,

        /// <summary>
        ///     Drilling to child members on the same level.
        /// </summary>
        esParentChild = 2,

        /// <summary>
        ///     Drilling to members on the next level.
        /// </summary>
        esNextLevel = 4,

        /// <summary>
        ///     Drilling to the next hierarchy.
        /// </summary>
        esNextHierarchy = 8
    }
}