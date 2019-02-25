using System;
using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Layout
{
    public class Intelligence : IStreamedObject
    {
        internal string fDisplayName = "";
        internal string fExpression = "";
        internal string fIntelligenceGroup = "";
        internal IntelligenceType fIntelligenceType = IntelligenceType.itCustom;
        internal List<Measure> fMeasures = new List<Measure>();
        internal Hierarchy fParent;

        public Intelligence(Hierarchy parent, string displayName, string expression)
        {
            fParent = parent;
            fDisplayName = displayName;
            fExpression = expression;
        }

        internal Intelligence(Hierarchy parent)
        {
            fParent = parent;
        }

        internal Intelligence(Level parent, IntelligenceType type)
        {
            fParent = parent.Hierarchy;
            fIntelligenceType = type;
            if (type == IntelligenceType.itMemberToDate)
            {
                fDisplayName = string.Format(RadarUtils.GetResStr("rsiMemberToDate"), parent.DisplayName);

                fExpression = "COALESCEEMPTY({2}(PERIODSTODATE(" + parent.UniqueName + ",{0}), {1}), 0)";

                fIntelligenceGroup = string.Format(RadarUtils.GetResStr("rsiTimeIntelligence"), fParent.DisplayName);

                return;
            }
            if (type == IntelligenceType.itMemberGrowth)
            {
                fDisplayName = string.Format(RadarUtils.GetResStr("rsiMemberGrowth"), parent.DisplayName);
                //                fExpression = "COALESCEEMPTY({2}({{0}}, {1}), 0) - COALESCEEMPTY({2}({PARALLELPERIOD(" + parent.UniqueName + ",1,{0})}, {1}), 0)";

                fExpression = "COALESCEEMPTY(({0}, {1}) - (PARALLELPERIOD(" + parent.UniqueName + ",1,{0}), {1}), 0)";

                fIntelligenceGroup = string.Format(RadarUtils.GetResStr("rsiTimeIntelligence"), fParent.DisplayName);
            }
        }

        public Hierarchy Parent
        {
            get => fParent;
            set => fParent = value;
        }

        public List<Measure> Measures => fMeasures;

        public string DisplayName
        {
            get => fDisplayName;
            set => fDisplayName = value;
        }

        public string Expression
        {
            get => fExpression;
            set => fExpression = value;
        }

        public string IntelligenceGroup
        {
            get => fIntelligenceGroup;
            set => fIntelligenceGroup = value;
        }

        public IntelligenceType IntelligenceType => fIntelligenceType;

        public MeasureShowMode FindShowMode(Measure measure)
        {
            foreach (var m in measure.ShowModes)
                if (m.LinkedIntelligence == this) return m;
            return null;
        }

        public MeasureShowMode AddMeasure(Measure measure)
        {
            if (measure == null) return null;
            var m = FindShowMode(measure);
            if (m != null) return m;

            m = new MeasureShowMode(measure, fDisplayName, MeasureShowModeType.smSpecifiedByEvent, "");

            m.fIntelligence = this;
            measure.ShowModes.Add(m);
            m.fVisible = true;
            fMeasures.Add(measure);

            var M1 = new Member(measure.Grid.Measures.Level, null, null);
            M1.DisplayName = fDisplayName;
            M1.SetUniqueName(Guid.NewGuid().ToString());
            M1.FDescription = "";
            M1.FMemberType = MemberType.mtMeasureMode;
            measure.Grid.Measures.Level.FUniqueNamesArray.Add(M1.UniqueName, M1);

            var M = measure.Grid.Measures.Level.FUniqueNamesArray[measure.UniqueName];
            M1.FVisible = true;
            M1.fVirtualID = M.Children.Count;
            M.Children.Add(M1);
            M1.FParent = M;
            M1.FDepth = 1;

            return m;
        }

        public void RemoveMeasure(Measure measure)
        {
            var m = FindShowMode(measure);
            if (m == null) return;

            fMeasures.Remove(measure);

            var i = measure.ShowModes.IndexOf(m);
            var M = measure.Grid.Measures.Level.FUniqueNamesArray[measure.UniqueName];
            M.Children.RemoveAt(i);

            measure.ShowModes.Remove(m);
            measure.Grid.Engine.ClearMeasureData(m);
        }

        public void ClearMeasures()
        {
            foreach (var measure in fMeasures)
            {
                var m = FindShowMode(measure);
                measure.ShowModes.Remove(m);
                measure.Grid.Engine.ClearMeasureData(m);
            }
            fMeasures.Clear();
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            /*
        internal List<Measure> fMeasures = new List<Measure>();
        internal string fDisplayName = "";
        internal string fExpression = "";
        internal string fIntelligenceGroup = "";
        internal IntelligenceType fIntelligenceType = IntelligenceType.itCustom;
			 
             */

            StreamUtils.WriteTag(writer, Tags.tgIntelligence);

            if (!string.IsNullOrEmpty(fDisplayName))
            {
                StreamUtils.WriteTag(writer, Tags.tgIntelligence_DisplayName);
                StreamUtils.WriteString(writer, fDisplayName);
            }

            if (fMeasures.Count > 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgIntelligence_MeasuresCount);
                StreamUtils.WriteInt32(writer, fMeasures.Count);

                foreach (var ms in fMeasures)
                    StreamUtils.WriteString(writer, ms.UniqueName);
            }

            if (!string.IsNullOrEmpty(fExpression))
            {
                StreamUtils.WriteTag(writer, Tags.tgIntelligence_Expression);
                StreamUtils.WriteString(writer, fExpression);
            }

            if (fIntelligenceType != IntelligenceType.itCustom)
            {
                StreamUtils.WriteTag(writer, Tags.tgIntelligence_Type);
                StreamUtils.WriteInt32(writer, (int) fIntelligenceType);
            }

            if (!string.IsNullOrEmpty(fIntelligenceGroup))
            {
                StreamUtils.WriteTag(writer, Tags.tgIntelligence_Group);
                StreamUtils.WriteString(writer, fIntelligenceGroup);
            }

            StreamUtils.WriteTag(writer, Tags.tgIntelligence_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            fMeasures.Clear();
            StreamUtils.CheckTag(reader, Tags.tgIntelligence);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgIntelligence_DisplayName:
                        fDisplayName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgIntelligence_Expression:
                        fExpression = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgIntelligence_Group:
                        fIntelligenceGroup = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgIntelligence_MeasuresCount:
                        var c = StreamUtils.ReadInt32(reader);
                        for (var i = 0; i < c; i++)
                        {
                            var s = StreamUtils.ReadString(reader);
                            var m = fParent.Dimension.Grid.Measures.Find(s);
                            if (m != null)
                            {
                                fMeasures.Add(m);
                                var sm = m.ShowModes.Find(DisplayName);
                                if (sm != null) sm.fIntelligence = this;
                            }
                        }
                        break;
                    case Tags.tgIntelligence_Type:
                        fIntelligenceType = (IntelligenceType) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgIntelligence_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        #endregion
    }
}