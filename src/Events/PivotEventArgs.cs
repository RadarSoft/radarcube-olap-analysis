using System;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Events
{
    /// <summary>
    ///     The pivot event parameters description
    /// </summary>
    public class PivotEventArgs : EventArgs
    {
        private LayoutArea? _fFrom;

        private PivotEventArgs(LayoutArea? from, LayoutArea to)
        {
            AllowPivoting = true;
            _fFrom = from;
            To = to;
        }

        internal PivotEventArgs(Hierarchy h, LayoutArea? from, LayoutArea to)
            : this(from, to)
        {
            Hierarchy = h;
        }

        internal PivotEventArgs(Measure m, LayoutArea? from, LayoutArea to)
            : this(from, to)
        {
            Measure = m;
        }

        /// <summary>
        ///     <para>Indicates if the pivoting operation is avaliable to an end user.</para>
        /// </summary>
        public bool AllowPivoting { get; set; }

        /// <summary>
        ///     Specifies the current area to move the hierarchy from. Null if the hierarchy is
        ///     moved from the inactive area.
        /// </summary>

        public LayoutArea? From
        {
            get
            {
                if (_fFrom == null)
                    return LayoutArea.laNone;
                return _fFrom.Value;
            }
        }

        /// <summary>
        ///     Specifies the target area to move the hierarchy to. Null if the hierarchy is
        ///     moved out of the active Cube area.
        /// </summary>
        public LayoutArea To { get; set; }

        /// <summary>The pivoted hierarchy</summary>
        public Hierarchy Hierarchy { get; }

        /// <summary>
        ///     The pivoted measure
        /// </summary>
        public Measure Measure { get; }
    }
}