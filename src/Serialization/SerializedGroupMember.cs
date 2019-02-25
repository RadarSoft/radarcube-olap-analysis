using System.Collections.Generic;
using System.ComponentModel;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    public class SerializedGroupMember
    {
        public string[] Children;

        [DefaultValue("")] public string Description = "";

        public string DisplayName;

        [DefaultValue("")] public string Parent = "";

        public CustomMemberPosition Position;
        public string UniqueName;

        public SerializedGroupMember()
        {
        }


        public SerializedGroupMember(GroupMember m)
        {
            Position = m.Position;
            DisplayName = m.DisplayName;
            UniqueName = m.UniqueName;
            if (!string.IsNullOrEmpty(m.Description)) Description = m.Description;
            if (m.Parent != null) Parent = m.Parent.UniqueName;
            var ch = new List<string>();
            foreach (var c in m.Children)
                ch.Add(c.UniqueName);
            if (ch.Count > 0) Children = ch.ToArray();
        }
    }
}