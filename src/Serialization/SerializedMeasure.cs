using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    public class SerializedMeasure
    {
        [DefaultValue(null)] public string[] ActiveModes;

        [DefaultValue(null)] [XmlArrayItem("ShowMode")] public MeasureShowModeType[] ActiveShowModes;

        [DefaultValue(null)] public string DefaultFormat;

        [DefaultValue("")] public string DisplayName = "";

        [DefaultValue("")] public string Expression = "";

        public SerizalizedMeasureFilter Filter;

        [DefaultValue(null)] public string[] IntelligenceParents;

        [DefaultValue(null)] public string[] Intelligences;

        [DefaultValue(null)] public string UniqueName;

        [XmlAttribute] [DefaultValue(true)] public bool Visible = true;

        [DefaultValue(true)] public bool VisibleInTree = true;

        public SerializedMeasure()
        {
        }

        public SerializedMeasure(Measure m)
        {
            UniqueName = m.UniqueName;
            VisibleInTree = m.VisibleInTree;
            var lmsm = new List<MeasureShowModeType>();
            var li = new List<string>();
            var lip = new List<string>();
            if (m.Expression.IsFill())
            {
                Expression = m.Expression;
                DisplayName = m.DisplayName;
                Visible = m.Visible;
            }

            foreach (var mode in m.ShowModes)
                if (mode.Visible)
                    if (mode.LinkedIntelligence != null)
                    {
                        li.Add(mode.LinkedIntelligence.Expression);
                        lip.Add(mode.LinkedIntelligence.fParent.UniqueName);
                    }
                    else
                    {
                        lmsm.Add(mode.Mode);
                    }

            ActiveShowModes = lmsm.ToArray();

            Intelligences = li.ToArray();
            IntelligenceParents = lip.ToArray();
            DefaultFormat = m.FDefaultFormat_;
            if (m.Filter != null)
                Filter = new SerizalizedMeasureFilter(m.Filter);
        }
    }
}