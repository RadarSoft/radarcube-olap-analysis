using System.Collections.Generic;

namespace RadarSoft.RadarCube.CubeStructure
{
    /// <summary>
    ///     Represents the list of members assigned to the hierarchy level or to any of the parent elements of the Parent-Child
    ///     hierarchy.
    /// </summary>
    /// <moduleiscollection />
    public class CubeMembers : List<CubeMember>
    {
        internal CubeMembers()
            : base(0)
        {
        }


        /// <summary>
        ///     Returns the CubeMember object with a unique name passed as the parameter or
        ///     null, if there's no such object in the list.
        /// </summary>
        /// <returns>The CubeMember</returns>
        /// <param name="UniqueName">The unique name of the member</param>
        public CubeMember Find(string UniqueName)
        {
            foreach (var m in this)
                if (m.FUniqueName == UniqueName) return m;
            return null;
        }

        /// <summary>
        ///     Returns from the specified list of the Cube members and all their descendants a
        ///     CubeMember object with a unique name passed as the parameter or null, if there's no
        ///     such object in the list.
        /// </summary>
        /// <returns>The CubeMember object</returns>
        /// <remarks>
        ///     <para>
        ///         Unlike the Find method, this one searches through its own Children list, but
        ///         also through the Children list of each CubeMember from the specified lists.
        ///     </para>
        /// </remarks>
        /// <param name="UniqueName">The unique member name</param>
        public CubeMember FindInChildren(string UniqueName)
        {
            var m = Find(UniqueName);
            if (m != null) return m;
            foreach (var m1 in this)
                if (m1.Children.Count > 0)
                {
                    var m2 = m1.Children.FindInChildren(UniqueName);
                    if (m2 != null) return m2;
                }
            return null;
        }
    }
}