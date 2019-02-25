using System;
using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Engine.Md
{
    public abstract class MetaLine : IStreamedObject
    {
        private const string _point = ".";
        private const int _MAXString = 1024;
        private readonly string[] _ints = InitThisArray();

        private int cache_HierID = -1;
        internal Line cache_line;
        private MeasureShowMode cache_sm;

        [NonSerialized] internal OlapControl FGrid;

        internal List<int> fHierArray;
        internal string fID = "";
        internal List<long> fIdxArray;
        internal List<Level> fLevels;
        internal long FLimit = 1;
        internal SortedList<string, Line> fLines;

        internal MetaLine()
        {
        }

        internal MetaLine(OlapControl AGrid, IList<int> LevelIndexes)
            : this()
        {
            FGrid = AGrid;
            fLevels = new List<Level>(LevelIndexes.Count);
            var b = 1;
            fIdxArray = new List<long>(LevelIndexes.Count);
            fHierArray = new List<int>(LevelIndexes.Count);
            fLines = new SortedList<string, Line>();
            if (LevelIndexes.Count == 0)
                return;

            for (var i = 0; i < LevelIndexes.Count; i++)
            {
                var L = AGrid.FEngine.FLevelsList[LevelIndexes[i]];
                fLevels.Add(L);
                fIdxArray.Add(FLimit);
                fHierArray.Add(b);
                FLimit *= L.CompleteMembersCount;
                b *= L.FDepth;
            }
            fID = RadarUtils.Join('.', LevelIndexes);
#if DEBUG
            if (fID == "5")
            {
            }
#endif
        }

        internal string ID => fID;

        internal OlapControl Grid => FGrid;

        internal long Limit => FLimit;

        internal List<Level> Levels => fLevels;

        internal void ClearRequestMap()
        {
            foreach (var l in fLines.Values)
                l.ClearRequestMap();
        }

        internal void Clear()
        {
            cache_HierID = -1;
            cache_sm = null;
            cache_line = null;
            foreach (var l in fLines.Values) l.Unregister();
            fLines.Clear();
        }

        internal int[] DecodeLineIdx(long LineIdx)
        {
            var Result = new int[fLevels.Count];
            var a = LineIdx;
            for (var i = fLevels.Count - 1; i > 0; i--)
            {
                var a1 = a / fIdxArray[i];
                a = a % fIdxArray[i];
                Result[i] = Convert.ToInt32(a1);
            }
            if (fLevels.Count > 0) Result[0] = Convert.ToInt32(a);
            return Result;
        }

        internal void FillMembers(long LineIdx, SortedList<int, Member> LevelsAndMembers)
        {
            LevelsAndMembers.Clear();
            LevelsAndMembers.Capacity = fLevels.Count;
            if (fLevels.Count < 1) return;

            var a = LineIdx;
            for (var i = fLevels.Count - 1; i > 0; i--)
            {
                var a1 = a / fIdxArray[i];
                a = a % fIdxArray[i];
                LevelsAndMembers.Add(fLevels[i].ID, fLevels[i].GetMemberByID(Convert.ToInt32(a1)));
            }
            LevelsAndMembers.Add(fLevels[0].ID, fLevels[0].GetMemberByID(Convert.ToInt32(a)));
        }

        internal int GetHierID(IList<Member> MembersArray)
        {
            var Result = 0;
            for (var i = 0; i < fLevels.Count; i++)
                Result += MembersArray[i].FDepth * fHierArray[i];
            return Result;
        }

        internal int GetHierIDFromRestriction(IList<Member> MembersArray, List<Level> source, IList<int> depth)
        {
            var Result = 0;
            for (var i = 0; i < fLevels.Count; i++)
                foreach (var m in MembersArray)
                    if (m.FLevel == fLevels[i] && m.FLevel.FHierarchy.Origin == HierarchyOrigin.hoParentChild)
                    {
                        var idx = source.IndexOf(m.FLevel);
                        if (idx >= 0 && depth[idx] > 0)
                            Result += depth[idx] * fHierArray[i];
                    }
            return Result;
        }

        private static string[] InitThisArray()
        {
            var res = new string[_MAXString];
            for (var i = 0; i < _MAXString; i++)
                res[i] = i + _point;
            return res;
        }

        protected string GetKey(int HierID, Measure AMeasure, MeasureShowMode Mode)
        {
            if (HierID < _MAXString)
                return _ints[HierID] + AMeasure.FUniqueName + Mode.DotUniqueName;
            return HierID + _point + AMeasure.FUniqueName + Mode.DotUniqueName;
        }

        internal virtual Line GetLine(int HierID, Measure AMeasure, MeasureShowMode Mode)
        {
            if (cache_HierID == HierID && cache_sm == Mode && cache_line != null)
                return cache_line;
            cache_HierID = HierID;
            cache_sm = Mode;
            return null;
        }

        internal long GetLineIdx(IList<Member> MembersArray)
        {
            long Result = 0;
            for (var i = 0; i < fLevels.Count; i++)
                Result += MembersArray[i].ID * fIdxArray[i];
            return Result;
        }

        internal void DoRetrieveData()
        {
            foreach (var l in fLines.Values)
                l.DoRetrieveData();
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            /*
        internal string fID = "";
        internal List<Level> fLevels;
        internal List<long> fIdxArray;
        internal List<int> fHierArray;
        internal SortedList<string, Line> fLines;
        internal long FLimit = 1;
             */

            StreamUtils.WriteTag(writer, Tags.tgMetaLine);

            StreamUtils.WriteTag(writer, Tags.tgMetaline_ID);
            StreamUtils.WriteString(writer, fID);

            StreamUtils.WriteTag(writer, Tags.tgMetaline_Levels);
            StreamUtils.WriteInt32(writer, fLevels.Count);
            foreach (var l in fLevels)
                StreamUtils.WriteString(writer, l.UniqueName);

            StreamUtils.WriteTag(writer, Tags.tgMetaline_IdxArray);
            StreamUtils.WriteInt32(writer, fIdxArray.Count);
            foreach (var l in fIdxArray)
                StreamUtils.WriteInt64(writer, l);

            StreamUtils.WriteTag(writer, Tags.tgMetaline_HierArray);
            StreamUtils.WriteInt32(writer, fHierArray.Count);
            foreach (var l in fHierArray)
                StreamUtils.WriteInt32(writer, l);

            StreamUtils.WriteTag(writer, Tags.tgMetaline_Lines);
            StreamUtils.WriteInt32(writer, fLines.Count);
            foreach (var l in fLines.Values)
                StreamUtils.WriteTypedStreamedObject(writer, l, Tags.tgLine);

            StreamUtils.WriteTag(writer, Tags.tgMetaline_Limit);
            StreamUtils.WriteInt64(writer, FLimit);

            StreamUtils.WriteTag(writer, Tags.tgMetaLine_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            FGrid = (OlapControl) options;
            StreamUtils.CheckTag(reader, Tags.tgMetaLine);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                int c;
                switch (tag)
                {
                    case Tags.tgMetaline_HierArray:
                        c = StreamUtils.ReadInt32(reader);
                        fHierArray = new List<int>(c);
                        for (var i = 0; i < c; i++)
                            fHierArray.Add(StreamUtils.ReadInt32(reader));
                        break;
                    case Tags.tgMetaline_Levels:
                        c = StreamUtils.ReadInt32(reader);
                        fLevels = new List<Level>(c);
                        for (var i = 0; i < c; i++)
                            fLevels.Add(FGrid.Dimensions.FindLevel(StreamUtils.ReadString(reader)));
                        break;
                    case Tags.tgMetaline_IdxArray:
                        c = StreamUtils.ReadInt32(reader);
                        fIdxArray = new List<long>(c);
                        for (var i = 0; i < c; i++)
                            fIdxArray.Add(StreamUtils.ReadInt64(reader));
                        break;
                    case Tags.tgMetaline_Lines:
                        c = StreamUtils.ReadInt32(reader);
                        fLines = new SortedList<string, Line>(c);
                        for (var i = 0; i < c; i++)
                        {
                            StreamUtils.ReadTag(reader); // skip Tags.tgLine
                            var l = (Line) StreamUtils.ReadTypedStreamedObject(reader, this);
                            if (l.fMode != null) fLines.Add(l.fID, l);
                        }
                        break;
                    case Tags.tgMetaline_ID:
                        fID = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgMetaline_Limit:
                        FLimit = StreamUtils.ReadInt64(reader);
                        break;
                    case Tags.tgMetaLine_EOT:
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