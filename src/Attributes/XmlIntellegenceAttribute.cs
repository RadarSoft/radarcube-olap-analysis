using System;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.Attributes
{
    internal class XmlIntellegenceAttribute : Attribute
    {
        internal virtual TargetSerialization Func(object sender)
        {
            return TargetSerialization.Auto;
        }

        internal string Save(object val)
        {
            return null;
        }

        internal object Read(string res)
        {
            return null;
        }
    }
}