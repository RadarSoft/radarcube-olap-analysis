using System;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.Events
{
    public class WritebackArgs : EventArgs
    {
        public WritebackArgs(IDataCell cell, object NewValue)
        {
            Cell = cell;
            this.NewValue = NewValue;
        }

        public IDataCell Cell { get; }

        public object OldValue => Cell.Data;

        public object NewValue { get; set; }

        public bool AllowWriteback { get; set; } = true;

        public WritebackMethod WritebackMethod { get; set; } = WritebackMethod.wmEqualAllocation;
    }
}