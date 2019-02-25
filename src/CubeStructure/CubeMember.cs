using System;
using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.CellSet.Md;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.CubeStructure
{
    /// <summary>Represents a Cube hierarchy member or a Cube measure.</summary>
    public class CubeMember : IDescriptionable
    {
        internal object _data;
        internal DateTime fBIDate;
        internal CubeMembers FChildren = new CubeMembers();
        internal int FChildrenCount = -1;
        internal string FDescription;
        internal string fDisplayName;
        internal bool FHideSystemGeneratedMember;
        internal CubeHierarchy FHierarchy;

        internal int fID;
        internal Dictionary<string, object> fInfoAttributes;
        internal bool? fIsLeaf;
        internal bool fIsTotal;
        internal int FMDXLevelIndex = -1;
        internal CubeMember FParent;
        internal CubeLevel FParentLevel;
        internal bool FRaggedVirtual;
        internal double FRank;

        internal string FShortName;
        internal string FStringID;

        internal string FStringParentID;

        // --------------------------------------------
        internal string FUniqueName;

        internal bool IsMDXCalculated;

        internal CubeMember(CubeHierarchy aHierarchy, CubeLevel aParentLevel,
            string displayName, string description, string uniqueName, string shortName, bool raggedEmpty,
            string levelName)
        {
            FHierarchy = aHierarchy;

            FParent = null;

            if (aHierarchy.FMDXLevelNames.Count > 0)
                FMDXLevelIndex = aHierarchy.FMDXLevelNames.IndexOf(levelName);

            FParentLevel = aParentLevel;
            FDescription = description;
            FUniqueName = uniqueName.Trim();
            FShortName = shortName.Trim();
            fDisplayName = displayName.Trim();
            FRaggedVirtual = raggedEmpty;
            FRank = aParentLevel.FUniqueNamesArray.Count;
            aParentLevel.FUniqueNamesArray.Add(FUniqueName, this);
            // aParentLevel.FUniqueNamesArray[FUniqueName] = this;
            aParentLevel.RegisterMember(this);
        }

        internal CubeMember(CubeHierarchy AHierarchy, CubeLevel AParentLevel)
        {
            FHierarchy = AHierarchy;
            FMDXLevelIndex = -1;
            FParentLevel = AParentLevel;
        }

        public object Data => _data;

        /// <summary>
        ///     get by FMDXLevelIndex from parent level
        /// </summary>
        internal MDXLevel MDXLevel
        {
            get
            {
                if (FMDXLevelIndex == -1 || ParentLevel == null || ParentLevel._MDXLevels.Count == 0)
                    return null;

                return ParentLevel._MDXLevels[FMDXLevelIndex];
            }
        }

        /// <summary>The field can be used for custom sorting of a hierarchy.</summary>
        public DateTime BIDate => fBIDate;

        /// <summary>
        ///     The string ID from the database.
        /// </summary>
        public string StringID
        {
            get => FStringID;
            private set
            {
                if (FStringID == value)
                    return;

                //                if (value == fDisplayName && (Object)value != (Object)fDisplayName)
                //                    value = fDisplayName;
                //#if DEBUG
                //                else
                //                {

                //                }
                //#endif

                FStringID = value;
            }
        }

        internal string StringParentID
        {
            get => FStringParentID;
            set => FStringParentID = value;
        }

        /// <summary>
        ///     The list of "neighbors" of the specified hierarchy member on the Cube
        ///     level.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Returns the list of hierarchy Children members of the same Parent as the
        ///         specified member.
        ///     </para>
        ///     <para>
        ///         Unlike the list returned by the Member.SiblingsList property, this one does
        ///         not depend on the sorting and grouping modes currently used in the Grid.
        ///     </para>
        /// </remarks>
        public CubeMembers SiblingsList
        {
            get
            {
                if (FParent == null) return FParentLevel.Members;
                if (FParent.FParentLevel == FParentLevel) return FParent.FChildren;
                return FParent.NextLevelChildren;
            }
        }

        /// <summary>A hierarchy member name displayed in the Grid.</summary>
        public string DisplayName
        {
            get => fDisplayName;
            set
            {
                if (value == null)
                {
                }

                fDisplayName = value;
            }
        }

        /// <summary>
        ///     A detailed description of a hierarchy member that appears as a pop-up window
        ///     (tooltip) when the cursor is pointed at the panel with the name of the member in the
        ///     Grid.
        /// </summary>
        public string Description
        {
            get => FDescription;
            set => FDescription = value;
        }

        /// <summary>
        ///     Contains a unique string member identifier.
        /// </summary>
        /// <remarks>
        ///     <para>Never visible to an end user.</para>
        /// </remarks>
        public string UniqueName => FUniqueName;

        /// <summary>
        ///     A short name of the hierarchy member.
        /// </summary>
        public string ShortName => FShortName;

        /// <summary>Indicates the Parent of the specified member.</summary>
        /// <remarks>
        ///     <para>
        ///         If the value of this property equals null, then it means that the element is
        ///         root in the hierarchy tree
        ///     </para>
        ///     <para></para>
        /// </remarks>
        public CubeMember Parent => FParent;

        /// <summary>References to the Cube level that contains the specified hierarchy member.</summary>
        public CubeLevel ParentLevel => FParentLevel;

        /// <summary>References to the same-level children list of the specified member.</summary>
        public CubeMembers Children => FChildren;

        /// <summary>
        ///     In a multilevel hierarchy references to the next-level children list of the
        ///     specified member.
        /// </summary>
        /// <remarks>For flat and Parent-Child hierarchies is always empty.</remarks>
        public CubeMembers NextLevelChildren { get; } = new CubeMembers();

        /// <summary>Reference to the Cube hierarchy that contains the specified member.</summary>
        public CubeHierarchy Hierarchy => FHierarchy;

        /// <summary>
        ///     True if this member is used as an auxiliary one in building ragged
        ///     hierarchies.
        /// </summary>
        /// <remarks>
        ///     This member is created upon initialization of a dimension tables to substitute
        ///     the missing members in ragged hierarchies.Thus, it doesn't exist in the
        ///     database.
        /// </remarks>
        public bool IsRaggedVirtual => FRaggedVirtual;

        internal bool IsFullyFetched
        {
            get
            {
                if (ParentLevel.Hierarchy == null) return true;
                if (ParentLevel.Hierarchy.Origin == HierarchyOrigin.hoParentChild)
                    return FChildrenCount == Children.Count;
                if (ParentLevel.Hierarchy.Origin == HierarchyOrigin.hoUserDefined)
                    return FChildrenCount == NextLevelChildren.Count;
                return true;
            }
        }

        /// <summary>
        ///     A unique numeric member identifier in the list of level members.
        /// </summary>
        /// <remarks>
        ///     This property is used by the RadarCube infrastructure. It is recommended to use
        ///     the UniqueName property instead of ID
        /// </remarks>
        public int ID => fID;

        internal bool HideSystemGeneratedMember
        {
            get => FHideSystemGeneratedMember;
            set => FHideSystemGeneratedMember = value;
        }

        /// <summary>
        ///     A numeric value associated with the specified hierarchy member. Used for the
        ///     default sorting.
        /// </summary>
        public double Rank => FRank;
#if DEBUG
        internal static int Comparer(CubeMember a, CubeMember b)
        {
            var c1 = a.Data as IComparable;
            var c2 = b.Data as IComparable;

            if (c1 != null && c2 != null)
                return c1.CompareTo(c2);

            return 0;
        }
#endif

        // From TOLAPCube Member ----------------------
        internal string MakeQualifiedName()
        {
            var result = Info.rsLeftBracket + FStringID + Info.rsMemberDelimiter + fDisplayName + Info.rsRightBracket;
            var M = this;
            while (M.Parent != null)
            {
                //                result = Info.rsLeftBracket + M.FParent.fDisplayName + Info.rsRightBracket + Info.rsMemberDelimiter + result;

                result = Info.rsLeftBracket + M.FParent.FStringID + Info.rsMemberDelimiter + M.FParent.fDisplayName +
                         Info.rsRightBracket + Info.rsMemberDelimiter + result;
                M = M.FParent;
            }
            result = Info.rsLeftBracket + Hierarchy.Dimension.DisplayName + Info.rsRightBracket +
                     Info.rsMemberDelimiter +
                     Info.rsLeftBracket + Hierarchy.DisplayName + Info.rsRightBracket + Info.rsMemberDelimiter + result;
            SetUniqueName(result);
            return result;
        }

        internal void WriteStream(BinaryWriter aBinaryWriter)
        {
            StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember);

            StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_DisplayName);
            StreamUtils.WriteString(aBinaryWriter, fDisplayName);

            //  FUniqueName
            StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_UniqueName);
            StreamUtils.WriteString(aBinaryWriter, FUniqueName);

            //  FShortName
            StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_ShortName);
            StreamUtils.WriteString(aBinaryWriter, FShortName);

            //FIsLeaf
            if (fIsLeaf != null)
            {
                StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_IsLeaf);
                StreamUtils.WriteBoolean(aBinaryWriter, fIsLeaf == true);
            }

            // FMDXLevelIndex 
            if (FMDXLevelIndex >= 0)
            {
                StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_LevelIndex);
                StreamUtils.WriteInt32(aBinaryWriter, FMDXLevelIndex);
            }

            if (FChildrenCount >= 0)
            {
                StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_ChildrenCount);
                StreamUtils.WriteInt32(aBinaryWriter, FChildrenCount);
            }

            if (_data is byte[])
            {
                var ms = new MemoryStream();
                var bytes = _data as byte[];
                ms.Write(bytes, 0, bytes.Length);
                ms.Flush();
                StreamUtils.WriteStream(aBinaryWriter, ms, Tags.tgCubeMember_Bytes);
            }

            //  fID
            StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_ID);
            StreamUtils.WriteInt32(aBinaryWriter, fID);

            //  FParent - doesn't store 

            //  FDescription
            if (!string.IsNullOrEmpty(FDescription))
            {
                StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_Description);
                StreamUtils.WriteString(aBinaryWriter, FDescription);
            }

            //  FRaggedVirtual
            if (FRaggedVirtual)
                StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_Ragged);

            //  FStringID
            if (!string.IsNullOrEmpty(FStringID))
            {
                StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_StringID);
                StreamUtils.WriteString(aBinaryWriter, FStringID);
            }

            //  FStringParentID
            if (!string.IsNullOrEmpty(FStringParentID))
            {
                StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_StringParentID);
                StreamUtils.WriteString(aBinaryWriter, FStringParentID);
            }

            //  FHideSystemGeneratedMember
            if (FHideSystemGeneratedMember)
                StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_HideSystemGeneratedMember);

            //  FIsTotal
            if (fIsTotal)
                StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_IsTotal);

            //  FRank
            StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_Rank);
            StreamUtils.WriteDouble(aBinaryWriter, FRank);

            //  FHideSystemGeneratedMember
            if (IsMDXCalculated)
                StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_IsMDXCalculated);

            //  //  fInfoAttributes: array of Variant;
            //  if length(fInfoAttributes) > 0 then
            //  begin
            //    WriteTag(Stream, tgCubeMember_InfoAttributes);
            //    WriteInteger(Stream, length(fInfoAttributes));
            //    for i := 0 to high(fInfoAttributes) do WriteVariant(Stream, fInfoAttributes[i]);
            //  end;

            StreamUtils.WriteTag(aBinaryWriter, Tags.tgCubeMember_EOT);
        }

        internal void ReadStream(BinaryReader aBinaryReader)
        {
            var _exit = false;
            do
            {
                var Tag = StreamUtils.ReadTag(aBinaryReader);
                switch (Tag)
                {
                    case Tags.tgCubeMember_DisplayName:
                        fDisplayName = StreamUtils.ReadString(aBinaryReader);
                        break;
                    case Tags.tgCubeMember_UniqueName:
                        FUniqueName = StreamUtils.ReadString(aBinaryReader);
                        break;
                    case Tags.tgCubeMember_ShortName:
                        FShortName = StreamUtils.ReadString(aBinaryReader);
                        break;
                    case Tags.tgCubeMember_ID:
                        fID = StreamUtils.ReadInt32(aBinaryReader);
                        break;
                    case Tags.tgCubeMember_ChildrenCount:
                        FChildrenCount = StreamUtils.ReadInt32(aBinaryReader);
                        break;
                    case Tags.tgCubeMember_Description:
                        FDescription = StreamUtils.ReadString(aBinaryReader);
                        break;
                    case Tags.tgCubeMember_Ragged:
                        FRaggedVirtual = true;
                        break;
                    case Tags.tgCubeMember_StringID:
                        FStringID = StreamUtils.ReadString(aBinaryReader);
                        break;
                    case Tags.tgCubeMember_StringParentID:
                        FStringParentID = StreamUtils.ReadString(aBinaryReader);
                        break;
                    case Tags.tgCubeMember_HideSystemGeneratedMember:
                        FHideSystemGeneratedMember = true;
                        break;
                    case Tags.tgCubeMember_Rank:
                        FRank = StreamUtils.ReadDouble(aBinaryReader);
                        break;
                    case Tags.tgCubeMember_LevelIndex:
                        FMDXLevelIndex = StreamUtils.ReadInt32(aBinaryReader);
                        break;
                    case Tags.tgCubeMember_Bytes:
                        _data = StreamUtils.ReadBytes(aBinaryReader);
                        break;
                    case Tags.tgCubeMember_IsTotal:
                        fIsTotal = true;
                        break;
                    case Tags.tgCubeMember_IsLeaf:
                        fIsLeaf = StreamUtils.ReadBoolean(aBinaryReader);
                        break;
                    case Tags.tgCubeMember_IsMDXCalculated:
                        IsMDXCalculated = true;
                        break;
                    case Tags.tgCubeMember_EOT:
                        _exit = true;
                        break;
                    default:
                        throw new Exception("Unknow tag: " + Tag);
                }
            } while (!_exit);
        }

        /// <summary>
        ///     Returns an unique name of the member
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return UniqueName;
        }

        /// <summary>Returns the Grid member corresponding to the specified Cube member.</summary>
        /// <returns>The Member object</returns>
        /// <param name="ALevel">The grid hierarchy level</param>
        public Member RetrieveGridMember(Level ALevel)
        {
            return ALevel.FStaticMembers[fID];
        }

        /// <summary>
        ///     Returns an attribute value for the specified member according to the level
        ///     attributes list index defined by the CubeLevel.InfoAttributes property.
        /// </summary>
        public string GetAttributeValue(string attributeName)
        {
            if (fInfoAttributes == null)
                fInfoAttributes = new Dictionary<string, object>();
            object o;
            if (!fInfoAttributes.TryGetValue(attributeName, out o))
            {
                o = FHierarchy.Dimension.Cube.RetrieveInfoAttribute(this, attributeName);
                fInfoAttributes.Add(attributeName, o);
            }
            return o == null ? "" : o.ToString();
        }

        public object GetAttributeAsObject(string attributeName)
        {
            if (fInfoAttributes == null)
                fInfoAttributes = new Dictionary<string, object>();
            object o;
            if (!fInfoAttributes.TryGetValue(attributeName, out o))
            {
                o = FHierarchy.Dimension.Cube.RetrieveInfoAttribute(this, attributeName);
                fInfoAttributes.Add(attributeName, o);
            }
            return o == null ? null : o;
        }

        internal void SetParent(CubeMember AParent)
        {
            FParent = AParent;
        }

        internal virtual void SetUniqueName(string AUniqueName)
        {
            FUniqueName = AUniqueName;
        }

        internal virtual void SetShortName(string AShortName)
        {
            FShortName = AShortName;
        }

        internal void SetRank(int Value)
        {
            FRank = Value;
        }

        #region IDescriptionable Members

        string IDescriptionable.DisplayName => DisplayName;

        string IDescriptionable.Description => Description;

        string IDescriptionable.UniqueName => UniqueName;

        #endregion
    }
}