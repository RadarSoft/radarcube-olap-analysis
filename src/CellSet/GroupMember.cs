using System;
using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.CellSet
{
    /// <summary>Describes groups of hierarchy members</summary>
    /// <remarks>
    ///     At design time, groups can be created by a programmer, at run time - through the
    ///     CreateGroup function or directly by an end user. The groups, along with the calculated
    ///     hierarchy members, represent custom members which are not mapped with the real objects
    ///     of the database or the MS Analysis server
    /// </remarks>
    public class GroupMember : CustomMember
    {
        internal bool FDeleteableByUser;

        internal GroupMember(Level AParentLevel, Member AParentMember,
            CubeMember ACubeMember)
            : base(AParentLevel, AParentMember, ACubeMember)
        {
            FMemberType = MemberType.mtGroup;
        }

        internal GroupMember(Level AParentLevel)
            : base(AParentLevel)
        {
            FMemberType = MemberType.mtGroup;
        }

        /// <summary>
        ///     Shows whether the specified group can be deleted from the context menu or the
        ///     hierarchy editor by an end user.
        /// </summary>
        public bool DeleteableByUser
        {
            get => FDeleteableByUser;
            set => FDeleteableByUser = value;
        }

        /// <summary>
        ///     Group caption. Unlike the caption of common or calculated hierarchy member, it
        ///     can be changed while working with the Grid.
        /// </summary>
        public override string DisplayName
        {
            get => base.DisplayName;
            set
            {
                base.DisplayName = value;
                if ((HierarchyState.hsActive & FLevel.FHierarchy.State) == HierarchyState.hsActive)
                    FLevel.Grid.FCellSet.Rebuild();
            }
        }

        protected internal override void WriteStream(BinaryWriter stream)
        {
            StreamUtils.WriteTag(stream, Tags.tgGroupMember);

            StreamUtils.WriteTag(stream, Tags.tgGroupMember_Position);
            StreamUtils.WriteInt32(stream, (int) fPosition);

            StreamUtils.WriteTag(stream, Tags.tgGroupMember_Deleteable);
            StreamUtils.WriteBoolean(stream, FDeleteableByUser);

            base.WriteStream(stream);

            StreamUtils.WriteTag(stream, Tags.tgGroupMember_EOT);
        }

        internal void PopulateListOfMembers(List<Member> list)
        {
            foreach (var m in Children)
            {
                if (m is CalculatedMember) continue;
                if (m is GroupMember)
                    ((GroupMember) m).PopulateListOfMembers(list);
                else if (m.Visible) list.Add(m);
            }
        }


        protected internal override void ReadStream(BinaryReader stream)
        {
            var _exit = false;
            do
            {
                var Tag = StreamUtils.ReadTag(stream);
                switch (Tag)
                {
                    case Tags.tgGroupMember:
                        //                        fPosition = (CustomMemberPosition)StreamUtils.ReadInt32(stream);
                        break;
                    case Tags.tgGroupMember_Deleteable:
                        FDeleteableByUser = StreamUtils.ReadBoolean(stream);
                        break;
                    case Tags.tgGroupMember_Position:
                        fPosition = (CustomMemberPosition) StreamUtils.ReadInt32(stream);
                        break;
                    case Tags.tgMember:
                        base.ReadStream(stream);
                        break;
                    case Tags.tgGroupMember_EOT:
                        _exit = true;
                        break;
                    default:
                        throw new Exception("Unknow tag: " + Tag);
                }
            } while (!_exit);
        }
    }
}