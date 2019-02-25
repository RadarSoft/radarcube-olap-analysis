using System.ComponentModel;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class ClientMeasureMode
    {
        public string DisplayName;

        [DefaultValue(true)] public bool IsDefault = true;

        public string UniqueName;

        public ClientMeasureMode()
        {
        }

        internal ClientMeasureMode(MeasureShowMode m, bool isDefault)
        {
            DisplayName = m.Caption;
            UniqueName = m.fUniqueName.ToString();
            IsDefault = isDefault;
        }
    }
}