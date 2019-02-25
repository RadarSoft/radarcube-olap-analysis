using System.Collections.Generic;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class GenericFilterGrid
    {
        public string ActionString;
        public string EditFilterString;
        public string FilterDescriptionString;
        public List<GenericFilterGridItem> Items = new List<GenericFilterGridItem>();
        public string ItemString;
        public string ResetFilterString;

        public GenericFilterGrid()
        {
        }

        internal GenericFilterGrid(OlapControl grid)
        {
            ActionString = RadarUtils.GetResStr("rsActions");
            ItemString = RadarUtils.GetResStr("rsItem");
            FilterDescriptionString = RadarUtils.GetResStr("rsFilterDescription");
            EditFilterString = RadarUtils.GetResStr("rsEditFilter");
            ResetFilterString = RadarUtils.GetResStr("repResetFilter");

            if (grid.Active)
            {
                foreach (var m in grid.Measures)
                    if (m.Filter != null)
                        Items.Add(new GenericFilterGridItem(m));

                foreach (var d in grid.Dimensions)
                foreach (var h in d.Hierarchies)
                {
                    var s = h.FilterDescription;
                    if (!string.IsNullOrEmpty(s))
                        Items.Add(new GenericFilterGridItem(h));
                }
            }
        }
    }
}