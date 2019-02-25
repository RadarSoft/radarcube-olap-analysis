using System;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     The class passed as a parameter in the Toolbar action event handler.
    /// </summary>
    public class ToolboxItemActionArgs : EventArgs
    {
        internal CommonToolboxButton fItem;

        /// <summary>
        ///     Initializes a new instance of the ToolboxItemActionArgs class with specific CommonToolboxButton item.
        /// </summary>
        public ToolboxItemActionArgs(CommonToolboxButton item)
        {
            fItem = item;
        }

        /// <summary>
        ///     Toolbox item the action was fulfilled on.
        /// </summary>
        public CommonToolboxButton Item => fItem;

        /// <summary>
        ///     A string passed as a parameter in a client-side callback function.
        /// </summary>
        /// <remarks>
        ///     See
        ///     <see cref="RadarSoft.RadarCube.NetCore.TCommonToolboxButton.ClientScript">CommonToolboxButton.ClientScript</see>
        ///     for details.
        /// </remarks>
        public string ResultValue { get; set; } = null;

        public CallbackData CallbackData { get; set; } = CallbackData.Nothing;

        public PostbackData PostbackData { get; set; } = PostbackData.Nothing;

        /// <summary>
        ///     Set this property to true to cancel the shandard handling of the button.
        /// </summary>
        public bool Handled { get; set; } = false;
    }
}