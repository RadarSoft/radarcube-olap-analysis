using System;
using System.Reflection;

namespace RadarSoft.RadarCube.Serialization
{
    internal class EventPropertyListGetterArgs : EventArgs
    {
        public EventPropertyListGetterArgs(Type AType)
        {
            Type = AType;
        }

        public Type Type { get; }

        public PropertyInfo[] PropertyInfo { get; set; }
    }
}