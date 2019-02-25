using System;

namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Enumerates the method of displaying the attribute value in the Grid.
    /// </summary>
    [Flags]
    public enum AttributeDispalyMode
    {
        /// <summary>
        ///     Does not display the value itself.
        /// </summary>
        None = 0,

        /// <summary>
        ///     The value of the attribute is displayed as a Tooltip, when the mouse coursor is held over the cell with the name of
        ///     the member.
        /// </summary>
        AsTooltip = 1,

        /// <summary>
        ///     The value of the attribute is displayed as an additional column or row in the Grid.
        /// </summary>
        AsColumn = 2
    }
}