using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Interfaces
{
    /// <summary>
    ///     The successor of the ICell interface, expanded by a set of properties and methods
    ///     to work with cells describing dimension levels
    /// </summary>
    public interface ILevelCell : ICell
    {
        /// <summary>Returns the hierarchical level of the specified cell.</summary>
        /// <remarks>
        ///     The instance class is created not only for hierarchies but for the Grid measures
        ///     whose list is represented by the Measures class. In this case, measures, not hierarchy
        ///     members, are the members of the level
        /// </remarks>
        Level Level { get; }

        /// <summary>
        ///     Possible drill actions that can be fulfilled with the level.
        /// </summary>
        PossibleDrillActions PossibleDrillActions { get; }

        /// <summary>
        ///     The hierarchy members' attribute, which name is displayed in the cell.
        /// </summary>
        InfoAttribute Attribute { get; }

        /// <summary>The indent of the cell contents.</summary>
        /// <remarks>
        ///     This indent is used to make the tree-like firmat of row area.
        /// </remarks>
        byte Indent { get; }

        /// <summary>
        ///     Drills all nodes of the current level in the way defined by the Mode parameter.
        /// </summary>
        void ExpandAllNodes(PossibleDrillActions Mode);

        /// <summary>Collapses all nodes of the current level.</summary>
        void CollapseAllNodes();
    }
}