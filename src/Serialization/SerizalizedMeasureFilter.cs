using System.ComponentModel;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    public class SerizalizedMeasureFilter
    {
        public OlapFilterCondition Condition;

        [DefaultValue("")] public string FirstValue = "";

        [DefaultValue(MeasureFilterRestriction.mfrAggregatedValues)]
        public MeasureFilterRestriction RestrictsTo = MeasureFilterRestriction.mfrAggregatedValues;

        [DefaultValue(null)] public string SecondFalue;

        public SerizalizedMeasureFilter()
        {
        }

        public SerizalizedMeasureFilter(MeasureFilter f)
        {
            Condition = f.FilterCondition;
            FirstValue = f.FirstValue;
            SecondFalue = f.SecondValue;
            RestrictsTo = f.RestrictsTo;
        }
    }
}