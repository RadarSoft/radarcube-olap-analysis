using System;

namespace RadarSoft.RadarCube.Serialization
{
    internal class EventSerializeProcessingArgs : EventArgs
    {
        public EventSerializeProcessingArgs(object AActiveObject, SerializeAction ASerializeAction)
        {
            ActiveObject = AActiveObject;
            SerializeAction = ASerializeAction;
        }

        internal SerializeAction SerializeAction { get; }

        public object ActiveObject { get; }
    }
}