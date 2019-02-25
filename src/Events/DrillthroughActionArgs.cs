using System;

namespace RadarSoft.RadarCube.Events
{
    public class DrillthroughActionArgs : EventArgs
    {
        internal DrillthroughActionArgs()
        {
        }

        public bool Handled { get; set; } = false;
    }
}