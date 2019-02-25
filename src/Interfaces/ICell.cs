using System.Collections.Generic;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.Interfaces
{
    /// <summary>
    ///     Successors of this interface provide all the necessary properties and methods to
    ///     manage the Grid cells.
    /// </summary>
    /// <remarks>
    ///     The interface contains properties and methods that are common to any of the Grid
    ///     cell, and is returned by Cells of the CellSet class. In fact, the CellSet.Cells
    ///     property always returns not the ICell interface itself but one of its successors -
    ///     IMemberCell, IDataCell or ILevelCell, depending on which cell has been addressed. The
    ///     returned interface type can be examined through the CellType property.
    /// </remarks>
    public interface ICell
    {
        /// <summary>A vertical coordinate (in cells) of the cell.</summary>
        int StartRow { get; }

        /// <summary>A horizontal coordinate (in cells) of the cell.</summary>
        int StartColumn { get; }

        /// <summary>
        ///     A vertical coordinate (in cells) of the cell with paging taken into
        ///     account.
        /// </summary>
        int PagedStartRow { get; }

        /// <summary>
        ///     A horizontal coordinate (in cells) of the cell with paging taken into
        ///     account.
        /// </summary>
        int PagedStartColumn { get; }

        /// <summary>The number of rows spanned by the cell.</summary>
        int RowSpan { get; }

        /// <summary>The number of columns spanned by the cell.</summary>
        int ColSpan { get; }

        /// <summary>Returns the value of the current Grid cell.</summary>
        /// <remarks>
        ///     This property contains a member or hierarchy level caption, or a formatted value
        ///     of the data cell. For the IDataCell interface you can get an unformatted cell value by
        ///     examining the Data property.
        /// </remarks>
        string Value { get; }

        /// <summary>A description of the specified Grid cell.</summary>
        string Description { get; }

        /// <summary>Contains the real type of the specified interface.</summary>
        /// <remarks>
        ///     <para>RadarCube never returns the ICell interface directly.</para>
        ///     <para>
        ///         You can learn what successor interface has been returned by examining this
        ///         property.
        ///     </para>
        /// </remarks>
        CellType CellType { get; }

        /// <summary>References to the CellSet Parent object the specified cell belongs to.</summary>
        CellSet.CellSet CellSet { get; }

        /// <summary>The list of MSAS Actions assigned to the specified cell</summary>
        List<CubeAction> CubeActions { get; }
    }
}