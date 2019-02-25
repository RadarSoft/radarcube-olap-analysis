using System;
using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.Events
{
    /// <summary>
    ///     The class for the RadarCube events related to the operations of Grid
    ///     cells.
    /// </summary>
    public class CellEventArgs : EventArgs
    {
        public CellEventArgs(ICell cell)
        {
            Cell = cell;
        }

        /// <summary>Provides all the necessary information of the given Grid's cell.</summary>
        /// <remarks>
        ///     The interface contains properties and methods that are common to any of the Grid
        ///     cell, and is returned by Cells of the CellSet class. In fact, the CellSet.Cells
        ///     property always returns not the ICell interface itself but one of its successors -
        ///     IMemberCell, IDataCell or ILevelCell, depending on which cell has been addressed. The
        ///     returned interface type can be examined through the CellType property.
        /// </remarks>
        public ICell Cell { get; }
    }
}