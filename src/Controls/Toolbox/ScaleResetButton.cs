using System;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the "Reset zoom to 100%" toolbox button
    /// </summary>
    public class ScaleResetButton : CommonToolboxButton
    {
        public ScaleResetButton()
        {
            Class = "ui-icon-font ui-icon-font-zoom";
        }

        private static Guid fID = new Guid("9b840a20-6cb2-4d1b-ac9d-44bf38d87925");

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
                    return "Reset zoom to 100%";
                return base.Tooltip;
            }
            set => base.Tooltip = value;
        }

        protected override string RealTooltip()
        {
            if (string.IsNullOrEmpty(base.Tooltip))
                return RadarUtils.GetResStr("rsResetZoom");
            return base.Tooltip;
        }

        protected override string GetDefaultClientScript()
        {
            return "RadarSoft.$('#" + GetGridID() + "').data('grid').resetScale(); return false;";
        }
    }
}