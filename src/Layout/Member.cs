using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
    ///     An object representing the hierarchy member on the grid level.
    /// </summary>
    [DebuggerDisplay("DisplayName = {DisplayName} Visible = {Visible} Filtered = {Filtered} UniqueName = {UniqueName}")]
    public class Member : IDescriptionable
    {
#if DEBUG
        private string _FDisplayName = "";

        internal string FDisplayName
        {
            get => _FDisplayName;
            set
            {
                if (value == null)
                {
                }
                if (value == "Bikes")
                {
                }
                if (value == "Components")
                {
                }


                if (value != null && value.Contains("[]"))
                {
                }
                _FDisplayName = value;
            }
        }

        private string _FUniqueName;

        internal string FUniqueName
        {
            get => _FUniqueName;
            set
            {
                if (value == "[Employees].[Photo].[34222.]")
                {
                }
                _FUniqueName = value;
            }
        }

        private object __data;

        internal object _data
        {
            get => __data;
            set
            {
                if (__data == value)
                    return;


                if (value is byte[])
                {
                }

                __data = value;
            }
        }
#else
        internal object _data;
        internal string FDisplayName;
        internal string FUniqueName;
#endif

        internal object Data => _data ?? DisplayName;


        /// <summary>A hierarchy member name displayed in the Grid.</summary>
        public virtual string DisplayName
        {
            get => FDisplayName;
            set => FDisplayName = value;
        }

#if DEBUG
        private Member _FParent;

        internal Member FParent
        {
            get => _FParent;
            set
            {
                _FParent = value;
            }
        }

        internal Level FLevel { get; set; }

#else
        internal Member FParent;
        internal Level FLevel;
#endif

        internal string FShortName;
        internal string FDescription;
        internal MemberType FMemberType = MemberType.mtCommon;
        internal Members FChildren = new Members();
        internal CubeMember FCubeMember;
        internal double FRank;
        internal int FDepth;

        internal int fVirtualID = -1;

        //        internal int FChildrenCount = 0;
        internal Members FNextLevelChildren = new Members();

#if DEBUG

        internal bool FFiltered { get; set; }
#else
        private bool FFiltered = false;
#endif

        // used in rsOLAPCube
        internal bool FTag = false;

        internal int FSortPosition;

        internal IEnumerable<ICellValue> GetAttributesAsObjects()
        {
            if (Level.CubeLevel == null || CubeMember == null)
                return null;

            var sb = new List<CellValue>();
            Func<object, object> f = x => x;

            Level.CubeLevel.InfoAttributes
                .Where(item => item.IsDisplayModeAsColumn)
                .ForEach(ia => sb.Add(new CellValue(ia.DisplayName, f(CubeMember.GetAttributeAsObject(ia.DisplayName))))
                );

            return sb.ToArray();
        }

        internal IEnumerable<ICellValue> GetToolTipAttributesAsObjects()
        {
            if (Level.CubeLevel == null || CubeMember == null)
                return null;

            var values = new List<CellValue>();
            Func<object, object> f = x => x;

            Level.CubeLevel.InfoAttributes
                .Where(item => item.IsDisplayModeAsTooltip)
                .ForEach(ia => values.Add(new CellValue(ia.DisplayName,
                             f(CubeMember.GetAttributeAsObject(ia.DisplayName))))
                );

            return values.ToArray();
        }


        protected internal virtual void WriteStream(BinaryWriter stream)
        {
            StreamUtils.WriteTag(stream, Tags.tgMember);

            if (!string.IsNullOrEmpty(UniqueName))
            {
                StreamUtils.WriteTag(stream, Tags.tgMember_UniqueName);
                StreamUtils.WriteString(stream, UniqueName);
            }

            if (FCubeMember == null)
            {
                if (!string.IsNullOrEmpty(DisplayName))
                {
                    StreamUtils.WriteTag(stream, Tags.tgMember_DisplayName);
                    StreamUtils.WriteString(stream, DisplayName);
                }
                if (!string.IsNullOrEmpty(FShortName))
                {
                    StreamUtils.WriteTag(stream, Tags.tgMember_ShortName);
                    StreamUtils.WriteString(stream, FShortName);
                }
                if (!string.IsNullOrEmpty(FDescription))
                {
                    StreamUtils.WriteTag(stream, Tags.tgMember_Description);
                    StreamUtils.WriteString(stream, FDescription);
                }
            }

            if (FMemberType != MemberType.mtCommon)
            {
                StreamUtils.WriteTag(stream, Tags.tgMember_MemberType);
                StreamUtils.WriteInt32(stream, (int) FMemberType);
            }

            StreamUtils.WriteTag(stream, Tags.tgMember_Rank);
            StreamUtils.WriteDouble(stream, FRank);

            if (fVirtualID >= 0)
            {
                StreamUtils.WriteTag(stream, Tags.tgMember_VirtualID);
                StreamUtils.WriteInt32(stream, fVirtualID);
            }

            if (!FVisible)
                StreamUtils.WriteTag(stream, Tags.tgMember_Visible);

            if (FFiltered)
                StreamUtils.WriteTag(stream, Tags.tgMember_Filtered);

            StreamUtils.WriteTag(stream, Tags.tgMember_EOT);
        }

        protected internal virtual void ReadStream(BinaryReader stream)
        {
            var _exit = false;
            do
            {
                var Tag = StreamUtils.ReadTag(stream);
                switch (Tag)
                {
                    case Tags.tgMember_Description:
                        FDescription = StreamUtils.ReadString(stream);
                        break;
                    case Tags.tgMember_UniqueName:
                        SetUniqueName(StreamUtils.ReadString(stream));
                        break;
                    case Tags.tgMember_ShortName:
                        FShortName = StreamUtils.ReadString(stream);
                        break;
                    case Tags.tgMember_VirtualID:
                        fVirtualID = StreamUtils.ReadInt32(stream);
                        break;
                    case Tags.tgMember_DisplayName:
                        DisplayName = StreamUtils.ReadString(stream);
                        break;
                    case Tags.tgMember_Filtered:
                        FFiltered = true;
                        break;
                    case Tags.tgMember_Visible:
                        FVisible = false;
                        break;
                    case Tags.tgMember_Rank:
                        FRank = StreamUtils.ReadDouble(stream);
                        break;
                    case Tags.tgMember_MemberType:
                        FMemberType = (MemberType) StreamUtils.ReadInt32(stream);
                        break;
                    case Tags.tgMember_EOT:
                        FChildren.Initialize(FCubeMember == null ? null : FCubeMember.Children, FLevel, this);
                        _exit = true;
                        break;
                    default:
                        throw new Exception("Unknow tag: " + Tag);
                }
            } while (!_exit);
        }

        internal void RestoreAfterSerialization()
        {
            if (FLevel != null && FLevel.CubeLevel != null)
            {
                FLevel.CubeLevel.FUniqueNamesArray.TryGetValue(UniqueName, out FCubeMember);
                if (FCubeMember != null)
                {
                    FDescription = FCubeMember.Description;
                    FShortName = FCubeMember.ShortName;
                    DisplayName = FCubeMember.DisplayName;
                    FChildren.FCubeMembers = FCubeMember.Children;
                }
            }
            if (FCubeMember != null)
            {
                FChildren.RestoreAfterSerialization(FCubeMember.Children);
                FNextLevelChildren.RestoreAfterSerialization(FCubeMember.NextLevelChildren);
            }
            else
            {
                FChildren.RestoreAfterSerialization(null);
                FNextLevelChildren.RestoreAfterSerialization(null);
            }
        }

        /// <summary>
        ///     Returns an unique name of the member
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return UniqueName;
        }

        /// <summary>
        ///     Returns true if a member passed by the "M" parameter either coincides with the
        ///     specified member or is its ancestor.
        /// </summary>
        public bool IsAncestorFor(Member M)
        {
            while (M != null)
            {
                if (M == this) return true;
                M = M.Parent;
            }
            return false;
        }


        internal bool DoUpdateFilterState(out bool HasFiltered)
        {
            FFiltered = !FVisible;
            var Result = FVisible;
            HasFiltered = FFiltered;
            if (FChildren.Count == 0 && FNextLevelChildren.Count == 0)
                return FVisible;
            FVisible = false;
            FFiltered = false;
            try
            {
                foreach (var m in FChildren)
                {
                    bool b;
                    FVisible = FVisible | m.DoUpdateFilterState(out b); // |! 
                    FFiltered = FFiltered || b;
                }
                foreach (var m in FNextLevelChildren)
                {
                    bool b;
                    FVisible = FVisible | m.DoUpdateFilterState(out b); // |! 
                    FFiltered = FFiltered || b;
                }
            }
            finally
            {
                HasFiltered = FFiltered;
            }
            return FVisible;
        }

        internal string GetName1()
        {
            if (FLevel.FHierarchy != null)
                return "[" + FLevel.FHierarchy.Dimension.DisplayName + "].[" +
                       FLevel.FHierarchy.DisplayName + "].[" + DisplayName + "]";
            return "[Measures].[" + DisplayName + "]";
        }

        internal string GetName2()
        {
            return FParent == null ? GetName1() : Parent.GetName2() + ".[" + DisplayName + "]";
        }

        internal Member(Level AParentLevel, Member AParentMember, CubeMember ACubeMember)
        {
            FLevel = AParentLevel;
            FNextLevelChildren.FParentMember = this;
            FParent = AParentMember;

            if (FParent != null && FParent.FLevel != FLevel) FParent.FNextLevelChildren.Add(this);
            if (FParent != null && FParent.FLevel == FLevel)
            {
                FDepth = AParentMember.FDepth + 1;
                FParent.FChildren.Add(this);
            }
            FDepth = FParent == null || FParent.Level != AParentLevel ? 0 : FParent.FDepth + 1;
            FCubeMember = ACubeMember;
            CubeMembers CM = null;
            if (ACubeMember != null) CM = ACubeMember.Children;
            FChildren.Initialize(CM, AParentLevel, this);

            if (FCubeMember != null)
            {
                DisplayName = FCubeMember.DisplayName;
                _data = FCubeMember.Data;
                SetUniqueName(FCubeMember.FUniqueName);
                FShortName = FCubeMember.FShortName;
                FDescription = FCubeMember.FDescription;
                AParentLevel.FStaticMembers.Add(this);
            }
            if (AParentMember != null)
            {
                FVisible = AParentMember.FVisible;
                FFiltered = AParentMember.FFiltered;
            }
            else
            {
                if (FLevel.FHierarchy != null)
                {
                    FVisible = FLevel.FHierarchy.UnfetchedMembersVisible;
                    FFiltered = !FLevel.FHierarchy.UnfetchedMembersVisible;
                }
            }
        }

        internal void SetUniqueName(string AUniqueName)
        {
#if DEBUG
            if (AUniqueName == "[Product].[All Products].[Drink].[Alcoholic Beverages]")
            {
            }
#endif
            FUniqueName = AUniqueName;
        }

        internal Member(Level AParentLevel)
        {
            FLevel = AParentLevel;
        }

        /// <summary>
        ///     Returns reference to the ancestor of the specified member placed at the
        ///     ADepth.
        /// </summary>
        public Member GetParentMember(int ADepth)
        {
            if (FDepth < ADepth) return null;
            var M = this;
            while (M.FDepth != ADepth) M = M.Parent;
            return M;
        }

        internal Member GetParentMember(Level level)
        {
            var M = this;
            while (M != null)
            {
                if (M.Level == level) return M;
                M = M.Parent;
            }
            return null;
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
        ///     <para>
        ///         For MS AS, a unique hierarchy member name is the same as its unique MDX name,
        ///         and for the Desktop version (with the TOLAPCube object as the data source) its
        ///         unique name is formed as follows:
        ///         [Dimension.DisplayName].[THieararchy.DisplayName].[Member.DisplayName]...[Member.DisplayName].
        ///     </para>
        ///     <para>
        ///         A chain of [Member.DisplayName]...[Member.DisplayName] is formed according
        ///         to the hierarchy structure of the members within the hierarchy. For example, if you
        ///         have a dimension Time, the hierarchy Fiscal Date containing the levels Year, Month
        ///         and Day, and we need to form an unique name for a member which corresponds to the
        ///         date May 7, 2006, then it will look as follows: [Time].[Fiscal
        ///         Date].[2006].[May].[7]
        ///     </para>
        /// </remarks>
        public virtual string UniqueName => FUniqueName;
#if DEBUG
        private bool _FVisible = true;

        internal bool FVisible
        {
            get
            {
                if (DisplayName == "Unit Sales")
                {
                }
                return _FVisible;
            }
            set
            {
                if (value == false && UniqueName ==
                    "[Product].[All Products].[Drink].[Alcoholic Beverages].[Beer and Wine].[Beer].[Good].[Good Imported Beer]"
                )
                {
                }

                if (_FVisible == value)
                    return;

                if (DisplayName == "Unit Sales")
                {
                }
                _FVisible = value;
            }
        }
#else
        internal bool FVisible = true;
#endif
        /// <summary>Defines if the specified member is visible in the Grid.</summary>
        /// <remarks>See Filtering Hierarchy Members for details.</remarks>
        public bool Visible
        {
            get => FVisible;
            set
            {
                if (FVisible == value)
                    return;
                FVisible = value;
                if (FMemberType == MemberType.mtMeasure)
                {
                    var M = FLevel.FMeasures.Find(UniqueName);
                    if (M != null) M.Visible = value;
                    return;
                }
                if (FMemberType == MemberType.mtCalculated)
                {
                    var G = FLevel.GetGrid();
                    if (G.IsUpdating == false && G.Active)
                    {
                        G.CellSet.Rebuild();
                        G.EndChange(GridEventType.geFilterAction, this);
                    }
                    return;
                }
                FLevel.FHierarchy.BeginUpdate();
                FChildren.SetVisible(value, FLevel.FHierarchy);
                FNextLevelChildren.SetVisible(value, FLevel.FHierarchy);
                if (!value)
                    FLevel.FFilter = null;
                FLevel.FHierarchy.EndUpdate();
                FLevel.FHierarchy.UpdateFilterState(true);
            }
        }

        /// <summary>The list of 'neighbors' of the specified hierarchy node.</summary>
        public Members SiblingsList
        {
            get
            {
                if (FParent == null)
                    return FLevel.Members;
                return FParent.Children.Count > 0 ? FParent.Children : FParent.NextLevelChildren;
            }
        }

        /// <summary>References to the Parent of the specified member.</summary>
        /// <remarks>
        ///     <para>
        ///         A parent of the specified member can belong to either the current level (in
        ///         case of the Parent-Child interconnection) or to the above level (in case of a
        ///         multilevel hierarchy).
        ///     </para>
        /// </remarks>
        public Member Parent => FParent;

        /// <summary>References to the level the specified member belongs to.</summary>
        public Level Level => FLevel;

        /// <summary>
        ///     True if the member is leaf.
        /// </summary>
        public bool IsLeaf
        {
            get
            {
                if (FChildren.Count > 0 || FNextLevelChildren.Count > 0)
                    return false;

                if (Level.Hierarchy == null)
                    return true;

                if (Level.Hierarchy.Origin == HierarchyOrigin.hoNamedSet
                    || Level.Hierarchy.Origin == HierarchyOrigin.hoAttribute)
                    return true;

                if (CubeMember == null)
                    return true;

                if (Level.Hierarchy.Dimension.FGrid.Cube.HasMemberChildren(this)) return false;

                if (MemberType != MemberType.mtGroup)
                {
                    var H = FLevel.FHierarchy;
                    if (FLevel.Index + 1 < H.Levels.Count && FCubeMember.fIsLeaf != true)
                        return false;
                }
                return true;
            }
        }

        /// <summary>Type of the specified member (regular, calculated, a group or a measure).</summary>
        public MemberType MemberType => FMemberType;

        /// <summary>
        ///     References to the object of the Cube level that represents the hierarchy member.
        ///     Can be null, if the specified member is a group or a calculated hierarchy
        ///     member.
        /// </summary>
        public CubeMember CubeMember => FCubeMember;

        /// <summary>Returns the list of same-level children of the specified hierarchy member.</summary>
        public Members Children => FChildren;

        /// <summary>
        ///     Returns the list of the specified hierarchy member's children from the next
        ///     level.
        /// </summary>
        public Members NextLevelChildren => FNextLevelChildren;

        /// <summary>
        ///     Indicates wether the specified member is auxiliary in building ragged
        ///     hierarchies.
        /// </summary>
        /// <remarks>
        ///     A given member is created upon initialization of dimension tables to substitute
        ///     the missing members in ragged hierarchies. This member doesn't exist in the
        ///     database.
        /// </remarks>
        public bool IsRaggedVirtual => FCubeMember != null ? FCubeMember.IsRaggedVirtual : false;

        /// <summary>
        ///     Depth of the the Parent-Child hierarchy where the specified member is
        ///     situated.
        /// </summary>
        /// <remarks>
        ///     For root members of the Parent-Child hierarchy on a given level the value of this
        ///     property equals 0.
        /// </remarks>
        public int Depth => FDepth;

        /// <summary>The property is used by the inner procedures of the RadarCube library.</summary>
        public double Rank
        {
            get => FRank;
            set => FRank = value;
        }

        /// <summary>
        ///     A member index identifier. Used internally by the RadarCube methods.
        /// </summary>
        /// <remarks>Not recommended for use outside the code of the RadarCube library</remarks>
        public int ID => FCubeMember != null ? FCubeMember.fID : fVirtualID;

        /// <summary>
        ///     The maximum number of levels between the current and the last one within a
        ///     Parent-Child hierarchy.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For the Parent-Child hierarchy leaves, this value equals 1. For members that
        ///         have Descendants with different DescendantDepth values, among them, it'll be the
        ///         maximum plus 1.
        ///     </para>
        ///     <para>
        ///         Thus, the property Level.Depth is calculated as the maximum of the values of
        ///         the DescendantDepth property for hierarchy members in the Level.Members
        ///         list.
        ///     </para>
        /// </remarks>
        public short ChildrenDepth
        {
            get
            {
                if (Children.Count == 0) return 1;
                short n = 1;
                foreach (var m in Children)
                    n = Math.Max(n, m.ChildrenDepth);
                n++;
                return n;
            }
        }

        /// <summary>
        ///     <para>
        ///         Indicates if a filter has been applied to the specified hierarchy. Returnes
        ///         <em>True</em>, if among all descendants of all hierarchy members is at least one
        ///         hidden.
        ///     </para>
        /// </summary>
        public bool Filtered => FVisible ? FFiltered : true;

        #region IDescriptionable Members

        string IDescriptionable.DisplayName => DisplayName;

        string IDescriptionable.Description => Description;

        string IDescriptionable.UniqueName => UniqueName;

        #endregion

        // from winforms for infoattributer in treelike grid mode
        internal string GetAttributesAsColumn()
        {
            if (Level.CubeLevel == null || CubeMember == null)
                return null;

            var sb = new StringBuilder();

            Level.CubeLevel.InfoAttributes
                .Where(item => item.IsDisplayModeAsColumn)
                .ForEach(ia =>
                         {
                             sb_AppendFormat(sb, "\n\t{0}: {1}", ia.DisplayName,
                                 CubeMember.GetAttributeValue(ia.DisplayName));
                         }
                );

            return sb.ToString();
        }

        internal readonly char[] __removecharts = {'\n', '\t'};

        internal string ExtractAttributesAsTooltip(bool useHTMLRendering)
        {
            return ExtractAttributesAsTooltip(null, useHTMLRendering);
        }

        internal string ExtractAttributesAsTooltip(string ADescription, bool useHTMLRendering)
        {
            if (Level.CubeLevel == null || CubeMember == null)
                return null;

            var startline = ADescription.IsNullOrEmpty() ? "\n" : "\n\t";
            var sb = new StringBuilder();

            Level.CubeLevel.InfoAttributes
                .Where(x => x.IsDisplayModeAsTooltip)
                .ForEach(ia =>
                         {
                             var s = string.Format("\n\t{0}: {1}", ia.DisplayName,
                                 CubeMember.GetAttributeValue(ia.DisplayName));

                             if (sb.Length == 0)
                             {
                                 s = string.Format("{0}: {1}", ia.DisplayName,
                                     CubeMember.GetAttributeValue(ia.DisplayName));
                             }
                             else
                             {
                                 if (ADescription.IsNullOrEmpty())
                                     s = string.Format("\n{0}: {1}", ia.DisplayName,
                                         CubeMember.GetAttributeValue(ia.DisplayName));
                                 else
                                     s = string.Format("{0}{1}: {2}", startline, ia.DisplayName,
                                         CubeMember.GetAttributeValue(ia.DisplayName));
                             }

                             if (useHTMLRendering)
                             {
                                 s = WebUtility.HtmlEncode(s);
                                 //if (sb.Length > 0)
                                 //{
                                 //    sb.Append("<br />");
                                 //}
                                 sb.Append(s);
                             }
                             else
                             {
                                 //if (sb.Length > 0) 
                                 //    sb.AppendLine();
                                 sb.Append(s);
                             }
                         });
            return sb.ToString();
        }

        private void sb_AppendFormat(StringBuilder sb, string AFormat, params object[] args)
        {
            sb.AppendFormat(AFormat, args);
        }
    }
}