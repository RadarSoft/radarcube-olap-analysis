using System;
using System.Collections.Generic;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Events;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>The list of hierarchy members.</summary>
    /// <moduleiscollection />
    public class Members : List<Member>
    {
        internal CubeMembers FCubeMembers;
        internal Level FParentLevel;
        internal Member FParentMember;

        internal Members()
            : base(0)
        {
        }

        /// <summary>
        ///     References to the sorresponding list of hierarchy members on the Cube
        ///     level.
        /// </summary>
        public CubeMembers CubeMembers => FCubeMembers;

        /// <summary>
        ///     The parent of the current member.
        /// </summary>
        public Member ParentMember => FParentMember;

#if DEBUG
        public new void Add(Member item)
        {
            if (item.UniqueName.IsFill() &&
                item.UniqueName.Contains(
                    "[Product].[All Products].[Drink].[Alcoholic Beverages].[Beer and Wine].[Beer].[Walrus]"))
            {
            }
            base.Add(item);
        }
#endif

        internal void RestoreAfterSerialization(CubeMembers cm)
        {
            FCubeMembers = cm;
            foreach (var m in this)
                m.RestoreAfterSerialization();
        }

        internal static void Sort(List<Member> members, MembersSortType ASortType)
        {
            //if (members.Count < 2) return;
            if (members.Count < 1) return;

            var FParentLevel = members[0].FLevel;
            var Grid = FParentLevel.Grid;

            if (members.Count > 1)
                if (FParentLevel.FHierarchy != null && FParentLevel.FHierarchy.OverrideSortMethods &&
                    Grid.EventMemberSortAssigned)
                {
                    members.Sort(new CustomComparer(Grid, ASortType));
                }
                else
                {
                    if (ASortType == MembersSortType.msTypeRelated) members.Sort(new NumberComparer());
                    if (ASortType == MembersSortType.msNameAsc) members.Sort(new AlphaAscComparer());
                    if (ASortType == MembersSortType.msNameDesc) members.Sort(new AlphaDescComparer());
                }

            var P = new List<Member>();
            var IsSorted = true;

            do
            {
                IsSorted = true;
                var ij = -1;
                foreach (var m in members)
                {
                    m.Children.Sort(Grid, ASortType);
                    ij++;
                    if (m is CustomMember && !P.Contains(m))
                    {
                        if (((CustomMember) m).Position == CustomMemberPosition.cmpFirst)
                        {
                            members.Remove(m);
                            members.Insert(0, m);
                        }
                        if (((CustomMember) m).Position == CustomMemberPosition.cmpLast)
                        {
                            members.Remove(m);
                            members.Add(m);
                        }
                        if (((CustomMember) m).Position == CustomMemberPosition.cmpGeneralOrder) continue;
                        P.Add(m);
                        IsSorted = false;
                        break;
                    }
                }
            } while (!IsSorted);
        }

        internal void Sort(OlapControl Grid, MembersSortType ASortType)
        {
            Sort(this, ASortType);
        }

        internal void UpdateRanks(Level Level)
        {
            SortedDictionary<string, Member> ss = null;
            if (Level.LevelType == HierarchyDataType.htStrings)
                ss = new SortedDictionary<string, Member>(StringComparer.CurrentCultureIgnoreCase);
            for (var i = 0; i < Count; i++)
            {
                var m = this[i];
                try
                {
                    switch (Level.LevelType)
                    {
                        case HierarchyDataType.htCommon:
                            m.FRank = m.CubeMember != null ? m.CubeMember.Rank : i;
                            break;
                        case HierarchyDataType.htNumbers:
                            if (!double.TryParse(m.DisplayName, out m.FRank))
                                m.FRank = i;
                            break;
                        case HierarchyDataType.htStrings:
                            ss.Add(m.UniqueName, m);
                            break;
                        case HierarchyDataType.htDates:
                            m.FRank = m.CubeMember != null ? m.CubeMember.Rank : i;

                            //m.FRank = RadarUtils.StringToDateTime(m.FDisplayName, Level.FormatString);
                            break;
                    }
                }
                catch
                {
                    m.FRank = i;
                }

                if (m.FMemberType == MemberType.mtCalculated &&
                    ((CalculatedMember) m).Position == CustomMemberPosition.cmpFirst)
                    m.FRank = double.MinValue;
                if (m.FMemberType == MemberType.mtCalculated &&
                    ((CalculatedMember) m).Position == CustomMemberPosition.cmpLast)
                    m.FRank = double.MaxValue;
                if (m.Children.Count > 0)
                    m.Children.UpdateRanks(Level);
            }
            if (ss != null)
            {
                var i = 0;
                foreach (var mm in ss.Values)
                    mm.FRank = i++;
            }
        }

        internal void Initialize(CubeMembers ACubeMembers, Level ALevel, Member AParentMember)
        {
            FCubeMembers = ACubeMembers;
            FParentLevel = ALevel;
            FParentMember = AParentMember;
            if (ACubeMembers == null) return;
        }

        internal void SetDepth(int ADepth)
        {
            foreach (var m in this)
            {
                m.FDepth = ADepth;
                m.Children.SetDepth(ADepth + 1);
            }
        }

        internal void CheckFlat(bool FirstLevel)
        {
            foreach (var m in this)
            {
                if (m.FNextLevelChildren.Count > 0 && m.FChildren.Count > 0)
                    throw new Exception(string.Format(RadarUtils.GetResStr("rsHierarchyNotFlat"), m.DisplayName));
                m.Children.CheckFlat(FirstLevel);
                if (!FirstLevel && m.Parent == null)
                    throw new Exception(string.Format(RadarUtils.GetResStr("rsNoParentMember"), m.DisplayName));
            }
        }

        internal void SetVisible(bool Value, Hierarchy H)
        {
            foreach (var M in this)
            {
                if (M.Visible != Value)
                {
                    M.FVisible = Value;
                    H.UpdateFilterState(true);
                }
                M.FChildren.SetVisible(Value, H);
                M.FNextLevelChildren.SetVisible(Value, H);
            }
        }

        internal Member FindMemberByName1(string MemberName)
        {
            foreach (var M in this)
                if (MemberName == M.GetName1())
                    return M;
            foreach (var M in this)
            {
                var Result = M.Children.FindMemberByName1(MemberName);
                if (Result != null) return Result;
            }
            return null;
        }

        internal Member FindMemberByName2(string MemberName)
        {
            foreach (var M in this)
            {
                if (MemberName == M.GetName2()) return M;
                if (M.GetName2().StartsWith(MemberName))
                {
                    var Result = M.Children.FindMemberByName2(MemberName);
                    if (Result != null) return Result;
                }
            }
            return null;
        }

        /// <summary>
        ///     Moves an object - the hierarchy member (or a measure object), passed by the
        ///     Member parameter, to the list, specified by the Index parameter.
        /// </summary>
        public void MoveMember(Member Member, int IndexTo)
        {
            if (!Contains(Member)) return;

            if (Member.Level.Members != this)
            {
                var i = Member.Level.Members.IndexOf(this[IndexTo]);
                if (i >= 0)
                    Member.Level.Members.MoveMember(Member, i);
            }
            else
            {
                Remove(Member);
                Insert(IndexTo, Member);
            }
        }

        private class AlphaAscComparer : IComparer<Member>
        {
            public int Compare(Member x, Member y)
            {
                return string.Compare(x.DisplayName, y.DisplayName, true);
            }
        }

        private class AlphaDescComparer : IComparer<Member>
        {
            public int Compare(Member x, Member y)
            {
                return string.Compare(y.DisplayName, x.DisplayName, true);
            }
        }

        private class NumberComparer : IComparer<Member>
        {
            public int Compare(Member x, Member y)
            {
                return Math.Sign(x.FRank - y.FRank);
            }
        }

        private class CustomComparer : IComparer<Member>
        {
            private readonly EventMemberSortArgs E;
            private readonly OlapControl FGrid;
            private MembersSortType FMethod;

            internal CustomComparer(OlapControl AGrid, MembersSortType CurrentMethod)
            {
                FGrid = AGrid;
                FMethod = CurrentMethod;
                E = new EventMemberSortArgs(CurrentMethod);
            }

            public int Compare(Member x, Member y)
            {
                E.fLow = x;
                E.fHigh = y;
                E.Result = 0;
                FGrid.EventMemberSort(E);
                return E.Result;
            }
        }
    }
}