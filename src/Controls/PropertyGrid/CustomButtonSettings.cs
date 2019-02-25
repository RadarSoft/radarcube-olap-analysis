using RadarSoft.RadarCube.Controls.Analysis;
using RadarSoft.RadarCube.Controls.Toolbox;

namespace RadarSoft.RadarCube.Controls.PropertyGrid
{
    public class CustomButtonSettings : PropertyGridSettings
    {
        internal CustomToolboxButton CustomButton { get; set; }

        public override void Initialize(OlapAnalysis olapAnalysis)
        {
            metadata = new CustomButtonMetadata();
            metadata.Initialize();

            values = new CustomButtonValues();
            ((CustomButtonValues) values).RootObject = CustomButton;
            values.Read();
        }
    }
}