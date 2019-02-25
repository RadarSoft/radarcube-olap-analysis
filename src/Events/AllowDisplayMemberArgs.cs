using System;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Events
{
    /// <summary>
    ///     An object passed as a parameter to the OlapGrid.OnAllowDisplayMember
    ///     event.
    /// </summary>
    /// <seealso cref="CustomOLAPControl.OnAllowDisplayMember">OnAllowDisplayMember Event (RadarSoft.RadarCube.Web.OlapControl)</seealso>
    public class AllowDisplayMemberArgs : EventArgs
    {
        internal AllowDisplayMemberArgs(ICubeAddress address, Member member, bool isTotal)
        {
            Address = address.Clone();
            Member = member;
            IsTotal = isTotal;
        }

        /// <summary>
        ///     Indicates whether the current member is allowed to be displayed in the
        ///     Grid.
        /// </summary>
        /// <remarks>True by default.</remarks>
        public bool Allow { get; set; } = true;

        /// <summary>The multidimensional address of the member.</summary>
        public ICubeAddress Address { get; }

        /// <summary>The member to be displayed.</summary>
        public Member Member { get; }

        /// <summary>
        ///     True if this cell is a "total".
        /// </summary>
        public bool IsTotal { get; }
    }
}