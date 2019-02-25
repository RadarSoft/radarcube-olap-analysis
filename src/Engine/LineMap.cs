using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.Engine
{
    internal class LineMap : IStreamedObject
    {
        private Dictionary<Level, HashSet<Member>> map;
        private Dictionary<Level, HashSet<Member>> request = new Dictionary<Level, HashSet<Member>>();

        internal void Clear()
        {
            map = null;

            if (request != null)
                request.Clear();
            request = null;
        }

        private Dictionary<Level, HashSet<Member>> CloneMap(Dictionary<Level, HashSet<Member>> item)
        {
            var res = new Dictionary<Level, HashSet<Member>>();
            foreach (var v in item)
                res.Add(v.Key, v.Value == null ? null : new HashSet<Member>(v.Value));

            return res;
        }

        internal void AddRequest(DrillAction member, DrillAction member2, Line line)
        {
            if (request == null)
                request = new Dictionary<Level, HashSet<Member>>();

            if (member == null)
            {
                request.Clear();
                foreach (var l in line.Levels)
                    request.Add(l, null);
                return;
            }

            IEnumerable<Member> mms = member.Members;
            if (member2 != null)
                mms = mms.Union(member2.Members);

            foreach (var m in mms)
            {
                HashSet<Member> mm;
                if (!request.TryGetValue(m.Level, out mm))
                {
                    mm = new HashSet<Member>();
                    request.Add(m.Level, mm);
                }
                if (mm != null)
                    mm.Add(m);
            }
        }

        internal void ClearRequestMap()
        {
            if (request != null)
                request.Clear();
            request = null;
        }

        internal void DoRetrieveData(Line line)
        {
            Dictionary<Level, HashSet<Member>> newmap;
            var req = DoMergeRequests2(line, out newmap);

            if (line.fM.Grid.Engine.CalculatedByServer(line.Measure) && req != null)
            {
                foreach (var v in req.Where(item => item.Value == null).ToArray())
                    req.Remove(v.Key);
                line.fM.FGrid.Engine.RetrieveLine2(req, line);
            }

            map = newmap;

            if (request != null)
                request.Clear();
        }

        private Dictionary<Level, HashSet<Member>> DoMergeRequests2(Line line,
            out Dictionary<Level, HashSet<Member>> newmap)
        {
            if (request == null)
            {
                newmap = map;
                return null;
            }

            Normalize(request, line, false);

            if (map == null)
            {
                line.ClearData();
                newmap = CloneMap(request);
                return request;
            }

            foreach (var k in map)
                if (request[k.Key] == null && k.Value != null)
                {
                    line.ClearData();
                    newmap = CloneMap(request);
                    return request;
                }

            Normalize(map, line, true);
            Tuple<Level, HashSet<Member>> diff = null;
            foreach (var k in request)
            {
                var hm = map[k.Key];
                if (hm == null) continue;
                var md = new HashSet<Member>(k.Value.Except(hm));
                if (md.Count > 0)
                {
                    if (diff != null)
                        if (diff.Item1 == k.Key)
                        {
                            foreach (var m in md)
                                diff.Item2.Add(m);
                        }
                        else
                        {
                            newmap = CloneMap(request);
                            line.ClearData();
                            return request;
                        }
                    diff = new Tuple<Level, HashSet<Member>>(k.Key, md);
                }
            }

            if (diff == null)
            {
                newmap = map;
                return null;
            }

            newmap = new Dictionary<Level, HashSet<Member>>();
            var newreq = new Dictionary<Level, HashSet<Member>>();
            foreach (var k in map)
                if (diff.Item1 != k.Key)
                {
                    newmap.Add(k.Key, k.Value == null ? null : new HashSet<Member>(k.Value));
                    newreq.Add(k.Key, k.Value);
                }
                else
                {
                    newmap.Add(k.Key, new HashSet<Member>(k.Value.Union(diff.Item2)));
                    newreq.Add(k.Key, new HashSet<Member>(diff.Item2));
                }
            return newreq;
        }

        private void Normalize(Dictionary<Level, HashSet<Member>> rq, Line line, bool addLevelsOnly)
        {
            if (!addLevelsOnly)
            {
                var tmp = new HashSet<Level>();
                foreach (var v in rq)
                    if (v.Value != null && CellSet.CellSet.enable_CompleteMembersCount_Little_Count(v))
                        tmp.Add(v.Key);

                foreach (var v in tmp)
                    rq.Remove(v);
            }

            IEnumerable<Level> ll = line.Levels;
            if (addLevelsOnly)
            {
                if (request != null)
                    ll = ll.Union(request.Keys);
            }
            else
            {
                if (map != null)
                    ll = ll.Union(map.Keys);
            }

            foreach (var l in ll)
                if (!rq.ContainsKey(l))
                    rq.Add(l, null);
        }

        internal void AddRequest(Dictionary<Level, Member> d)
        {
            if (request == null)
                request = new Dictionary<Level, HashSet<Member>>();

            foreach (var k in d)
            {
                HashSet<Member> mm;
                if (!request.TryGetValue(k.Key, out mm))
                {
                    mm = new HashSet<Member>();
                    request.Add(k.Key, mm);
                }
                if (!mm.Contains(k.Value))
                    mm.Add(k.Value);
            }
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgLineMap);

            if (map != null)
            {
                StreamUtils.WriteTag(writer, Tags.tgLineMap_Map);
                StreamUtils.WriteInt32(writer, map.Count);
                foreach (var ll in map)
                {
                    StreamUtils.WriteString(writer, ll.Key.UniqueName);
                    if (ll.Value != null)
                    {
                        StreamUtils.WriteInt32(writer, ll.Value.Count);
                        foreach (var m in ll.Value)
                            StreamUtils.WriteString(writer, m.UniqueName);
                    }
                    else
                    {
                        StreamUtils.WriteInt32(writer, -1);
                    }
                }
            }
            StreamUtils.WriteTag(writer, Tags.tgLineMap_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            var g = (OlapControl) options;
            if (map == null)
                map = new Dictionary<Level, HashSet<Member>>();
            else
                map.Clear();
            StreamUtils.CheckTag(reader, Tags.tgLineMap);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgLineMap_Map:
                        var lcnt = StreamUtils.ReadInt32(reader);
                        for (var j = 0; j < lcnt; j++)
                        {
                            var uniqName = StreamUtils.ReadString(reader);
                            var l = g.Dimensions.FindLevel(uniqName);
                            var mcnt = StreamUtils.ReadInt32(reader);
                            if (mcnt >= 0)
                            {
                                var mm = new HashSet<Member>();
                                for (var k = 0; k < mcnt; k++)
                                {
                                    var mname = StreamUtils.ReadString(reader);
                                    if (l != null)
                                    {
                                        var m = l.FindMember(mname);
                                        if (m != null)
                                            mm.Add(m);
                                    }
                                }
                                if (l != null)
                                    map.Add(l, mm);
                            }
                            else if (l != null)
                            {
                                map.Add(l, null);
                            }
                        }
                        break;
                    case Tags.tgLineMap_EOT:
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