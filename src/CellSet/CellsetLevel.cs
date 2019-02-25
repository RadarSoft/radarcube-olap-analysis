using System.Collections.Generic;
using System.Linq;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.CellSet
{
    internal class CellsetLevel
    {
        internal readonly Level FLevel;
        internal InfoAttribute Attribute = null;
        internal int FCellsCount = 0;
        internal int FColSpan;
        internal int FDepth = 1;
        internal int FDivingLevel;
        internal int fID;
        internal byte FIndent = 0;
        internal int FRowSpan;
        internal int FStartCol;
        internal int FStartRow;

        internal CellsetLevel(Level ALevel)
        {
            DebugLogging.WriteLine("CellsetLevel.ctor({0})", ToString());
            FLevel = ALevel;
        }

        internal IEnumerable<CellsetMember> AllChildren()
        {
            DebugLogging.WriteLine("CellsetLevel.AllChildren() ({0})", ToString());

            return FLevel.Grid.CellSet.FRowMembers
                .SelectMany(item => item.AllChildren())
                .Union(
                    FLevel.Grid.CellSet.FColumnMembers
                        .SelectMany(item => item.AllChildren()))
                .Where(item => item.FLevel == this);
        }

        internal int GetInternalIndex(IList<Hierarchy> hl)
        {
            var h = FLevel.Hierarchy;

            if (h == null)
                if (FLevel.GetGrid().FLayout.fMeasurePosition == MeasurePosition.mpFirst)
                {
                    if (FLevel.FMembers[0].MemberType == MemberType.mtMeasure)
                        return -2;
                    return -1;
                }
                else
                {
                    if (FLevel.FMembers[0].MemberType == MemberType.mtMeasure)
                        return 100000001;
                    return 100000002;
                }
            var r = hl.IndexOf(h) * 100000 + FLevel.Index * 100;

            if (Attribute == null)
                return r;
            return r + FLevel.CubeLevel.InfoAttributes.IndexOf(Attribute) + 1;
        }
    }
}