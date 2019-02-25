using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Tools;
using RadarSoft.RadarCube.State;
using System.IO;
using Microsoft.Extensions.Caching.Memory;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.Engine.Md
{
    [Serializable]
    [DebuggerDisplay("MdLine ID = {ID} fIndexes = {fIndexes}")]
    internal class MdLine : Line
    {
        [NonSerialized] private int fCounter;

        [NonSerialized] internal LineData[] fData;

        [NonSerialized] internal long[] fIndexes;

        [NonSerialized] internal LineData[] fNewData;

        [NonSerialized] internal long[] fNewIndexes;

        public MdLine()
        {
        }

        internal MdLine(MetaLine AMetaLine, string ID, string MeasureID, MeasureShowMode Mode, int HierID)
            : base(AMetaLine, ID, MeasureID, Mode, HierID)
        {
            DebugLogging.WriteLine("MdLine.ctor(ID={0} of MetaLine.ID={1})", ID, AMetaLine.ID);

            fData = new LineData[0];
            fIndexes = new long[0];
        }

        internal override void ClearData()
        {
            fIndexes = new long[0];
            fData = new LineData[0];
        }

        internal override IEnumerable<Member> GetMembersList(ICubeAddress a, Members MembersList)
        {
            Dictionary<long, MembersListStorage> sd = null;
            if (fSortedData == null)
            {
                long cnt = 1;
                for (var i = 0; i < a.LevelsCount; i++)
                    cnt *= a.Levels(i).CompleteMembersCount;
                if (cnt <= 300) return MembersList.ToArray();
                fSortedData = new Dictionary<string, Dictionary<long, MembersListStorage>>();
            }

            CheckLineAlive();

            //return MembersList.ToArray();

            var sb = new StringBuilder();
            for (var i = 0; i < a.LevelsCount; i++)
            {
                var l = a.Levels(i);
                if (l != MembersList[0].Level)
                    sb.Append(l.ID + "|");
            }
            var lkey = sb.ToString();

            if (!fSortedData.TryGetValue(lkey, out sd))
            {
                sd = new Dictionary<long, MembersListStorage>();
                fSortedData.Add(lkey, sd);
            }

            var muls = new long[a.LevelsCount];

            long multiplier = 1;
            long sdkey = 0;
            for (var i = 0; i < a.LevelsCount; i++)
            {
                var l = a.Levels(i);
                if (l != MembersList[0].Level)
                {
                    sdkey += a.Members(i).ID * multiplier;
                    muls[i] = multiplier;
                    multiplier *= l.CompleteMembersCount;
                }
                else
                {
                    muls[i] = -1;
                }
            }

            if (sd.Count == 0)
                for (var i = 0; i < fIndexes.Length; i++)
                {
                    var mm = fM.DecodeLineIdx(fIndexes[i]);
                    long ckey = 0;
                    var id = -1;
                    for (var j = 0; j < a.LevelsCount; j++)
                        if (muls[j] > 0)
                            ckey += mm[j] * muls[j];
                        else
                            id = mm[j];
                    MembersListStorage ss;
                    if (!sd.TryGetValue(ckey, out ss))
                    {
                        ss = new MembersListStorage();
                        sd.Add(ckey, ss);
                    }
                    ss.ids.Add(id);
                }

            MembersListStorage mms = null;
            if (!sd.TryGetValue(sdkey, out mms)) return new List<Member>();

            return mms.GetList(MembersList[0].Level);
        }

        internal override List<CubeDataNumeric> RetrieveCubeData(List<Member> restriction)
        {
            RetrieveDataForChart(restriction);

            var Result = new List<CubeDataNumeric>();
            int[] r = null;
            if (restriction != null)
            {
                r = new int[fM.fLevels.Count];
                foreach (var m in restriction)
                {
                    var i = fM.fLevels.IndexOf(m.Level);
                    r[i] = i >= 0 ? m.ID : -1;
                }
            }
            for (var i = 0; i < fIndexes.Length; i++)
            {
                double v = 0;
                try
                {
                    v = Convert.ToDouble(fData[i].Value);
                }
                catch
                {
                    continue;
                }
                var mm = fM.DecodeLineIdx(fIndexes[i]);
                // Exclude grouped members
                var grouped = false;
                var invisible = false;
                for (var j = 0; j < mm.Length; j++)
                {
                    if (Levels[j].FStaticMembers.Count >= mm[j])
                    {
                        var tm = Levels[j].GetMemberByID(mm[j]);
                        if (tm.Parent is GroupMember)
                        {
                            grouped = true;
                            break;
                        }
                    }
                    var m2 = Levels[j].GetMemberByID(mm[j]);
                    if (!m2.Visible)
                    {
                        invisible = true;
                        break;
                    }
                }
                if (grouped || invisible) continue;
                if (r != null)
                {
                    var b = true;
                    for (var j = 0; j < r.Length; j++)
                        if (r[j] >= 0 && r[j] != mm[j])
                        {
                            b = false;
                            break;
                        }
                    if (!b) continue;
                }
                var d = new CubeDataNumeric();
                d.MemberIDs = mm;
                d.Value = v;
                d.FormattedValue = fData[i].FormattedValue;
                d.LineIdx = fIndexes[i];
                Result.Add(d);
            }
            return Result;
        }

        private void RetrieveDataForChart(List<Member> restriction)
        {
            CheckLineAlive();

            if (fData.Length == 0)
            {
                var src = new Dictionary<Level, HashSet<Member>>();
                if (restriction != null)
                    foreach (var m in restriction)
                    {
                        var ll = new HashSet<Member>(new[] {m});

                        src.Add(m.Level, ll);
                    }
                foreach (var l in Levels)
                    if (l.Members.Count == 0)
                        fM.FGrid.Cube.DoRetrieveMembers3(fM.FGrid, l.CubeLevel);
                fM.FGrid.Engine.RetrieveLine2(src, this);
            }
        }

        internal override bool GetNumericData(long lineIdx, out double data)
        {
            RetrieveDataForChart(new List<Member>());

            var i = Array.BinarySearch(fIndexes, lineIdx);
            if (i < 0)
            {
                data = 0;
                return false;
            }
            try
            {
                data = Convert.ToDouble(fData[i].Value);
            }
            catch
            {
                data = 0;
                return false;
            }
            return true;
        }

        internal override bool GetNumericData(long lineIdx, out double data, out string formatted)
        {
            RetrieveDataForChart(new List<Member>());

            var i = Array.BinarySearch(fIndexes, lineIdx);
            if (i < 0)
            {
                data = 0;
                formatted = null;
                return false;
            }
            try
            {
                data = Convert.ToDouble(fData[i].Value);
                formatted = fData[i].FormattedValue;
            }
            catch
            {
                data = 0;
                formatted = null;
                return false;
            }
            return true;
        }


        internal override void DoDeserialize()
        {
            fIndexes = null;
            fData = null;
            fNewIndexes = null;
            fNewData = null;
        }

        internal void AddData(long LineIdx, LineData Data)
        {
            if (Array.BinarySearch(fIndexes, LineIdx) >= 0)
                return;

            fNewIndexes[fCounter] = LineIdx;
            fNewData[fCounter++] = Data;
        }

        internal override void EndMergeSeries()
        {
            DebugLogging.WriteLine("MdLine.EndMergeSeries()");

            fSortedData = null;

            //if (fCacheKey != null)
            //    fM.Grid.Cache.Remove(fCacheKey);

            //if (fM.Grid.Cache != null)
            //    fCacheKey = Guid.NewGuid() + "ln";

            Array.Resize(ref fNewIndexes, fCounter + fIndexes.Length);
            fIndexes.CopyTo(fNewIndexes, fCounter);
            fIndexes = fNewIndexes;

            Array.Resize(ref fNewData, fCounter + fData.Length);
            fData.CopyTo(fNewData, fCounter);
            fData = fNewData;

            Array.Sort(fIndexes, fData);
            FRange = null;

            //if (fM.Grid.Cache != null)
            //{
                string pathToCachDependencyFile = fM.Grid.SessionState.WorkingDirectoryName + TempDirectory.CacheDependencyFile;
            //    if (!File.Exists(pathToCachDependencyFile))
            //        File.Create(pathToCachDependencyFile).Dispose();

            //    if (!fM.Grid.Cache.TryGetValue(fCacheKey, out object o))
            //    {
            //        //fM.Grid.Cache.Set(fCacheKey, new Tuple<long[], LineData[]>(fIndexes, fData), TimeSpan.FromMinutes(5));
            //        fM.Grid.Cache.Set(fCacheKey, new Tuple<long[], LineData[]>(fIndexes, fData), new FileCacheDependency(pathToCachDependencyFile));
            //        DateTime d = DateTime.Now;
            //        DebugLogging.WriteLine("Grid.Cache.Insert:fCacheKey={0}, Time:{1}, pathToCachDependencyFile:{2}", fCacheKey, d.ToString(), pathToCachDependencyFile);
            //    }
            //}
        }

        internal override Tuple<double, double> GetRange()
        {
            if (FRange != null)
                return FRange;

            if (fData == null || fData.Length == 0)
                return null;
            double min;

            try
            {
                min = Convert.ToDouble(fData[0].Value);
            }
            catch
            {
                return null;
            }

            var max = min;

            for (var i = 1; i < fData.Length; i++)
                try
                {
                    var o = Convert.ToDouble(fData[i].Value);
                    if (o < min)
                        min = o;
                    else if (o > max) max = o;
                }
                catch
                {
                }
            FRange = new Tuple<double, double>(min, max);
            return FRange;
        }

        internal override void CheckLineAlive()
        {
            return;

            //if (fIndexes != null && fIndexes.Length > 0)
            //    return;
            //if (fCacheKey != null)
            //{
            //    if (fM.Grid.Cache.TryGetValue(fCacheKey, out object o))
            //    {
            //        var t = o as Tuple<long[], LineData[]>;
            //        fIndexes = t.Item1;
            //        fData = t.Item2;
            //    }
            //}

            //if (fIndexes == null)
            //{
            //    fData = new LineData[0];
            //    fIndexes = new long[0];
            //    //Unregister();
            //}
        }

        internal override bool GetCell(ICubeAddress Address, out object Value)
        {
            CheckLineAlive();

            var i = Array.BinarySearch(fIndexes, Address.FLineIdx);
            if (i >= 0)
            {
                Value = fData[i].Value;
                return true;
            }

            //fM.FGrid.isCalculatedState = true;
            base.GetCell(Address, out Value);
            //fM.FGrid.isCalculatedState = false;

            i = Array.BinarySearch(fIndexes, Address.FLineIdx);
            if (i < 0)
            {
                Value = null;
                return false;
            }
            Value = fData[i].Value;
            return true;
        }

        internal bool GetCellFormatted(ICubeAddress Address, out object Value, out CellFormattingProperties Formatted)
        {
            CheckLineAlive();

            Formatted = new CellFormattingProperties();
            var i = Array.BinarySearch(fIndexes, Address.FLineIdx);
            if (i < 0)
            {
                Value = null;
                Formatted.FormattedValue = "";
                return false;
            }
            Value = fData[i].Value;
            if (Address.Measure != null && Address.Measure.FCubeMeasure != null &&
                Address.MeasureMode.Mode == MeasureShowModeType.smNormal &&
                Address.Measure.DefaultFormat != Address.Measure.CubeMeasure.DefaultFormat)
            {
                Formatted.FormattedValue = Address.Measure.FormatValue(Value, Address.Measure.DefaultFormat);
            }
            else
            {
                var d = fData[i];
                Formatted.FormattedValue = d.FormattedValue;
                if (d.Colors != null)
                {
                    Formatted.BackColor = d.Colors.BackColor;
                    Formatted.ForeColor = d.Colors.ForeColor;
                }
                if (d.Fonts != null)
                {
                    Formatted.FontStyle = d.Fonts.Style;

                    Formatted.FontFamily = d.Fonts.FontName;
                    Formatted.FontSize = d.Fonts.FontSize;
                }
            }
            return true;
        }

        internal override void StartMergeSeries(int estimatedCount)
        {
            DebugLogging.WriteLine("MdLine.StartMergeSeries(estimatedCount={0})", estimatedCount);
            CheckLineAlive();
            fNewIndexes = new long[estimatedCount];
            fNewData = new LineData[estimatedCount];
            fCounter = 0;
        }

        internal override void DoWriteStream(BinaryWriter writer)
        {
            base.DoWriteStream(writer);

            StreamUtils.WriteTag(writer, Tags.tgMdLine_Indexes);
            StreamUtils.WriteInt32(writer, fIndexes.Length);
            for (var i = 0; i < fIndexes.Length; i++)
                StreamUtils.WriteInt64(writer, fIndexes[i]);

            StreamUtils.WriteTag(writer, Tags.tgMdLine_Data);
            StreamUtils.WriteInt32(writer, fData.Length);
            for (var i = 0; i < fData.Length; i++)
                StreamUtils.WriteTypedStreamedObject(writer, fData[i], Tags.tgLineData);
        }

        internal override void DoReadStream(BinaryReader reader, Tags tag)
        {
            switch (tag)
            {
                case Tags.tgMdLine_Indexes:
                    var c = StreamUtils.ReadInt32(reader);
                    fIndexes = new long[c];
                    for (var i = 0; i < c; i++)
                        fIndexes[i] = StreamUtils.ReadInt32(reader);
                    break;

                case Tags.tgMdLine_Data:
                    var cd = StreamUtils.ReadInt32(reader);
                    fData = new LineData[cd];
                    for (var i = 0; i < cd; i++)
                    {
                        StreamUtils.ReadTag(reader); // skip Tags.tgLineData
                        var ld = (LineData)StreamUtils.ReadTypedStreamedObject(reader);
                        fData[i] = ld;
                    }
                    break;
                default:
                    base.DoReadStream(reader, tag);
                    break;
            }

        }
    }
}