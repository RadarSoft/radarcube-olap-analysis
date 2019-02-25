using System;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the "Zoom in" toolbox button
    /// </summary>
    public class ScaleIncreaseButton : CommonToolboxButton
    {
        public ScaleIncreaseButton()
        {
            Class = "ui-icon-font ui-icon-font-zoomin";
        }

        private static Guid fID = new Guid("C5D2653F-0B15-4cab-8C75-E78834B7820F");

        public override string ButtonID
        {
            get => fID.ToString();
            set
            {
                ;
            }
        }

        public override string Tooltip
        {
            get
            {
                if (string.IsNullOrEmpty(base.Tooltip))
                    return "Zoom in";
                return base.Tooltip;
            }
            set => base.Tooltip = value;
        }

        protected override string RealTooltip()
        {
            if (string.IsNullOrEmpty(base.Tooltip))
                return RadarUtils.GetResStr("rsZoomIn");
            return base.Tooltip;
        }

        protected override string GetDefaultClientScript()
        {
            return "RadarSoft.$('#" + GetGridID() + "').data('grid').increaseScale(); return false;";
        }
    }
}