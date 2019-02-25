using System.Collections.Generic;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>
    ///     A design-time collection of calculated hierarchy members.
    /// </summary>
    /// <moduleiscollection />
    //[Serializable]
    public class DimensionCalculatedMembers : List<DimensionCalculatedMember>
    {
        /// <summary>
        ///     Creates an instance of the DimensionCalculatedMembers collection
        /// </summary>
        /// <param name="AHierarchy">An owner of this collection</param>
        public DimensionCalculatedMembers(Hierarchy AHierarchy)
        {
            Hierarchy = AHierarchy;
        }

        /// <summary>The hierarchy where the calculated members belong to</summary>
        public Hierarchy Hierarchy { get; }
    }
}