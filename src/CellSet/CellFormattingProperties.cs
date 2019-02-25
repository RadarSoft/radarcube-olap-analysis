using System.Drawing;

namespace RadarSoft.RadarCube.CellSet
{
    /// <summary>Represents the formatting properties of a Cube cell</summary>
    public class CellFormattingProperties
    {
        public CellFormattingProperties()
        {
            //         fBackColor = Color.White;
        }

        internal CellFormattingProperties(string formattedValue)
        {
            FormattedValue = formattedValue;
        }


        internal CellFormattingProperties(string formattedValue, Color backColor, Color foreColor,
            FontStyle? fontStyle, string fontName, double? fontSize)
        {
            FormattedValue = formattedValue;
            BackColor = backColor;
            ForeColor = foreColor;
            FontStyle = fontStyle;
            FontFamily = fontName;
            FontSize = fontSize;
        }

        /// <summary>
        ///     A formatted value of a cell
        /// </summary>
        public string FormattedValue { get; set; }

        /// <summary>
        ///     A background color of a cell
        /// </summary>
        public Color BackColor { get; set; } = Color.Empty;

        /// <summary>
        ///     A foreground color of a cell
        /// </summary>
        public Color ForeColor { get; set; } = Color.Empty;

        public FontStyle? FontStyle { get; set; }

        public string FontFamily { get; set; }

        /// <summary>A font size of the cell.</summary>
        public double? FontSize { get; set; }
    }
}