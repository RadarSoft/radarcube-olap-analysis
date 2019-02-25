using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.Events
{
    /// <summary>
    ///     This object is passed to the OnContextMenuClick event and allows you to
    ///     find out which menu item was selected and on which cell.
    /// </summary>
    /// <remarks>
    ///     For an example of how to use this class see the appropriate OnShowContextMenu event
    ///     description.
    /// </remarks>
    public class ContextMenuClickArgs : CellEventArgs
    {
        internal ContextMenuClickArgs(string value, ICell cell)
            : base(cell)
        {
            MenuItemValue = value;
        }

        /// <summary>The value of the MenuItem.Value property of the selected menu item.</summary>
        public string MenuItemValue { get; }
    }
}