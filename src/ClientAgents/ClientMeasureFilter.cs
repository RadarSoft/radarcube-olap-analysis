using System.ComponentModel;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class ClientMeasureFilter
    {
        [DefaultValue(OlapFilterCondition.fcGreater)]
        public OlapFilterCondition Condition = OlapFilterCondition.fcGreater;

        [DefaultValue("0")] public string FirstValue = "0";

        [DefaultValue(true)] public bool IsAggregatesRestricted = true;

        [DefaultValue("0")] public string SecondValue = "0";

        public ClientMeasureFilter()
        {
        }

        internal ClientMeasureFilter(MeasureFilter m)
        {
            Condition = m.FilterCondition;
            FirstValue = m.FirstValue;
            SecondValue = m.SecondValue;
            IsAggregatesRestricted = m.RestrictsTo == MeasureFilterRestriction.mfrAggregatedValues;
        }
    }
}