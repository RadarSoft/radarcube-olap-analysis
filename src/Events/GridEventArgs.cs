using System;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.Events
{
    public class GridEventArgs : EventArgs
    {
        internal GridEventArgs(OlapControl AGrid, GridEventType AEventType, object[] AData)
        {
            Grid = AGrid;
            EventType = AEventType;
            Data = AData;
        }

        public OlapControl Grid { get; }

        public GridEventType EventType { get; }

        public object[] Data { get; }
    }
}