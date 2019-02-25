using System.Collections.Generic;

namespace RadarSoft.RadarCube.CellSet
{
    internal class CellSetMembers : List<CellsetMember>
    {
        internal int FSiblingsCount = -1;

        public new void Add(CellsetMember item)
        {
#if DEBUG
            if (item.DisplayName == "1996")
            {
            }

            if (item.FLevel != null && item.FLevel.FLevel.DisplayName == "Quarter")
            {
            }

#endif
            base.Add(item);
        }

        public new void AddRange(IEnumerable<CellsetMember> collection)
        {
            base.AddRange(collection);
        }
    }
}