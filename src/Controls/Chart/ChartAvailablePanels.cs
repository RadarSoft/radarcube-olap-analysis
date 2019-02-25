using System.ComponentModel;

namespace RadarSoft.RadarCube.Controls.Chart
{
    public class ChartAvailablePanels
    {
        public ChartAvailablePanels()
        {
            ColumnsArea = true;
            CubeStructureTree = true;
            FiltersArea = true;
            QuickFilter = true;
            RowsArea = true;
            ValuesArea = true;

            ColorArea = true;
            SizeArea = true;
            ShapeArea = true;
            DetailsArea = true;
            LegendsArea = true;
        }

        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        [Description("")]
        public bool QuickFilter { get; set; }

        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        [Description("")]
        public bool CubeStructureTree { get; set; }

        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        [Description("")]
        public bool RowsArea { get; set; }

        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        [Description("")]
        public bool ColumnsArea { get; set; }

        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        [Description("")]
        public bool ValuesArea { get; set; }

        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        [Description("")]
        public bool FiltersArea { get; set; }

        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        [Description("")]
        public bool ColorArea { get; set; }

        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        [Description("")]
        public bool SizeArea { get; set; }

        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        [Description("")]
        public bool ShapeArea { get; set; }

        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        [Description("")]
        public bool DetailsArea { get; set; }

        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        [Description("")]
        public bool LegendsArea { get; set; }
    }
}