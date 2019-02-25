using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>
    ///     An abstract ancestor to the classes GroupMember and CalculatedMember that
    ///     represents user-created groups and calculated members accordingly on the Grid
    ///     level.
    /// </summary>
    //[Serializable]
    public class CustomMember : Member
    {
        internal CustomMemberPosition fPosition;

        internal CustomMember(Level AParentLevel, Member AParentMember, CubeMember ACubeMember)
            :
            base(AParentLevel, AParentMember, ACubeMember)
        {
            if (AParentMember == null) FVisible = true;
        }

        internal CustomMember(Level AParentLevel)
            :
            base(AParentLevel)
        {
            if (FParent == null) FVisible = true;
        }

        /// <summary>
        ///     Defines a visible position in the Grid for the specified member among other
        ///     members of the same hierarchy level.
        /// </summary>
        public CustomMemberPosition Position
        {
            get => fPosition;
            set => fPosition = value;
        }
    }
}