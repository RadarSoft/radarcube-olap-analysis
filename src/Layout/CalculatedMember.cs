using System;
using System.IO;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>
    ///     The class represents a calculated hierarchy member or calculated measure created
    ///     on the client's side.
    /// </summary>
    ////[Serializable]
    public class CalculatedMember : CustomMember
    {
        internal string FExpression;

        internal CalculatedMember(Level AParentLevel, Member AParentMember,
            CubeMember ACubeMember)
            : base(AParentLevel, AParentMember, ACubeMember)
        {
        }

        internal CalculatedMember(Level AParentLevel)
            : base(AParentLevel)
        {
        }

        /// <summary>A valid MDX-style expression.</summary>
        /// <remarks>
        ///     The value of this property will be used as an MDX expression describing the
        ///     member in the WITH MEMBER clause of the MDX queries passed to the server.
        /// </remarks>
        /// <example>
        ///     <code lang="CS" title="[New Example]">
        /// 		<![CDATA[
        /// Hierarchy h = OlapAnalysis1.Dimensions.FindHierarchyByDisplayName("Sales Channel");
        /// CalculatedMember M = h.CreateCalculatedMember("Reseller - Internet", "",
        ///     h.Levels[0], null, CustomMemberPosition.cmpLast);
        /// M.Expression = "[Sales Channel].[Reseller] - [Sales Channel].[Internet]";]]>
        /// 	</code>
        /// </example>
        public string Expression
        {
            get => FExpression;
            set
            {
                if (FExpression != value)
                {
                    FExpression = value;
                    Level.Grid.Engine.ClearIncludedMetalines(Level);
                    if (Level.Hierarchy.IsUpdating)
                    {
                        Level.Hierarchy.UpdateFilterState(true);
                    }
                    else
                    {
                        if (!Level.Grid.IsUpdating && Level.Grid.Active)
                            Level.Grid.CellSet.Rebuild();
                    }
                }
            }
        }

        protected internal override void WriteStream(BinaryWriter stream)
        {
            StreamUtils.WriteTag(stream, Tags.tgCalculatedMember);

            StreamUtils.WriteTag(stream, Tags.tgCalculatedMember_Position);
            StreamUtils.WriteInt32(stream, (int) fPosition);

            if (!string.IsNullOrEmpty(FExpression))
            {
                StreamUtils.WriteTag(stream, Tags.tgCalculatedMember_Expresion);
                StreamUtils.WriteString(stream, FExpression);
            }

            base.WriteStream(stream);

            StreamUtils.WriteTag(stream, Tags.tgCalculatedMember_EOT);
        }

        protected internal override void ReadStream(BinaryReader stream)
        {
            var _exit = false;
            do
            {
                var Tag = StreamUtils.ReadTag(stream);
                switch (Tag)
                {
                    case Tags.tgCalculatedMember_Position:
                        fPosition = (CustomMemberPosition) StreamUtils.ReadInt32(stream);
                        break;
                    case Tags.tgCalculatedMember_Expresion:
                        FExpression = StreamUtils.ReadString(stream);
                        break;
                    case Tags.tgMember:
                        base.ReadStream(stream);
                        break;
                    case Tags.tgCalculatedMember_EOT:
                        _exit = true;
                        break;
                    default:
                        throw new Exception("Unknow tag: " + Tag);
                }
            } while (!_exit);
        }
    }
}