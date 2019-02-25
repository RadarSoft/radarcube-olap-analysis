using System;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.CellSet
{
    /// <exclude />
    public class Group
    {
        internal bool FDeleteableByUser = true;
        internal string FDescription;
        internal string FDisplayName;
        internal CustomMemberPosition FPosition = CustomMemberPosition.cmpLast;
        internal string FUniqueName = Guid.NewGuid().ToString();

        public Group(Hierarchy AHierarchy)
        {
            Hierarchy = AHierarchy;
        }

        /// <summary>The hierarchy where the group will go to.</summary>
        public Hierarchy Hierarchy { get; }

        /// <summary>A display name of the group.</summary>
        /// <remarks>
        ///     The name of the group shown in the Grid cell. This property's value will be
        ///     assigned to the Member.DisplayName property
        /// </remarks>
        public string DisplayName
        {
            get => FDisplayName;
            set => FDisplayName = value;
        }

        /// <summary>If True, then an end user is able to delete the group.</summary>
        /// <remarks>
        ///     This property's value will be assigned to the GroupMember.DeleteableByUser
        ///     property
        /// </remarks>
        public bool DeleteableByUser
        {
            get => FDeleteableByUser;
            set => FDeleteableByUser = value;
        }

        /// <summary>A unique name of the group.</summary>
        /// <remarks>This property's value will be assigned to the Member.UniqueName property</remarks>
        public string UniqueName
        {
            get => FUniqueName;
            set => FUniqueName = value;
        }

        /// <summary>Detailed description of the group.</summary>
        /// <remarks>This property's value will be assigned to the Member.Description property</remarks>
        public string Description
        {
            get => FDescription;
            set => FDescription = value;
        }

        /// <summary>The position for the group member among other members of the hierarchy.</summary>
        /// <remarks>
        ///     This property's value will be assigned to the GroupMember.Position
        ///     property
        /// </remarks>
        public CustomMemberPosition Position
        {
            get => FPosition;
            set => FPosition = value;
        }
    }
}