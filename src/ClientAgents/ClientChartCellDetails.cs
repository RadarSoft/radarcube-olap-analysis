using System.ComponentModel;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class ClientChartCellDetails
    {
        [DefaultValue("")] public string ColorMember = "";

        [DefaultValue(1E-300)] public double ColorValue = 1E-300;

        [DefaultValue("")] public string ColorValueFormatted = "";

        [DefaultValue(null)] public string[] DetailMembers = null;

        [DefaultValue("")] public string ShapeMember = "";

        [DefaultValue(1E-300)] public double ShapeValue = 1E-300;

        [DefaultValue("")] public string ShapeValueFormatted = "";

        [DefaultValue("")] public string SizeMember = "";

        [DefaultValue(1E-300)] public double SizeValue = 1E-300;

        [DefaultValue("")] public string SizeValueFormatted = "";

        [DefaultValue("")] public object XValue = "";

        [DefaultValue("")] public string XValueFormatted = "";

        [DefaultValue("")] public object YValue = "";

        [DefaultValue("")] public string YValueFormatted = "";
    }
}