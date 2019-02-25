using System.Collections.Generic;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.CellSet
{
    /// <exclude />
    /// <moduleiscollection />
    public class Groups : List<Group>
    {
        internal Groups(Hierarchy AHierarchy)
        {
            Hierarchy = AHierarchy;
        }

        /// <summary>
        ///     Reference to the hierarchy containing a given collection.
        /// </summary>
        public Hierarchy Hierarchy { get; }
    }
}