using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.CellSet.Md;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.CubeStructure
{
    /// <summary>Describes the level of the hierarchy.</summary>
    /// <remarks>
    ///     <para>
    ///         Typically levels of a multilevel hierarchy are themselves attribute (flat)
    ///         hierarchies. They belong to the same dimension as the multilevel one they pertain
    ///         to.
    ///     </para>
    ///     <para>
    ///         The hierarchy relations among hierarchy members are described by the
    ///         CubeMember.Parent and CubeMember.Children properties.
    ///     </para>
    /// </remarks>
    //[Serializable]
    public class CubeLevel : IStreamedObject, IDescriptionable
    {
        internal List<MDXLevel> _MDXLevels;
        internal BIMembersType FBIMembersType = BIMembersType.ltNone;

        [NonSerialized] internal BucketCollection fBuckets = null; // internal BucketCollection fBuckets = null;

        internal CubeHierarchy FCubeHierarchy;

        [NonSerialized] internal SortedList<string, CubeMember> FDatabaseIDs;

        [NonSerialized] internal object fDataTable = null; // internal TDataTable fDataTable

        internal string fDataTableName = null;

        internal string FDescription;
        internal bool fDiscreted = false;
        internal int fDisplayField = -1;
        internal string FDisplayName;

        [NonSerialized] internal List<CubeMember> FetchedMembers = new List<CubeMember>();

        private int FFetchedCount;
        //---------------------------------------

        //private
        internal int FFirstLevelMembersCount; // for MSAS only - amount of members at first parent-child hierarchy level

        internal string FFormatString;

        //        [NonSerialized]
        //        internal SortedList<string, CubeMember> FShortNamesArray = new SortedList<string, CubeMember>();
        internal int fID = -1;

        internal int fIDField = -1;
        internal InfoAttributes FInfoAttributes = new InfoAttributes();
        internal int fLevelIndex = -1;
        internal HierarchyDataType FLevelType;

        [NonSerialized] internal CubeMembers FMembers = new CubeMembers();

        internal int FMembersCount = -1; // the real amount of level members

        [NonSerialized] private int fNewID;

        internal int fParentField = -1;
        internal CubeLevel fParentLevel = null; // internal TOLAPCubeLevel fParentLevel = null;

        [NonSerialized]
        internal object fPathFromFactTable = null; // internal List<TDataRelation> fPathFromFactTable = null;

        internal bool fProcessParentChild = false;

        [NonSerialized] internal List<Bucket> fRecordToBucketArray = null;

        internal CubeHierarchy FSourceHierarchy;

        [NonSerialized] internal string FSourceHierarchyName;

        internal string FUniqueName;

        [NonSerialized]
        internal Dictionary<string, CubeMember> FUniqueNamesArray = new Dictionary<string, CubeMember>();

        // From TOLAPCubeLevel ------------------
        internal bool fUseOnReadFactTable = true;

        internal int MDXLevelIndex;

        internal CubeLevel(CubeHierarchy AHierarchy, CubeHierarchy ASourceHierarchy)
        {
            FCubeHierarchy = AHierarchy;
            FSourceHierarchy = ASourceHierarchy;

            if (SourceHierarchy != null)
            {
                FDisplayName = SourceHierarchy.DisplayName;
                FUniqueName = SourceHierarchy.UniqueName;

                if (AHierarchy.Dimension.Cube.GetProductID() == RadarUtils.GetCurrentDesktopProductID())
                    FUniqueName += "1";

                FDescription = SourceHierarchy.Description;
                FBIMembersType = SourceHierarchy.BIMembersType;
                FInfoAttributes = SourceHierarchy.InfoAttributes.Clone();
                FFormatString = SourceHierarchy.FormatString;
            }
            else
            {
                FDisplayName = AHierarchy.DisplayName;
                FUniqueName = AHierarchy.UniqueName;
                FDescription = AHierarchy.Description;
                FBIMembersType = AHierarchy.BIMembersType;
                FInfoAttributes = AHierarchy.InfoAttributes.Clone();
                FFormatString = AHierarchy.FormatString;
            }
        }

        internal CubeLevel(CubeHierarchy AHierarchy)
        {
            FCubeHierarchy = AHierarchy;
            FInfoAttributes = AHierarchy.InfoAttributes.Clone();
        }

        internal CubeLevel(CubeHierarchy AHierarchy, CubeHierarchy ASourceHierarchy,
            string displayName, string description, string uniqueName)
        {
            FCubeHierarchy = AHierarchy;
            FSourceHierarchy = ASourceHierarchy;

            FDisplayName = displayName;
            FDescription = description;
            FUniqueName = uniqueName;
            if (FSourceHierarchy != null)
            {
                FBIMembersType = FSourceHierarchy.BIMembersType;
                FLevelType = FSourceHierarchy.FTypeOfMembers;
                FInfoAttributes = SourceHierarchy.InfoAttributes.Clone();
                FFormatString = SourceHierarchy.FormatString;
            }
            else
            {
                FBIMembersType = FCubeHierarchy.BIMembersType;
                FLevelType = FCubeHierarchy.FTypeOfMembers;
                FInfoAttributes = FCubeHierarchy.InfoAttributes.Clone();
                FFormatString = FCubeHierarchy.FormatString;
            }
        }

        internal bool IsDiscretizationMode => SourceHierarchy.FDiscretizationMethod != DiscretizationMethod.dmNone;

        internal bool NeedDiscretization => IsDiscretizationMode && !fDiscreted;

        internal SortedList<string, CubeMember> DatabaseIDs
        {
            get => FDatabaseIDs;
            set => FDatabaseIDs = value;
        }

        internal Dictionary<string, CubeMember> UniqueNamesArray => FUniqueNamesArray;

        internal bool IsFullyFetched
        {
            get
            {
                if (FMembersCount <= 0)
                    return false;
                return FMembers.Count == FMembersCount;
            }
        }

        /// <summary>A level name displayed in the Grid.</summary>
        public string DisplayName
        {
            get => FDisplayName;
            set => FDisplayName = value;
        }

        /// <summary>
        ///     Reference to the hierarchy containing the current level.
        /// </summary>
        /// <remarks>
        ///     Don't confuse this property with the SourceHierarchy that indicates a multilevel
        ///     hierarchy on which basis the current level was created
        /// </remarks>
        public CubeHierarchy Hierarchy => FCubeHierarchy;

        /// <summary>
        ///     Indicates an attribute hierarchy on the basis of which the level was
        ///     created
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Don't confuse this property with Hierarchy, which indicates the hierarchy
        ///         that contains the specified level, and thus, unlike SourceHierarchy, can never
        ///         equal null.
        ///     </para>
        ///     <para>
        ///         Let's consider an example: suppose we have the user-composed Date hierarchy
        ///         that consists of three levels Year, Quarter and Month.
        ///     </para>
        ///     <para>
        ///         Thus, at Cube design time, this Date hierarchy of the hoUserDefined type
        ///         contains in its ChildrenList references to three hierarchies of the hoAttribute
        ///         type named Year, Quarter and Month, that belong to the same dimension as the Date
        ///         hierarchy.
        ///     </para>
        /// </remarks>
        public CubeHierarchy SourceHierarchy
        {
            get
            {
                if (FSourceHierarchy != null) return FSourceHierarchy;
                return FCubeHierarchy;
            }
        }

        /// <summary>
        ///     A detailed description of the level that appears as a tooltip window when the
        ///     cursor is pointed at the panel with the level caption in the Grid.
        /// </summary>
        public string Description
        {
            get => FDescription;
            set => FDescription = value;
        }

        /// <summary>
        ///     Contains a unique string level identifier.
        /// </summary>
        public string UniqueName => FUniqueName;

        /// <summary>
        ///     The list of root members that create the Parent-Child hierarchy within the current level, or the complete list of
        ///     the members of the level, if the level is not hierarchical.
        /// </summary>
        /// <remarks>For non-hierarchical levels the Depth property always equals 1.</remarks>
        public CubeMembers Members => FMembers;

        /// <summary>
        ///     Indicates the type of the current level's members. Is used in the default
        ///     procedures of sorting members.
        /// </summary>
        public HierarchyDataType LevelType
        {
            get => FLevelType;
            set => FLevelType = value;
        }

        /// <summary>
        ///     Reserved for the future use.
        /// </summary>
        public string FormatString => FFormatString;

        /// <summary>A unique level identifier in the list of the initialized Cube levels.</summary>
        /// <remarks>
        ///     The ID is set through the RadarCube.RegisterLevel procedure, and used in the
        ///     Cube cells' addresses
        /// </remarks>
        public int ID => fID;

        /// <summary>
        ///     A business intelligence type of level members
        /// </summary>
        [Browsable(false), DefaultValue(BIMembersType.ltNone)]
        public BIMembersType BIMembersType
        {
            get { return FBIMembersType; }
        }

        /// <summary>
        ///     Contains the list of hierarchy members attributes names.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Attributes values for every hierarchy member are available through the
        ///         CubeMember.InfoAttributes property.
        ///     </para>
        ///     <para>
        ///         For the TOLAPCube component, the information about the table fields
        ///         containing the attributes of members, is available through the
        ///         CubeHierarchy.InfoAttributes property.
        ///     </para>
        /// </remarks>
        public InfoAttributes InfoAttributes => FInfoAttributes;

        internal void CreateDatabaseIDs()
        {
            if (FDatabaseIDs == null) FDatabaseIDs = new SortedList<string, CubeMember>();
        }

        internal Bucket RecordToBucket(int index)
        {
            if (fRecordToBucketArray == null)
                return null;
            //    fRecordToBucketArray = new List<Bucket>();
            return fRecordToBucketArray[index];
        }

        internal Dictionary<string, CubeMember> FUniqueNamesArrayCreate(int ACount)
        {
            var res = new Dictionary<string, CubeMember>(ACount);
            return res;
        }

        internal void RegisterMember(CubeMember m)
        {
            m.fID = FetchedMembers.Count;
            FetchedMembers.Add(m);
        }

        private static int CompareMembersByID(CubeMember M1, CubeMember M2)
        {
            if (M1.fID > M2.fID) return 1;
            if (M1.fID < M2.fID) return -1;
            return 0;
        }

        private void DoAssignID(CubeMembers members)
        {
            foreach (var M in members)
            {
                M.fID = fNewID++;
                if (M.FChildren.Count > 0) DoAssignID(M.FChildren);
            }
        }

        internal void SortFetchedMembersByHierarchy()
        {
            // First, reassign ID for all members
            fNewID = 0;
            DoAssignID(FMembers);
            // Eventially sort FetchedMembes
            FetchedMembers.Sort(CompareMembersByID);
        }

        [OnSerializing]
        private void DoSerialize(StreamingContext context)
        {
            FFetchedCount = FetchedMembers.Count;
        }

        [OnDeserialized]
        private void DoDeserialize(StreamingContext context)
        {
            CreateFetchedMembersArray();
        }

        internal void CreateFetchedMembersArray()
        {
            //if (FetchedMembers == null) 
            FetchedMembers = new List<CubeMember>(new CubeMember[FFetchedCount]);
        }

        /// <summary>Searches for a hierarchy member on the current level by its name.</summary>
        /// <returns>The CubeMember object</returns>
        /// <remarks>Returns null, if there's no hierarchy member with such name</remarks>
        public CubeMember FindMember(string MemberName)
        {
            CubeMember m = null;
            if (FUniqueNamesArray.TryGetValue(MemberName, out m)) return m;
            foreach (var m1 in FUniqueNamesArray.Values)
                if (string.Compare(m1.FShortName, MemberName, true) == 0) return m1;
            return null;
        }

        /// <summary>Returns a unique name of the level</summary>
        public override string ToString()
        {
            return UniqueName;
        }

        /// <summary>Searches for a hierarchy member on the current level by its unique name.</summary>
        /// <returns>The CubeMember object</returns>
        /// <param name="UniqueName">The unique name of the member</param>
        public CubeMember FindMemberByUniqueName(string UniqueName)
        {
            CubeMember m = null;
            if (FUniqueNamesArray.TryGetValue(UniqueName, out m)) return m;
            return null;
        }

        /// <summary>
        ///     Returns a hierarchicy member with the passed ID from the specified
        ///     level.
        /// </summary>
        /// <returns>The CubeMember object</returns>
        public CubeMember GetMemberByID(int ID)
        {
            return FetchedMembers[ID];
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            FFetchedCount = FetchedMembers.Count;
            StreamUtils.WriteTag(writer, Tags.tgCubeLevel);

            if (FFirstLevelMembersCount > 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeLevel_FirstLevelMembersCount);
                StreamUtils.WriteInt32(writer, FFirstLevelMembersCount);
            }

            StreamUtils.WriteTag(writer, Tags.tgCubeLevel_DisplayName);
            StreamUtils.WriteString(writer, FDisplayName);

            StreamUtils.WriteTag(writer, Tags.tgCubeLevel_UniqueName);
            StreamUtils.WriteString(writer, FUniqueName);

            if (!string.IsNullOrEmpty(FDescription))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeLevel_Description);
                StreamUtils.WriteString(writer, FDescription);
            }

            StreamUtils.WriteTag(writer, Tags.tgCubeLevel_MDXLevelIndex);
            StreamUtils.WriteInt32(writer, MDXLevelIndex);

            StreamUtils.WriteTag(writer, Tags.tgCubeLevel_MembersCount);
            StreamUtils.WriteInt32(writer, FMembersCount);

            StreamUtils.WriteTag(writer, Tags.tgCubeLevel_LevelType);
            StreamUtils.WriteByte(writer, (byte) FLevelType);

            if (!string.IsNullOrEmpty(FFormatString))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeLevel_FormatString);
                StreamUtils.WriteString(writer, FFormatString);
            }

            StreamUtils.WriteTag(writer, Tags.tgCubeLevel_FetchedCount);
            StreamUtils.WriteInt32(writer, FFetchedCount);

            StreamUtils.WriteTag(writer, Tags.tgCubeLevel_ID);
            StreamUtils.WriteInt32(writer, fID);

            if (FInfoAttributes != null)
                for (var i = 0; i < FInfoAttributes.Count; i++)
                {
                    StreamUtils.WriteTag(writer, Tags.tgCubeLevel_InfoAttributeClass);
                    StreamUtils.WriteStreamedObject(writer, FInfoAttributes[i]);
                }

            if (FBIMembersType != BIMembersType.ltNone)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeLevel_BIMembersType);
                StreamUtils.WriteByte(writer, (byte) FBIMembersType);
            }

            if (FSourceHierarchy != null)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeLevel_SourceHierarchy);
                StreamUtils.WriteString(writer, FSourceHierarchy.UniqueName);
            }

            StreamUtils.WriteTag(writer, Tags.tgCubeLevel_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgCubeLevel);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgCubeLevel_FirstLevelMembersCount:
                        FFirstLevelMembersCount = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeLevel_DisplayName:
                        FDisplayName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeLevel_UniqueName:
                        FUniqueName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeLevel_Description:
                        FDescription = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeLevel_MDXLevelIndex:
                        MDXLevelIndex = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeLevel_MembersCount:
                        FMembersCount = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeLevel_LevelType:
                        FLevelType = (HierarchyDataType) StreamUtils.ReadByte(reader);
                        break;
                    case Tags.tgCubeLevel_FormatString:
                        FFormatString = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeLevel_SourceHierarchy:
                        FSourceHierarchyName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeLevel_FetchedCount:
                        FFetchedCount = StreamUtils.ReadInt32(reader);
                        CreateFetchedMembersArray();
                        break;
                    case Tags.tgCubeLevel_ID:
                        fID = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeLevel_InfoAttributeClass:
                        var ti = new InfoAttribute();
                        StreamUtils.ReadStreamedObject(reader, ti);
                        FInfoAttributes.Add(ti);
                        break;
                    case Tags.tgCubeLevel_BIMembersType:
                        FBIMembersType = (BIMembersType) StreamUtils.ReadByte(reader);
                        break;
                    case Tags.tgCubeLevel_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        #endregion

        #region IDescriptionable Members

        string IDescriptionable.DisplayName => DisplayName;

        string IDescriptionable.Description => Description;

        string IDescriptionable.UniqueName => UniqueName;

        #endregion
    }
}