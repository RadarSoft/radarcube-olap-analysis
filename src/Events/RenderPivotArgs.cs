using System;

namespace RadarSoft.RadarCube.Events
{
    public class RenderPivotArgs : EventArgs
    {
        internal RenderPivotArgs(object itemToRender, string text)
        {
            ItemToRender = itemToRender;
            Text = text;
        }

        public object ItemToRender { get; }

        public string Text { get; set; }
    }
}