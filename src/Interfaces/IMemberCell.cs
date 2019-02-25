using System.Collections.Generic;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Interfaces
{
    /// <summary>
    ///     The interface successor of the ICell interface, expanded by a set of properties
    ///     and methods to work with cells describing dimension members
    /// </summary>
    public interface IMemberCell : ICell
    {
        /// <summary>Possible drill actions that can be performed with the specified cell.</summary>
        /// <remarks>
        ///     <para>
        ///         Any action returned by the method can be fulfilled by calling the DrillAction
        ///         method.
        ///     </para>
        /// </remarks>
        PossibleDrillActions PossibleDrillActions { get; }

        /// <summary>
        ///     Returns the object of the Member type encapsulated a member dimension, the data about which is derived in a given
        ///     cell.
        /// </summary>
        /// <summary>Returns a Member object related to the specified cell</summary>
        /// <remarks>
        ///     Note that an object of the Member type returned by this property can be related
        ///     not only to a hierarchy member, but to a measure or a measure show mode as well.
        /// </remarks>
        Member Member { get; }

        /// <summary>The indent of the cell contents.</summary>
        /// <remarks>
        ///     This indent is used to make the tree-like format of row area.
        /// </remarks>
        byte Indent { get; }

        /// <summary>Indicates if the specified cell is 'Total'.</summary>
        bool IsTotal { get; }

        /// <summary>Returns Parent of a given cell or null, if a cell is root.</summary>
        /// <remarks>
        ///     Root cells are the very left cells in the row field and the very upper cells in
        ///     the column field
        /// </remarks>
        IMemberCell Parent { get; }

        /// <summary>Indicates the number of the Children contained in the specified cell.</summary>
        int ChildrenCount { get; }

        /// <summary>Returns the number of cells adjoining the current one (including it).</summary>
        /// <remarks>
        ///     Cells can be named "neighbors" if they are located on the same level and have the
        ///     same Parent with the specified cell.
        /// </remarks>
        int SiblingsCount { get; }

        /// <summary>
        ///     Index of the current cell is in the Siblings list.
        /// </summary>
        int SiblingsOrder { get; }

        /// <summary>
        ///     Returns the right or the bottom neighbor cell regarding the current one depending
        ///     on the area or nil if that cell does not exist or a cell cannot be named as adjoining.
        /// </summary>
        /// <summary>
        ///     Returns the right or the bottom neighbor cell against the current one depending
        ///     on the area or null.
        /// </summary>
        /// <remarks>
        ///     Cells can be named "neighbors" if they are located on the same level and have the
        ///     same Parent.
        /// </remarks>
        IMemberCell NextMember { get; }

        /// <summary>
        ///     Returns the left or the top neighbor cell regarding the current one depending
        ///     on the area or nil, if that cell does not exist or cannot be named as adjoining.
        /// </summary>
        /// <summary>
        ///     Cells can be named as "neighbors" if they are located on the same level and have
        ///     the same parent.
        /// </summary>
        /// <remarks>
        ///     Cells can be named "neighbors" if they are located on the same level and have the
        ///     same Parent.
        /// </remarks>
        IMemberCell PrevMember { get; }

        /// <summary>
        ///     Indicates whether the specified cell is a leaf, i.e. has no Children in the
        ///     current CellSet configuration.
        /// </summary>
        bool IsLeaf { get; }

        /// <summary>
        ///     Indicates the position of the specified cell within a multidimensional
        ///     cube.
        /// </summary>
        ICubeAddress Address { get; }

        /// <summary>
        ///     Specifies the row and the column areas where the specified cell is
        ///     situated.
        /// </summary>
        LayoutArea Area { get; }

        /// <summary>Returns the cellset level of the specified cell.</summary>
        ILevelCell Level { get; }

        bool IsPager { get; }

        /// <summary>Indicates if the member is a group member.</summary>
        bool IsGroup { get; }

        bool IsInFrame { get; }
        int CurrentPage { get; }

        /// <summary>
        ///     Returns itself if this cell represens an hierachy member, otherwise (if the this
        ///     cell represents a measure or measure display mode) returns the closest parent cell
        ///     representing an hierarchy member
        /// </summary>
        IMemberCell HierarchyMemberCell { get; }

        /// <summary>
        ///     The hierarchy member attribute, which value is displayed in the cell.
        /// </summary>
        InfoAttribute Attribute { get; }

        /// <summary>User-defined comment to this cell.</summary>
        string Comment { get; set; }

        IEnumerable<ICellValue> Values { get; }

        IEnumerable<ICellValue> Descriptions { get; }

        /// <summary>
        ///     Drills the specified cell in the way defined by the Mode parameter.
        /// </summary>
        /// <param name="Mode"></param>
        void DrillAction(PossibleDrillActions Mode);

        /// <summary>
        ///     Rolls up all children of the specified cell.
        /// </summary>
        void DrillUp();

        /// <summary>Returns a Child of the specified cell according to the Index parameter.</summary>
        /// <remarks>
        ///     <para>
        ///         Children for the specified cell are built according to the sorting methods
        ///         applied to the CellSet.
        ///     </para>
        ///     <para>
        ///         You can learn the number of Children of the specified cell by checking the
        ///         ChildrenCount property.
        ///     </para>
        /// </remarks>
        IMemberCell Children(int Index);

        /// <summary>Returns a neighbor cell of the current one depending on its index.</summary>
        /// <remarks>
        ///     <para>
        ///         Neighbor cells are located on the same level, and have the same parent. The
        ///         number of neighbour cells (including the current one) is specified in the
        ///         SiblingsCount property. The rank of the current cell in the list of neighbours is
        ///         specified in the SiblingsOrder property. Preceding and subsequent cells of the
        ///         specified one are retrieved through the PrevMember and NextMember
        ///         properties.
        ///     </para>
        /// </remarks>
        IMemberCell Siblings(int Index);

        void PageTo(int page);
    }
}