using System.ComponentModel;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    public class SerializedCalculatedMember
    {
        [DefaultValue("")] public string Description = "";

        public string DisplayName;

        [DefaultValue("")] public string Expression = "";

        [DefaultValue("")] public string Parent = "";

        public CustomMemberPosition Position;
        public string UniqueName;

        public SerializedCalculatedMember()
        {
        }

        public SerializedCalculatedMember(CalculatedMember m)
        {
            Position = m.Position;
            DisplayName = m.DisplayName;
            UniqueName = m.UniqueName;
            if (!string.IsNullOrEmpty(m.Description)) Description = m.Description;
            if (m.Parent != null) Parent = m.Parent.UniqueName;
            if (!string.IsNullOrEmpty(m.Expression))
                Expression = m.Expression;
        }
    }
}