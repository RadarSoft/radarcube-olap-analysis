using System;
using System.Collections.Generic;
using System.ComponentModel;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    public class SerializedLevel
    {
        private readonly Level level;
        public SerializedCalculatedMember[] CalculatedMembers;
        public SerizalizedFilter Filter;
        public SerializedGroupMember[] GroupMembers;

        [DefaultValue(MembersSortType.msTypeRelated)] public MembersSortType SortType = MembersSortType.msTypeRelated;

        public string[] UnvisibleMembers;

        [DefaultValue(false)] public bool Visible;

        public AttributeDispalyMode[] VisibleAttributeModes;
        public string[] VisibleAttributes;
        public string[] VisibleMembers;

        public SerializedLevel()
        {
        }

        public SerializedLevel(Level L)
        {
            level = L;
            Visible = L.Visible;
            if (L.Filter != null)
                Filter = new SerizalizedFilter(L.Filter);
            SortType = L.SortType;
            if (L.Hierarchy.Levels.Count > 0)
            {
                var visible = new List<string>();
                var unvisible = new List<string>();
                foreach (var m in L.Hierarchy.Levels[0].Members)
                {
                    if (m.FLevel == level)
                    {
                        if (m.Visible && !m.Filtered && !L.Hierarchy.UnfetchedMembersVisible)
                            visible.Add(m.UniqueName);

                        if (!m.Visible && L.Hierarchy.UnfetchedMembersVisible)
                            unvisible.Add(m.UniqueName);
                    }
                    FillMembersList(m, visible, unvisible);
                }
                if (unvisible.Count > 0)
                    UnvisibleMembers = unvisible.ToArray();
                if (visible.Count > 0)
                    VisibleMembers = visible.ToArray();

                var va = new Dictionary<string, AttributeDispalyMode>();
                foreach (var ia in L.CubeLevel.InfoAttributes)
                    if (ia.DisplayMode != AttributeDispalyMode.None)
                        va.Add(ia.DisplayName, ia.DisplayMode);
                if (va.Count > 0)
                {
                    VisibleAttributes = new string[va.Count];
                    VisibleAttributeModes = new AttributeDispalyMode[va.Count];
                    va.Keys.CopyTo(VisibleAttributes, 0);
                    va.Values.CopyTo(VisibleAttributeModes, 0);
                }
            }


            var lg = new List<SerializedGroupMember>();
            var lc = new List<SerializedCalculatedMember>();

            foreach (var m in L.FUniqueNamesArray.Values)
            {
                if (m is GroupMember)
                    lg.Add(new SerializedGroupMember(m as GroupMember));
                if (m is CalculatedMember)
                    lc.Add(new SerializedCalculatedMember(m as CalculatedMember));
            }
            if (lg.Count > 0)
                GroupMembers = lg.ToArray();
            if (lc.Count > 0)
                CalculatedMembers = lc.ToArray();
        }

        internal bool Restore(Level L)
        {
            if (L == null)
                throw new ArgumentNullException("L");

            var result = false;

            if (Filter != null)
            {
                var ff = new Filter(L, Filter.FilterType,
                    Filter.MeasureName != null
                        ? L.Grid.Measures.Find(Filter.MeasureName)
                        : null, Filter.Condition, Filter.FirstValue, Filter.SecondFalue);

                if (Filter.MDXLevelName != null)
                    ff.MDXLevelName = Filter.MDXLevelName;
                L.FFilter = ff;
            }

            L.FVisible = Visible;
            L.FSortType = SortType;
            if (L.CubeLevel != null)
                L.Grid.FEngine.FLevelsList[L.CubeLevel.ID] = L;

            if (VisibleAttributes != null)
                for (var i = 0; i < VisibleAttributes.Length; i++)
                {
                    var ia = L.CubeLevel.InfoAttributes.Find(VisibleAttributes[i]);
                    if (ia != null)
                        ia.DisplayMode = VisibleAttributeModes[i];
                }
            else
                for (var i = 0; i < L.CubeLevel.InfoAttributes.Count; i++)
                    L.CubeLevel.InfoAttributes[i].DisplayMode = AttributeDispalyMode.None;

            var members = new HashSet<string>();

            if (VisibleMembers != null)
                foreach (var s in VisibleMembers)
                    members.Add(s);

            if (UnvisibleMembers != null)
                foreach (var s in UnvisibleMembers)
                    members.Add(s);

            if (GroupMembers != null)
                foreach (var gm_ in GroupMembers)
                {
                    members.Remove(gm_.UniqueName);
                    if (gm_.Children != null)
                        foreach (var s in gm_.Children)
                            members.Add(s);
                }

            if (members.Count > 0)
                L.Grid.Cube.RetrieveAscendants(L.Hierarchy, members);

            if (GroupMembers != null)
                foreach (var gm in GroupMembers)
                {
                    var l = new List<Member>();
                    if (gm.Children != null)
                        foreach (var s in gm.Children)
                        {
                            var m = L.Hierarchy.FindMemberByUniqueName(s);
                            if (m != null) l.Add(m);
                        }
                    var gm_ = L.Hierarchy.FindMemberByUniqueName(gm.UniqueName) as GroupMember;

                    if (gm_ == null)
                    {
                        L.Hierarchy.CreateGroup(gm.DisplayName, gm.Description,
                            L, L.Hierarchy.FindMemberByUniqueName(gm.Parent), true, gm.Position, l);
                    }
                    else
                    {
                        L.Hierarchy.ClearGroup(gm_);
                        L.Hierarchy.MoveToGroup(gm_, l.ToArray());
                    }
                }

            if (CalculatedMembers != null)
                foreach (var cm in CalculatedMembers)
                    if (L.Hierarchy.FindMemberByUniqueName(cm.UniqueName) == null)
                    {
                        var m = L.Hierarchy.CreateCalculatedMember(cm.DisplayName, cm.Description,
                            L, L.Hierarchy.FindMemberByUniqueName(cm.Parent), cm.Position);
                        if (!string.IsNullOrEmpty(cm.Expression))
                            m.Expression = cm.Expression;
                    }

            if (VisibleMembers != null)
                foreach (var s in VisibleMembers)
                {
                    var m = L.FindMember(s);
                    if (m != null)
                    {
                        m.Visible = true;
                        if (m.Children != null)
                            foreach (var c in m.Children)
                                c.Visible = true;
                        result = true;
                    }
                }

            if (UnvisibleMembers != null)
                foreach (var s in UnvisibleMembers)
                {
                    var m = L.FindMember(s);
                    if (m != null)
                    {
                        m.Visible = false;
                        result = true;
                    }
                }
            return result;
        }

        private void FillMembersList(Member member, List<string> visible, List<string> unvisible)
        {
            if (!member.Visible || !member.Filtered)
                return;

            foreach (var m in member.Children)
            {
                if (m.FLevel == level)
                {
                    if (m.Visible && !m.Filtered)
                        visible.Add(m.UniqueName);
                    if (!m.Visible)
                        unvisible.Add(m.UniqueName);
                }
                FillMembersList(m, visible, unvisible);
            }
            foreach (var m in member.NextLevelChildren)
            {
                if (m.FLevel == level)
                {
                    if (m.Visible && !m.Filtered)
                        visible.Add(m.UniqueName);
                    if (!m.Visible)
                        unvisible.Add(m.UniqueName);
                }
                FillMembersList(m, visible, unvisible);
            }
        }
    }
}