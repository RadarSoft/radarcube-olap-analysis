using System;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class AxesLayoutAttribute : Attribute
    {
        public AxesLayoutAttribute(DockPanelArea ALayoutArea)
        {
            LayoutArea = ALayoutArea;
        }

        public DockPanelArea LayoutArea { get; }
    }
}