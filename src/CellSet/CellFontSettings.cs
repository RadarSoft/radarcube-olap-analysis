using System;
using System.Drawing;

namespace RadarSoft.RadarCube.CellSet
{
    internal class CellFontSettings
    {
        internal string FontName;

        internal double? FontSize;

        internal FontStyle? Style;

        internal static CellFontSettings GetFontSettings(object fontFlags, object fontName, object fontSize)
        {
            if (fontFlags == null && fontName == null && fontSize == null) return null;
            var cs = new CellFontSettings();
            if (fontFlags != null)
            {
                cs.Style = FontStyle.Regular;

                var i = Convert.ToInt32(fontFlags);

                if ((i & 1) > 0) cs.Style |= FontStyle.Bold;
                if ((i & 2) > 0) cs.Style |= FontStyle.Italic;
                if ((i & 4) > 0) cs.Style |= FontStyle.Underline;
                if ((i & 8) > 0) cs.Style |= FontStyle.Strikeout;
            }
            if (fontName != null)
                cs.FontName = fontName.ToString();

            if (fontSize != null)
                cs.FontSize = Convert.ToDouble(fontSize);
            return cs;
        }
    }
}