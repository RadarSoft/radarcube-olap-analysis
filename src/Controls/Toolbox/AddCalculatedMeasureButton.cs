using System;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the "Add calculated measure" toolbox button
    /// </summary>
    public class AddCalculatedMeasureButton : CommonToolboxButton
    {
        public AddCalculatedMeasureButton()
        {
            Class = "ui-icon-font ui-icon-font-calculator-b";
        }

        private static Guid fID = new Guid("6F1A322A-9F78-4fc9-8D62-93A6475541E5");

        public override string ButtonID
        {
            get => fID.ToString();
            set
            {
                ;
            }
        }

        //protected override string RealImage()
        //{
        //    if (string.IsNullOrEmpty(Image))
        //        return fOwner.ImageUrl("Calculated_add.gif");
        //    return Image;
        //}

        protected override string GetDefaultClientScript()
        {
            return "RadarSoft.$('#" + GetGridID() +
                   "').data('grid').showDialog('createcalculatedmeasure'); return false;";
        }

        protected override string RealTooltip()
        {
            if (string.IsNullOrEmpty(Tooltip))
                return RadarUtils.GetResStr("rsCreateCalcMeasure");
            return Tooltip;
        }
    }
}