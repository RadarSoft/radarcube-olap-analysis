using RadarSoft.RadarCube.CubeStructure;

namespace RadarSoft.RadarCube.Layout
{
    internal class MemberWrapper : Member
    {
        private readonly Measure _Measure;

        internal MemberWrapper(Level AParentLevel, Member AParentMember, CubeMember ACubeMember, Measure AMeasure)
            : base(AParentLevel, AParentMember, ACubeMember)
        {
            _Measure = AMeasure;
        }

        public override string DisplayName
        {
            get => _Measure.DisplayName;
            set => _Measure.DisplayName = value;
        }

        public override string UniqueName => _Measure.UniqueName;
    }
}