using System;
using System.ComponentModel;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    internal class LocalizedDisplayNameAttribute : DisplayNameAttribute
    {
        public LocalizedDisplayNameAttribute(string key)
            : base(key)
        {
            DisplayNameValue = RadarUtils.GetResStr(key);
        }
    }
}