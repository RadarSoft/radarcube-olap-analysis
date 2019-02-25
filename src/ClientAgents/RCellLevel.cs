using System.ComponentModel;
using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class RCellLevel
    {
        [DefaultValue(false)] public bool IsFiltered;

        [DefaultValue(false)] public bool IsMeasuresCell;

        public RCellLevel()
        {
        }

        public RCellLevel(ILevelCell lc)
        {
            if (lc.Level != null && lc.Level.Measures != null)
            {
                var m = lc.Level.Measures.Find(lc.Level.UniqueName);
                if (m != null)
                    IsFiltered = m.Filter != null;
                else
                    IsMeasuresCell = true;
            }
            if (lc.Level != null && lc.Level.Hierarchy != null)
                IsFiltered = lc.Level.Hierarchy.Filtered || lc.Level.Hierarchy.FilteredByLevelFilters;
        }
    }
}