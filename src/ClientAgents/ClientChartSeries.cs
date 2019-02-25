using System.ComponentModel;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class ClientChartSeries
    {
        [DefaultValue("")] public string ColorMember = "";

        public ClientChartCellDetails[] Details;

        [DefaultValue("")] public string Measure = "";

        [DefaultValue("")] public string ShapeMember = "";

        [DefaultValue("")] public string SizeMember = "";
    }
}