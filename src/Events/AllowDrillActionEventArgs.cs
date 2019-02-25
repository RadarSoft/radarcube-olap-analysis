using System;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.Events
{
    /// <summary>
    ///     An object passed as a parameter to the OlapGrid.AllowDrillAction
    ///     event.
    /// </summary>
    /// <seealso cref="TCustomOLAPControl.AllowDrillAction">AllowDrillAction Event (RadarSoft.RadarCube.Web.OlapControl)</seealso>
    public class AllowDrillActionEventArgs : EventArgs
    {
        internal AllowDrillActionEventArgs(IMemberCell cell, PossibleDrillActions actions)
        {
            Cell = cell;
            Actions = actions;
        }

        /// <summary>
        ///     The Grid cell, for which the set of possible Drill actions is defined.
        /// </summary>
        public IMemberCell Cell { get; }

        /// <summary>
        ///     The set of Drill actions for this Grid cell. This set can be modified only
        ///     in the way of decreasing the number of available drilling operations.
        /// </summary>
        public PossibleDrillActions Actions { get; set; }
    }
}