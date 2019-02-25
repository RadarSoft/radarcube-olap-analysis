using System;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Events
{
    /// <summary>Contains a set of parameters for the OlapControl.OnInitHierarchy event</summary>
    public class EventInitHierarchyArgs : EventArgs
    {
        internal Hierarchy H;

        /// <summary>
        ///     References to the hierarchy, upon which initialization the
        ///     OlapControl.OnInitHierarchy event has been called.
        /// </summary>
        public Hierarchy Hierarchy => H;
    }
}