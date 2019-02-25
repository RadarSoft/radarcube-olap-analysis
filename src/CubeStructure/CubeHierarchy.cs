using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.CubeStructure
{
    /// <summary>
    ///     Represents a hierarchy (a composite part of a dimension) responsible for the
    ///     structure of the Cube.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The hierarchy is one of the two basic elements of the visual structure of the
    ///         Cube that along with the measure forms the axes of the OLAP-slice.
    ///     </para>
    ///     <para>
    ///         There are three types of hierarchies: attribute (flat) hierarchies, composite
    ///         (multilevel) and prime at the Parent-Child intersection.
    ///     </para>
    ///     <para>
    ///         Objects of the CubeDimension class are responsible for the information
    ///         specific for the structure of the Cube. The data stored by the instances of the
    ///         class remains unchanged when the Cube is active and there are operations performed
    ///         in it.
    ///     </para>
    ///     <para>
    ///         For the operational information and the information assigned to the visual
    ///         part, see description of the Hierarchy class.
    ///     </para>
    /// </remarks>
    //[Serializable]
    public class CubeHierarchy : IStreamedObject, IDescriptionable, IPropertyGridLinker
    {
        private bool? _isuseHierarchyOnReadFactTable;

        internal ContentAlignment FAlignment = ContentAlignment.MiddleLeft;
        internal bool FAllowChangeTotalAppearance = true;
        internal bool FAllowFilter = true;
        internal bool FAllowHierarchyEditor = true;
        internal bool FAllowMultiselect = true;
        internal bool FAllowPopupOnCaptions = true;
        internal bool FAllowPopupOnLabels = true;
        internal bool FAllowRegroup = true;
        internal bool FAllowResort = true;
        internal bool FAllowSwapMembers = true;
        internal string FBaseNamedSetHierarchies;
        internal BIMembersType FBIType = BIMembersType.ltNone;
        internal bool FCalculatedByRows;

        internal string FChildren = "";

        internal string fChildrenHierarchies;
        internal List<CubeHierarchy> FChildrenList = new List<CubeHierarchy>();
        internal CubeLevels FCubeLevels;

        [NonSerialized] private object FDataSortedArray = null; // the object of List<TSortedColumnDataItem> type

        internal string FDataTable; // The name of the table to hold the hierarchy data
        internal string FDescription = "";

        internal string FDescriptionField = "";
        internal int FDiscretizationBucketCount;
        internal string FDiscretizationBucketFormat = "";
        internal DiscretizationMethod FDiscretizationMethod = DiscretizationMethod.dmNone;
        internal string FDisplayField;
        internal Type FDisplayFieldType = typeof(string);
        internal string FDisplayFolder = "";
        internal string FDisplayName = "";

        internal string FFormatString = "";
        internal string FIDField;
        internal Type FIDFieldType;
        internal InfoAttributes FInfoAttributes = new InfoAttributes();
        internal List<string> FMDXLevelNames = new List<string>();
        internal MemberIsNull FMemberIsNull = MemberIsNull.nmUnknownMemberVisible;
        internal MembersWithData FMembersWithData = MembersWithData.dmNonLeafDataHidden;

        internal HierarchyOrigin FOrigin = HierarchyOrigin.hoUnknown;


        internal string FParentField = "";
        internal Type FParentFieldType;

        [NonSerialized] internal object FPathFromFactTable = null; // List<TRelation>

        internal bool FShowEmptyLines;
        internal bool FTakeFiltersIntoCalculations = true;

        internal TotalAppearance FTotalAppearance = TotalAppearance.taLast;
        internal string FTotalCaption = "";
        internal HierarchyDataType FTypeOfMembers = HierarchyDataType.htCommon;
        internal string FUniqueName = Guid.NewGuid().ToString();
        internal string FUnknownMemberName = "";
        internal bool FVisible = true;
        private MemoryStream membersstream;

        /// <summary>Creates an instance of the CubeHierarchy type</summary>
        public CubeHierarchy()
        {
            // ***
            FInfoAttributes = new InfoAttributes(this);
            // ***

            FDiscretizationBucketFormat = "%{First} - %{Last}";

            FCubeLevels = new CubeLevels(this);
        }

        public CubeHierarchy(CubeDimension ADimension)
            : this()
        {
            Init(ADimension);
        }

        internal bool? IsUseHierarchyOnReadFactTable
        {
            get
            {
                return _isuseHierarchyOnReadFactTable ??
                       (_isuseHierarchyOnReadFactTable = Levels.All(level => level.fUseOnReadFactTable));
            }
            set => _isuseHierarchyOnReadFactTable = value;
        }

        /// <summary>
        ///     A business intelligence type of hierarchy members.
        /// </summary>
        [Category("Advanced (manage carefully)")]
        [DefaultValue(BIMembersType.ltNone)]
        [Description("A business intelligence type of hierarchy members.")]
        [NotifyParentProperty(true)]
        public BIMembersType BIMembersType
        {
            get => FBIType;
            set => FBIType = value;
        }

        /// <summary>
        ///     Defines the default alignment style of hierarchy members' captions in the Grid
        ///     cells
        /// </summary>
        [Category("Appearance")]
        [Description("Defines the default alignment style of the hierarchy members' captions in the Grid cells")]
        [DefaultValue(ContentAlignment.MiddleLeft)]
        [NotifyParentProperty(true)]
        public ContentAlignment Alignment

        {
            get => FAlignment;
            set => FAlignment = value;
        }

        /// <summary>
        ///     Defines the position (first, last or hidden) of the "Total" member in the
        ///     specified hierarchy.
        /// </summary>
        [Category("Behavior")]
        [Description(
            "Defines the position (first, last or hidden) of the \"Total\" member in the specified hierarchy.")]
        [DefaultValue(TotalAppearance.taLast)]
        [NotifyParentProperty(true)]
        public TotalAppearance TotalAppearance
        {
            get => FTotalAppearance;
            set => FTotalAppearance = value;
        }

        /// <summary>
        ///     Defines whether the sorting functions of the specified hierarchy are available
        ///     for an end user through the Grid interface.
        /// </summary>
        [Category("Behavior")]
        [Description(
            "Defines whether the sorting functions of the specified hierarchy are available for an end user through the Grid interface.")]
        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        public bool AllowResort
        {
            get => FAllowResort;
            set => FAllowResort = value;
        }

        /// <summary>
        ///     Defines whether an end user is allowed to change the position of the "Total"
        ///     member.
        /// </summary>
        [Category("Behavior")]
        [Description("Defines whether an end user is allowed to change the position of the \"Total\" member.")]
        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        public bool AllowChangeTotalAppearance
        {
            get => FAllowChangeTotalAppearance;
            set => FAllowChangeTotalAppearance = value;
        }

        /// <summary>Defines whether an end user is allowed to filter hierarchy members.</summary>
        [Category("Behavior")]
        [Description("Defines whether an end user is allowed to filter hierarchy members.")]
        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        public bool AllowFilter
        {
            get => FAllowFilter;
            set => FAllowFilter = value;
        }

        /// <summary>
        ///     Defines whether an end user is allowed to create/delete/modify groups of the
        ///     specified hierarchy through the Grid interface.
        /// </summary>
        [Category("Behavior")]
        [Description(
            "Defines whether an end user is allowed to create/delete/modify groups of the specified hierarchy through the Grid interface.")]
        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        public bool AllowRegroup
        {
            get => FAllowRegroup;
            set => FAllowRegroup = value;
        }

        /// <summary>
        ///     Defines whether an end user is allowed to filter hierarchies through Hierarchies
        ///     Editor.
        /// </summary>
        [Category("Behavior")]
        [Description("Defines whether an end user is allowed to filter hierarchies through Hierarchies Editor.")]
        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        public bool AllowHierarchyEditor
        {
            get => FAllowHierarchyEditor;
            set => FAllowHierarchyEditor = value;
        }

        /// <summary>
        ///     Defines whether it is possible to call a popup menu by right-clicking on a
        ///     hierarchy or level caption cell.
        /// </summary>
        [Category("Behavior")]
        [Description(
            "Defines whether it is possible to call a popup menu by right-clicking on a hierarchy or level caption cell.")]
        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        public bool AllowPopupOnLevelCaptions
        {
            get => FAllowPopupOnCaptions;
            set => FAllowPopupOnCaptions = value;
        }

        /// <summary>
        ///     Defines whether it is possible to call a popup menu by right-clicking on a
        ///     hierarchy member cell.
        /// </summary>
        [Category("Behavior")]
        [Description(
            "Defines whether it is possible to call a popup menu by right-clicking on a hierarchy member cell.")]
        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        public bool AllowPopupOnMembers
        {
            get => FAllowPopupOnLabels;
            set => FAllowPopupOnLabels = value;
        }

        /// <summary>
        ///     Allows end users to select several members in filters simultaneously.
        /// </summary>
        [Category("Behavior")]
        [Description("Allows end users to select several members in filters simultaneously.")]
        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        public bool AllowMultiselect
        {
            get => FAllowMultiselect;
            set => FAllowMultiselect = value;
        }

        /// <summary>
        ///     Defines whether the specified hierarchy is visible to an end user in the Cube
        ///     Structure tree.
        /// </summary>
        [Category("Behavior")]
        [Description("Defines whether the specified hierarchy is visible to an end user in the Cube Structure tree.")]
        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        public bool Visible
        {
            get => FVisible;
            set => FVisible = value;
        }

        /// <summary>
        ///     Defines whether during the aggregation of the Cube cells the visibility state of
        ///     the specified hierarchy is taken into consideration.
        /// </summary>
        [Category("Behavior")]
        [Description(
            "Defines whether during the aggregation of the Cube cells the visibility state of the specified hierarchy is taken into consideration.")]
        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        public bool TakeFiltersIntoCalculations
        {
            get => FTakeFiltersIntoCalculations;
            set => FTakeFiltersIntoCalculations = value;
        }

        /// <summary>
        ///     If set to True, then the hierarchy members with no aggregated data in the current
        ///     OLAP slice (i.e. rows or columns of the Grid in the data area corresponding to that
        ///     member will be empty) are displayed in the Grid.
        /// </summary>
        [Category("Behavior")]
        [Description(
            "If set to True, then the hierarchy members with no aggregated data in the current OLAP slice are displayed in the Grid.")]
        [DefaultValue(false)]
        [NotifyParentProperty(true)]
        public bool ShowEmptyLines
        {
            get => FShowEmptyLines;
            set => FShowEmptyLines = value;
        }

        /// <summary>The name of the data table where the hierarchy takes data from.</summary>
        [Category("Advanced (manage carefully)")]
        [Description("The name of the data table where the hierarchy takes data from.")]
        [NotifyParentProperty(true)]
        [RefreshProperties(RefreshProperties.All)]
        public string DataTable
        {
            get => FDataTable;
            set => FDataTable = value;
        }

        /// <summary>Reference to a dimension containing the specified hierarchy.</summary>
        [Browsable(false)]
        public CubeDimension Dimension { get; private set; }

        /// <summary>
        ///     Represents the list of hierarchical levels and contains a collection of objects of the CubeLevel type.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Attribute (flat) hierarchies (Origin = hoAttribute) and hierarchies of the
        ///         Parent-Children type (Origin = hoParentChild) have only one level. Custom or
        ///         composite hierarchies have two and more levels.
        ///     </para>
        ///     <para>
        ///         The hierarchies are available only upon activation of the Cube. When the Cube
        ///         is inactive, this list is either empty or does not exist.
        ///     </para>
        /// </remarks>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CubeLevels Levels => FCubeLevels;

        /// <summary>
        ///     A list of attribute hierarchies difining the levels of the current
        ///     hierarchy.
        /// </summary>
        /// <remarks>Used in the Direct-version only.</remarks>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<CubeHierarchy> ChildrenList => FChildrenList;

        /// <summary>
        ///     Is a concatenation of flat hierarchies that form levels of the specified
        ///     hierarchy.
        /// </summary>
        /// <remarks>
        ///     <para>Used in the Direct-version only</para>
        ///     <para>The string is composed by the following pattern:</para>
        ///     <para>"[Hierarchy1.UniqueName][Hierarchy2.UniqueName]", and so on.</para>
        /// </remarks>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DefaultValue(null)]
        public string Children
        {
            get
            {
                if (ChildrenList.Count == 0) return null;

                var list = new StringBuilder();
                foreach (var ch in ChildrenList)
                {
                    if (list.Length > 0) list.Append("|");
                    list.Append(ch.UniqueName);
                }
                return list.ToString();
            }
            set => fChildrenHierarchies = value;
        }

        /// <summary>A table field name with a caption of a hierarchy member</summary>
        [NotifyParentProperty(true)]
        public string DisplayField
        {
            get
            {
                if (FCalculatedByRows && BIMembersType == BIMembersType.ltNone && string.IsNullOrEmpty(FFormatString))
                    return null;
                return FDisplayField;
            }
            set
            {
                FDisplayField = value;
                if (FDisplayName == "" && FOrigin == HierarchyOrigin.hoAttribute)
                    DisplayName = FDisplayField;
                if (value == "") DisplayFieldType = null;
            }
        }

        /// <summary>
        ///     A table field type containing the data about members names of the specified
        ///     hierarchy.
        /// </summary>
        [Category("Advanced (manage carefully)")]
        [Description("A table field type containing the data about members names of the specified hierarchy.")]
        [NotifyParentProperty(true)]
        public Type DisplayFieldType
        {
            get => FDisplayFieldType;
            set => FDisplayFieldType = value;
        }

        /// <summary>
        ///     For Parent-Child hierarchies it's a table field name containing a key referring
        ///     to the parent record.
        /// </summary>
        [Category("Advanced (manage carefully)")]
        [Description(
            "For Parent-Child hierarchies it's a table field name containing a key referring to the parent record.")]
        [DefaultValue("")]
        [NotifyParentProperty(true)]
        public string ParentField
        {
            get => FParentField;
            set
            {
                FParentField = value;
                if (value == "") ParentFieldType = null;
            }
        }

        /// <summary>Gets or sets a unique name for a default member of the hierarchy.</summary>
        [Category("Advanced (manage carefully)")]
        [Description("Gets or sets the unique name for a default member of the Hierarchy.")]
        [DefaultValue(null)]
        [NotifyParentProperty(true)]
        public string DefaultMember { get; set; }

        /// <summary>A table field type containing the parent key of the specified hierarchy.</summary>

        [Browsable(false)]
        [NotifyParentProperty(true)]
        public Type ParentFieldType
        {
            get => FParentFieldType;
            set => FParentFieldType = value;
        }

        /// <summary>
        ///     For Parent-Child hierarchies it is a table field name containing a primary key of
        ///     the table.
        /// </summary>
        [Category("Advanced (manage carefully)")]
        [Description("For Parent-Child hierarchies it is a table field name containing a primary key of the table.")]
        [NotifyParentProperty(true)]
        public string IDField
        {
            get => FIDField;
            set
            {
                FIDField = value;
                if (string.IsNullOrEmpty(value))
                    IDFieldType = null;
            }
        }

        [Browsable(false)]
        [NotifyParentProperty(true)]
        public Type IDFieldType
        {
            get => FIDFieldType;
            set => FIDFieldType = value;
        }

        /// <summary>Gets or sets a hierarchy caption for an end user.</summary>
        [Description("Gets or sets a hierarchy caption for an end user.")]
        [Category("Appearance")]
        [NotifyParentProperty(true)]
        public string DisplayName
        {
            get => FDisplayName;
            set => FDisplayName = value;
        }

        /// <summary>
        ///     Gets or sets a hierarchy member caption containing an aggregated value of all
        ///     hierarchy members.
        /// </summary>
        /// <remarks>
        ///     For example, <em>Total</em>, <em>All periods</em>, <em>All products</em> and so
        ///     on
        /// </remarks>
        [Description(
            "Gets or sets a hierarchy member caption containing an aggregated value of all hierarchy members.")]
        [Category("Appearance")]
        [DefaultValue("")]
        [NotifyParentProperty(true)]
        public string TotalCaption
        {
            get => FTotalCaption;
            set => FTotalCaption = value;
        }

        /// <summary>A detailed hierarchy description.</summary>
        /// <remarks>
        ///     Text assigned to this property will pop up as a tooltip upon pointing the cursor
        ///     at the hierarchy name
        /// </remarks>
        [Description("A detailed hierarchy description.")]
        [Category("Appearance")]
        [DefaultValue("")]
        [NotifyParentProperty(true)]
        public string Description
        {
            get => FDescription;
            set => FDescription = value;
        }

        /// <summary>Specifies a hierarchy type (flat, multilevel or Parent-Child).</summary>
        [Category("Advanced (manage carefully)")]
        [Description("Specifies a hierarchy type (flat, multilevel or Parent-Child)")]
        [NotifyParentProperty(true)]
        public HierarchyOrigin Origin
        {
            get => FOrigin;
            set => FOrigin = value;
        }

        /// <summary>
        ///     An unique string hierarchy identifier.
        /// </summary>
        /// <remarks>
        ///     Never visible to an end user. It is not recommended to make changes in the value
        ///     of this property.
        /// </remarks>
        [Description("An unique string hierarchy identifier.")]
        [Category("Behavior")]
        [NotifyParentProperty(true)]
        public string UniqueName
        {
            get => FUniqueName;
            set => FUniqueName = value;
        }

        /// <summary>Is applied to a hierarchy when the dimension members are built.</summary>
        /// <remarks>
        ///     <para>
        ///         If the property has a value different from dmNone then the Cube creates
        ///         hierarchy members on its own by taking this property to define the particular
        ///         method.
        ///     </para>
        ///     <para>There are three possible values of the property:</para>
        ///     <list type="bullet">
        ///         <item>dmNone - no discretization is applied to the hierarchy.</item>
        ///         <item>
        ///             dmEqualRanges - groups hierarchy members into ranges with the same number
        ///             of members.
        ///         </item>
        ///         <item>
        ///             dmEqualAreas - creates ranges so that the total population is distributed
        ///             equally across the ranges.
        ///         </item>
        ///     </list>
        ///     <para>
        ///         The number of ranges in the dimension is defined by the
        ///         DiscretizationBucketCount property.
        ///     </para>
        ///     <para>For more details, see Hierarchy ranges (discretization).</para>
        /// </remarks>
        [Browsable(false)]
        [NotifyParentProperty(true)]
        [DefaultValue(DiscretizationMethod.dmNone)]
        public DiscretizationMethod DiscretizationMethod
        {
            get => FDiscretizationMethod;
            set
            {
                return;
                FDiscretizationMethod = value;
            }
        }

        /// <summary>
        ///     The number of ranges (discretization member) in the hierarchy.
        /// </summary>
        /// <remarks>
        ///     The property defines the number of ranges the discretized hierarchy will have.
        ///     This property is only used if the DiscretizationMethod value differs from the dmNone.
        ///     If the DiscretizationBucketCount = 0 (default value) then the Cube will define the
        ///     number of ranges as a square root of the number of real members.
        /// </remarks>
        [Browsable(false)]
        [NotifyParentProperty(true)]
        [DefaultValue(0)]
        public int DiscretizationBucketCount
        {
            get => FDiscretizationBucketCount;
            set
            {
                return;
                if (FDiscretizationBucketCount == value)
                    return;

                FDiscretizationBucketCount = value;
            }
        }

        /// <summary>
        ///     Defines the range naming template for hierarchies to which the discretization is applied.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Use the property to define the naming template, which will be used on
        ///         assigning names to created ranges. The default value is "%{First} - %{Last}",
        ///         which, for example for a hierarchy with 40 members and DiscretizationBucketCount =
        ///         4, will provide the naming of ranges like:
        ///     </para>
        ///     <para>0: "1 - 10"</para>
        ///     <para>1: "11 - 20"</para>
        ///     <para>2: "21 - 30"</para>
        ///     <para>3: "31 - 40"</para>
        ///     <para>
        ///         For more information about ranges naming see Hierarchy ranges
        ///         (discretization). See also: Class CubeHierarchy
        ///     </para>
        /// </remarks>
        [Browsable(false)]
        [NotifyParentProperty(true)]
        [DefaultValue("%{First} - %{Last}")]
        public string DiscretizationBucketFormat
        {
            get => FDiscretizationBucketFormat;
            set => FDiscretizationBucketFormat = value;
        }

        /// <summary>
        ///     The type of hierarchy members (for the sorting purpose).
        /// </summary>
        /// <remarks>
        ///     Used to form time-based hierarchies without event handling and to specify the
        ///     sorting method of members by default.
        /// </remarks>
        [Category("Sorting")]
        [DefaultValue(HierarchyDataType.htCommon)]
        [NotifyParentProperty(true)]
        [Description("The type of hierarchy members (for the sorting purpose).")]
        public HierarchyDataType TypeOfMembers
        {
            get => FTypeOfMembers;
            set => FTypeOfMembers = value;
        }

        /// <summary>The formatting string for the auto calculated fields.</summary>
        /// <remarks>
        ///     Applied together with the TypeOfMembers property to determine hierarchies which
        ///     members are calculated in the database fields (for example, to transform DateTime of
        ///     the database field values in the names of months, years, days, and so on).
        /// </remarks>
        [Category("Advanced (manage carefully)")]
        [Description("The formatting string for the auto calculated fields")]
        [DefaultValue("")]
        [NotifyParentProperty(true)]
        public string FormatString
        {
            get => FFormatString;
            set => FFormatString = value;
        }

        /// <summary>
        ///     Indicates the calculated hierarchy.
        /// </summary>
        /// <remarks>
        ///     For the Direct-version only. Calculations are performed when fetching the
        ///     information from database. As source data, values of the current row of the fact table
        ///     and appropriate rows of dimension tables are used.
        /// </remarks>
        [Category("Advanced (manage carefully)")]
        [Description("Indicates the calculated hierarchy")]
        [DefaultValue(false)]
        [NotifyParentProperty(true)]
        public bool CalculatedByRows
        {
            get => FCalculatedByRows;
            set => FCalculatedByRows = value;
        }

        /// <summary>A table field name containing the descriptions of the hierarchy members.</summary>
        [Browsable(false)]
        [DefaultValue("")]
        [NotifyParentProperty(true)]
        public string MemberDescriptionField
        {
            get => FDescriptionField;
            set => FDescriptionField = value;
        }

        /// <summary>
        ///     A folder inside the node with a dimension name, where the name of the specified
        ///     hierarchy is placed. (In the Cube Structure window).
        /// </summary>
        [Browsable(false)]
        [DefaultValue("")]
        [NotifyParentProperty(true)]
        public string DisplayFolder
        {
            get => FDisplayFolder;
            set => FDisplayFolder = value;
        }

        /// <summary>
        ///     Defines the behavior of the Cube when it meets the value in the fact table
        ///     corresponding to a non-leaf member.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Use the property to specify Cube's actions when it meets a in a fact table a
        ///         value for a hierarchy member which itself has child members.
        ///     </para>
        ///     <para>
        ///         From the RadarCube's perspective leaf members contain fact data; while
        ///         non-leaf members contain data derived from aggregations of child members.
        ///     </para>
        ///     <para>
        ///         In a Parent-Child hierarchy, however, some non-leaf members may also have
        ///         data derived fact data, in addition to data aggregated from child members. For such
        ///         non-leaf members fact data may be placed in system-generated child members.
        ///     </para>
        ///     <para>
        ///         For these non-leaf members in a Parent-Child hierarchy, special
        ///         system-generated child members may be created that contain the underlying fact
        ///         table data. They contain a value directly associated with a non-leaf member.
        ///     </para>
        ///     <para>
        ///         Setting the MembersWithData property to dmNonLeafDataHidden hides such
        ///         members, while setting the property to dmNonLeafDataVisible exposes them. That does
        ///         not override the normal aggregation behavior for non-leaf members; the data member
        ///         is always included as a child member for the purposes of aggregation.
        ///     </para>
        ///     <para>
        ///         Data members are very useful when aggregating measures along organizational
        ///         dimensions with Parent-Child hierarchies. For example, the following diagram shows
        ///         a dimension, representing gross sales volume of products, with three levels. The
        ///         first level shows the gross sales volume for all salespersons. The second level
        ///         contains the gross sales volume for all sales staff by sales manager, and the third
        ///         level contains the gross sales volume for all sales staff by salesperson.
        ///     </para>
        ///     <para>
        ///         <img src="images/10-Data-Members.gif" />
        ///         <para>
        ///             In case of the Sales Manager 1 member, aggregating the values of the
        ///             Salesperson 1 and Salesperson 2 members would typically derive the value of the
        ///             member. However, because Sales Manager 1 also can sell products, that member
        ///             may also contain data derived from the fact table because there may be gross
        ///             sales associated with Sales Manager 1.
        ///         </para>
        ///         <para>
        ///             The individual commissions for each sales staff member can vary. For
        ///             sales managers, two different scales are used to compute commissions for their
        ///             individual gross sales, as opposed to the total of gross sales generated by
        ///             their salespersons. In this case, the ability to access the underlying fact
        ///             table data for non-leaf members becomes important.
        ///         </para>
        ///         <para>The property may have the following values:</para>
        ///         <list type="table">
        ///             <listheader>
        ///                 <term>Value</term>
        ///                 <description>Description</description>
        ///             </listheader>
        ///             <item>
        ///                 <term>dmLeafMembersOnly</term>
        ///                 <description>
        ///                     Read only records corresponding to the leaves in the
        ///                     hierarchy. Ignore the entire row of the fact table with data for the
        ///                     non-leaf members.
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <term>dmNonLeafDataException</term>
        ///                 <description>
        ///                     Raise an exception if the fact table contains data for
        ///                     non-leaf member in the hierarchy.
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <term>dmNonLeafDataHidden</term>
        ///                 <description>
        ///                     Create an additional system-generated data member with the
        ///                     same name in the hierarchy and associate the row with it.Do not show
        ///                     this member in the Grid. In this case the values of totals may differ
        ///                     from the actual sum of members.
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <term>dmNonLeafDataVisible</term>
        ///                 <description>
        ///                     Create an additional system-generated data member with the
        ///                     same name in the hierarchy and associate the row with it. Show this
        ///                     member as an additional child of the non-leaf member along with other
        ///                     siblings.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///         <para></para>
        ///     </para>
        /// </remarks>
        [Description(
            "Defines the behavior of the Cube when it meets the value in the fact table corresponding to a non-leaf member.")]
        [Category("Behavior")]
        [DefaultValue(MembersWithData.dmNonLeafDataHidden)]
        [NotifyParentProperty(true)]
        public MembersWithData MembersWithData
        {
            get => FMembersWithData;
            set => FMembersWithData = value;
        }

        /// <summary>
        ///     Defines the Cube's actions when a fact table contains the NULL value instead of a
        ///     reference to a hierarchy member.
        /// </summary>
        /// <remarks>
        ///     <para>The property may have the following values:</para>
        ///     <para></para>
        ///     <list type="table">
        ///         <listheader>
        ///             <term>Value</term>
        ///             <description>Description</description>
        ///         </listheader>
        ///         <item>
        ///             <term>nmIgnoreRecord</term>
        ///             <description>Ignore the entire row of the fact table.</description>
        ///         </item>
        ///         <item>
        ///             <term>nmException</term>
        ///             <description>Raise an exception.</description>
        ///         </item>
        ///         <item>
        ///             <term>nmUnknownMemberHidden</term>
        ///             <description>
        ///                 Create an empty (virtual) member in the hierarchy and
        ///                 associate the row to it. Do not show this new member in the Grid. In this
        ///                 case the values of totals may differ from the sum of members.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>nmUnknownMemberVisible</term>
        ///             <description>
        ///                 Create an empty (virtual) member in the hierarchy and
        ///                 associate the row with this member. Show the empty member in the Grid along
        ///                 with the others.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        [Description(
            "Defines the Cube's actions when a fact table contains the NULL value instead of a reference to a hierarchy member.")]
        [Category("Behavior")]
        [DefaultValue(MemberIsNull.nmUnknownMemberVisible)]
        [NotifyParentProperty(true)]
        public MemberIsNull MemberIsNull
        {
            get => FMemberIsNull;
            set => FMemberIsNull = value;
        }

        /// <summary>
        ///     The name for an NULL dimension member, if any appears in the dimension on
        ///     reading the fact table.
        /// </summary>
        /// <remarks>
        ///     Use the property to define the default name for empty members. Empty members may
        ///     appear in a hierarchy if the fact table contains NULL values
        /// </remarks>
        [Description(
            "The name for an NULL dimension member, if any appears in the dimension on reading the fact table.")]
        [Category("Appearance")]
        [DefaultValue("")]
        [NotifyParentProperty(true)]
        public string UnknownMemberName
        {
            get => FUnknownMemberName;
            set => FUnknownMemberName = value;
        }

        /// <summary>
        ///     List of additional attributes (data table fields), containing the auxiliary
        ///     information for every member of the specified hierarchy.
        /// </summary>
        /// <remarks>
        ///     <para>For the Direct-version.</para>
        ///     <para>
        ///         For the MOlapCube component this information is available only after hierarchy
        ///         initialization.
        ///     </para>
        /// </remarks>
        [Category("Appearance")]
        [Description(
            "List of additional attributes (data table fields), containing the auxiliary information for every member of the specified hierarchy.")]
        [NotifyParentProperty(true)]
        public InfoAttributes InfoAttributes => FInfoAttributes;

        [OnSerializing]
        private void SerializeMembers(StreamingContext context)
        {
            membersstream = new MemoryStream();
            var writer = new BinaryWriter(membersstream);
            WriteStream(writer);
        }

        [OnSerialized]
        private void SerializeMembersEnd(StreamingContext context)
        {
            membersstream = null;
        }

        [OnDeserialized]
        private void DeserializeMembers(StreamingContext context)
        {
            membersstream.Position = 0;
            var reader = new BinaryReader(membersstream);
            ReadStream(reader);
            membersstream = null;
        }

        /// <summary>Returns the unique name of the hierarchy</summary>
        public override string ToString()
        {
            return UniqueName;
        }

        internal void WriteStream(BinaryWriter stream)
        {
            if (FCubeLevels.Count == 0) return;
            for (var i = 0; i < FCubeLevels.Count; i++)
            {
                var l = FCubeLevels[i];
                StreamUtils.WriteInt32(stream, l.FUniqueNamesArray.Count);
                foreach (var m in l.FUniqueNamesArray.Values)
                    m.WriteStream(stream);
            }
            DoMembersWriteStream(stream, FCubeLevels[0].Members, Tags.tgCubeHierarchy_ChildrenCount, 0);
        }

        internal void DoMembersWriteStream(BinaryWriter stream, CubeMembers members, Tags tag, int levelindex)
        {
            var level = FCubeLevels[levelindex];
            StreamUtils.WriteTag(stream, tag);
            StreamUtils.WriteInt32(stream, members.Count);
            for (var i = 0; i < members.Count; i++)
            {
                var m = members[i];
                StreamUtils.WriteString(stream, m.FUniqueName);
                var b = true;
                if (m.Children.Count > 0)
                {
                    b = false;
                    DoMembersWriteStream(stream, m.Children, Tags.tgCubeHierarchy_ChildrenCount, levelindex);
                }
                if (m.NextLevelChildren.Count > 0)
                {
                    b = false;
                    DoMembersWriteStream(stream, m.NextLevelChildren, Tags.tgCubeHierarchy_NextLevelChildrenCount,
                        levelindex + 1);
                }
                if (b)
                    StreamUtils.WriteTag(stream, Tags.tgCubeHierarchy_LeafMember);
            }
        }

        internal void DoMembersReadStream(BinaryReader stream, CubeMembers members, int levelindex,
            CubeMember parent, bool isNextlevel)
        {
            var memberscount = StreamUtils.ReadInt32(stream);
            members.Capacity = memberscount;
            var level = FCubeLevels[levelindex];
            for (var i = 0; i < memberscount; i++)
            {
                var memberindex = StreamUtils.ReadString(stream);
                CubeMember m;
                level.FUniqueNamesArray.TryGetValue(memberindex, out m);
                level.FetchedMembers[m.fID] = m;
                m.FParent = parent;
                members.Add(m);
                if (isNextlevel) level.Members.Add(m);
                var Tag = StreamUtils.ReadTag(stream);
                switch (Tag)
                {
                    case Tags.tgCubeHierarchy_LeafMember:
                        break;
                    case Tags.tgCubeHierarchy_ChildrenCount:
                        DoMembersReadStream(stream, m.Children, levelindex, m, false);
                        break;
                    case Tags.tgCubeHierarchy_NextLevelChildrenCount:
                        DoMembersReadStream(stream, m.NextLevelChildren, levelindex + 1, m, true);
                        break;
                }
            }
        }

        internal void ReadStream(BinaryReader stream)
        {
            for (var i = 0; i < FCubeLevels.Count; i++)
            {
                var l = FCubeLevels[i];
                //l.FetchedMembers = null;
                var memberscount = StreamUtils.ReadInt32(stream);
                l.FMembers = new CubeMembers();
                l.FUniqueNamesArray = l.FUniqueNamesArrayCreate(memberscount);
                for (var j = 0; j < memberscount; j++)
                {
                    var Tag = StreamUtils.ReadTag(stream);
                    if (Tag != Tags.tgCubeMember)
                        throw new Exception("Unknown tag: " + Tag);
                    var m = new CubeMember(this, l);
                    m.ReadStream(stream);
                    l.FUniqueNamesArray.Add(m.UniqueName, m);
                }
            }
            if (FCubeLevels.Count > 0)
            {
                StreamUtils.ReadTag(stream); //stream.ReadInt32();
                DoMembersReadStream(stream, FCubeLevels[0].Members, 0, null, false);
            }
        }

        internal CubeLevel FindLevel(string uniqueName)
        {
            foreach (var l in Levels)
                if (l.UniqueName == uniqueName) return l;
            return null;
        }

        private void DataSetDisposed(object sender, EventArgs e)
        {
            //fSourceTable = null;
            DisplayField = "";
            ParentField = "";
            IDField = "";
            foreach (var i in FInfoAttributes)
                i.SourceField = "";
        }

        internal void Init(CubeDimension ADimension)
        {
            Dimension = ADimension;

            if (ADimension == null || ADimension.FCube == null)
            {
                if (string.IsNullOrEmpty(FTotalCaption))
                    FTotalCaption = RadarUtils.GetResStr("rsTotalCaption");
                if (string.IsNullOrEmpty(FDiscretizationBucketFormat))
                    FDiscretizationBucketFormat = RadarUtils.GetResStr("rsDefaultBucketFormat");
                if (string.IsNullOrEmpty(FUnknownMemberName))
                    FUnknownMemberName = RadarUtils.GetResStr("rsDefaultUnknownMemberName");
            }
            else
            {
                if (string.IsNullOrEmpty(FTotalCaption))
                    FTotalCaption = RadarUtils.GetResStr("rsTotalCaption");
                if (string.IsNullOrEmpty(FDiscretizationBucketFormat))
                    FDiscretizationBucketFormat = RadarUtils.GetResStr("rsDefaultBucketFormat");
                if (string.IsNullOrEmpty(FUnknownMemberName))
                    FUnknownMemberName = RadarUtils.GetResStr("rsDefaultUnknownMemberName");
            }

            // resolving for Children hierarchies
            ResolveChildrenAndSource();
        }

        /// <summary>
        ///     Restores children and source hierarchies
        /// </summary>
        internal void ResolveChildrenAndSource()
        {
            if (fChildrenHierarchies != null)
                foreach (var s in fChildrenHierarchies.Split('|'))
                {
                    var ch = Dimension.Hierarchies.Find(s);
                    if (ch != null && !ChildrenList.Contains(ch) && ch != this)
                        ChildrenList.Add(ch);
                }
            foreach (var l in FCubeLevels)
                if (l.FSourceHierarchyName != null)
                {
                    l.FSourceHierarchy = Dimension.Hierarchies.Find(l.FSourceHierarchyName);
                    l.FSourceHierarchyName = null;
                }
            if (ChildrenList.Count > 0)
            {
                var lh = new List<CubeHierarchy>();
                foreach (var h in ChildrenList)
                    if (!lh.Contains(h)) lh.Add(h);
                if (lh.Count < ChildrenList.Count)
                {
                    ChildrenList.Clear();
                    ChildrenList.AddRange(lh);
                }
            }
        }

        internal CubeHierarchy Clone()
        {
            var target = new CubeHierarchy();
            target.Init(Dimension);
            //target.fSourceTable = fSourceTable;
            target.FDataTable = FDataTable;

            target.FChildren = FChildren;

            target.FDisplayField = FDisplayField;
            target.FDisplayFieldType = DisplayFieldType;
            target.FParentField = FParentField;
            target.FParentFieldType = FParentFieldType;
            target.FIDField = FIDField;
            target.FIDFieldType = FIDFieldType;
            target.FDisplayName = FDisplayName;
            target.FTotalCaption = FTotalCaption;
            target.FDescription = FDescription;
            target.FOrigin = FOrigin;
            target.FUniqueName = FUniqueName;
            target.FDiscretizationMethod = FDiscretizationMethod;
            target.FDiscretizationBucketCount = FDiscretizationBucketCount;
            target.FDiscretizationBucketFormat = FDiscretizationBucketFormat;
            target.FTypeOfMembers = FTypeOfMembers;
            target.FFormatString = FFormatString;
            target.FCalculatedByRows = FCalculatedByRows;
            target.FDescriptionField = FDescriptionField;
            target.FMembersWithData = FMembersWithData;
            target.FDisplayFolder = FDisplayFolder;
            target.FMemberIsNull = FMemberIsNull;
            target.FUnknownMemberName = FUnknownMemberName;
            target.FAlignment = FAlignment;
            target.FTotalAppearance = FTotalAppearance;
            target.FAllowResort = FAllowResort;
            target.FAllowChangeTotalAppearance = FAllowChangeTotalAppearance;
            target.FAllowFilter = FAllowFilter;
            target.FAllowRegroup = FAllowRegroup;
            target.FAllowPopupOnCaptions = FAllowPopupOnCaptions;
            target.FAllowPopupOnLabels = FAllowPopupOnLabels;
            target.FVisible = FVisible;
            target.FAllowSwapMembers = FAllowSwapMembers;
            target.FAllowMultiselect = FAllowMultiselect;
            target.FShowEmptyLines = FShowEmptyLines;
            target.FInfoAttributes = FInfoAttributes.Clone();
            target.FBIType = FBIType;
            return target;
        }

        internal void BIFlat_UpdateRanksFor()
        {
            foreach (CubeMember m in Levels[0].Members)
            {
                DateTime d = RadarUtils.FromOADate(m.FRank);
                switch (FBIType)
                {
                    case BIMembersType.ltTimeDayOfMonth:
                        m.FRank = d.Day;
                        break;
                    case BIMembersType.ltTimeDayOfWeekLong:
                        m.FRank = (int)d.DayOfWeek;
                        break;
                    case BIMembersType.ltTimeDayOfWeekShort:
                        m.FRank = (int)d.DayOfWeek;
                        break;
                    case BIMembersType.ltTimeDayOfYear:
                        m.FRank = d.Day + 32 * d.Month;
                        break;
                    case BIMembersType.ltTimeHalfYear:
                        m.FRank -= d.Year;
                        break;
                    case BIMembersType.ltTimeHour:
                        m.FRank = d.Hour;
                        break;
                    case BIMembersType.ltTimeMinute:
                        m.FRank = d.Minute;
                        break;
                    case BIMembersType.ltTimeMonthLong:
                        m.FRank = d.Month;
                        break;
                    case BIMembersType.ltTimeMonthNumber:
                        m.FRank = d.Month;
                        break;
                    case BIMembersType.ltTimeMonthShort:
                        m.FRank = d.Month;
                        break;
                    case BIMembersType.ltTimeQuarter:
                        m.FRank -= d.Year;
                        break;
                    case BIMembersType.ltTimeSecond:
                        m.FRank = d.Second;
                        break;
                    case BIMembersType.ltTimeWeekOfYear:
                        m.FRank = d.Day + 32 * d.Month;
                        break;
                }
            }
        }

        /// <summary>Searches for a hierarchy member by its unique name.</summary>
        /// <param name="UniqueName">The unique name of the member</param>
        public CubeMember FindMemberByUniqueName(string UniqueName)
        {
            foreach (var l in Levels)
            {
                var Result = l.FindMemberByUniqueName(UniqueName);
                if (Result != null) return Result;
            }
            return null;
        }

        #region IPropertyGridLinker

        IDictionary<string, IList<string>> IPropertyGridLinker.TableToIDFields { get; set; }

        string IPropertyGridLinker.DataTable
        {
            get => DataTable;
            set => DataTable = value;
        }

        string IPropertyGridLinker.DisplayField
        {
            get => DisplayField;
            set => DisplayField = value;
        }

        #endregion


        #region IStreamedObject members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy);

            StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_Origin);
            StreamUtils.WriteByte(writer, (byte) FOrigin);

            StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DisplayName);
            StreamUtils.WriteString(writer, FDisplayName);

            StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_UniqueName);
            StreamUtils.WriteString(writer, FUniqueName);

            if (!string.IsNullOrEmpty(FDescription))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_Description);
                StreamUtils.WriteString(writer, FDescription);
            }

            if (!string.IsNullOrEmpty(DefaultMember))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DefaultMember);
                StreamUtils.WriteString(writer, DefaultMember);
            }

            if (FTotalCaption != RadarUtils.GetResStr("rsTotalCaption"))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_TotalCaption);
                StreamUtils.WriteString(writer, FTotalCaption);
            }

            if (FCubeLevels.Count > 0)
                StreamUtils.WriteStreamedObject(writer, FCubeLevels, Tags.tgCubeHierarchy_Levels);

            if (!string.IsNullOrEmpty(FDisplayField))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DisplayField);
                StreamUtils.WriteString(writer, FDisplayField);
            }

            if (!string.IsNullOrEmpty(FParentField))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_ParentField);
                StreamUtils.WriteString(writer, FParentField);
            }

            if (!string.IsNullOrEmpty(FIDField))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_IDField);
                StreamUtils.WriteString(writer, FIDField);
            }

            if (!string.IsNullOrEmpty(FBaseNamedSetHierarchies))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_BaseNamedSetHierarchies);
                StreamUtils.WriteString(writer, FBaseNamedSetHierarchies);
            }

            if (FTypeOfMembers != HierarchyDataType.htCommon)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_TypeOfMembers);
                StreamUtils.WriteInt32(writer, (int) FTypeOfMembers);
            }

            if (FInfoAttributes.Count > 0)
                StreamUtils.WriteStreamedObject(writer, FInfoAttributes, Tags.tgCubeHierarchy_InfoAttributes);

            if (FCalculatedByRows)
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_CalculatedByRows);

            //
            if (!string.IsNullOrEmpty(fChildrenHierarchies))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_Children);
                StreamUtils.WriteString(writer, fChildrenHierarchies);
            }

            if (!string.IsNullOrEmpty(FFormatString))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_FormatString);
                StreamUtils.WriteString(writer, FFormatString);
            }

            if (FMDXLevelNames.Count > 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_MDXLevelNames);
                StreamUtils.WriteInt32(writer, FMDXLevelNames.Count);
                foreach (var s in FMDXLevelNames)
                    StreamUtils.WriteString(writer, s);
            }

            if (!string.IsNullOrEmpty(FDescriptionField))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DescriptionField);
                StreamUtils.WriteString(writer, FDescriptionField);
            }

            if (FDisplayFieldType != typeof(string))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DisplayFieldType);
                StreamUtils.WriteType(writer, FDisplayFieldType);
            }

            if (FParentFieldType != null)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_ParentFieldType);
                StreamUtils.WriteType(writer, FParentFieldType);
            }

            if (FIDFieldType != null)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_IDFieldType);
                StreamUtils.WriteType(writer, FIDFieldType);
            }

            if (!string.IsNullOrEmpty(FDisplayFolder))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DisplayFolder);
                StreamUtils.WriteString(writer, FDisplayFolder);
            }

            if (!string.IsNullOrEmpty(FDataTable))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DataTable);
                StreamUtils.WriteString(writer, FDataTable);
            }

            if (FMembersWithData != MembersWithData.dmNonLeafDataHidden)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_MembersWithData);
                StreamUtils.WriteInt32(writer, (int) FMembersWithData);
            }

            if (FMemberIsNull != MemberIsNull.nmUnknownMemberVisible)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_MemberIsNull);
                StreamUtils.WriteInt32(writer, (int) FMemberIsNull);
            }

            if (FUnknownMemberName != RadarUtils.GetResStr("rsDefaultUnknownMemberName"))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_UnknownMemberName);
                StreamUtils.WriteString(writer, FUnknownMemberName);
            }

            if (FDiscretizationMethod != DiscretizationMethod.dmNone)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DiscretizationMethod);
                StreamUtils.WriteInt32(writer, (int) FDiscretizationMethod);
            }

            if (FDiscretizationBucketCount != 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DiscretizationBucketCount);
                StreamUtils.WriteInt32(writer, FDiscretizationBucketCount);
            }

            if (FDiscretizationBucketFormat != RadarUtils.GetResStr("rsDefaultBucketFormat"))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DiscretizationBucketFormat);
                StreamUtils.WriteString(writer, FDiscretizationBucketFormat);
            }

            if (FAlignment != ContentAlignment.MiddleLeft)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_Alignment);
                StreamUtils.WriteInt32(writer, (int) FAlignment);
            }

            if (FTotalAppearance != TotalAppearance.taLast)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_TotalAppearance);
                StreamUtils.WriteInt32(writer, (int) FTotalAppearance);
            }

            if (!FAllowResort)
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DenyResort);

            if (!FAllowChangeTotalAppearance)
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DenyChangeTotalAppearance);

            if (!FAllowRegroup)
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DenyRegroup);

            if (!FAllowFilter)
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DenyFilter);

            if (!FAllowPopupOnCaptions)
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DenyPopupOnCaptions);

            if (!FAllowPopupOnLabels)
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DenyPopupOnLabels);

            if (!FAllowHierarchyEditor)
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DenyHierarchyEditor);

            if (!FAllowSwapMembers)
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_DenySwapMembers);

            if (!Visible)
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_NotVisible);

            if (!FTakeFiltersIntoCalculations)
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_NotTakeFiltersIntoCalculations);

            if (FShowEmptyLines)
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_ShowEmptyLines);

            if (FBIType != BIMembersType.ltNone)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_BIType);
                StreamUtils.WriteInt32(writer, (int) FBIType);
            }

            if (FCubeLevels != null)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_MembersStream);
                WriteStream(writer);
            }

            StreamUtils.WriteTag(writer, Tags.tgCubeHierarchy_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgCubeHierarchy);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgCubeHierarchy_MembersStream:
                        ReadStream(reader);
                        break;
                    case Tags.tgCubeHierarchy_Alignment:
                        FAlignment = (ContentAlignment) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeHierarchy_BIType:
                        FBIType = (BIMembersType) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeHierarchy_CalculatedByRows:
                        FCalculatedByRows = true;
                        break;
                    case Tags.tgCubeHierarchy_Children:
                        fChildrenHierarchies = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_DataTable:
                        FDataTable = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_DenyChangeTotalAppearance:
                        FAllowChangeTotalAppearance = false;
                        break;
                    case Tags.tgCubeHierarchy_DenyFilter:
                        FAllowFilter = false;
                        break;
                    case Tags.tgCubeHierarchy_DenyHierarchyEditor:
                        FAllowHierarchyEditor = false;
                        break;
                    case Tags.tgCubeHierarchy_DenyPopupOnCaptions:
                        FAllowPopupOnCaptions = false;
                        break;
                    case Tags.tgCubeHierarchy_DenyPopupOnLabels:
                        FAllowPopupOnLabels = false;
                        break;
                    case Tags.tgCubeHierarchy_DenyRegroup:
                        FAllowRegroup = false;
                        break;
                    case Tags.tgCubeHierarchy_DenyResort:
                        FAllowResort = false;
                        break;
                    case Tags.tgCubeHierarchy_DenySwapMembers:
                        FAllowSwapMembers = false;
                        break;
                    case Tags.tgCubeHierarchy_Description:
                        FDescription = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_DefaultMember:
                        DefaultMember = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_DescriptionField:
                        FDescriptionField = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_DiscretizationBucketCount:
                        FDiscretizationBucketCount = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeHierarchy_DiscretizationBucketFormat:
                        FDiscretizationBucketFormat = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_DiscretizationMethod:
                        FDiscretizationMethod = (DiscretizationMethod) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeHierarchy_DisplayField:
                        FDisplayField = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_DisplayFieldType:
                        FDisplayFieldType = StreamUtils.ReadType(reader);
                        break;
                    case Tags.tgCubeHierarchy_DisplayFolder:
                        FDisplayFolder = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_DisplayName:
                        FDisplayName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_FormatString:
                        FFormatString = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_HierarchyAppearance:
                        StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeHierarchy_IDField:
                        FIDField = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_BaseNamedSetHierarchies:
                        FBaseNamedSetHierarchies = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_IDFieldType:
                        FIDFieldType = StreamUtils.ReadType(reader);
                        break;
                    case Tags.tgCubeHierarchy_InfoAttributes:
                        StreamUtils.ReadStreamedObject(reader, FInfoAttributes);
                        break;
                    case Tags.tgCubeHierarchy_Levels:
                        StreamUtils.ReadStreamedObject(reader, FCubeLevels);
                        break;
                    case Tags.tgCubeHierarchy_MDXLevelNames:
                        var c = StreamUtils.ReadInt32(reader);
                        FMDXLevelNames = new List<string>(c);
                        for (var i = 0; i < c; i++)
                            FMDXLevelNames.Add(StreamUtils.ReadString(reader));
                        break;
                    case Tags.tgCubeHierarchy_MemberIsNull:
                        FMemberIsNull = (MemberIsNull) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeHierarchy_MembersWithData:
                        FMembersWithData = (MembersWithData) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeHierarchy_NotTakeFiltersIntoCalculations:
                        FTakeFiltersIntoCalculations = false;
                        break;
                    case Tags.tgCubeHierarchy_NotVisible:
                        FVisible = false;
                        break;
                    case Tags.tgCubeHierarchy_Origin:
                        FOrigin = (HierarchyOrigin) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeHierarchy_ParentField:
                        FParentField = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_ParentFieldType:
                        FParentFieldType = StreamUtils.ReadType(reader);
                        break;
                    case Tags.tgCubeHierarchy_ShowEmptyLines:
                        FShowEmptyLines = true;
                        break;
                    case Tags.tgCubeHierarchy_TotalAppearance:
                        FTotalAppearance = (TotalAppearance) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeHierarchy_TotalCaption:
                        FTotalCaption = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_TypeOfMembers:
                        FTypeOfMembers = (HierarchyDataType) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeHierarchy_UniqueName:
                        FUniqueName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_UnknownMemberName:
                        FUnknownMemberName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeHierarchy_EOT:
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