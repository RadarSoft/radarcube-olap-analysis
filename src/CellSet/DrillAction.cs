using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.CellSet
{
    [DebuggerDisplay(
        "Level={Level} ParentLevel={ParentLevel} ParentMember={ParentMember} Members={MembersAsString} DrillUps={DrillUpsAsString} ")]
    internal class DrillAction
    {
        /// <summary>
        ///     |
        /// </summary>
        internal const string __SEPARATOR = "|";

        /// <summary>
        ///     ~%~
        /// </summary>
        internal const string __INNER_SEP = "~%~";

        /// <summary>
        ///     |%|
        /// </summary>
        internal const string __MAIN_SEP = "|%|";

        internal Level Level;
        internal Level ParentLevel;
#if DEBUG
        internal DebugLinkedList<Member> Members = new DebugLinkedList<Member>();
#else
        internal LinkedList<Member> Members = new LinkedList<Member>();
#endif
        //internal HashSet<Member> DrillUps;
        internal HashSet<Member> DrillUps = new HashSet<Member>();

        internal DrillAction()
        {
        }

        internal DrillAction(Level level, CellsetMember member)
            : this()
        {
            ParentLevel = member.FMember.Level;
            Level = level;
            var cur = member;
            while (cur != null)
            {
                if (cur.FMember != null)
                    Members.AddLast(cur.FMember);
                cur = cur.FParent;
            }
        }

        //internal static PossibleDrillActions GetDrilledAction(Member m, HashSet<DrillAction> actions)
        //{
        //    return GetDrilledAction(m, actions, null);
        //}

        internal static PossibleDrillActions GetDrilledAction(Member m, HashSet<DrillAction> actions, CellsetMember ASM)
        {
            if (m.FMemberType == MemberType.mtGroup && m.Children.Count == 0)
                return PossibleDrillActions.esNone;

            foreach (var a in actions)
                if (ASM == null)
                {
                    if (a.Members.Count > 0 && a.Members.First() == m)
                        return a.Method;

                    if (a.ParentLevel == m.Level && a.DrillUps != null && !a.DrillUps.Contains(m))
                        return a.Method;
                }
                else
                {
                    if (ASM.IsThisDrillAction(a))
                        return a.Method;

                    if (ASM.FMember.Children.Count == 0)
                        if (a.ParentLevel == m.Level
                            && a.DrillUps != null
                            && !a.DrillUps.Contains(m)
                            && a.IsAllDrilled)
                            return a.Method;
                }

            return PossibleDrillActions.esNone;
        }

        public PossibleDrillActions Method
        {
            get
            {
                if (Level == ParentLevel) return PossibleDrillActions.esParentChild;
                return Level.Hierarchy == ParentLevel.Hierarchy
                    ? PossibleDrillActions.esNextLevel
                    : PossibleDrillActions.esNextHierarchy;
            }
        }

        public bool IsAllDrilled
        {
            get
            {
                if (ParentLevel == Level)
                    return false;

                var m = Members.FirstOrDefault();
                if (m == null)
                    return true;

                return m.Level != ParentLevel;
            }
        }

        public Member ParentMember
        {
            get
            {
                var m = Members.FirstOrDefault();
                return m != null && m.Level == ParentLevel ? m : null;
            }
        }

        public override string ToString()
        {
            var sb = new string[4];
            if (Level != null)
                sb[0] = Level.ToString();
            if (ParentLevel != null)
                sb[1] = ParentLevel.ToString();
            if (Members != null)
                sb[2] = MembersAsString;
            if (DrillUps != null)
                sb[3] = DrillUpsAsString;

            return string.Join(__MAIN_SEP, sb);
        }

        internal string MembersAsString
        {
            get { return string.Join(__INNER_SEP, Members.Select(item => item.UniqueName).ToArray()); }
        }

        internal string DrillUpsAsString
        {
            get { return string.Join(__INNER_SEP, DrillUps.Select(item => item.UniqueName).ToArray()); }
        }

        public static DrillAction FromString(OlapControl grid, string str)
        {
            var da = new DrillAction();
            var sb = str.Split(new[] {__MAIN_SEP}, StringSplitOptions.None);
            da.Level = grid.Dimensions.FindLevel(sb[0]);
            da.ParentLevel = grid.Dimensions.FindLevel(sb[1]);
            if (da.Level == null || da.ParentLevel == null)
                return null;
            var m = sb[2].Split(new[] {__INNER_SEP}, StringSplitOptions.None);
            foreach (var s in m)
            {
#if DEBUG
                if (s == "[Product].[All Products].[Drink].[Alcoholic Beverages]")
                {
                }
#endif
                if (string.IsNullOrEmpty(s))
                    continue;
                var member = grid.FindMemberByName(s);
                if (member == null)
                {
                    var tm = grid.Measures.Find(s);
                    if (tm == null)
                        return null;

                    member = tm.Member;
                    if (member == null)
                        return null;
                }
                da.Members.AddLast(member);
            }
            m = sb[3].Split(new[] {__INNER_SEP}, StringSplitOptions.None);

            if (da.IsAllDrilled)
                da.DrillUps = new HashSet<Member>();

            foreach (var s in m)
            {
                if (string.IsNullOrEmpty(s))
                    continue;

                var member = grid.FindMemberByName(s);
                if (member == null)
                {
                    member = grid.Measures.Find(s).Member;
                    if (member == null)
                        return null;
                }

                if (da.DrillUps == null)
                    da.DrillUps = new HashSet<Member>();
                da.DrillUps.Add(member);
            }
            return da;
        }

        internal string GetUniqueName()
        {
            return ParentLevel.UniqueName + __SEPARATOR + Level.UniqueName + __SEPARATOR + Method;
        }

        /// <summary>
        ///     ParentLevel.UniqueName + "|" + Level.UniqueName + "|" + Method.ToString();
        /// </summary>
        internal string UniqueName => GetUniqueName();
    }
}