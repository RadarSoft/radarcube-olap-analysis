using RadarSoft.RadarCube.Controls.Analysis;

namespace RadarSoft.RadarCube.Controls.PropertyGrid
{
    public class OlapAnalysisSettings : PropertyGridSettings
    {
        public override void Initialize(OlapAnalysis olapAnalysis)
        {
            metadata = new OlapAnalysisMetadata();
            metadata.Initialize();

            values = new OlapAnalysisValues();
            values.RootObject = olapAnalysis;
            values.Read();
        }
    }
}