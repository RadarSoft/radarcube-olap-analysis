using System.Collections.Generic;
using System.ComponentModel;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class ClientHierarchy
    {
        [DefaultValue(true)] public bool AllowFilter = true;

        [DefaultValue("")] public string Description = "";

        public string DisplayName;

        [DefaultValue("")] public string Group = "";

        [DefaultValue(false)] public bool IsDate;

        [DefaultValue(false)] public bool IsFiltered;

        [DefaultValue(null)] public ClientLevel[] Levels;

        [DefaultValue(HierarchyOrigin.hoAttribute)] public HierarchyOrigin Origin = HierarchyOrigin.hoAttribute;

        [DefaultValue(false)] public bool UnfetchedMembersVisible;

        public string UniqueName;

        public ClientHierarchy()
        {
        }

        internal ClientHierarchy(Hierarchy h, OlapControl grid)
        {
            DisplayName = h.DisplayName;
            UniqueName = h.UniqueName;
            Description = h.Description;
            Group = h.Origin == HierarchyOrigin.hoNamedSet
                ? RadarUtils.GetResStr("rsNamedSets")
                : (h.CubeHierarchy.DisplayFolder ?? "");
            IsFiltered = h.Filtered || h.FilteredByLevelFilters;
            Origin = h.Origin;
            IsDate = h.Dimension.CubeDimension.fDimensionType == DimensionType.dtTime;
            AllowFilter = h.AllowFilter && h.AllowHierarchyEditor && grid.AllowFiltering;
            UnfetchedMembersVisible = h.UnfetchedMembersVisible;
            if (h.Levels != null && h.Levels.Count > 0)
            {
                var l = new List<ClientLevel>(h.Levels.Count);
                foreach (var ll in h.Levels)
                    l.Add(new ClientLevel(ll));
                Levels = l.ToArray();
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}