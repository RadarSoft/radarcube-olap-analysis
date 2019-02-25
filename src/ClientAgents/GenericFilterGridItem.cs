using System.ComponentModel;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class GenericFilterGridItem
    {
        [DefaultValue(OlapFilterCondition.fcGreater)]
        public OlapFilterCondition Condition = OlapFilterCondition.fcGreater;

        public string DisplayName;

        [DefaultValue(null)] public string FilterDescription;

        [DefaultValue("0")] public string FirstValue = "0";

        [DefaultValue(true)] public bool IsAggregatesRestricted = true;

        [DefaultValue(false)] public bool IsMeasure;

        [DefaultValue("0")] public string SecondValue = "0";

        public string UniqueName;

        public GenericFilterGridItem()
        {
        }

        internal GenericFilterGridItem(Measure m)
        {
            UniqueName = m.UniqueName;
            DisplayName = m.DisplayName;
            IsMeasure = true;
            if (m.Filter != null)
            {
                FilterDescription = m.Filter.Description;
                Condition = m.Filter.FilterCondition;
                FirstValue = m.Filter.FirstValue;
                SecondValue = m.Filter.FirstValue;
                IsAggregatesRestricted = m.Filter.RestrictsTo == MeasureFilterRestriction.mfrAggregatedValues;
            }
        }

        internal GenericFilterGridItem(Hierarchy h)
        {
            UniqueName = h.UniqueName;
            DisplayName = h.DisplayName;
            FilterDescription = h.FilterDescription;
        }
    }
}