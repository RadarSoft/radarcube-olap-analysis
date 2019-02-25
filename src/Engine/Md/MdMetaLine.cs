using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Engine.Md
{
    [Serializable]
    [DebuggerDisplay("OlapCubeMetaLine ID = {ID}")]
    internal class MdMetaLine : MetaLine
    {
        internal MdMetaLine(OlapControl AGrid, IList<int> LevelIndexes)
            : base(AGrid, LevelIndexes)
        {
        }

        public MdMetaLine()
        {
            DebugLogging.WriteLine("OlapCubeMetaLine.ctor(ID=null)");
        }

        [Conditional("DEBUG")]
        private void DebugLogging_WriteLine(IList<int> LevelIndexes)
        {
            if (DebugLogging.Verify("OlapCubeMetaLine.ctor()"))
                return;

            var levels = "(" + string.Join(", ", Levels.Select(x => x.DisplayName).ToArray()) + ")";

            DebugLogging.WriteLine("OlapCubeMetaLine.ctor(ID={0} AGrid, LevelIndexes={1}={2})", ID,
                Extentions.ConvertToString(LevelIndexes), levels);
        }

        internal override Line GetLine(int HierID, Measure AMeasure, MeasureShowMode Mode)
        {
            var l = base.GetLine(HierID, AMeasure, Mode);
            if (l != null)
                return l;

            var key = GetKey(HierID, AMeasure, Mode);

            if (fLines.TryGetValue(key, out l))
            {
                cache_line = l;
                return l;
            }
            l = new MdLine(this, key, AMeasure.UniqueName, Mode, HierID);
            fLines.Add(key, l);
            cache_line = l;
            return l;
        }
    }
}