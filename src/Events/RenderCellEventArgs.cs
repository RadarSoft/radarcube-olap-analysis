using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.Events
{
    /// <summary>
    ///     This object is passed to the TOLAPgrid.OnRenderCell event, that allows changing
    ///     the contents and the rendering style of a specified Grid cell.
    /// </summary>
    public class RenderCellEventArgs : CellEventArgs
    {
        public RenderCellEventArgs(ICell cell)
            : base(cell)
        {
            Text = cell.Value;
            Tooltip = cell.Description;

            if (cell is IMemberCell)
            {
                var mc = (IMemberCell) cell;
                if (mc.Member != null)
                {
                    var s = mc.Member.ExtractAttributesAsTooltip(true);
                    if (!string.IsNullOrEmpty(s)) Tooltip = s;
                }
            }
        }

        /// <summary>
        ///     The text rendered into the Grid cell. It is rendered "as is" and that allows
        ///     using the HTML and Jscript codes inside the cell.
        /// </summary>
        /// <remarks>
        ///     To use this property to its full extent you'll have to obtain a commercial
        ///     version of RadarCube
        /// </remarks>
        public string Text { get; set; }

        /// <summary>
        ///     The text that appears as a tooltip, when the mouse is over the cell.
        /// </summary>
        public string Tooltip { get; set; }
    }
}