using System;
using System.ComponentModel;
using System.Linq;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class ClientLevel
    {
        [DefaultValue(-1)] public int ChildrenCount = -1;

        [DefaultValue(0)] public int CustomChildrenCount;

        [DefaultValue("")] public string Description = "";

        public string DisplayName;

        [DefaultValue(null)] public ClientLevelFilter Filter;

        public string UniqueName;

        [DefaultValue(false)] public bool Visible;

        public ClientLevel()
        {
        }


        internal ClientLevel(Level l)
        {
            DisplayName = l.DisplayName;
            UniqueName = l.UniqueName;
            Description = l.Description;
            if (l.Filter != null)
                Filter = new ClientLevelFilter(l.Filter);

            CustomChildrenCount = l.FUniqueNamesArray.Values.Where(e => e.Parent == null).Count();
            if (l.Hierarchy.Origin == HierarchyOrigin.hoParentChild)
                ChildrenCount = Math.Max(l.CubeLevel.FFirstLevelMembersCount, l.CubeLevel.Members.Count);
            else
                ChildrenCount = l.CubeLevel.FMembersCount;
            Visible = l.Visible;
        }
    }
}