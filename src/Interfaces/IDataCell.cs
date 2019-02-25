using System.Collections.Generic;
using System.Data;
using System.Drawing;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.Interfaces
{
    /// <summary>
    ///     The successor of the ICell interface, expanded by a set of properties and methods
    ///     to work with the cells specifically from the data area.
    /// </summary>
    /// <summary>
    ///     The successor of the ICell interface which is expanded by a set of properties and
    ///     methods to work with the cells specifically from the data area.
    /// </summary>
    public interface IDataCell : ICell
    {
        /// <summary>
        ///     The real (unformatted) value of the Cube cell returned by the function of
        ///     aggregation.
        /// </summary>
        /// <remarks>
        ///     To retrieve a formatted value that will be displayed in the Grid cell, examine
        ///     the Value property.
        /// </remarks>
        object Data { get; }

        /// <summary>The backround color of the cell</summary>
        Color BackColor { get; }

        /// <summary>The font color of the cell</summary>
        Color ForeColor { get; }

        /// <summary>The style of the font in the cell</summary>
        FontStyle? FontStyle { get; }

        /// <summary>The name of the font in the cell</summary>

        string FontFamily { get; }

        /// <summary>The size of the font in the cell</summary>
        double? FontSize { get; }

        /// <summary>
        ///     Identifies the position of the specified cell within a multidimensional
        ///     cube.
        /// </summary>
        ICubeAddress Address { get; }

        /// <summary>
        ///     Returns the IMemberCell interface to the nearest Grid cell in the area of rows
        ///     linked to the hierarchy members names.
        /// </summary>
        /// <summary>
        ///     Returns, as the IMemberCell interface, a reference to the closest Grid cell,
        ///     associated with the specified hierarchy member, from the row area.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For the selected data cell, a cell highlighted in the picture as violet is
        ///         returned:
        ///     </para>
        ///     <para>
        ///         <img src="images/RowColumn.jpg" />
        ///     </para>
        /// </remarks>
        IMemberCell RowMember { get; }

        /// <summary>
        ///     Returns, as the IMemberCell interface, a reference to the closest Grid cell,
        ///     associated with the specified hierarchy member, from the columns area.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For the selected data cell, a cell highlighted in the picture as yellow is
        ///         returned.
        ///     </para>
        ///     <para>
        ///         <img src="images/RowColumn.jpg" />
        ///     </para>
        /// </remarks>
        IMemberCell ColumnMember { get; }

        /// <summary>
        ///     Indicates if at least one member of the Grid that the specified cell belongs to
        ///     is Total.
        /// </summary>
        bool IsTotal { get; }

        /// <summary>Indicates whether the specified cell belongs to a 'Total' column.</summary>
        bool IsTotalVertical { get; }

        /// <summary>Indicates whether the specified cell belongs to a 'Total' row.</summary>
        bool IsTotalHorizontal { get; }

        /// <summary>User-defined comment to this cell.</summary>
        string Comment { get; set; }

        /// <summary>
        ///     In the DataSet parameter it passes the source data table aggregated in the
        ///     current Grid cell.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         By the RowsToFetch parameter you can limit a number of records returned by
        ///         the method. If a value of the RowsToFetch parameter equals 0, then all records
        ///         aggregated in the current Grid cell are returned by the method.
        ///     </para>
        ///     <para>
        ///         For the Grid, where the MOlapCube component serves as a data source, a set of
        ///         data obtained by the MDX "DRILLTHROUGH" command is a result of this method.
        ///     </para>
        /// </remarks>
        void Drillthrough(DataTable ADataSet, int RowsToFetch);

        void Drillthrough(DataTable ADataSet, int RowsToFetch, ICollection<string> columns);

        void Drillthrough(DataTable ADataSet, string mdx);

        void Drillthrough(DataTable dataTable, int RowsToFetch, DrillThroughMethod DrillThroughMethod);

        void Drillthrough(DataTable dataTable, int RowsToFetch, ICollection<string> columns,
            DrillThroughMethod DrillThroughMethod);

        /// <summary>
        ///     Updates the fact table contents for the records aggregated in the current Grid cell.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For the Grid, where the MOlapCube component serves as a data source, the MDX
        ///         "Writeback" command with the appropriate parameters is fulfilled.
        ///     </para>
        ///     <para>
        ///         For the Grid, where the TOLAPCube component serves as a data source, the
        ///         OnWriteback event is called.
        ///     </para>
        /// </remarks>
        void Writeback(object NewValue, WritebackMethod Method);
    }
}