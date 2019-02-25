using System.ComponentModel;

namespace RadarSoft.RadarCube.Enums
{
    public enum ExportType
    {
        [Description("Export to xls format")] XLS,
        [Description("Export to xlsx format")] XLSX,
        [Description("Export to txt format")] TXT,
        [Description("Export to csv format")] CSV,
        [Description("Export to html format")] HTML,
        [Description("Export to pdf format")] PDF,

        //[Description("Export to xml format")]
        //XML,
        //[Description("Export to xps format")]
        //XPS,
        [Description("Export to bmp format")] BMP,
        [Description("Export to tiff format")] TIFF,
        [Description("Export to jpeg format")] JPG,
        [Description("Export to png format")] PNG,
        [Description("Export to gif format")] GIF
    }
}