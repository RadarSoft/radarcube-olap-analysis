using System;
using System.Drawing;

namespace RadarSoft.RadarCube.CellSet
{
    internal class CellColorSettings
    {
        internal Color BackColor = Color.Empty;
        internal Color ForeColor = Color.Empty;

        internal static CellColorSettings GetColorSettings(object backColor, object foreColor)
        {
            if (backColor == null && foreColor == null) return null;
            var cs = new CellColorSettings();
            if (backColor != null)
            {
                var c = Convert.ToUInt32(backColor);

                cs.BackColor = Color.FromArgb(Convert.ToInt32(c & 0xFF),
                    Convert.ToInt32(c & 0xFF00) >> 8, Convert.ToInt32(c & 0xFF0000) >> 16);
            }
            if (foreColor != null)
            {
                var c = Convert.ToUInt32(foreColor);

                cs.ForeColor = Color.FromArgb(Convert.ToInt32(c & 0xFF),
                    Convert.ToInt32(c & 0xFF00) >> 8, Convert.ToInt32(c & 0xFF0000) >> 16);
            }
            return cs;
        }
    }
}