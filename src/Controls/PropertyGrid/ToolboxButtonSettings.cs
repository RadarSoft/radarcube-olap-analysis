using RadarSoft.RadarCube.Controls.Analysis;
using RadarSoft.RadarCube.Controls.Toolbox;

namespace RadarSoft.RadarCube.Controls.PropertyGrid
{
    public class ToolboxButtonSettings : PropertyGridSettings
    {
        internal CommonToolboxButton Button { get; set; }

        public override void Initialize(OlapAnalysis olapAnalysis)
        {
            metadata = new ToolboxButtonMetadata();
            metadata.Initialize();

            values = new ToolboxButtonValues();
            ((ToolboxButtonValues) values).RootObject = Button;
            values.Read();
        }
    }
}