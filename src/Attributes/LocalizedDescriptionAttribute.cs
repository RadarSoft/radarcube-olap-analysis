using System;
using System.ComponentModel;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    internal sealed class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private bool m_initialized = false;

        public LocalizedDescriptionAttribute(string key)
            : base(key)
        {
            DescriptionValue = RadarUtils.GetResStr(key);
        }

        public LocalizedDescriptionAttribute(string key, ToolType aToolType)
            : base(key)
        {
        }
    }
}