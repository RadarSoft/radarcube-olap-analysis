using System;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the "Zoom out" toolbox button
    /// </summary>
    public class ScaleDecreaseButton : CommonToolboxButton
    {
        public ScaleDecreaseButton()
        {
            Class = "ui-icon-font ui-icon-font-zoomout";
        }

        private static Guid fID = new Guid("1ACC371F-0B15-4d1b-8C75-E78834B7820F");

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
                    return "Zoom out";
                return base.Tooltip;
            }
            set => base.Tooltip = value;
        }

        protected override string RealTooltip()
        {
            if (string.IsNullOrEmpty(base.Tooltip))
                return RadarUtils.GetResStr("rsZoomOut");
            return base.Tooltip;
        }

        protected override string GetDefaultClientScript()
        {
            return "RadarSoft.$('#" + GetGridID() + "').data('grid').decreaseScale(); return false;";
        }
    }
}