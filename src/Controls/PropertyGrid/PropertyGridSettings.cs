using RadarSoft.RadarCube.Controls.Analysis;

namespace RadarSoft.RadarCube.Controls.PropertyGrid
{
    public class PropertyGridSettings
    {
        public PropertyGridMetadata metadata;
        public bool showButtons = true;
        public PropertyGridValues values;

        public virtual void Initialize(OlapAnalysis olapAnalysis)
        {
        }
    }
}