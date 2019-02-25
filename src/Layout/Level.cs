using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>Hierarchy levels are described by the objects of this type</summary>
    /// <remarks>
    ///     The instance class is created not only for hierarchies but for the Grid measures
    ///     whose list is represented by the Measures class. In this case, measures, not hierarchy
    ///     members, are the members of the level
    /// </remarks>
    public class Level : IStreamedObject, IDescriptionable
    {
        internal Hierarchy FHierarchy;
        [NonSerialized] internal CubeLevel FCubeLevel;
        [NonSerialized] internal Members FMembers = new Members();

        internal short fIndex;
        internal Measures FMeasures;

        [NonSerialized] internal SortedList<string, Member> FUniqueNamesArray = new SortedList<string, Member>();

        internal SortedList<string, Member> CreateSortedList(int ACount)
        {
            return new SortedList<string, Member>(ACount);
        }

        [NonSerialized] internal List<Member> FStaticMembers = new List<Member>();

        internal List<Member> CreateListMember(int ACount)
        {
            return new List<Member>(ACount);
        }

        internal List<Member> CreateListMember(IEnumerable<Member> AMembers)
        {
            return new List<Member>(AMembers);
        }

        internal int FDepth;

        internal Filter FFilter;
        internal MembersSortType FSortType = MembersSortType.msTypeRelated;

#if DEBUG
        private bool _FVisible;

        internal bool FVisible
        {
            get => _FVisible;
            set
            {
                if (_FVisible == value)
                    return;

                _FVisible = value;
            }
        }
#else
        internal bool FVisible = false;
#endif

        public bool Visible
        {
            get => FVisible;
            set
            {
                if (Grid.CellsetMode == CellsetMode.cmGrid)
                    return;

                if (FVisible != value && Hierarchy != null)
                {
                    FVisible = value;
                    var allHidden = Hierarchy.Levels.All(x => !x.Visible);
                    foreach (var l in Hierarchy.Levels)
                        if (l.Visible)
                        {
                            allHidden = false;
                            break;
                        }
                    if (allHidden)
                    {
                        var la = Grid.AxesLayout.ContainInLayoutArea(Hierarchy);
                        Grid.PivotingOut(Hierarchy, la);
                    }
                    Grid.FLayout.CheckExpandedLevels();
                    if (!Grid.IsUpdating)
                        Grid.CellSet.Rebuild();
                    Grid.EndChange(GridEventType.gePivotAction, this);
                }
            }
        }

        public override string ToString()
        {
            return UniqueName;
        }

        internal void SetSortPosition()
        {
            for (var i = 0; i < FMembers.Count; i++)
                FMembers[i].FSortPosition = i;
        }

        internal void CreateNewMembers()
        {
            var newmembers = new List<Member>();
            var b = false;

            for (var i = FStaticMembers.Count; i < FCubeLevel.FetchedMembers.Count; i++)
            {
                b = true;
                var cm = FCubeLevel.FetchedMembers[i];
                var cmparent = cm.Parent;
                if (cmparent != null && cmparent.FParentLevel == FCubeLevel) // the child member on the current level
                {
                    Member parent = null;
                    parent = FStaticMembers[cmparent.fID];
                    var m = new Member(this, parent, cm);
                    newmembers.Add(m);
                    m.FRank = cm.FRank;
                }
                else
                {
                    Member parent = null;
                    if (cmparent != null)
                        parent = FHierarchy.FLevels[fIndex - 1].FStaticMembers[cmparent.fID];
                    var m = new Member(this, parent, cm);
                    newmembers.Add(m);
                    m.FRank = cm.FRank;
                    FMembers.Add(m);
                }
            }
            if (b)
            {
                foreach (var m in FStaticMembers)
                {
                    m.FRank = m.CubeMember.FRank;
                }
                Sort();
            }
            Grid.Cube.CheckAreLeaves(newmembers);
        }

        private List<Member> newmembers2;

        internal void CreateNewMembersLight(bool isFinal)
        {
            if (!isFinal)
            {
                if (newmembers2 == null) newmembers2 = new List<Member>();
                for (var i = FStaticMembers.Count; i < FCubeLevel.FetchedMembers.Count; i++)
                {
                    var cm = FCubeLevel.FetchedMembers[i];
                    var cmparent = cm.Parent;
                    if (cmparent != null && cmparent.FParentLevel == FCubeLevel
                    ) // the child member on the current level
                    {
                        Member parent = null;
                        parent = FStaticMembers[cmparent.fID];
                        var m = new Member(this, parent, cm);
                        newmembers2.Add(m);
                        m.FRank = cm.FRank;
                    }
                    else
                    {
                        Member parent = null;
                        if (cmparent != null)
                            parent = FHierarchy.FLevels[fIndex - 1].FStaticMembers[cmparent.fID];
                        var m = new Member(this, parent, cm);
                        newmembers2.Add(m);
                        m.FRank = cm.FRank;
                        FMembers.Add(m);
                    }
                }
            }
            if (isFinal)
            {
                foreach (var m in FStaticMembers)
                    m.FRank = m.CubeMember.FRank;
                Sort();
                Grid.Cube.CheckAreLeaves(newmembers2);
                newmembers2 = null;
            }
        }

        /// <remarks>Returns True, if at least one member is fetched for the specified level.</remarks>
        public bool Initialized => FMembers.Count > 0;

        /// <summary>
        ///     Fetches all members of the specified hierarchy level from the server. Before
        ///     calling the method, all members of all previous levels must to be already
        ///     fetched
        /// </summary>
        /// <remarks>
        ///     If some of the members of the specified level have already been fetched, then it
        ///     fetches the rest of the members
        /// </remarks>
        public void Initialize()
        {
            if (Hierarchy == null) return;
            if (FCubeLevel.IsFullyFetched) return;
            bool hasNewMembers;
            Hierarchy.Dimension.Grid.Cube.RetrieveMembersPartial(Hierarchy.Dimension.Grid, this,
                0, -1, null, out hasNewMembers);

            //FStaticMembers = new List<Member>(FCubeLevel.FUniqueNamesArray.Count);
            FMembers.Initialize(FCubeLevel.FMembers, this, null);
            if (hasNewMembers) CreateNewMembers();
        }


        /// <summary>
        ///     Defines the page view settings (whether a mode is activated, and the number of
        ///     items per page) for the specified hierarchy level.
        /// </summary>
        public PagerSettings PagerSettings { get; }

        internal void RestoreAfterSerialization(OlapControl grid)
        {
            if (FHierarchy != null)
            {
                FCubeLevel = FHierarchy.FCubeHierarchy.Levels[FHierarchy.Levels.IndexOf(this)];
                FMembers.RestoreAfterSerialization(FCubeLevel.Members);
            }
            else
            {
                FMembers.RestoreAfterSerialization(null);
            }

            PagerSettings.FGrid = grid;
        }

        internal Level PreviosLevel
        {
            get
            {
                if (Hierarchy == null)
                    return null;
                if (Index - 1 < 0)
                    return null;
                return Hierarchy.Levels[Index - 1];
            }
        }

        internal Level NextLevel
        {
            get
            {
                if (Hierarchy == null) return null;
                if (Index + 1 == Hierarchy.Levels.Count) return null;
                return Hierarchy.Levels[Index + 1];
            }
        }

        internal OlapControl GetGrid()
        {
            return FMeasures == null ? FHierarchy.Dimension.Grid : FMeasures.Grid;
        }

        internal void RegisterMembers()
        {
            var n = FCubeLevel != null ? FCubeLevel.FMembersCount : 0;
            for (var i = 0; i < FUniqueNamesArray.Count; i++)
                FUniqueNamesArray.Values[i].fVirtualID = i + n;
            FDepth = FHierarchy.GetDefaultLevelDepth();
            foreach (var m in FMembers)
                FDepth = Math.Max(FDepth, m.ChildrenDepth);
            FMembers.UpdateRanks(this);
            Sort();
        }

        internal Level(Hierarchy AHierarchy, Measures AMeasures)
        {
            FHierarchy = AHierarchy;
            FMeasures = AMeasures;
            FDepth = AHierarchy == null ? 1 : AHierarchy.GetDefaultLevelDepth();

            PagerSettings = new PagerSettings(false, 0, null);
            FSortType = MembersSortType.msTypeRelated;
        }

        internal Level(Hierarchy AHierarchy, CubeLevel ACubeLevel, Measures AMeasures)
        {
            FHierarchy = AHierarchy;
            FDepth = AHierarchy == null ? 1 : AHierarchy.GetDefaultLevelDepth();
            FCubeLevel = ACubeLevel;

            if (FHierarchy != null)
                PagerSettings = new PagerSettings(AHierarchy.Dimension.Grid.AllowPaging,
                    AHierarchy.Dimension.Grid.LinesInPage, AHierarchy.Dimension.Grid);
            else
                PagerSettings = new PagerSettings(false, 0, AMeasures.Grid);

            FMeasures = AMeasures;
            if (FCubeLevel != null)
            {
                FStaticMembers = CreateListMember(FCubeLevel.FUniqueNamesArray.Count);
                AHierarchy.Dimension.Grid.FEngine.FLevelsList[ACubeLevel.ID] = this;
            }
            else
            {
                FStaticMembers = CreateListMember(0);
                FMembers.Initialize(null, this, null);
                for (var i = 0; i < FMeasures.Count; i++)
                {
                    var M = new Member(this, null, null);
                    var m_ = FMeasures[i];
                    M.DisplayName = m_.FDisplayName;
                    M.SetUniqueName(m_.UniqueName);
                    M.FDescription = m_.FDescription;
                    M.FMemberType = MemberType.mtMeasure;
                    M.FVisible = m_.FVisible;
                    M.fVirtualID = i;
                    Members.Add(M);
                    FUniqueNamesArray.Add(M.UniqueName, M);
                    for (var j = 0; j < m_.ShowModes.Count; j++)
                    {
                        var M1 = new Member(this, null, null);
                        var sm = m_.ShowModes[j];
                        M1.DisplayName = sm.Caption;
                        M1.SetUniqueName(Guid.NewGuid().ToString());
                        M1.FDescription = "";
                        M1.FMemberType = MemberType.mtMeasureMode;
                        FUniqueNamesArray.Add(M1.UniqueName, M1);
                        M1.FVisible = sm.Visible;
                        M1.fVirtualID = j;
                        M.Children.Add(M1);
                        M1.FParent = M;
                        M1.FDepth = 1;
                    }
                }
            }
            if (FCubeLevel != null && FCubeLevel.FSourceHierarchy != null)
            {
                TotalCaption = FCubeLevel.FSourceHierarchy.TotalCaption;
                var H = FHierarchy.Dimension.Hierarchies.Find(FCubeLevel.FSourceHierarchy.UniqueName);
                if (H != null) FSortType = H.SortType;
            }
            else
            {
                if (AHierarchy != null) TotalCaption = AHierarchy.TotalCaption;
            }
        }

        internal void Sort()
        {
            Members.Sort(FHierarchy.Dimension.Grid, FSortType);
        }

        internal IList<GroupMember> CreateGroupList(Member AParent)
        {
            if (AParent != null && AParent.MemberType == MemberType.mtGroup) return new List<GroupMember>();
            return FUniqueNamesArray.Values.Where(item => item is GroupMember && item.Parent == AParent)
                .Cast<GroupMember>().ToList();
        }

        /// <summary>Searches for a member of the specified hierarchy level by its name.</summary>
        public Member FindMemberByName(string MemberName)
        {
            Member m;
            if (FUniqueNamesArray.TryGetValue(MemberName, out m)) return m;
            if (FCubeLevel != null)
            {
                CubeMember M;
                if (FCubeLevel.FUniqueNamesArray.TryGetValue(MemberName, out M))
                    return FStaticMembers[M.ID];
            }
            m = Members.FindMemberByName2(MemberName);
            if (m != null)
                return m;
            m = Members.FindMemberByName1(MemberName);
            if (m != null)
                return m;

            for (var i = 0; i < FUniqueNamesArray.Count; i++)
            {
                m = FUniqueNamesArray.Values[i];
                if (string.Compare(m.DisplayName, MemberName, true) == 0)
                    return m;
            }
            for (var i = 0; i < FStaticMembers.Count; i++)
            {
                m = FStaticMembers[i];
                if (string.Compare(m.DisplayName, MemberName, true) == 0)
                    return m;
            }
            return null;
        }

        /// <summary>
        ///     Searches for a member with the unique name from the list specified by the
        ///     AUniqueMemberName parameter or null, if there's no such member.
        /// </summary>
        public Member FindMember(string AUniqueMemberName)
        {
            if (FCubeLevel != null)
            {
                if (Members.Count == 0 && PreviosLevel != null)
                {
                    var l1 = PreviosLevel;
                    var lmv1 = PreviosLevel.Members
                        .Select(x => x.CubeMember)
                        .Where(item => !item.IsFullyFetched).ToList();

                    if (lmv1.Count > 0)
                        Grid.Cube.RetrieveDescendants(Grid, lmv1, CubeLevel);
                }

                CubeMember M;
                if (FCubeLevel.FUniqueNamesArray.TryGetValue(AUniqueMemberName, out M))
                    return FStaticMembers[M.ID];
            }
            Member m;
            if (FUniqueNamesArray.TryGetValue(AUniqueMemberName, out m))
                return m;
            return null;
        }

        /// <summary>
        ///     Returns True, if the specified level contains at least one member with the
        ///     property Visible set to False.
        /// </summary>
        public bool IsFiltered(Member ParentMember)
        {
            foreach (var m in FMembers)
                if ((ParentMember == null || ParentMember == m.Parent) && m.Filtered) return true;
            return false;
        }

        /// <remarks>
        ///     Used by the internal procedures of the RadarCube library, and not recommended for
        ///     external use
        /// </remarks>
        /// <summary>Returns a hierarchicy member with the passed ID from the specified level</summary>
        public Member GetMemberByID(int ID)
        {
            var n = FCubeLevel == null ? 0 : FCubeLevel.FMembersCount - 1;

            return ID > n ? FUniqueNamesArray.Values[ID - n - 1] : FStaticMembers[ID];
        }

        /// <summary>A level name to be displayed in the Grid.</summary>
        public string DisplayName => FCubeLevel != null ? FCubeLevel.DisplayName : RadarUtils.GetResStr("rsMeasures");

        /// <summary>
        ///     References to the instance of the Hierarchy class that includes the specified
        ///     level.
        /// </summary>
        /// <remarks>Can be null, if the specified level contains the list of measures</remarks>
        public Hierarchy Hierarchy => FHierarchy;

        /// <summary>
        ///     References to the instance of the Measures object, if the specified level
        ///     contains measures as its own members; and null, if the specified level contains
        ///     hierarchy members.
        /// </summary>
        public Measures Measures => FMeasures;

        /// <summary>
        ///     Description of the level that appears as a pop-up window (tooltip) when the
        ///     cursor is pointed at the name of the level in the Grid.
        /// </summary>
        public string Description => FCubeLevel != null ? FCubeLevel.Description : "";

        /// <summary>
        ///     Contains a unique string level identifier.
        /// </summary>
        /// <remarks>
        ///     The value of this property always equals the value of the CubeLevel.UniqueName
        ///     property
        /// </remarks>
        public string UniqueName => FCubeLevel != null ? FCubeLevel.UniqueName : "Measures";

        /// <summary>
        ///     Specifies the level members type (string, numeric, datetime or measure).
        /// </summary>
        public HierarchyDataType LevelType => FCubeLevel != null ? FCubeLevel.FLevelType : HierarchyDataType.htMeasures;

        public string FormatString => FCubeLevel != null ? FCubeLevel.FFormatString : "";

        /// <summary>
        ///     <para>Defines the depth of the Parent-Child hierarchy on the specified level</para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Equals 1, if a given level does not contain the Parent-Child hierarchy within
        ///         itself.
        ///     </para>
        ///     <para>
        ///         The level depth can be changed during the OLAP-analysis, for example, when
        ///         hierarchy members are relocated to or from the group
        ///     </para>
        /// </remarks>
        public int Depth => FDepth;

        /// <summary>Returns the list of members of the specified level.</summary>
        /// <remarks>
        ///     If the specified level contains the Parent-Child hierarchy, then this property
        ///     returns the list of roots of this level
        /// </remarks>
        public Members Members => FMembers;

        /// <summary>
        ///     Reference to the corresponding instance of the CubeLevel class on the Cube
        ///     level.
        /// </summary>
        /// <remarks>Can be null, if the specified level contains a list of measures</remarks>
        public CubeLevel CubeLevel => FCubeLevel;

        /// <summary>
        ///     Gets or seth the "Total" caption of this level.
        /// </summary>
        public string TotalCaption { get; set; }

        /// <summary>
        ///     Indicates where in the Grid the Total-member for the specified level is located
        ///     (first, last or hidden).
        /// </summary>
        public TotalAppearance TotalAppearance =>
            FHierarchy != null ? FHierarchy.FTotalAppearance : TotalAppearance.taInvisible;

        /// <summary>A unique numeric level identifier used in internal procedures of RadarCube</summary>
        /// <remarks>
        ///     <para>Is not recommended for use</para>
        /// </remarks>
        public int ID => FCubeLevel != null ? FCubeLevel.fID : -1;

        /// <summary>The number of the level members.</summary>
        /// <remarks>
        ///     <para>
        ///         If the members of the hierarchy level do not contain the same-level Children,
        ///         the value of this property is the same as the value of the Members.Count
        ///         property.
        ///     </para>
        /// </remarks>
        public int CompleteMembersCount
        {
            get
            {
                if (FCubeLevel != null)
                {
                    if (FCubeLevel.FMembersCount < 0)
                        throw new Exception(DisplayName + ": number of members hasn't been initialized yet");
                    return FCubeLevel.FMembersCount + FUniqueNamesArray.Count;
                }
                return FUniqueNamesArray.Count;
            }
        }

        /// <summary>
        ///     Defines the context filter applied to the level.
        /// </summary>
        /// <remarks>See the Filter class description for the details</remarks>
        public Filter Filter
        {
            get => FFilter;
            set
            {
                if (FFilter == value) return;
                if (value != null && !Grid.Cube.IsFilterAllowed(value))
                    throw new Exception("This type of the context filter is not supported by RadarCube yet.");
                FHierarchy.BeginUpdate();
                if (FHierarchy.Filtered)
                    FHierarchy.ResetFilter();
                FFilter = value;
                FHierarchy.EndUpdate();
                FHierarchy.UpdateFilterState(true);
            }
        }

        /// <summary>
        ///     The sorting method (ascending, descending or default) applied to the level
        ///     members.
        /// </summary>
        /// <remarks>
        ///     The standard sorting methods can be overriden by setting the value of the
        ///     Hierarchy.OverrideSortMethods property to True, and by handling the
        ///     OlapGrid.OnMemberSort event.
        /// </remarks>
        public MembersSortType SortType
        {
            get => FSortType;
            set
            {
                if (FSortType != value)
                {
                    FSortType = value;
                    Sort();
                    if (FHierarchy.Dimension.Grid.IsUpdating == false) FHierarchy.Dimension.Grid.CellSet.Rebuild();
                }
            }
        }

        /// <summary>
        ///     References to the Grid that includes the hierarchy with the specified
        ///     level.
        /// </summary>
        public OlapControl Grid => FMeasures == null ? FHierarchy.Dimension.Grid : FMeasures.Grid;

        /// <summary>
        ///     The ordinal number of the specified level in the level collection of the
        ///     specified hierarchy.
        /// </summary>
        public short Index => fIndex;


        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgLevel);

            if (FDepth != 1)
            {
                StreamUtils.WriteTag(writer, Tags.tgLevel_Depth);
                StreamUtils.WriteInt32(writer, FDepth);
            }

            if (FFilter != null)
                StreamUtils.WriteStreamedObject(writer, FFilter, Tags.tgLevel_Filter);

            StreamUtils.WriteTag(writer, Tags.tgLevel_Index);
            StreamUtils.WriteInt32(writer, Convert.ToInt32(fIndex));

            StreamUtils.WriteStreamedObject(writer, PagerSettings, Tags.tgLevel_PagerSettings);

            if (FSortType != MembersSortType.msTypeRelated)
            {
                StreamUtils.WriteTag(writer, Tags.tgLevel_SortType);
                StreamUtils.WriteInt32(writer, (int) FSortType);
            }

            if (Visible)
                StreamUtils.WriteTag(writer, Tags.tgLevel_Visible);

            StreamUtils.WriteTag(writer, Tags.tgLevel_TotalCaption);
            StreamUtils.WriteString(writer, TotalCaption);

            StreamUtils.WriteTag(writer, Tags.tgLevel_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgLevel);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgLevel_Depth:
                        FDepth = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgLevel_Filters:
                        StreamUtils.SkipValue(reader);
                        break;
                    case Tags.tgLevel_Index:
                        fIndex = Convert.ToInt16(StreamUtils.ReadInt32(reader));
                        break;
                    case Tags.tgLevel_PagerSettings:
                        StreamUtils.ReadStreamedObject(reader, PagerSettings);
                        break;
                    case Tags.tgLevel_SortType:
                        FSortType = (MembersSortType) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgLevel_TotalCaption:
                        TotalCaption = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgLevel_Filter:
                        FFilter = new Filter(this);
                        StreamUtils.ReadStreamedObject(reader, FFilter);
                        break;
                    case Tags.tgLevel_Visible:
                        FVisible = true;
                        break;
                    case Tags.tgLevel_EOT:
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