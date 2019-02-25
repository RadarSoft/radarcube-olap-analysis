using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Engine.Md;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.Engine
{
    public abstract class Line : IStreamedObject
    {
        private Measure _Measure;

        internal LineMap CurrentMap = new LineMap();

#if DEBUG
        private string _fCacheKey;
        internal string fCacheKey
        {
            get
            {
                return _fCacheKey;
            }
            set
            {
                _fCacheKey = value;
            }
        }
#else
        internal string fCacheKey;
#endif
        internal List<int> fDepthes;
        internal int fHierId;
        internal string fID;

        internal MetaLine fM;
        internal string fMeasureID;
        internal MeasureShowMode fMode;
        internal Tuple<double, double> FRange = null;

        [NonSerialized] internal Dictionary<string, Dictionary<long, MembersListStorage>> fSortedData = null;

        internal Line(MetaLine AMetaLine, string ID, string MeasureID, MeasureShowMode Mode, int FHierID)
        {
            fM = AMetaLine;
            fID = ID;
            fMeasureID = MeasureID;
            fMode = Mode;
            fHierId = FHierID;
            fDepthes = new List<int>(AMetaLine.fHierArray.Count);
            for (var i = 0; i < AMetaLine.fHierArray.Count; i++)
                fDepthes.Add(0);
            var a = FHierID;
            for (var i = AMetaLine.fLevels.Count - 1; i > 0; i--)
            {
                fDepthes[i] = a / AMetaLine.fHierArray[i];
                a = a % AMetaLine.fHierArray[i];
            }
            if (AMetaLine.fLevels.Count > 0)
                fDepthes[0] = a;
            //fM.FGrid.FEngine.RetrieveLine(this, Address);
        }

        internal Line()
        {
        }

        internal MetaLine MetaLine => fM;

        internal string ID => fID;

        internal List<Level> Levels => fM.fLevels;

        //internal List<int> Depthes
        //{
        //    get { return fDepthes; }
        //}
        internal List<long> Multipliers => fM.fIdxArray;

        internal Measure Measure
        {
            get
            {
                if (_Measure == null)
                    _Measure = fM.FGrid.fMeasures[fMeasureID];
                return _Measure;
            }
        }

        [OnDeserialized]
        private void Deserialize(StreamingContext context)
        {
            DoDeserialize();
        }

        internal virtual List<CubeDataNumeric> RetrieveCubeData(List<Member> restriction)
        {
            return null;
        }

        internal abstract void DoDeserialize();

        internal void Unregister()
        {
            CurrentMap.Clear();
            //if (fCacheKey != null)
            //    fM.Grid.Cache.Remove(fCacheKey);
            fCacheKey = null;
        }

        internal abstract void StartMergeSeries(int estimatedCount);

        internal abstract Tuple<double, double> GetRange();

        internal abstract void EndMergeSeries();

        internal abstract void ClearData();
        internal abstract void CheckLineAlive();

        internal virtual bool GetCell(ICubeAddress Address, out object Value)
        {
            Value = null;
            if (fM.FGrid.DeferLayoutUpdate) return false;
            var r = new Dictionary<Level, Member>(Address.FLevelsAndMembers.Count);
            var levelcount = Address.LevelsCount;
            for (var i = 0; i < levelcount; i++)
            {
                var m = Address.Members(i);
                if (m.FMemberType == MemberType.mtCalculated && string.IsNullOrEmpty(((CalculatedMember) m).Expression))
                    return false;
                r.Add(Address.Levels(i), m);
            }

            if (fM.FGrid.isCalculatedState)
            {
                CurrentMap.ClearRequestMap();
                CurrentMap.AddRequest(r);
                DoRetrieveData();
                CurrentMap.ClearRequestMap();
            }

            return false;
        }

        internal virtual bool GetNumericData(long lineIdx, out double data)
        {
            data = 0;
            return false;
        }

        internal virtual bool GetNumericData(long lineIdx, out double data, out string formatted)
        {
            data = 0;
            formatted = null;
            return false;
        }

        internal void AddRequest(DrillAction action, DrillAction action2)
        {
            CurrentMap.AddRequest(action, action2, this);
        }

        internal void AddRequest(List<Member> restriction)
        {
            var cur = new Dictionary<Level, Member>(restriction.Count);
            foreach (var r1 in restriction)
                cur.Add(r1.Level, r1);
            CurrentMap.AddRequest(cur);
        }

        internal void ClearRequestMap()
        {
            CurrentMap.ClearRequestMap();
        }

        internal void DoRetrieveData()
        {
            CurrentMap.DoRetrieveData(this);
        }

        internal abstract IEnumerable<Member> GetMembersList(ICubeAddress a, Members MembersList);

        internal class MembersListStorage
        {
            internal List<int> ids = new List<int>();
            private List<Member> Members;

            internal List<Member> GetList(Level l)
            {
                if (Members == null)
                {
                    Members = ids.Select(item => l.GetMemberByID(item)).ToList();
                    Layout.Members.Sort(Members, Members[0].Level.SortType);
                    ids = null;
                }
                return Members;
            }
        }


        #region IStreamedObject Members

        internal virtual void DoWriteStream(BinaryWriter writer)
        {
        }

        internal virtual void DoReadStream(BinaryReader reader, Tags tag)
        {
            StreamUtils.SkipValue(reader);
        }

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgLine);

            StreamUtils.WriteTag(writer, Tags.tgLine_ID);
            StreamUtils.WriteString(writer, fID);

            StreamUtils.WriteTag(writer, Tags.tgLine_Measure);
            StreamUtils.WriteString(writer, fMeasureID);

            StreamUtils.WriteTag(writer, Tags.tgLine_Mode);
            StreamUtils.WriteGuid(writer, fMode.fUniqueName);

            StreamUtils.WriteTag(writer, Tags.tgLine_CacheKey);
            StreamUtils.WriteString(writer, fCacheKey);

            StreamUtils.WriteTag(writer, Tags.tgLine_HierID);
            StreamUtils.WriteInt32(writer, fHierId);

            StreamUtils.WriteStreamedObject(writer, CurrentMap, Tags.tgLine_CurrentMap);

            StreamUtils.WriteTag(writer, Tags.tgLine_Depthes);
            StreamUtils.WriteInt32(writer, fDepthes.Count);
            for (var i = 0; i < fDepthes.Count; i++)
                StreamUtils.WriteInt32(writer, fDepthes[i]);

            DoWriteStream(writer);

            StreamUtils.WriteTag(writer, Tags.tgLine_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            fM = (MetaLine) options;
            StreamUtils.CheckTag(reader, Tags.tgLine);
            for (var exit = false; !exit;)
            {
                int c;
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgLine_CacheKey:
                        fCacheKey = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgLine_Depthes:
                        c = StreamUtils.ReadInt32(reader);
                        fDepthes = new List<int>(c);
                        for (var i = 0; i < c; i++)
                            fDepthes.Add(StreamUtils.ReadInt32(reader));
                        break;
                    case Tags.tgLine_ID:
                        fID = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgLine_HierID:
                        fHierId = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgLine_Measure:
                        fMeasureID = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgLine_Mode:
                        fMode = Measure.ShowModes.ShowModeById(StreamUtils.ReadGuid(reader));
                        break;
                    case Tags.tgLine_CurrentMap:
                        StreamUtils.ReadStreamedObject(reader, CurrentMap, fM.FGrid);
                        break;
                    case Tags.tgLine_EOT:
                        exit = true;
                        break;
                    default:
                        DoReadStream(reader, tag);
                        break;
                }
            }
        }

        #endregion
    }
}