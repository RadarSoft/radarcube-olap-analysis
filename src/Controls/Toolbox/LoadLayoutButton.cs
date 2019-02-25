using System;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the "Load Grid state" toolbox button
    /// </summary>
    public class LoadLayoutButton : CommonToolboxButton
    {
        private static Guid fID = new Guid("C706B091-E49E-4e17-B28D-89DBBAB1CE20");

        public override string ButtonID
        {
            get => fID.ToString();
            set
            {
                ;
            }
        }

        /// <summary>
        ///     The \"File name\" prompt
        /// </summary>
        public string FileNamePrompt { get; set; }

        public override string Tooltip
        {
            get
            {
                if (string.IsNullOrEmpty(base.Tooltip))
                    return "Load a Grid layout";
                return base.Tooltip;
            }
            set => base.Tooltip = value;
        }

        protected override string RealImage()
        {
            if (string.IsNullOrEmpty(Image))
                return fOwner.ImageUrl("Load.gif");
            return fOwner.MapPath(Image);
        }

        protected override string GetDefaultClientScript()
        {
            return "RadarSoft.$('#" + GetGridID() + "').data('grid').showLoadlayoutDialog(); return false;";
        }
    }
}