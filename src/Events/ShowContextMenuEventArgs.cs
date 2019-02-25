using RadarSoft.RadarCube.Controls.Menu;
using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.Events
{
    /// <summary>An object passed as a parameter to the OlapGrid.OnShowContextMenu event.</summary>
    /// <remarks>
    ///     In this method, you can redefine items of the context menu, called when right
    ///     clicking on the OLAP Grid cells. For an example of how to use the class see the
    ///     OlapGrid.OnShowContextMenu event description.
    /// </remarks>
    public class ShowContextMenuEventArgs : CellEventArgs
    {
        public ShowContextMenuEventArgs(ICell cell, IDescriptionable cubeItem, ContextMenu menu)
            : base(cell)
        {
            ContextMenu = menu;
            CubeItem = cubeItem;
        }


        public IDescriptionable CubeItem { get; }

        /// <summary>
        ///     The context menu instance.
        /// </summary>
        public ContextMenu ContextMenu { get; }
    }
}