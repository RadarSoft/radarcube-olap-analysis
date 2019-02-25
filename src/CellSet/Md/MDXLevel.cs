using System.Diagnostics;
using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.CellSet.Md
{
    [DebuggerDisplay("MDXLevel UniqueName = {UniqueName}, Isfullfetched = {Isfullfetched}")]
    internal class MDXLevel : IDescriptionable
    {
        public readonly int _memberscount;

        public int _Currentmemberscount;
        internal bool _isfullfetched;

        public MDXLevel(string uniqueName, int memberCount)
        {
            UniqueName = uniqueName;
            _memberscount = memberCount;

            _Currentmemberscount = 0;
        }

        public bool Isfullfetched => _Currentmemberscount == _memberscount;

        public string DisplayName => null;

        public string Description => null;

        public string UniqueName { get; }

        internal void IncrementMembersCount()
        {
            _Currentmemberscount++;
        }
    }
}