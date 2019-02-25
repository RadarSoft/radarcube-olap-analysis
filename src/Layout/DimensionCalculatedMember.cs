using System;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>
    ///     A calculated hierarchy member described in the "Grid layout" editor (at design time).
    /// </summary>
    //[Serializable]
    public class DimensionCalculatedMember
    {
        internal string FDescription;
        internal string FDisplayName;
        internal string FExpression;
        internal CustomMemberPosition FPosition = CustomMemberPosition.cmpLast;
        internal string FUniqueName = Guid.NewGuid().ToString();

        /// <summary>Creates an instance of the DimensionCalculatedMember class</summary>
        /// <param name="AHierarchy">Owner of this member</param>
        public DimensionCalculatedMember(Hierarchy AHierarchy)
        {
            Hierarchy = AHierarchy;
        }

        /// <summary>Specifies the hierarchy for a future calculated member.</summary>
        public Hierarchy Hierarchy { get; }

        /// <summary>
        ///     Not used in the current version.
        /// </summary>
        public string Expression
        {
            get => FExpression;
            set => FExpression = value;
        }

        /// <summary>Specifies the display name of a future calculated dimension member.</summary>
        public string DisplayName
        {
            get => FDisplayName;
            set => FDisplayName = value;
        }

        /// <summary>Specifies the position for the future member of the Grid.</summary>
        public CustomMemberPosition Position
        {
            get => FPosition;
            set => FPosition = value;
        }

        /// <summary>Specifies the unique name for a future calculated dimension member.</summary>
        public string UniqueName
        {
            get => FUniqueName;
            set => FUniqueName = value;
        }

        /// <summary>
        ///     Text description of the future calculated dimension member.
        /// </summary>
        public string Description
        {
            get => FDescription;
            set => FDescription = value;
        }
    }
}