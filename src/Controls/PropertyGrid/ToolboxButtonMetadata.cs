using System;
using RadarSoft.RadarCube.Controls.Toolbox;

namespace RadarSoft.RadarCube.Controls.PropertyGrid
{
    public class ToolboxButtonMetadata : PropertyGridMetadata
    {
        public PropertyMetadata ButtonID;
        public PropertyMetadata ClientScript;
        public PropertyMetadata Image;
        public PropertyMetadata IsPressed;
        public PropertyMetadata PressedImage;
        public PropertyMetadata PressedText;
        public PropertyMetadata Text;
        public PropertyMetadata Tooltip;
        public PropertyMetadata Visible;

        protected override Type RootType => typeof(CustomToolboxButton);
    }
}