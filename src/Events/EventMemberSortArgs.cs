using System;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Events
{
    /// <summary>
    ///     An object passed to the OlapGrid.OnMemberSort event of the hierarchy members
    ///     sorting
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When the Hierarchy property is set to True, then OverrideSortMethods. When
    ///         changing a user mode of the hierarchy sorting, the OlapGrid.OnMemberSort event is
    ///         called instead of the standard sorting procedures which always allow sorting the
    ///         hierarchy members in the order a programmer needs.
    ///     </para>
    ///     <para>
    ///         A programmer should compare two hierarcy members passed with the MemberHigh
    ///         and MemberLow properties and to assign "true" or "false" of a given statement to
    ///         the IsTrue property.
    ///     </para>
    /// </remarks>
    public class EventMemberSortArgs : EventArgs
    {
        internal Member fHigh;
        internal Member fLow;

        internal EventMemberSortArgs(MembersSortType SortingMethod)
        {
            this.SortingMethod = SortingMethod;
        }

        /// <summary>
        ///     A hierarchy member conventionally called "junior" for the applied sorting
        ///     method.
        /// </summary>
        public Member MemberLow => fLow;

        /// <summary>
        ///     A hierarchy member conventionally called "senior" for the applied sorting
        ///     method.
        /// </summary>
        public Member MemberHigh => fHigh;

        /// <summary>
        ///     Should contain the result of comparison of members. Set to True if MemberHigh &gt;
        ///     MemberLow.
        /// </summary>
        public int Result { get; set; }

        /// <summary>The sorting method to the specified hierarchy applied by an end user.</summary>
        public MembersSortType SortingMethod { get; }
    }
}