using System;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the "Save layout" toolbox button
    /// </summary>
    public class SaveLayoutToolboxButton : ExportButtonCommon
    {
        private static Guid fID = new Guid("56B4A187-2112-4569-870D-913021761B4C");

        protected override string GetDefaultClientScript()
        {
            var script = "var grid = RadarSoft.$('#" + GetGridID() + "').data('grid');";
            script += " var args = '" + ButtonID + "|'";
            script += " + grid.parsChartTypes();";
            script += " grid.export(args);";
            return script;
        }

        public override string Tooltip
        {
            get
            {
                if (string.IsNullOrEmpty(base.Tooltip))
                    return "Export the current Grid layout";
                return base.Tooltip;
            }
            set => base.Tooltip = value;
        }

        public override string ButtonID
        {
            get => fID.ToString();
            set
            {
                ;
            }
        }

        public override string FileName
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        protected override string RealImage()
        {
            if (string.IsNullOrEmpty(Image))
                return fOwner.ImageUrl("Save.gif");
            return Image;
        }
    }
}