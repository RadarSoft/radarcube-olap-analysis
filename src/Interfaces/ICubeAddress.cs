using System;
using System.Collections.Generic;
using System.Linq;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Interfaces
{
    /// <summary>
    ///     Describes the cell address in a multidimensional OLAP cube, as well as cells as
    ///     aggregated data
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The cell address is specified by determined (i.e. not Total) dimension
    ///         members. Thus, if the property LevelsCount of the interface equals 0, then it means
    ///         that all the dimensions are in their undetermined states, and a cell described by
    ///         this interface is the grand Total cell, where the content of the whole table was
    ///         aggregated.
    ///     </para>
    ///     <para>For more details see Cube Cell Addrress.</para>
    /// </remarks>
    public class ICubeAddress : ICloneable
    {
        private Measure _cached_measure;
        private string _FMeasureID;
        internal OlapControl FGrid;
        internal int FHierID;

        internal SortedList<int, Member> FLevelsAndMembers;
        internal string FLineID;
        internal long FLineIdx;
        internal int FModeID;
        internal int FTag;

        public ICubeAddress(OlapControl AGrid)
        {
            FGrid = AGrid;
            FLineID = string.Empty;
            FLineIdx = 0;
            FHierID = 0;
            FMeasureID = string.Empty;
            FModeID = 0;
            FLevelsAndMembers = CreateSortedList();
        }

        internal ICubeAddress(OlapControl AGrid, string ALineID, int AHierID,
            long ALineIdx, string AMeasureID, int AModeID, int ATag)
        {
            FLevelsAndMembers = CreateSortedList();
            FGrid = AGrid;
            FLineID = ALineID;
            FHierID = AHierID;
            FLineIdx = ALineIdx;
            FMeasureID = AMeasureID;
            FModeID = AModeID;
            FTag = ATag;
            SilentInit();
        }

        internal ICubeAddress(OlapControl AGrid, string ALineID, int AHierID,
            long ALineIdx, string AMeasureID, int AModeID)
        {
            FLevelsAndMembers = CreateSortedList();
            FGrid = AGrid;
            FLineID = ALineID;
            FHierID = AHierID;
            FLineIdx = ALineIdx;
            FMeasureID = AMeasureID;
            FModeID = AModeID;
            SilentInit();
        }

        public ICubeAddress(OlapControl AGrid, IList<Member> AMembers)
        {
            FMeasureID = string.Empty;
            FModeID = 0;
            FLevelsAndMembers = CreateSortedList();
            FGrid = AGrid;
            var L = AMembers.Count;
            foreach (var m in AMembers)
                if (m.FMemberType == MemberType.mtMeasure)
                {
                    FMeasureID = m.UniqueName;
                }
                else
                {
                    if (m.FMemberType == MemberType.mtMeasureMode)
                        FModeID = m.Parent.Children.IndexOf(m);
                    else
                        FLevelsAndMembers.Add(m.FLevel.ID, m);
                }
            var M = AGrid.FEngine.GetMetaline(FLevelsAndMembers.Keys);
            FLineID = M.fID;
            FLineIdx = M.GetLineIdx(FLevelsAndMembers.Values);
            FHierID = M.GetHierID(FLevelsAndMembers.Values);
        }

        internal string FMeasureID
        {
            get => _FMeasureID;
            set
            {
                if (_FMeasureID == value)
                    return;

                _FMeasureID = value;
                _cached_measure = null;
            }
        }

        internal bool IsCalculatedByExpression
        {
            get
            {
                if (Measure != null && !string.IsNullOrEmpty(Measure.Expression)) return true;
                return FLevelsAndMembers.Values.Any(item => item is CalculatedMember &&
                                                            !string.IsNullOrEmpty(((CalculatedMember) item)
                                                                .Expression));
            }
        }

        public int Tag
        {
            get => FTag;
            set => FTag = value;
        }

        /// <summary>
        ///     Gets the measure of the multidimensional cube cell.
        /// </summary>
        public Measure Measure
        {
            get
            {
                if (_cached_measure != null)
                    return _cached_measure;
                _cached_measure = FMeasureID == string.Empty ? null : FGrid.Measures[FMeasureID];
                return _cached_measure;
            }
            set
            {
                if (value != null)
                    FMeasureID = value.UniqueName;
                else
                    FMeasureID = string.Empty;
            }
        }

        public MeasureShowMode MeasureMode
        {
            get
            {
                var m = Measure;
                if (m == null)
                    return null;
                return m.ShowModes[FModeID];
            }
            set
            {
                if (value == null)
                {
                    FMeasureID = string.Empty;
                    FModeID = 0;
                }
                else
                {
                    Measure = value.Measure;
                    FModeID = value.Measure.ShowModes.IndexOf(value);
                }
            }
        }

        /// <summary>
        ///     The quantity of multidimensional cube dimensions different from Total.
        /// </summary>
        public int LevelsCount => FLevelsAndMembers.Count;

        object ICloneable.Clone()
        {
            return Clone();
        }

        private SortedList<int, Member> CreateSortedList()
        {
            return new SortedList<int, Member>();
        }

        public override string ToString()
        {
            return FLineID + "|" + FHierID + "|" + FLineIdx + "|"
                   + FMeasureID + '|' + FModeID + "|" + FTag;
        }


        internal static ICubeAddress FromString(OlapControl grid, string str)
        {
            var a = new ICubeAddress(grid);
            var ss = str.Split('|');
            if (ss.Length != 6)
                throw new NotSupportedException("Bad ICubeAddress string");
            a.FLineID = ss[0];
            a.FHierID = int.Parse(ss[1]);
            a.FLineIdx = long.Parse(ss[2]);
            a.FMeasureID = ss[3];
            a.FModeID = int.Parse(ss[4]);
            a.FTag = int.Parse(ss[5]);
            a.SilentInit();
            return a;
        }

        internal void SilentInit()
        {
            FLevelsAndMembers.Clear();
            var M = FGrid.FEngine.GetMetaline(FLineID);
            M.FillMembers(FLineIdx, FLevelsAndMembers);
            FHierID = M.GetHierID(FLevelsAndMembers.Values);
        }

        private bool ClearHierarchy(Hierarchy H, Member current)
        {
            for (var i = FLevelsAndMembers.Count - 1; i >= 0; i--)
            {
                var m = FLevelsAndMembers.Values[i];

                if (m.FLevel.FHierarchy == H)
                {
                    if (current.Level.Index < m.Level.Index)
                        return false;
                    if (current.Level.Index == m.Level.Index && current.FDepth < m.FDepth)
                        return false;
                    FLevelsAndMembers.RemoveAt(i);
                }
            }
            return true;
        }

        /// <summary>
        ///     Indicates whether the specified hierarchy member is "determined" for the current
        ///     multidimensional cube address.
        /// </summary>
        /// <param name="M">A hierarchy member indicating the attribution to the current address</param>
        public bool HasMember(Member M)
        {
            return FLevelsAndMembers.Values.Contains(M);
        }

        /// <summary>
        ///     Determines a cell value according to a cube coordinate. The cube axis is determined by the property Level
        ///     of the Member object type passed as a measure, and by the object of the Member type, in fact,
        ///     is determined a specific value of the cube coordinate across this axis.
        /// </summary>
        /// <summary>
        ///     Determines (makes the coordinate assigned) a cell value according to a Cube
        ///     coordinate.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Cube axis represented by a hierarchy, that the specified member belongs to,
        ///         is determined by the level where the specified member is placed (the Levels
        ///         property) and by the specified hierarchy member.
        ///     </para>
        ///     <para>For more details see Cube Cell Address.</para>
        ///     <para>
        ///         A measure appropriate to the cell can be determined by setting the Measure
        ///         property.
        ///     </para>
        /// </remarks>
        public void AddMember(Member AMember)
        {
#if DEBUG
            if (AMember != null && AMember.DisplayName == "Q1")
            {
            }
#endif

            switch (AMember.FMemberType)
            {
                case MemberType.mtMeasure:
                case MemberType.mtMeasureMode:
                    if (AMember.FMemberType == MemberType.mtMeasure)
                        FMeasureID = AMember.UniqueName;
                    else
                        FModeID = AMember.Parent.Children.IndexOf(AMember);
                    break;
                default:
                    var H = AMember.FLevel.FHierarchy;
                    if (!ClearHierarchy(H, AMember))
                        return;

                    var i = FLevelsAndMembers.IndexOfKey(AMember.FLevel.ID);
                    if (i < 0)
                        FLevelsAndMembers.Add(AMember.FLevel.ID, AMember);
                    else
                        FLevelsAndMembers.Values[i] = AMember;

                    var M = FGrid.FEngine.GetMetaline(FLevelsAndMembers.Keys);
                    FLineID = M.fID;
                    FLineIdx = M.GetLineIdx(FLevelsAndMembers.Values);
                    FHierID = M.GetHierID(FLevelsAndMembers.Values);
                    break;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="members"></param>
        internal void AddMembersIfDeepMembersFirst(List<Member> members)
        {
            var lh = new List<Hierarchy>();
            foreach (var aMember in members)
                if (aMember.FMemberType != MemberType.mtMeasure && aMember.FMemberType != MemberType.mtMeasureMode)
                {
                    var h = aMember.FLevel.Hierarchy;
                    if (lh.Contains(h))
                        continue;
                    lh.Add(h);
                    var i = FLevelsAndMembers.IndexOfKey(aMember.FLevel.ID);
                    if (i < 0)
                        FLevelsAndMembers.Add(aMember.FLevel.ID, aMember);
                    else
                        FLevelsAndMembers.Values[i] = aMember;
                }
                else
                {
                    if (aMember.FMemberType == MemberType.mtMeasure)
                        FMeasureID = aMember.UniqueName;
                    else
                        FModeID = aMember.Parent.Children.IndexOf(aMember);
                }
            var M = FGrid.FEngine.GetMetaline(FLevelsAndMembers.Keys);
            FLineID = M.fID;
            FLineIdx = M.GetLineIdx(FLevelsAndMembers.Values);
            FHierID = M.GetHierID(FLevelsAndMembers.Values);
        }

        /// <summary>
        ///     Joins the coordinates of two Cube cells: the current and the one passed as the
        ///     parameter.
        /// </summary>
        /// <remarks>
        ///     Most often is used to get the resulting Cube cell coordinates when the
        ///     coordinates of the rows and the columns are known.
        /// </remarks>
        public void Merge(ICubeAddress Address)
        {
            for (var i = 0; i < Address.LevelsCount; i++)
                AddMember(Address.Members(i));
            if (Address.Measure != null) Measure = Address.Measure;
            FModeID = Address.FModeID;
        }

        /// <summary>Clears the earlier determined values of multidimensional Cube axes.</summary>
        /// <remarks>
        ///     After the implementation of the method, the Cube cell shall indicate grand Total
        ///     for all dimensions or levels of the Cube. To describe cell coordinates again, define
        ///     them through the AddMember method or set a measure value to the Measure
        ///     property.
        /// </remarks>
        public void Clear()
        {
            FLineID = string.Empty;
            FLineIdx = 0;
            FHierID = 0;
            FLevelsAndMembers.Clear();
            FMeasureID = string.Empty;
            FModeID = 0;
        }

        /// <summary>
        ///     <para>
        ///         Deletes the hierarchy level, thus resetting the corresponsing hierarchy in
        ///         the undetermined state.
        ///     </para>
        /// </summary>
        public void ClearLevel(Level ALevel)
        {
            if (ALevel.FHierarchy != null)
            {
                var i = FLevelsAndMembers.IndexOfKey(ALevel.ID);
                if (i < 0) return;
                FLevelsAndMembers.RemoveAt(i);
                var M = FGrid.FEngine.GetMetaline(FLevelsAndMembers.Keys);
                FLineID = M.fID;
                FLineIdx = M.GetLineIdx(FLevelsAndMembers.Values);
                FHierID = M.GetHierID(FLevelsAndMembers.Values);
            }
            else
            {
                FMeasureID = string.Empty;
                FModeID = 0;
            }
        }

        /// <summary>
        ///     Returns the member that belongs to the passed hierarchy level or null, if the
        ///     level isn't determined for the specified multidimensional address.
        /// </summary>
        public Member GetMemberByLevel(Level ALevel)
        {
            Member m;
            return FLevelsAndMembers.TryGetValue(ALevel.ID, out m) ? m : null;
        }

        /// <summary>
        ///     Returns the member that belongs to the passed hierarchy or null, if the hierachy
        ///     isn't determined for the given multidimensional address.
        /// </summary>
        public Member GetMemberByHierarchy(Hierarchy AHierarchy)
        {
            foreach (var m in FLevelsAndMembers.Values)
                if (m.FLevel.FHierarchy == AHierarchy) return m;
            return null;
        }

        /// <summary>Returns one of the determined (not Total) Cube dimensions.</summary>
        /// <remarks>
        ///     The number of determined Cube dimensions is indicated by the LevelsCount
        ///     property, and the specific members that are coordinate values across the appropriate
        ///     Cube axes can be retrieved by using the Members property.
        /// </remarks>
        public Level Levels(int Index)
        {
            return FLevelsAndMembers.Values[Index].FLevel;
        }

        /// <summary>
        ///     Returns one of the determined (not Total) hierarchy members defining a coordinate
        ///     value on the appropriate Cube axis.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The number of determined hierarchy members is indicated in the LevelsCount
        ///         property. The levels appropriate to it, describing non-Total cell coordinates, can
        ///         be retrieved through the Levels property.
        ///     </para>
        /// </remarks>
        public Member Members(int Index)
        {
            return FLevelsAndMembers.Values[Index];
        }

        /// <summary>Duplicates a Cube cell address.</summary>
        public ICubeAddress Clone()
        {
            //ICubeAddress res = new ICubeAddress(FGrid, FLineID, FHierID, FLineIdx, FMeasureID, FModeID, FTag);
            //return res;

            var res = new ICubeAddress(FGrid);
            res.FLevelsAndMembers = CreateSortedList();
            res.FGrid = FGrid;
            res.FLineID = FLineID;
            res.FHierID = FHierID;
            res.FLineIdx = FLineIdx;
            res.FMeasureID = FMeasureID;
            res.FModeID = FModeID;
            res.FTag = FTag;

            // ex-Silent init
            FLevelsAndMembers.ForEach(x => res.FLevelsAndMembers.Add(x.Key, x.Value));
            return res;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ICubeAddress)) return false;
            var A = obj as ICubeAddress;
            return A.FGrid == FGrid && A.FLineID == FLineID && A.FHierID == FHierID &&
                   A.FLineIdx == FLineIdx && A.FMeasureID == FMeasureID && A.FModeID == FModeID && A.FTag == FTag;
        }

        public override int GetHashCode()
        {
            return Convert.ToInt32(RadarUtils.ComputeCRC(ToString()));
        }

        public static bool operator ==(ICubeAddress a, ICubeAddress b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if ((object) a == null || (object) b == null)
                return false;

            return a.FGrid == b.FGrid && a.FLineID == b.FLineID && a.FHierID == b.FHierID &&
                   a.FLineIdx == b.FLineIdx && a.FMeasureID == b.FMeasureID && a.FModeID == b.FModeID &&
                   a.FTag == b.FTag;
        }

        public static bool operator !=(ICubeAddress a, ICubeAddress b)
        {
            return !(a == b);
        }
    }
#if DEBUG
    //public class TSortedList<TKey, TValue> : SortedList<TKey, TValue>
    //{
    //    public new void Add(TKey k, TValue v)
    //    {
    //        base.Add(k, v);
    //    }

    //    public TSortedList()
    //        :base()
    //    {            
    //    }
    //}
#endif
}