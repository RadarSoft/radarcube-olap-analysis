using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>
    ///     A complementary class of the Grid level for the CubeHierarchy class. It adds
    ///     properties and methods that allow you to set up hierarchy appearance for an end
    ///     user
    /// </summary>
    public class Hierarchy : IStreamedObject, IDescriptionable
    {
        internal bool FUnfetchedMembersVisibility = true;
        internal string FUniqueName;
        internal string FDisplayName;
        [NonSerialized] internal CubeHierarchy FCubeHierarchy;

        internal Levels FLevels { get; set; }

        internal DimensionCalculatedMembers FCalculatedMembers;
        internal MembersSortType FSortType = MembersSortType.msTypeRelated;
        internal string FFormatString;

        internal TotalAppearance FTotalAppearance = TotalAppearance.taLast;
        internal bool FAllowResort = true;
        internal bool FAllowFilter = true;
        internal bool FAllowPopupOnLabels = true;
        internal bool FAllowPopupOnCaptions = true;
        internal bool FAllowRegroup = true;
        internal bool FAllowChangeTotalAppearance = true;
        internal bool FVisible = true;
        internal int FUpdatingLevel;
        internal Groups FMemberGroups;
        internal bool FTakeFiltersIntoCalculations = true;

#if DEBUG
        internal bool FFiltered { get; set; }
#else
        internal bool FFiltered = false;
#endif
        internal bool FFilterChanged;
        internal bool FAllowSwapMembers = true;
#if DEBUG
        internal bool FInitialized { get; set; }
#else
        internal bool FInitialized = false;
#endif
        public bool Initialized => FInitialized;

        internal bool FOverrideSortMethods;
        internal bool FShowEmptyLines;
        internal bool FCorruptedAfterReadStream = false;
        internal bool FAllowMultiselect = true;
        internal Dimension FDimension;
        internal List<Intelligence> fIntelligence = new List<Intelligence>();

        private MemoryStream membersstream;

        /// <summary>
        ///     Defines the visible/invisible state of the current hierarchy members not yet
        ///     fetched from the server. Used for correct creating of filters in the RadarCube for MS
        ///     Analysis version
        /// </summary>
        [Browsable(false)]
        public bool UnfetchedMembersVisible
        {
            get => FUnfetchedMembersVisibility;
            set => FUnfetchedMembersVisibility = value;
        }

        internal void DefaultInit()
        {
            if (FCubeHierarchy.FCubeLevels.Count == 0)
                Dimension.Grid.Cube.RetrieveLevels(FCubeHierarchy, Dimension.Grid);

            if ((HierarchyState.hsInitialized & State) != HierarchyState.hsInitialized
                && Dimension.Grid.Active)
                InitHierarchy(IsDate ? -1 : 0);
        }

        internal void DefaultInit(int levelsCount)
        {
            if (FCubeHierarchy.FCubeLevels.Count == 0)
                Dimension.Grid.Cube.RetrieveLevels(FCubeHierarchy, Dimension.Grid);
            if ((HierarchyState.hsInitialized & State) != HierarchyState.hsInitialized
                && Dimension.Grid.Active) InitHierarchy(IsDate ? -1 : levelsCount);
            for (var i = 0; i < levelsCount; i++)
                Levels[i].Initialize();
        }


        [OnSerializing]
        private void SerializeMembers(StreamingContext context)
        {
            membersstream = new MemoryStream();
            var writer = new BinaryWriter(membersstream);
            WriteStream(writer);
        }

        /// <summary>The list of intelligence abilities for the specified hierarchy</summary>
        public List<Intelligence> Intelligence => fIntelligence;


        [OnSerialized]
        private void SerializeMembersEnd(StreamingContext context)
        {
            membersstream = null;
        }

        internal void WriteStream(BinaryWriter stream)
        {
            if (FLevels == null) return;
            for (var i = 0; i < FLevels.Count; i++)
            {
                var l = FLevels[i];
                StreamUtils.WriteInt32(stream, l.FUniqueNamesArray.Count);
                for (var j = 0; j < l.FUniqueNamesArray.Count; j++)
                {
                    var m = l.FUniqueNamesArray.Values[j];
                    m.WriteStream(stream);
                }
                StreamUtils.WriteInt32(stream, l.FStaticMembers.Count);
                for (var j = 0; j < l.FStaticMembers.Count; j++)
                {
                    var m = l.FStaticMembers[j];
                    m.WriteStream(stream);
                }
            }

            DoMembersWriteStream(stream, FLevels[0].Members, Tags.tgHierarchy_ChildrenCount, 0);
        }

        private void DoMembersWriteStream(BinaryWriter stream, Members members, Tags tag, int levelindex)
        {
            var level = FLevels[levelindex];
            StreamUtils.WriteTag(stream, tag);
            StreamUtils.WriteInt32(stream, members.Count);
            for (var i = 0; i < members.Count; i++)
            {
                var m = members[i];
                if (m.FCubeMember != null)
                {
                    StreamUtils.WriteTag(stream, Tags.tgHierarchy_StaticMember);
                    StreamUtils.WriteInt32(stream, m.FCubeMember.ID);
                }
                else
                {
                    StreamUtils.WriteTag(stream, Tags.tgHierarchy_VirtualMember);
                    var index = level.FUniqueNamesArray.IndexOfKey(m.UniqueName);
                    StreamUtils.WriteInt32(stream, index);
                }
                var b = true;
                if (m.Children.Count > 0)
                {
                    b = false;
                    DoMembersWriteStream(stream, m.Children, Tags.tgHierarchy_ChildrenCount, levelindex);
                }
                if (m.FNextLevelChildren.Count > 0)
                {
                    b = false;
                    DoMembersWriteStream(stream, m.FNextLevelChildren, Tags.tgHierarchy_NextLevelChildrenCount,
                        levelindex + 1);
                }
                if (b)
                    StreamUtils.WriteTag(stream, Tags.tgHierarchy_LeafMember);
            }
        }

        internal void DoMembersReadStream(BinaryReader stream, Members members, int levelindex,
            Member parent, bool isNextlevel)
        {
            var memberscount = StreamUtils.ReadInt32(stream);
            members.Capacity = memberscount;
            var level = FLevels[levelindex];
            members.FParentLevel = level;
            for (var i = 0; i < memberscount; i++)
            {
                var membertag = StreamUtils.ReadTag(stream);
                var memberindex = StreamUtils.ReadInt32(stream);
                var m = membertag == Tags.tgHierarchy_StaticMember
                    ? level.FStaticMembers[memberindex]
                    : level.FUniqueNamesArray.Values[memberindex];
                m.FParent = parent;
                m.FDepth = parent == null || parent.FLevel != m.FLevel ? 0 : parent.FDepth + 1;
                members.Add(m);
                if (isNextlevel) level.Members.Add(m);
                var Tag = StreamUtils.ReadTag(stream);
                switch (Tag)
                {
                    case Tags.tgHierarchy_LeafMember:
                        break;
                    case Tags.tgHierarchy_ChildrenCount:
                        DoMembersReadStream(stream, m.Children, levelindex, m, false);
                        break;
                    case Tags.tgHierarchy_NextLevelChildrenCount:
                        DoMembersReadStream(stream, m.FNextLevelChildren, levelindex + 1, m, true);
                        break;
                }
            }
        }

        internal void ReadStream(BinaryReader stream)
        {
            if (FLevels == null) return;
            for (var i = 0; i < FLevels.Count; i++)
            {
                var l = FLevels[i];
                var memberscount = StreamUtils.ReadInt32(stream);
                l.FMembers = new Members();
                l.Members.FParentLevel = l;
                l.FUniqueNamesArray = l.CreateSortedList(memberscount);
                for (var j = 0; j < memberscount; j++)
                {
                    var Tag = StreamUtils.ReadTag(stream);
                    Member m = null;
                    switch (Tag)
                    {
                        case Tags.tgCalculatedMember:
                            m = new CalculatedMember(l);
                            break;
                        case Tags.tgGroupMember:
                            m = new GroupMember(l);
                            break;
                        case Tags.tgMember:
                            m = new Member(l);
                            break;
                        default:
                            throw new Exception("Unknown tag: " + Tag);
                    }
                    m.ReadStream(stream);
                    l.FUniqueNamesArray.Add(m.UniqueName, m);
                }

                memberscount = StreamUtils.ReadInt32(stream);
                l.FStaticMembers = l.CreateListMember(new Member[memberscount]);
                for (var j = 0; j < memberscount; j++)
                {
                    var Tag = StreamUtils.ReadTag(stream);
                    Member m = null;
                    switch (Tag)
                    {
                        case Tags.tgCalculatedMember:
                            m = new CalculatedMember(l);
                            break;
                        case Tags.tgGroupMember:
                            m = new GroupMember(l);
                            break;
                        case Tags.tgMember:
                            m = new Member(l);
                            break;
                        default:
                            throw new Exception("Unknown tag: " + Tag);
                    }
                    m.ReadStream(stream);
                    l.FStaticMembers[j] = m;
                }
            }
            StreamUtils.ReadTag(stream); // maybe ReadInt32
            DoMembersReadStream(stream, FLevels[0].Members, 0, null, false);
        }

        internal void RestoreAfterSerialization()
        {
            FCubeHierarchy = FDimension.CubeDimension.Hierarchies.Find(FUniqueName);
            if (FLevels != null)
            {
                FLevels.RestoreAfterSerialization(FCubeHierarchy.FCubeLevels);
                Sort();
            }
        }

        /// <exclude />
        public Hierarchy(Dimension ADimension)
        {
            FDimension = ADimension;
            FCalculatedMembers = new DimensionCalculatedMembers(this);
            FMemberGroups = new Groups(this);
            TotalCaption = RadarUtils.GetResStr("rsTotalCaption");
        }

        public override string ToString()
        {
            return UniqueName;
        }

        /// <summary>
        ///     Shows only passed level of the hierarchy by making all other levels invisible.
        /// </summary>
        /// <param name="level">Level to show</param>
        public void DetailsHierarchyTo(Level level)
        {
            switch (level.Grid.CellsetMode)
            {
                case CellsetMode.cmGrid:

                    foreach (var l in Levels.Except(new[] {level}))
                        l.FVisible = false;
                    //level.Visible = true;
                    break;
                case CellsetMode.cmChart:

                    foreach (var l in Levels)
                        l.FVisible = false;
                    level.Visible = true;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal void CreateLevels(CubeLevels ACubeLevels)
        {
            FLevels = new Levels(this, ACubeLevels);
        }

        internal void InitHierarchyProperties(CubeHierarchy CubeHierarchy)
        {
            FCubeHierarchy = CubeHierarchy;
            FUniqueName = CubeHierarchy.UniqueName;
            TotalCaption = CubeHierarchy.TotalCaption;
            FFormatString = CubeHierarchy.FormatString;

            FTotalAppearance = CubeHierarchy.FTotalAppearance;
            FAllowResort = CubeHierarchy.FAllowResort;
            FAllowChangeTotalAppearance = CubeHierarchy.FAllowChangeTotalAppearance;
            FAllowFilter = CubeHierarchy.FAllowFilter;
            FAllowRegroup = CubeHierarchy.FAllowRegroup;
            FAllowPopupOnCaptions = CubeHierarchy.FAllowPopupOnCaptions;
            FAllowPopupOnLabels = CubeHierarchy.FAllowPopupOnLabels;
            FVisible = CubeHierarchy.FVisible;
            FAllowSwapMembers = CubeHierarchy.FAllowSwapMembers;
            FAllowMultiselect = CubeHierarchy.FAllowMultiselect;
            if (FDimension.FGrid.CellsetMode == CellsetMode.cmGrid)
                FShowEmptyLines = CubeHierarchy.FShowEmptyLines;
        }

        internal void DeleteCustomMember(CustomMember M)
        {
            M.Level.Members.Remove(M);
            M.FLevel.FUniqueNamesArray.Remove(M.UniqueName);
            M.FLevel.RegisterMembers();
            if (M.Parent != null)
            {
                M.Parent.Children.Remove(M);
                if (M.FParent.FLevel != M.FLevel) M.Parent.FNextLevelChildren.Remove(M);
            }
        }

        /// <summary>Removes all calculated members (if there are any) from the hierarchy</summary>
        public void DeleteCalculatedMembers()
        {
            if (FLevels == null) return;
            foreach (var l in FLevels)
                for (var j = l.FUniqueNamesArray.Count - 1; j >= 0; j--)
                    if (l.FUniqueNamesArray.Values[j].MemberType == MemberType.mtCalculated)
                        DeleteCustomMember((CustomMember) l.FUniqueNamesArray.Values[j]);
        }

        internal bool HasManyLevels => FLevels.Count > 1 || CubeHierarchy.FMDXLevelNames.Count > 1;

        /// <summary>
        ///     Removes all groups from the hierarchy
        /// </summary>
        public void DeleteGroups()
        {
            if (FLevels == null) return;
            foreach (var l in FLevels)
                for (var j = l.FUniqueNamesArray.Count - 1; j >= 0; j--)
                    if (l.FUniqueNamesArray.Values[j].MemberType == MemberType.mtGroup)
                    {
                        var gm = (GroupMember) l.FUniqueNamesArray.Values[j];
                        ClearGroup(gm);
                        DeleteCustomMember(gm);
                    }
        }

        private bool DoRetrieveFilteredMember(Member parent, ref Member member)
        {
            foreach (var m in parent.Children)
            {
                if (!m.Visible) continue;
                if (m.Filtered)
                {
                    if (!DoRetrieveFilteredMember(m, ref member)) return false;
                }
                else
                {
                    if (member != null) return false;
                    member = m;
                }
            }
            foreach (var m in parent.NextLevelChildren)
            {
                if (!m.Visible) continue;
                if (m.Filtered)
                {
                    if (!DoRetrieveFilteredMember(m, ref member)) return false;
                }
                else
                {
                    if (member != null) return false;
                    member = m;
                }
            }
            return true;
        }

        /// <summary>
        ///     Returns a member, if it's the only one vizible for this hierarchy, otherwise returns null.
        /// </summary>
        public Member RetrieveFilteredMember()
        {
            CheckInit();
            if (!Filtered) return null;
            Member Result = null;
            foreach (var m in Levels[0].Members)
            {
                if (!m.Visible) continue;
                if (!m.Filtered)
                {
                    if (Result != null) return null;
                    Result = m;
                }
                else if (!DoRetrieveFilteredMember(m, ref Result))
                {
                    return null;
                }
            }
            return Result;
        }

        internal void DoMoveToGroup(GroupMember Group, Member Member)
        {
            if (Group == Member || Member.MemberType != MemberType.mtCommon) return;
            DoMoveFromGroup(Member);
            if (Group.Parent != Member.Parent)
                throw new Exception(RadarUtils.GetResStr("rsIllegalGrouping_Parents"));

            if (Group.Level != Member.Level)
                throw new Exception(RadarUtils.GetResStr("rsIllegalGrouping_Levels"));

            var M = Member.Parent == null || Member.Parent.Level != Member.Level
                ? Member.Level.Members
                : Member.Parent.Children;
            M.Remove(Member);
            Group.Children.Add(Member);
            if (Member.FParent != null && Member.FParent.FLevel != Member.FLevel)
                Member.FParent.FNextLevelChildren.Remove(Member);
            Member.FParent = Group;
            Member.FDepth++;
            Member.Children.SetDepth(Member.FDepth + 1);
        }

        internal void DoMoveFromGroup(Member Member)
        {
            if (Member.Parent == null || !(Member.Parent is GroupMember))
                return;

            var Group = Member.Parent as GroupMember;
            Members M = Group.Parent == null || Group.Parent.Level != Group.Level
                ? Group.Level.Members
                : M = Group.Parent.Children;
            M.Add(Member);
            Member.FParent = Group.Parent;
            if (Member.FParent != null && Member.FParent.FLevel != Member.FLevel)
                Member.FParent.FNextLevelChildren.Add(Member);

            Member.FDepth--;
            Member.Children.SetDepth(Member.FDepth + 1);
            Group.Children.Remove(Member);
        }

        internal int GetDefaultLevelDepth()
        {
            return Origin == HierarchyOrigin.hoParentChild ? CubeHierarchy.FMDXLevelNames.Count : 1;
        }

        internal void DoClearGroup(GroupMember Group)
        {
            var M = Group.Parent == null || Group.Parent.Level != Group.Level
                ? Group.Level.Members
                : Group.Parent.Children;
            if (Group.Children.Count == 0) return;
            foreach (var m_ in Group.Children)
            {
                M.Add(m_);
                m_.FParent = Group.Parent;
                if (m_.FParent != null && m_.FParent.FLevel != m_.FLevel)
                    m_.FParent.FNextLevelChildren.Add(m_);
                m_.FDepth--;
                m_.Children.SetDepth(m_.FDepth + 1);
            }
            Group.Children.Clear();
            if (!IsUpdating) M.Sort(Dimension.Grid, FSortType);
            Group.Level.FDepth = GetDefaultLevelDepth();
            foreach (var m_ in Group.Level.Members)
                Group.Level.FDepth = Math.Max(Group.Level.FDepth, m_.ChildrenDepth);
        }

        internal void CheckFlat()
        {
            foreach (var l in FLevels) l.Members.CheckFlat(FLevels.IndexOf(l) == 0);
        }

        internal void ApplyDefaultFilter()
        {
            if (Levels == null || Levels.Count == 0) return;
            if (Filtered || FilteredByLevelFilters) return;
            if (string.IsNullOrEmpty(CubeHierarchy.DefaultMember)) return;
            var m = FindMemberByUniqueName(CubeHierarchy.DefaultMember);
            if (m == null) return;

            BeginUpdate();
            ResetFilter();
            UnfetchedMembersVisible = false;
            foreach (var mm in Levels[0].Members)
                mm.Visible = false;
            m.Visible = true;
            EndUpdate();
        }

        internal bool DoAllowPivoting(Hierarchy probe)
        {
            if (probe == null) return true;
            if (Origin == HierarchyOrigin.hoNamedSet && probe.Origin == HierarchyOrigin.hoNamedSet)
            {
                string[] bh = CubeHierarchy.FBaseNamedSetHierarchies.Split(',');
                var l = new List<string>(bh);
                bh = probe.CubeHierarchy.FBaseNamedSetHierarchies.Split(',');
                foreach (var s in bh)
                    if (l.Contains(s))
                    {
                        Dimension.Grid._callbackClientErrorString = RadarUtils.GetResStr("rsWrongNamedSetPivoting",
                            probe.DisplayName, DisplayName);
                        Dimension.Grid.callbackData = CallbackData.ClientError;
                        return false;
                    }
                return true;
            }
            if (Origin == HierarchyOrigin.hoNamedSet)
            {
                string[] bh = CubeHierarchy.FBaseNamedSetHierarchies.Split(',');
                foreach (var s in bh)
                    if (s == probe.UniqueName)
                    {
                        Dimension.Grid._callbackClientErrorString = RadarUtils.GetResStr("rsWrongNamedSetPivoting",
                            probe.DisplayName, DisplayName);
                        Dimension.Grid.callbackData = CallbackData.ClientError;
                        return false;
                    }
                return true;
            }
            if (probe.Origin == HierarchyOrigin.hoNamedSet)
            {
                string[] bh = probe.CubeHierarchy.FBaseNamedSetHierarchies.Split(',');
                foreach (var s in bh)
                    if (s == UniqueName)
                    {
                        Dimension.Grid._callbackClientErrorString = RadarUtils.GetResStr("rsWrongNamedSetPivoting",
                            probe.DisplayName, DisplayName);
                        Dimension.Grid.callbackData = CallbackData.ClientError;
                        return false;
                    }
                return true;
            }
            return true;
        }

        internal void CheckInit()
        {
            if (!State.HasFlag(HierarchyState.hsInitialized))
                throw new Exception(
                    string.Format(RadarUtils.GetResStr("rsHierarchyNotInit"), DisplayName));
        }

        internal void UpdateFilterState(bool FilterChanged)
        {
            FFilterChanged = FFilterChanged || FilterChanged;
            if (IsUpdating || !FFilterChanged) return;
            FFiltered = false;

            foreach (var m in FLevels[0].Members)
            {
                bool B;
                m.DoUpdateFilterState(out B);
                FFiltered = FFiltered || B;
            }
            if (!UnfetchedMembersVisible && !FFiltered)
                FFiltered = Levels.Any(item => item.CompleteMembersCount > item.Members.Count);

            if (FTakeFiltersIntoCalculations && Dimension.Grid.FFilteredHierarchies != null)
            {
                foreach (var l in FLevels)
                    Dimension.Grid.Engine.ClearDependedMetalines(l);
                Dimension.Grid.Engine.ClearIncludedHierarchy(this);
                var i = Dimension.Grid.FFilteredHierarchies.IndexOf(this);
                if (Origin != HierarchyOrigin.hoNamedSet)
                    if (FFiltered)
                    {
                        if (i < 0) Dimension.Grid.FFilteredHierarchies.Add(this);
                    }
                    else
                    {
                        if (i >= 0) Dimension.Grid.FFilteredHierarchies.RemoveAt(i);
                    }
            }

            if (Dimension.Grid.IsUpdating == false)
                Dimension.Grid.CellSet.Rebuild();

            FFilterChanged = false;
            Dimension.Grid.EndChange(GridEventType.geFilterAction, this);
        }

        public void UpdateRanks()
        {
            foreach (var l in Levels) l.Members.UpdateRanks(l);
            Sort();
        }

        /// <summary>Searches for the hierarchy member by its name.</summary>
        /// <remarks>
        ///     The way the names of the hierarchy members are formed is described in Finding a
        ///     Hierarchy Member section.
        /// </remarks>
        public Member FindMemberByName(string MemberName)
        {
            CheckInit();
            foreach (var l in Levels)
            {
                var Result = l.FindMemberByName(MemberName);
                if (Result != null)
                    return Result;
            }
            return null;
        }

        /// <summary>Searches for the hierarchy member by its unique name.</summary>
        /// <param name="UniqueName">The unique name of the member</param>
        public Member FindMemberByUniqueName(string UniqueName)
        {
            CheckInit();
            foreach (var l in Levels)
            {
                var Result = l.FindMember(UniqueName);
                if (Result != null) return Result;
            }
            return null;
        }

        private void DoInitIntelligence()
        {
            if (IsDate && Dimension.Grid.Cube as IMOlapCube != null)
            {
                for (var i = 0; i < Levels.Count - 1; i++)
                {
                    var ti = new Intelligence(Levels[i], IntelligenceType.itMemberToDate);
                    if (ti.Expression != "") Intelligence.Add(ti);
                }

                for (var i = 0; i < Levels.Count; i++)
                {
                    var ti = new Intelligence(Levels[i], IntelligenceType.itMemberGrowth);
                    if (ti.Expression != "") Intelligence.Add(ti);
                }
            }
        }

        internal bool IsDate => Dimension.CubeDimension.fDimensionType == DimensionType.dtTime;

        /// <summary>
        ///     Initializes the hierarchy, retrieving the information about its levels and
        ///     members.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For MSAS version the levelsToFetch parameter defines the number of levels,
        ///         whose members are altogether fetched. If the parameter value is negative then the
        ///         data about all the members of the current hierarchy is fetched.
        ///     </para>
        ///     <para>
        ///         Note: By deault in the MSAS version the hierarchy levels contain only the
        ///         members used for building the Cellset. This speeds up operating the Cube and saves
        ///         memory, though may be a bit inconvenient for programmers. "Manual" initialization
        ///         of a hierarchy by calling this method allows to change the default pattern and
        ///         fetch the desired number of members.
        ///     </para>
        /// </remarks>
        /// <param name="levelsToFetch">
        ///     A number of hierarchy levels, the information about the members being fetched from a server.
        ///     If the parameter equals 0, then only the information about the hierarchy levels can be fetched from a server and
        ///     its members are not fetched.
        ///     If the parameter is negative, then full information about all levels and members of a given hierarchy is being
        ///     fetched.
        /// </param>
        public void InitHierarchy(int levelsToFetch)
        {
            if (FInitialized)
                return;

            if (FCubeHierarchy.FCubeLevels.Count == 0)
                Dimension.Grid.Cube.RetrieveLevels(FCubeHierarchy, Dimension.Grid);

            if (levelsToFetch < 0)
                levelsToFetch = 9999;

            Dimension.Grid.FEngine.FLevelsList.Capacity = Math.Max(Dimension.Grid.Cube.FLevelsList.Count,
                Dimension.Grid.FEngine.FLevelsList.Capacity);

            while (Dimension.Grid.FEngine.FLevelsList.Count < Dimension.Grid.Cube.FLevelsList.Count)
                Dimension.Grid.FEngine.FLevelsList.Add(null);

            CreateLevels(FCubeHierarchy.FCubeLevels);

            if (Origin == HierarchyOrigin.hoNamedSet)
            {
                bool dummy;
                Dimension.Grid.Cube.RetrieveMembersPartial(Dimension.Grid, Levels[0], 0, -1, null, out dummy);
                Levels[0].CubeLevel.FMembersCount = Levels[0].CubeLevel.Members.Count;
            }

            if (levelsToFetch > 0)
                for (var i = 0; i < Math.Min(levelsToFetch + 1, FLevels.Count); i++)
                    FLevels[i].Initialize();

            if (Levels.Count == 0)
                throw new Exception(string.Format(RadarUtils.GetResStr("rsNoLevelsInHierarchy"), DisplayName));

            DoInitIntelligence();

            foreach (var c_ in CalculatedMembers)
            {
                var C = new CalculatedMember(Levels[0], null, null);
                C.DisplayName = c_.DisplayName;
                C.SetUniqueName(c_.FUniqueName);
                C.FDescription = c_.FDescription;
                C.FMemberType = MemberType.mtCalculated;
                C.fPosition = c_.Position;
                C.FExpression = c_.Expression;
                Levels[0].Members.Add(C);
            }

            foreach (var g_ in MemberGroups)
            {
                var G = new GroupMember(Levels[0], null, null);
                G.DisplayName = g_.FDisplayName;
                G.SetUniqueName(Levels[0].Hierarchy.UniqueName + ".[" + g_.FDisplayName + "]");
                G.FDescription = g_.FDescription;
                G.FMemberType = MemberType.mtGroup;
                G.FDeleteableByUser = g_.FDeleteableByUser;
                Levels[0].Members.Add(G);
            }

            FInitialized = true;
            UpdateRanks();

            Dimension.Grid.EventInitHierarchy(this);

            foreach (var l in Levels)
                l.RegisterMembers();

            CheckFlat();
        }

        /// <summary>
        ///     Sorts the members of the specified hierarchy according to the assigned values of
        ///     the Hierarchy.SortType and Level.SortType properties.
        /// </summary>
        public void Sort()
        {
            Sort(true);
        }

        internal void Sort(bool needRebuild)
        {
            CheckInit();
            foreach (var l in Levels) l.Members.Sort(Dimension.Grid, l.FSortType);
            if ((HierarchyState.hsActiveExpanded & State) == HierarchyState.hsActiveExpanded &&
                Dimension.Grid.IsUpdating == false && needRebuild)
                Dimension.Grid.FCellSet.Rebuild();
        }

        /// <summary>Clears all filters applied to the hierarchy.</summary>
        /// <remarks>
        ///     Clears all the filters applied to the hierarchy members (the Visibility property)
        ///     and the context filters described by the Level.Filter property.
        /// </remarks>
        public void ResetFilter()
        {
            if (FFiltered || FilteredByLevelFilters)
            {
                BeginUpdate();
                DoSetFilter(true);
                EndUpdate();
            }
        }

        internal void ResetHierarchy()
        {
            if (FLevels != null && FLevels.Count > 0)
            {
                ResetFilter();
                DeleteCalculatedMembers();
                foreach (var l in Levels)
                    l.FVisible = false;
                Levels[0].FVisible = true;
            }
        }

        internal void DoSetFilter(bool state, bool deleteFromFilterArea)
        {
            if (FLevels != null && FLevels.Count > 0)
            {
                FLevels[0].Members.SetVisible(state, this);
                foreach (var l in Levels)
                {
                    if (l.FFilter != null) UpdateFilterState(true);
                    l.FFilter = null;
                }
            }
            UnfetchedMembersVisible = state;
            if (state && FDimension.FGrid.FFilteredHierarchies != null)
            {
                FDimension.FGrid.FFilteredHierarchies.Remove(this);
                if (deleteFromFilterArea)
                    FDimension.FGrid.PivotingOut(this, LayoutArea.laPage);
                FFiltered = false;
            }
        }

        internal void DoSetFilter(bool state)
        {
            DoSetFilter(state, true);
        }

        /// <summary>
        ///     Prevents unnecessary updates during the batch change of the hierarchy members
        ///     visibility, as well as while adding/deleting the groups and calculated members.
        /// </summary>
        /// <remarks>
        ///     Delays rebuilding of the Cellset and updating of the Grid (caused by setting the
        ///     members' visibility status) until the EndUpdate() method is called
        /// </remarks>
        public void BeginUpdate()
        {
            FUpdatingLevel++;
        }

        /// <summary>
        ///     <para>
        ///         Allows rebuilding of the Cellset and updating of the Grid (caused by setting
        ///         the members' visibility status) delayed by the call of the BeginUpdate()
        ///         method.
        ///     </para>
        /// </summary>
        public void EndUpdate()
        {
            if (IsUpdating)
            {
                FUpdatingLevel--;
                if (FUpdatingLevel == 0)
                {
                    Sort(false);
                    UpdateFilterState(FFilterChanged);
                }
            }
        }

        //  function ShowEditor: boolean;
        /// <summary>Creates a calculated hierarchy member</summary>
        /// <remarks>
        ///     <para>
        ///         MSAS version supports two ways of calculating members, added by this
        ///         method:
        ///     </para>
        ///     <list type="bullet">
        ///         <item></item>
        ///         <item>
        ///             With an MDX expression (if an MDX formula for calculating member is
        ///             specified by the CalculatedMember.Expression property).
        ///         </item>
        ///         <item>
        ///             With the OlapGrid.OnCalcMember event handler (if the Expression property
        ///             for this member is not specified)
        ///         </item>
        ///     </list>
        ///     <para>The Direct version supports only the latter.</para>
        /// </remarks>
        /// <param name="ADisplayName">Specifies the name of the calculated member to be shown in the Grid</param>
        /// <param name="ADescription">Provides the description of the calculated member</param>
        /// <param name="ALevel">Defines the level for the calculated member to be placed to</param>
        /// <param name="AParent">Defines the Parent of the calculated member</param>
        /// <param name="APosition">Defines the position of the calculated member in relation to its neighbors</param>
        public CalculatedMember CreateCalculatedMember(string ADisplayName, string ADescription,
            Level ALevel, Member AParent, CustomMemberPosition APosition)
        {
            CheckInit();
            var HS = State;

            if (ALevel == null)
                ALevel = Levels[0];

            var Result = new CalculatedMember(ALevel, AParent, null);
            Result.DisplayName = ADisplayName;
            Result.FDescription = ADescription;
            Result.FMemberType = MemberType.mtCalculated;
            Result.fPosition = APosition;
            Result.SetUniqueName(ALevel.Hierarchy.UniqueName + ".[" + ADisplayName + "]");
            ALevel.FUniqueNamesArray.Add(Result.UniqueName, Result);

            if (AParent == null || AParent.Level != ALevel)
                ALevel.Members.Add(Result);
            else
                AParent.Children.Add(Result);

            if (!IsUpdating)
                ALevel.Sort();

            ALevel.RegisterMembers();
            CheckFlat();
            Dimension.Grid.Engine.Clear();
            if ((HierarchyState.hsActive & HS) == HierarchyState.hsActive)
                Dimension.Grid.CellSet.Rebuild();
            Dimension.Grid.EndChange(GridEventType.geAddCalculatedMember, Result);
            return Result;
        }

        /// <summary>
        ///     Creates a calculated hierarchy member.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         MSAS version supports two ways of calculating members, added by this
        ///         method:
        ///     </para>
        ///     <list type="bullet">
        ///         <item></item>
        ///         <item>
        ///             With an MDX expression (if an MDX formula for calculating member is
        ///             specified by the CalculatedMember.Expression property).
        ///         </item>
        ///         <item>
        ///             With the OlapGrid.OnCalcMember event handler (if the Expression property
        ///             for this member is not specified)
        ///         </item>
        ///     </list>
        ///     <para>The Direct version supports only the latter.</para>
        /// </remarks>
        /// <param name="ADisplayName">Specifies the name of the calculated member to be shown in the Grid</param>
        /// <param name="ADescription">Provides the description of the calculated member</param>
        /// <param name="APrecedingMember">Defines the preceding member for the new calculated one</param>
        public CalculatedMember CreateCalculatedMember(string ADisplayName, string ADescription,
            Member APrecedingMember)
        {
            CheckInit();
            var HS = State;

            var P = APrecedingMember.Parent;
            var L = APrecedingMember.FLevel;
            var Result = new CalculatedMember(L, P, null);
            Result.DisplayName = ADisplayName;
            Result.FMemberType = MemberType.mtCalculated;
            Result.fPosition = CustomMemberPosition.cmpGeneralOrder;
            Result.SetUniqueName(L.Hierarchy.UniqueName + ".[" + ADisplayName + "]");
            L.FUniqueNamesArray.Add(Result.UniqueName, Result);
            Result.FDescription = ADescription;
            var R = APrecedingMember.FRank;
            var M = P == null || P.Level != L ? L.Members : P.Children;

            foreach (var m_ in M)
                if (m_.FRank < R && APrecedingMember.FRank < m_.FRank) R = m_.FRank;
            R = (R + APrecedingMember.FRank) / 2;
            if (R <= APrecedingMember.FRank) R = R + 1;
            Result.FRank = R;
            var i = M.IndexOf(APrecedingMember);
            M.Insert(i + 1, Result);
            L.RegisterMembers();
            CheckFlat();
            Dimension.Grid.Engine.Clear();
            if ((HierarchyState.hsActive & HS) == HierarchyState.hsActive) Dimension.Grid.CellSet.Rebuild();
            Dimension.Grid.EndChange(GridEventType.geAddCalculatedMember, Result);
            return Result;
        }

        /// <summary>Creates a group for the existing hierarchy members to be placed in.</summary>
        public GroupMember CreateGroup(string ADisplayName, string ADescription, Level ALevel,
            Member AParent, bool DeleteableByUser, CustomMemberPosition APosition, IEnumerable<Member> Members)
        {
            CheckInit();
            var HS = State;

            if (ALevel == null)
                ALevel = Levels[0];

            var Result = new GroupMember(ALevel, AParent, null);
            Result.DisplayName = ADisplayName;
            Result.FDescription = ADescription;
            Result.FDeleteableByUser = DeleteableByUser;
            Result.fPosition = APosition;
            Result.SetUniqueName(ALevel.Hierarchy.UniqueName + ".[" + ADisplayName + "]");
            ALevel.FUniqueNamesArray.Add(Result.UniqueName, Result);
            if (AParent == null || AParent.Level != ALevel)
                ALevel.Members.Add(Result);
            else
                AParent.Children.Add(Result);
            ALevel.RegisterMembers();
            if (!IsUpdating) ALevel.Sort();

            foreach (var m in Members) DoMoveToGroup(Result, m);
            if (!IsUpdating) Result.Children.Sort(Dimension.Grid, FSortType);
            Result.Level.FDepth = GetDefaultLevelDepth();
            foreach (var m in Result.Level.Members)
                Result.Level.FDepth = Math.Max(Result.Level.FDepth, m.ChildrenDepth);

            CheckFlat();
            Dimension.Grid.Engine.Clear();
            if ((HierarchyState.hsActive & HS) == HierarchyState.hsActive)
                Dimension.Grid.CellSet.Rebuild();
            Dimension.Grid.EndChange(GridEventType.geAddGroup, Result);
            foreach (var m in Members)
                Dimension.Grid.EndChange(GridEventType.geMoveMemberToGroup, Result, m);
            return Result;
        }

        /// <summary>Creates a group for the existing hierarchy members to be placed in.</summary>
        public GroupMember CreateGroup(string aDisplayName, CustomMemberPosition aPosition,
            IEnumerable<Member> members)
        {
            return CreateGroup(aDisplayName, "", null, null, true, aPosition, members);
        }

        /// <summary>Creates a group for the existing hierarchy members to be placed in.</summary>
        public GroupMember CreateGroup(string ADisplayName, string ADescription, Member APrecedingMember,
            bool DeleteableByUser, IEnumerable<Member> Members)
        {
            CheckInit();
            var HS = State;

            var P = APrecedingMember.Parent;
            var L = APrecedingMember.FLevel;
            var Result = new GroupMember(L, P, null);
            Result.DisplayName = ADisplayName;
            Result.fPosition = CustomMemberPosition.cmpGeneralOrder;
            Result.FDeleteableByUser = DeleteableByUser;
            Result.SetUniqueName(L.Hierarchy.UniqueName + ".[" + ADisplayName + "]");
            L.FUniqueNamesArray.Add(Result.UniqueName, Result);
            Result.FDescription = ADescription;
            var R = APrecedingMember.FRank;
            var M = P == null || P.Level != L ? L.Members : P.Children;

            foreach (var m in M)
                if (m.FRank < R && APrecedingMember.FRank < m.FRank) R = m.FRank;
            R = (R + APrecedingMember.FRank) / 2;
            if (R <= APrecedingMember.FRank) R = R + 1;
            Result.FRank = R;
            var i = M.IndexOf(APrecedingMember);
            M.Insert(i + 1, Result);
            L.RegisterMembers();

            if (!IsUpdating) L.Sort();

            foreach (var m in Members) DoMoveToGroup(Result, m);
            if (!IsUpdating) Result.Children.Sort(Dimension.Grid, FSortType);
            Result.Level.FDepth = GetDefaultLevelDepth();
            foreach (var m in Result.Level.Members)
                Result.Level.FDepth = Math.Max(Result.Level.FDepth, m.ChildrenDepth);

            CheckFlat();
            Dimension.Grid.Engine.Clear();
            if ((HierarchyState.hsActive & HS) == HierarchyState.hsActive)
                Dimension.Grid.CellSet.Rebuild();
            Dimension.Grid.EndChange(GridEventType.geAddGroup, Result);
            foreach (var m in Members)
                Dimension.Grid.EndChange(GridEventType.geMoveMemberToGroup, Result, m);
            return Result;
        }

        /// <summary>Clears the contents of the group passed as the parameter.</summary>
        public void ClearGroup(GroupMember Group)
        {
            DoClearGroup(Group);
            Dimension.Grid.FEngine.ClearIncludedMetalines(Group.Level);
            if ((HierarchyState.hsActive & State) == HierarchyState.hsActive) Dimension.Grid.CellSet.Rebuild();
            CheckFlat();
            Dimension.Grid.EndChange(GridEventType.geClearGroup, Group);
        }

        /// <summary>Deletes the specified calculated member of the specified hierarchy.</summary>
        public void DeleteCalculatedMember(CalculatedMember M)
        {
            DeleteCustomMember(M);
            CheckFlat();
            Dimension.Grid.Engine.Clear();
            if (!Dimension.Grid.IsUpdating && Dimension.Grid.Active)
                Dimension.Grid.CellSet.Rebuild();
            Dimension.Grid.EndChange(GridEventType.geDeleteCustomMember, M);
        }

        /// <summary>Deletes the specified group.</summary>
        public void DeleteGroup(GroupMember Group)
        {
            DoClearGroup(Group);
            DeleteCustomMember(Group);
            Dimension.Grid.FEngine.ClearIncludedMetalines(Group.Level);
            if ((HierarchyState.hsActive & State) == HierarchyState.hsActive) Dimension.Grid.CellSet.Rebuild();
            CheckFlat();
            Dimension.Grid.Engine.Clear();
            Dimension.Grid.EndChange(GridEventType.geDeleteCustomMember, Group);
        }

        /// <summary>
        ///     <para>Places the specified hierarchy members into the specified group.</para>
        /// </summary>
        public void MoveToGroup(GroupMember Group, params Member[] Members)
        {
            CheckInit();
            var HS = State;

            foreach (var m in Members)
                DoMoveToGroup(Group, m);

            if (!IsUpdating)
                Group.Children.Sort(Dimension.Grid, FSortType);

            Group.Level.FDepth = GetDefaultLevelDepth();

            foreach (var m in Group.Level.Members)
                Group.Level.FDepth = Math.Max(Group.Level.FDepth, m.ChildrenDepth);

            Dimension.Grid.FEngine.ClearIncludedMetalines(Group.Level);
            if ((HierarchyState.hsActive & State) == HierarchyState.hsActive && !IsUpdating)
                Dimension.Grid.CellSet.Rebuild();

            CheckFlat();

            foreach (var m in Members)
                Dimension.Grid.EndChange(GridEventType.geMoveMemberToGroup, Group, m);
        }

        /// <summary>Removes the specified hierarchy members from the specified group.</summary>
        public void MoveFromGroup(params Member[] Members)
        {
            if (Members.Length == 0 || Members[0].Parent == null || !(Members[0].Parent is GroupMember)) return;
            var Group = Members[0].Parent as GroupMember;
            foreach (var m in Members) DoMoveFromGroup(m);
            var M = Group.Parent == null || Group.Parent.Level != Group.Level
                ? Group.Level.Members
                : Group.Parent.Children;
            if (!IsUpdating) M.Sort(Dimension.Grid, FSortType);
            Group.Level.FDepth = GetDefaultLevelDepth();
            foreach (var m in Group.Level.Members)
                Group.Level.FDepth = Math.Max(Group.Level.FDepth, m.ChildrenDepth);
            Dimension.Grid.FEngine.ClearIncludedMetalines(Group.Level);
            if ((HierarchyState.hsActive & State) == HierarchyState.hsActive) Dimension.Grid.CellSet.Rebuild();
            CheckFlat();
            foreach (var m in Members)
                Dimension.Grid.EndChange(GridEventType.geMoveMemberFromGroup, m);
        }

        /// <summary>
        ///     If this property returns True, then the hierarchy is in its update state.
        /// </summary>
        public bool IsUpdating => FUpdatingLevel > 0;

        /// <summary>References to the complementary class of the CubeHierarchy cube level.</summary>
        /// <remarks>
        ///     <para>
        ///         This means that for this hierarchy the BeginUpdate method is called, and the
        ///         pair EndUpdate method is not.
        ///     </para>
        ///     <para>
        ///         If the hierarchy is in its update state, then the change of the Visible
        ///         property of its members does not lead to an immediate redrawing of the Grid. It
        ///         occurs at the point of the calling the EndUpdate function
        ///     </para>
        /// </remarks>
        public CubeHierarchy CubeHierarchy => FCubeHierarchy;

        /// <summary>A hierarchy name displayed as its caption in the Cube structure panel.</summary>
        /// <remarks>
        ///     <para>
        ///         A value of this property always equals a value of the DisplayName property of
        ///         the complementary CubeHierarchy object
        ///     </para>
        /// </remarks>
        public string DisplayName => FCubeHierarchy != null ? FCubeHierarchy.DisplayName : "";

        /// <summary>
        ///     Description of the hierarchy that appears as a pop-up window (tooltip) when the
        ///     cursor is pointed at the specified hierarchy's caption in the Cube structure
        ///     panel.
        /// </summary>
        /// <remarks>
        ///     The value of this property always equals the value of the Description property of
        ///     the complementary CubeHierarchy object.
        /// </remarks>
        public string Description => FCubeHierarchy != null ? FCubeHierarchy.Description : "";

        public HierarchyDataType TypeOfData =>
            FCubeHierarchy != null ? FCubeHierarchy.FTypeOfMembers : HierarchyDataType.htCommon;

        /// <summary>
        ///     Specifies a grouping algorithm of the hierarchy members in buckets.
        /// </summary>
        /// <remarks>
        ///     This property's value always equals the value of the
        ///     CubeHierarchy.RangeTransformMethod property
        /// </remarks>
        public DiscretizationMethod RangeTransformMethod => FCubeHierarchy != null
            ? FCubeHierarchy.FDiscretizationMethod
            : DiscretizationMethod.dmNone;

        /// <summary>
        ///     Specifies a number of bucket members for the retrieved hierarchy members to be
        ///     grouped in.
        /// </summary>
        /// <remarks>
        ///     The value of this property always equals the value of the
        ///     CubeHierarchy.RangeMembersCount property
        /// </remarks>
        public int RangeMembersCount => FCubeHierarchy != null ? FCubeHierarchy.FDiscretizationBucketCount : 0;

        /// <summary>
        ///     Specifies the type (flat, parent-child or multilevel) of the
        ///     hierarchy.
        /// </summary>
        /// <remarks>
        ///     The value of this property always equals the value of the Origin property of the
        ///     complementary CubeHierarchy object
        /// </remarks>
        public HierarchyOrigin Origin => FCubeHierarchy != null ? FCubeHierarchy.FOrigin : HierarchyOrigin.hoUnknown;

        /// <summary>The collection of hierarchy levels represented by the Level type objects.</summary>
        /// <remarks>
        ///     <para>
        ///         Flat (Origin = hoAttribute) and parent-child hierarchies (Origin =
        ///         hoParentChild) have only one level. Multilevel hierarchies have two and
        ///         more levels.
        ///     </para>
        ///     <para>
        ///         Available only after activation the Cube. When the Cube is inactive, this
        ///         list is either empty or null
        ///     </para>
        /// </remarks>
        public Levels Levels => FLevels;

        /// <summary>
        ///     The list of attributes (fields containing additional information) for the members
        ///     of the specified hierarchy.
        /// </summary>
        public InfoAttributes InfoAttributes => FCubeHierarchy != null ? FCubeHierarchy.FInfoAttributes : null;

        /// <summary>References to the Dimension object containing the specified hierarchy.</summary>
        public Dimension Dimension => FDimension;

        /// <summary>
        ///     Describes the current state of the hierarchy (whether it is initialized,
        ///     collapsed or expanded, placed in the active area).
        /// </summary>
        public HierarchyState State
        {
            get
            {
                var Result = HierarchyState.hsNone;
                if (!FInitialized) return Result;
                Result = HierarchyState.hsInitialized;
                IList<Hierarchy> PA = Dimension.Grid.FLayout.fRowAxis;

                foreach (var h in PA)
                    if (h == this)
                    {
                        Result |= HierarchyState.hsActive;
                        break;
                    }
                if ((HierarchyState.hsActive & Result) == 0)
                {
                    PA = Dimension.Grid.FLayout.fColumnAxis;
                    foreach (var h in PA)
                        if (h == this)
                        {
                            Result |= HierarchyState.hsActive;
                            break;
                        }
                }
                if ((HierarchyState.hsActive & Result) == 0)
                {
                    PA = Dimension.Grid.FLayout.fDetailsAxis;
                    foreach (var h in PA)
                        if (h == this)
                        {
                            Result |= HierarchyState.hsActive;
                            break;
                        }
                }

                if ((HierarchyState.hsActive & Result) == 0)
                    if (this == Dimension.Grid.FLayout.ColorBackAxisItem ||
                        this == Dimension.Grid.FLayout.fColorForeAxisItem ||
                        this == Dimension.Grid.FLayout.fSizeAxisItem ||
                        this == Dimension.Grid.FLayout.fShapeAxisItem)
                        Result |= HierarchyState.hsActive;
                if ((HierarchyState.hsActive & Result) != HierarchyState.hsActive || !Dimension.Grid.Active)
                    return Result;

                var PA1 = Dimension.Grid.CellSet.FRowLevels;
                foreach (var l in PA1)
                    if (l.FLevel.Hierarchy == this) return Result | HierarchyState.hsActiveExpanded;
                PA1 = Dimension.Grid.CellSet.FColumnLevels;
                foreach (var l in PA1)
                    if (l.FLevel.Hierarchy == this) return Result | HierarchyState.hsActiveExpanded;
                return Result;
            }
        }

        /// <summary>
        ///     Shows whether the filter is applied to this hierarchy (if among all members of
        ///     this hierarchy there is at least one whose Visible property is set to False).
        /// </summary>
        public bool Filtered => FFiltered;

        /// <summary>
        ///     Defines whether the hierarchy has context filters applied on at least one level.
        /// </summary>
        /// <remarks>See the Filter class description for the details</remarks>
        public bool FilteredByLevelFilters
        {
            get
            {
                if (Levels == null) return false;
                foreach (var l in Levels)
                    if (l.Filter != null) return true;
                return false;
            }
        }

        internal bool IsFullyFetched
        {
            get { return Levels.All(item => item.CubeLevel.IsFullyFetched); }
        }

        private void DoFilteredMembersCaptions(Member root, StringBuilder sb, bool useVisible)
        {
            Members ms = null;
            if (root == null)
                ms = Levels[0].Members;
            else if (root.NextLevelChildren.Count > 0)
                ms = root.NextLevelChildren;
            else
                ms = root.Children;
            foreach (var m in ms)
                if (m.Filtered & m.Visible)
                {
                    DoFilteredMembersCaptions(m, sb, useVisible);
                }
                else if (m.Visible == useVisible)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");
                    if (m.DisplayName.IsNullOrEmpty() && m.Data is byte[])
                    {
#warning TODO(ppti) replace to pictures
                        sb.Append("[..]");
                    }
                    else
                    {
                        sb.Append(m.DisplayName);
                    }
                }
            if (sb.ToString() == "" && useVisible)
                sb.Append(RadarUtils.GetResStr("rsAllMembersHidden"));
        }

        /// <summary>
        ///     Describes the filter applied to the hierarchy.
        /// </summary>
        public string FilterDescription
        {
            get
            {
                if (FFiltered)
                {
                    var loaded = IsFullyFetched;
                    var sb = new StringBuilder();
                    if (!loaded && UnfetchedMembersVisible)
                    {
                        DoFilteredMembersCaptions(null, sb, false);

                        return string.Format(RadarUtils.GetResStr("rsAllExcept"),
                            sb.ToString());
                    }

                    if (!loaded && !UnfetchedMembersVisible)
                    {
                        DoFilteredMembersCaptions(null, sb, true);
                        return sb.ToString();
                    }
                    DoFilteredMembersCaptions(null, sb, false);

                    var except = string.Format(RadarUtils.GetResStr("rsAllExcept"),
                        sb.ToString());
                    sb = new StringBuilder();
                    DoFilteredMembersCaptions(null, sb, true);
                    var filtered = sb.ToString();
                    if (filtered.Length < except.Length)
                        return filtered;
                    return except;
                }
                if (FilteredByLevelFilters)
                {
                    var sb = new StringBuilder();
                    if (FLevels.Count == 1 && FLevels[0].Filter != null)
                        return FLevels[0].Filter.Description;
                    foreach (var l in FLevels)
                        if (l.Filter != null)
                        {
                            if (sb.Length > 0)
                                sb.Append(", ");
                            sb.Append(l.DisplayName + ": " + l.Filter.Description);
                        }
                    return sb.ToString();
                }
                return null;
            }
        }

        /// <summary>
        ///     Contains a unique string hierarchy identifier.
        /// </summary>
        /// <remarks>
        ///     The value of this property always equals the value of the
        ///     CubeHierarchy.UniqueName property
        /// </remarks>
        public string UniqueName
        {
            get => FUniqueName;
            set => FUniqueName = value;
        }

        /// <summary>
        ///     The caption of the member containing an aggregated value of all hierarchy
        ///     members.
        /// </summary>
        /// <remarks>
        ///     The value of this property always equals the value of the
        ///     CubeHierarchy.TotalCaption property
        /// </remarks>
        public string TotalCaption { get; set; }

        /// <summary>
        ///     The sorting mode for all hierarchy members.
        /// </summary>
        /// <remarks>
        ///     Each hierarchy level can be sorted in its own way (for example, one level can be
        ///     sorted in the ascending order, another - in descending). However, if a value is
        ///     assigned to this property, all hierarchy levels will be sorted in the specified
        ///     way.
        /// </remarks>
        public MembersSortType SortType
        {
            get => FSortType;
            set => FSortType = value;
        }

        internal string FormatString
        {
            get => FFormatString;
            set => FFormatString = value;
        }

        /// <exclude />
        public DimensionCalculatedMembers CalculatedMembers => FCalculatedMembers;

        /// <exclude />
        public Groups MemberGroups => FMemberGroups;

        /// <summary>
        ///     Indicates where in the Grid the "Total" member for the specified hierarchy is
        ///     located (first, last or hidden).
        /// </summary>
        public TotalAppearance TotalAppearance
        {
            get => FTotalAppearance;
            set
            {
                if (FTotalAppearance != value)
                {
                    FTotalAppearance = value;
                    if (Dimension.Grid.Active)
                        Dimension.Grid.CellSet.Rebuild();
                }
            }
        }

        /// <summary>
        ///     Indicates whether the sorting functions of the specified hierarchy are available
        ///     to an end user through the Grid interface and the Hierarchy Editor.
        /// </summary>
        /// <remarks>By default set to <em>True</em></remarks>
        public bool AllowResort
        {
            get => FAllowResort;
            set => FAllowResort = value;
        }

        /// <summary>
        ///     By setting this property to True, a programmer can replace the standard sorting
        ///     procedures of the hierarchy members with his own which are called when an end user
        ///     switches between the sorting modes.
        /// </summary>
        /// <remarks>
        ///     If a programmer sets this property to True, then he has to handle the
        ///     OlapGrid.OnMemberSort event.
        /// </remarks>
        public bool OverrideSortMethods
        {
            get => FOverrideSortMethods;
            set => FOverrideSortMethods = value;
        }

        /// <summary>
        ///     Indicates whether it is allowed for an end user to change the position of Total
        ///     cells in the specified hierarchy in the Hierarchy Editor.
        /// </summary>
        /// <remarks>By default set to <em>True</em>.</remarks>
        public bool AllowChangeTotalAppearance
        {
            get => FAllowChangeTotalAppearance;
            set => FAllowChangeTotalAppearance = value;
        }

        /// <summary>
        ///     Defines whether it is possible to filter hierarchy members in the Hierarchy
        ///     Editor.
        /// </summary>
        /// <remarks>By default is set to <em>True</em>.</remarks>
        public bool AllowFilter
        {
            get => FAllowFilter;
            set => FAllowFilter = value;
        }

        /// <summary>
        ///     Indicates whether it is possible for an end user to create/delete/modify groups
        ///     of the specified hierarchy through the Grid interface and the Hierarchy Editor.
        /// </summary>
        /// <remarks>By default is set to <em>True</em>.</remarks>
        public bool AllowRegroup
        {
            get
            {
                if (Origin == HierarchyOrigin.hoNamedSet) return false;
                return FAllowRegroup;
            }
            set => FAllowRegroup = value;
        }

        /// <summary>
        ///     Defines whether a hierarchy editor icon appears on the Grid panels with the names
        ///     of hierarchies and its levels.
        /// </summary>
        /// <summary>
        ///     Defines whether an end user is allowed to filter hierarchies through Hierarchies
        ///     Editor.
        /// </summary>
        /// <remarks>By default is set to <em>True</em>.</remarks>
        public bool AllowHierarchyEditor { get; set; } = true;

        /// <summary>
        ///     Defines whether it is possible to call a popup menu by right-clicking on a
        ///     hierarchy or level caption cell.
        /// </summary>
        /// <remarks>
        ///     <para>By default is set to <em>True</em>.</para>
        ///     <para>
        ///         Text of the additional description of the hierarchy level is stored in the
        ///         Level.Description property. A tooltip is not depicted if this property contains an
        ///         empty string.
        ///     </para>
        /// </remarks>
        public bool AllowPopupOnLevelCaptions
        {
            get => FAllowPopupOnCaptions;
            set => FAllowPopupOnCaptions = value;
        }

        /// <summary>
        ///     Defines whether it is possible to call a popup menu by right-clicking on a
        ///     hierarchy member cell
        /// </summary>
        public bool AllowPopupOnMembers
        {
            get => FAllowPopupOnLabels;
            set => FAllowPopupOnLabels = value;
        }

        /// <summary>
        ///     Allows you to swap the hierarchy members in the Grid using the
        ///     drag-n-drop.
        /// </summary>
        public bool AllowSwapMembers
        {
            get => FAllowSwapMembers;
            set => FAllowSwapMembers = value;
        }

        /// <summary>
        ///     Allows you to use the mutliselect feature in filters.
        /// </summary>
        public bool AllowMultiselect
        {
            get => FAllowMultiselect;
            set => FAllowMultiselect = value;
        }

        public Level FirstVisibleLevel()
        {
            return FLevels != null ? FLevels.FirstOrDefault(l => l.Visible) : null;
        }
        //internal Level LastVisibleLevel()
        //{
        //    return FLevels != null ? FLevels.LastOrDefault(l => l.Visible) : null;
        //}

        /// <summary>
        ///     Indicates whether the hierarchy is visible to an end user in the Structure cube
        ///     window.
        /// </summary>
        public bool Visible
        {
            get => FVisible;
            set => FVisible = value;
        }

        /// <summary>
        ///     Indicates whether during the aggregation of the Cube cells the visibility state
        ///     of the specified hierarchy is taken into account.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If this property is set to True, then the contents of the Total-cells for the
        ///         specified hierarchy will be aggregated taking into consideration the
        ///         visibility/invisibility status of its members.
        ///     </para>
        ///     <para>
        ///         If this property is set to False, then the Total-cells for the specified
        ///         hierarchy will be aggregated as if all the members of the specified hierarchy were
        ///         visible.
        ///     </para>
        ///     <para>
        ///         In that case the only thing the visibility/invisibility status of a hierarchy
        ///         member will define - whether the member will be visible or invisible in the Grid,
        ///         when the hierarchy is in its expanded state.
        ///     </para>
        /// </remarks>
        public bool TakeFiltersIntoCalculations
        {
            get => FTakeFiltersIntoCalculations;
            set
            {
                if (FTakeFiltersIntoCalculations == value) return;
                FTakeFiltersIntoCalculations = value;
                if (FLevels == null) return;
                var E = Dimension.Grid.Engine;
                foreach (var l in FLevels) E.ClearDependedMetalines(l);
                var i = Dimension.Grid.FFilteredHierarchies.IndexOf(this);
                if (Origin != HierarchyOrigin.hoNamedSet)
                    if (FFiltered && FTakeFiltersIntoCalculations)
                    {
                        if (i < 0) Dimension.Grid.FFilteredHierarchies.Add(this);
                    }
                    else
                    {
                        if (i >= 0) Dimension.Grid.FFilteredHierarchies.RemoveAt(i);
                    }
                if (Dimension.Grid.CellSet != null) Dimension.Grid.CellSet.Rebuild();
            }
        }

        /// <summary>
        ///     If set to True, then the hierarchy members whose aggregated data does not exist
        ///     in the current OLAP slice (i.e. rows or columns of the Grid in the data area
        ///     corresponding to that member will be empty) are displayed in the Grid.
        /// </summary>
        public bool ShowEmptyLines
        {
            get => Dimension != null && Dimension.Grid != null &&
                   Dimension.Grid.CellsetMode == CellsetMode.cmChart
                ? true
                : FShowEmptyLines;
            set
            {
                if (FShowEmptyLines != value)
                {
                    FShowEmptyLines = value;
                    if (FInitialized)
                        FDimension.FGrid.FEngine.ClearIncludedHierarchy(this);
                    if ((HierarchyState.hsActive & State) == HierarchyState.hsActive)
                        Dimension.Grid.CellSet.Rebuild();
                }
            }
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgHierarchy);

            StreamUtils.WriteTag(writer, Tags.tgHierarchy_UniqueName);
            StreamUtils.WriteString(writer, FUniqueName);

            if (!FAllowChangeTotalAppearance)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_DenyChangeTotalAppearance);

            if (!FAllowFilter)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_DenyFilter);

            if (!AllowHierarchyEditor)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_DenyHierarchyEditor);

            if (!FAllowPopupOnCaptions)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_DenyPopupOnCaptions);

            if (!FAllowPopupOnLabels)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_DenyPopupOnLabels);

            if (!FAllowRegroup)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_DenyRegroup);

            if (!FAllowResort)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_DenyResort);

            if (!FAllowSwapMembers)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_DenySwapMembers);

            if (!FAllowMultiselect)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_DenyMultiselect);

            if (!string.IsNullOrEmpty(FDisplayName))
            {
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_DisplayName);
                StreamUtils.WriteString(writer, FDisplayName);
            }

            if (FFiltered)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_Filtered);

            if (!string.IsNullOrEmpty(FFormatString))
            {
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_FormatString);
                StreamUtils.WriteString(writer, FFormatString);
            }

            if (FInitialized)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_Initialized);

            if (fIntelligence.Count > 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_Intelligence);
                StreamUtils.WriteList(writer, fIntelligence);
            }

            if (FLevels != null && FLevels.Count > 0)
                StreamUtils.WriteStreamedObject(writer, FLevels, Tags.tgHierarchy_Levels);

            if (FOverrideSortMethods)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_OverrideSortMethods);

            if (FShowEmptyLines)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_ShowEmptyLines);

            if (FSortType != MembersSortType.msTypeRelated)
            {
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_SortType);
                StreamUtils.WriteInt32(writer, (int) FSortType);
            }

            if (!FTakeFiltersIntoCalculations)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_NotTakeFiltersIntoCalculations);

            if (FTotalAppearance != TotalAppearance.taLast)
            {
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_TotalAppearance);
                StreamUtils.WriteInt32(writer, (int) FTotalAppearance);
            }

            if (TotalCaption != RadarUtils.GetResStr("rsTotalCaption"))
            {
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_TotalCaption);
                StreamUtils.WriteString(writer, TotalCaption);
            }

            if (!FUnfetchedMembersVisibility)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_NotUnfetchedMembersVisibility);

            if (!FVisible)
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_NotVisible);

            if (FLevels != null)
            {
                StreamUtils.WriteTag(writer, Tags.tgHierarchy_MembersStream);
                WriteStream(writer);
            }

            StreamUtils.WriteTag(writer, Tags.tgHierarchy_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgHierarchy);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgHierarchy_MembersStream:
                        ReadStream(reader);
                        Sort();
                        break;
                    case Tags.tgHierarchy_NotVisible:
                        FVisible = false;
                        break;
                    case Tags.tgHierarchy_NotUnfetchedMembersVisibility:
                        FUnfetchedMembersVisibility = false;
                        break;
                    case Tags.tgHierarchy_TotalCaption:
                        TotalCaption = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgHierarchy_TotalAppearance:
                        FTotalAppearance = (TotalAppearance) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgHierarchy_NotTakeFiltersIntoCalculations:
                        FTakeFiltersIntoCalculations = false;
                        break;
                    case Tags.tgHierarchy_SortType:
                        FSortType = (MembersSortType) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgHierarchy_ShowEmptyLines:
                        FShowEmptyLines = true;
                        break;
                    case Tags.tgHierarchy_OverrideSortMethods:
                        FOverrideSortMethods = true;
                        break;
                    case Tags.tgHierarchy_Levels:
                        FLevels = new Levels(this);
                        StreamUtils.ReadStreamedObject(reader, FLevels);
                        break;
                    case Tags.tgHierarchy_Intelligence:
                        StreamUtils.CheckTag(reader, Tags.tgList);
                        fIntelligence.Clear();
                        for (var exit1 = false; !exit1;)
                        {
                            var tag1 = StreamUtils.ReadTag(reader);
                            switch (tag1)
                            {
                                case Tags.tgList_Count:
                                    var c = StreamUtils.ReadInt32(reader);
                                    break;
                                case Tags.tgList_Item:
                                    var it = new Intelligence(this);
                                    // BeforeRead
                                    StreamUtils.ReadStreamedObject(reader, it);
                                    // AfterRead
                                    fIntelligence.Add(it);
                                    break;
                                case Tags.tgList_EOT:
                                    exit1 = true;
                                    break;
                                default:
                                    StreamUtils.SkipValue(reader);
                                    break;
                            }
                        }
                        break;
                    case Tags.tgHierarchy_Initialized:
                        FInitialized = true;
                        break;
                    case Tags.tgHierarchy_Apearance:
                        StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgHierarchy_FormatString:
                        FFormatString = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgHierarchy_Filtered:
                        FFiltered = true;
                        break;
                    case Tags.tgHierarchy_DisplayName:
                        FDisplayName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgHierarchy_DenySwapMembers:
                        FAllowSwapMembers = false;
                        break;
                    case Tags.tgHierarchy_DenyMultiselect:
                        FAllowMultiselect = false;
                        break;
                    case Tags.tgHierarchy_DenyResort:
                        FAllowResort = false;
                        break;
                    case Tags.tgHierarchy_DenyRegroup:
                        FAllowRegroup = false;
                        break;
                    case Tags.tgHierarchy_DenyPopupOnLabels:
                        FAllowPopupOnLabels = false;
                        break;
                    case Tags.tgHierarchy_DenyPopupOnCaptions:
                        FAllowPopupOnCaptions = false;
                        break;
                    case Tags.tgHierarchy_DenyHierarchyEditor:
                        AllowHierarchyEditor = false;
                        break;
                    case Tags.tgHierarchy_DenyFilter:
                        FAllowFilter = false;
                        break;
                    case Tags.tgHierarchy_DenyChangeTotalAppearance:
                        FAllowChangeTotalAppearance = false;
                        break;
                    case Tags.tgHierarchy_UniqueName:
                        FUniqueName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgHierarchy_EOT:
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

        internal static Hierarchy getFromData(object AData)
        {
            var res = AData as Hierarchy;
            if (res != null)
                return res;
            var level = AData as Level;
            if (level != null)
            {
                res = level.Hierarchy;
                if (res == null)
                    res = level.Hierarchy;
            }

            return res;
        }
    }
}