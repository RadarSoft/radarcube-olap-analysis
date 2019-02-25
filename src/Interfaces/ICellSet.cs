namespace RadarSoft.RadarCube.Interfaces
{
    internal interface ICellSet
    {
        int ColumnCount { get; }
        int FixedColumns { get; }
        int FixedRows { get; }
        int RowCount { get; }
        ICell Cells(int Column, int Row);
    }
}