using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.CellSet
{
    internal class CellValue : ICellValue
    {
        public CellValue(string Header, object Content)
        {
            this.Header = Header;
            Value = Content;
        }

        public object Header { get; set; }
        public object Value { get; set; }
    }
}