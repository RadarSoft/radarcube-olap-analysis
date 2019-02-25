using System.ComponentModel;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class ClientMember
    {
        [DefaultValue("")] public string Description = "";

        public string DisplayName;

        [DefaultValue(false)] public bool Filtered;

        [DefaultValue(true)] public bool IsLeaf = true;

        public string UniqueName;

        [DefaultValue(true)] public bool Visible = true;

        public ClientMember()
        {
        }

        public ClientMember(Member m)
        {
            DisplayName = m.DisplayName;
            UniqueName = m.UniqueName;
            Visible = m.Visible;
            Filtered = m.Filtered;
            IsLeaf = m.IsLeaf;
            Description = m.Description;
        }
    }
}