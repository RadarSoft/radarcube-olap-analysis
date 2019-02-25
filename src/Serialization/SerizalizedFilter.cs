using System.ComponentModel;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    public class SerizalizedFilter
    {
        public OlapFilterCondition Condition;
        public OlapFilterType FilterType;

        [DefaultValue("")] public string FirstValue = "";

        [DefaultValue(null)] public string MDXLevelName;

        [DefaultValue(null)] public string MeasureName;

        [DefaultValue(null)] public string SecondFalue;

        public SerizalizedFilter()
        {
        }

        public SerizalizedFilter(Filter f)
        {
            FilterType = f.FilterType;
            MeasureName = f.fMeasureName;
            Condition = f.FilterCondition;
            FirstValue = f.FirstValue;
            SecondFalue = f.SecondValue;
            MDXLevelName = f.fLevelName;
        }
    }
}