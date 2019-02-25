using System.ComponentModel;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class ClientLevelFilter
    {
        [DefaultValue(OlapFilterCondition.fcGreater)]
        public OlapFilterCondition Condition = OlapFilterCondition.fcGreater;

        [DefaultValue(OlapFilterType.ftOnValue)] public OlapFilterType FilterType = OlapFilterType.ftOnValue;

        [DefaultValue("0")] public string FirstValue = "0";

        [DefaultValue("")] public string MeasureName = "";

        [DefaultValue("0")] public string SecondValue = "0";

        public ClientLevelFilter()
        {
        }

        internal ClientLevelFilter(Filter f)
        {
            Condition = f.FilterCondition;
            FilterType = f.FilterType;
            MeasureName = f.fMeasureName;
            FirstValue = f.FirstValue;
            SecondValue = f.SecondValue;
        }
    }
}