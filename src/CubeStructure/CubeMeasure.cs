using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.CubeStructure
{
    /// <summary>Describes measures of the Cube.</summary>
    public class CubeMeasure : IStreamedObject, IDescriptionable, IPropertyGridLinker
    {
        internal OlapFunction FAggregateFunction = OlapFunction.stSum;
        internal Controls.Cube.RadarCube FCube;
        internal object FDataSortedArray; // the object of List<TSortedColumnDataItem> type
        internal string FDataTable; // The name of the measure table
        internal string FDefaultFormat = "Currency";
        internal string FDescription = "";
        internal string FDisplayFolder = "";


        internal string FDisplayName = "";
        internal string FExpression = "";
        internal Type FFieldType = typeof(double);
        internal bool FIsKPI;
        internal int FKPIStatusImageIndex = -1;

        internal int FKPITrendImageIndex = -1;
        internal int fMeasureID = -1;
        private string FSourceField;
        internal string FUniqueName = Guid.NewGuid().ToString();
        internal bool FVisible;

        /// <summary>
        ///     Creates an instance of the CubeMeasure class
        /// </summary>
        public CubeMeasure()
        {
            VisibleInTree = true;
            CalculatedByRows = false;
        }

        public CubeMeasure(Controls.Cube.RadarCube ACube)
            : this()
        {
            Init(ACube);
        }

        /// <summary>
        ///     References to the object of the RadarCube type that contains the specified
        ///     measure.
        /// </summary>
        [Browsable(false)]
        public Controls.Cube.RadarCube Cube => FCube;

        /// <summary>Defines whether the measure is visible in the Grid by default.</summary>
        [Description("Defines whether the measure is visible in the Grid by default.")]
        [Category("Behavior")]
        [DefaultValue(false)]
        [NotifyParentProperty(true)]
        public bool Visible
        {
            get => FVisible;
            set => FVisible = value;
        }

        /// <summary>Defines whether the measure is visible in the Cube Structure Tree.</summary>
        [Description("Defines whether the measure is visible in the Cube Structure Tree.")]
        [Category("Behavior")]
        [DefaultValue(true)]
        [NotifyParentProperty(true)]
        public bool VisibleInTree { get; set; }

        /// <summary>A detailed description of the measure.</summary>
        [Category("Appearance")]
        [Description("A detailed description of the measure.")]
        [DefaultValue("")]
        [NotifyParentProperty(true)]
        public string Description
        {
            get => FDescription;
            set => FDescription = value;
        }

        /// <summary>
        ///     If True, then the specified measure represents a Key Peformance Indicator. For a
        ///     client to MS Analysis only.
        /// </summary>
        [Browsable(false)]
        public bool IsKPI => FIsKPI;

        /// <summary>
        ///     A caption of the measure.
        /// </summary>
        [Category("Appearance")]
        [Description("A caption of the measure.")]
        [NotifyParentProperty(true)]
        public string DisplayName
        {
            get => FDisplayName;
            set => FDisplayName = value;
        }

        /// <summary>
        ///     An unique string measure identifier.
        /// </summary>
        [Category("Behavior")]
        [Description("An unique string measure identifier.")]
        [NotifyParentProperty(true)]
        public string UniqueName
        {
            get => FUniqueName;
            set => FUniqueName = value;
        }

        /// <summary>
        ///     An icon index which is used to display KPI values.
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DefaultValue(-1)]
        public int KPIStatusImageIndex
        {
            get => FKPIStatusImageIndex;
            set => FKPIStatusImageIndex = value;
        }

        /// <summary>
        ///     An icon index which is used to display KPI values.
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DefaultValue(-1)]
        public int KPITrendImageIndex
        {
            get => FKPITrendImageIndex;
            set => FKPITrendImageIndex = value;
        }

        /// <summary>
        ///     Reserved for prospective versions. Not used in a given version.
        /// </summary>
        [Browsable(false)]
        [DefaultValue("")]
        [NotifyParentProperty(true)]
        public string Expression
        {
            get => FExpression;
            set => FExpression = value;
        }

        /// <summary>
        ///     The name of the data table the measure takes data from.
        /// </summary>
        [Category("Advanced (manage carefully)")]
        [Description("The name of the data table the measure takes data from")]
        [NotifyParentProperty(true)]
        [RefreshProperties(RefreshProperties.All)]
        public string DataTable
        {
            get => FDataTable;
            set => FDataTable = value;
        }

        /*
                /// <summary>
                /// The type of the calculated field. Only set this property if CalculatedByRows is true. 
                /// The value of the property is converted to the real type by the standard Type.GetType function.
                /// </summary>
                [Category("Advanced (manage carefully)"),
                Description("The type of the calculated field. Only set this property if CalculatedByRows is true")]
                public string CalculatedFieldType
                {
                    get { return FFieldType.ToString(); }
                    set { FFieldType = Type.GetType(value, true); }
                }
        */
        /// <summary>
        ///     <para>
        ///         The mask defining the formatting rules for the values of the specified
        ///         measure.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The format string specified in this property, determines the display mask of
        ///         the formatted measure value and has different specification, depending on the type
        ///         of the output data.
        ///     </para>
        ///     <para>
        ///         To begin with, let's take a look at five types of the masks which have their
        ///         own specifiers:
        ///     </para>
        ///     <para></para>
        ///     <list type="table">
        ///         <listheader>
        ///             <term>Specifier</term>
        ///             <description>Description</description>
        ///         </listheader>
        ///         <item>
        ///             <term>
        ///                 <pre>
        ///                     <code>
        /// Standard
        /// </code>
        ///                 </pre>
        ///             </term>
        ///             <description>
        ///                 Depends on the data type. For integer types the numeric mask
        ///                 "#,#" is applied. For floating point types the numeric mask "#,0.00" is
        ///                 applied. For the Currency type the mask "Currency" is applied (see the next
        ///                 table row for the description). For the DateTime type the formatting of the
        ///                 DateTime.ToShortDateString is applied. For all other types a special format
        ///                 is not applied.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <pre>
        ///                     <code>
        /// Currency
        /// </code>
        ///                 </pre>
        ///             </term>
        ///             <description>
        ///                 The format is applied for the numeric data and the format
        ///                 string is taken from the OlapGrid.CurrencyFormatString property. If this
        ///                 property returns an empty string, the default currency format is
        ///                 used.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <pre>
        ///                     <code>
        /// Short Date
        /// </code>
        ///                 </pre>
        ///             </term>
        ///             <description>
        ///                 <para>
        ///                     For data of the DateTime type. The formatting of the
        ///                     DateTime.ToShortDateString is applied.
        ///                 </para>
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <pre>
        ///                     <code>
        /// Short Time
        /// </code>
        ///                 </pre>
        ///             </term>
        ///             <description>
        ///                 For data of the DateTime type. The formatting of the
        ///                 DateTime.ToShortTimeString is applied.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <term>
        ///                 <pre>
        ///                     <code>
        /// Percent
        /// </code>
        ///                 </pre>
        ///             </term>
        ///             <description>
        ///                 For numeric data. The numeric mask of the "#,0.00%" format is
        ///                 used.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        [Category("Behavior")]
        [Description("The mask defining the formatting rules for the values of the specified measure.")]
        [DefaultValue("Currency")]
        [NotifyParentProperty(true)]
        public string DefaultFormat
        {
            get => FDefaultFormat;
            set => FDefaultFormat = value;
        }

        /// <summary>
        ///     Defines the type of the aggregate function for a measure (Sum, Count,
        ///     etc.).
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The data fetched from the datavbase is aggregated by the TOLAPCube, while the
        ///         MOlapCube shows only the data already aggregated by the MS AS server.
        ///     </para>
        /// </remarks>
        [Category("Behavior")]
        [Description("Defines the type of the aggregate function for a measure (Sum, Count, etc.).")]
        [DefaultValue(OlapFunction.stSum)]
        [NotifyParentProperty(true)]
        public OlapFunction AggregateFunction
        {
            get => FAggregateFunction;
            set => FAggregateFunction = value;
        }

        /// <summary>
        ///     Indicates a measure calculated at the level of rows upon the fetching of data
        ///     from the fact table. Used only in the TOLAPCube component.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         A value of this property when set to True shows that the specified measure is
        ///         to be calculated by the TOLAPCube.OnReadFactTable event.
        ///     </para>
        ///     <para>
        ///         There are three types of calculated measures, each of them is calculated at a
        ///         different time and based on different data.
        ///     </para>
        ///     <para>
        ///         Calculated measures of the first type are calculated while a fact table is
        ///         being read - for each row separately. They are based on its field values and all
        ///         the related dimension tables field values. The meaning of these calculated measures
        ///         conforms to view's or table's calculated fields and, if it is more convenient, can
        ///         be substituted accordingly.
        ///     </para>
        ///     <para>
        ///         Calculated measures of the second type are calculated while the Cube slice is
        ///         being calculated, and and use for the current cell's data array aggregation the
        ///         algorithm described by the programmer.
        ///     </para>
        ///     <para>
        ///         Calculated measures of the third type are calculated right before the Cube
        ///         slice display, on the basis of the current slice data.
        ///     </para>
        /// </remarks>
        [Category("Advanced (manage carefully)")]
        [Description(
            "Indicates a measure calculated at the level of rows upon the fetching of data from the fact table. Used only in the TOLAPCube component.")]
        [DefaultValue(false)]
        [NotifyParentProperty(true)]
        public bool CalculatedByRows { get; set; }

        /// <summary>
        ///     Data source field name for a given measure. For TOLAPCube only. Applicable
        ///     only if the measure is not calculated.
        /// </summary>
        [Category("Advanced (manage carefully)")]
        [Description(
            "Data source field name for the specified measure. Used in TOLAPCube only. Applicable only if the measure is not calculated.")]
        [NotifyParentProperty(true)]
        public string SourceField
        {
            get => CalculatedByRows ? null : FSourceField;
            set => FSourceField = value;
        }

        /// <summary>The type of the data source field for a measure.</summary>
        [Browsable(false)]
        public Type SourceFieldType
        {
            get => FFieldType;
            set => FFieldType = value;
        }

        /// <summary>
        ///     The folder name where the specified measure will be placed when its name is
        ///     displayed in the "Cube Structure" window of the Measures subtree.
        /// </summary>
        [Browsable(false)]
        [DefaultValue("")]
        [NotifyParentProperty(true)]
        public string DisplayFolder
        {
            get => FDisplayFolder;
            set => FDisplayFolder = value;
        }

        internal void Init(Controls.Cube.RadarCube ACube)
        {
            FCube = ACube;
        }

        /// <summary>
        ///     Returns an unique name of the measure
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return UniqueName;
        }

        private void DataSetDisposed(object sender, EventArgs e)
        {
            //FSourceTable = null;
            FSourceField = "";
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
            get => SourceField;
            set => SourceField = value;
        }

        #endregion

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgCubeMeasure);

            if (!string.IsNullOrEmpty(FDisplayName))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_DisplayName);
                StreamUtils.WriteString(writer, FDisplayName);
            }

            if (!string.IsNullOrEmpty(FDisplayFolder))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_DisplayFolder);
                StreamUtils.WriteString(writer, FDisplayFolder);
            }

            if (!string.IsNullOrEmpty(FExpression))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_Expression);
                StreamUtils.WriteString(writer, FExpression);
            }

            StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_UniqueName);
            StreamUtils.WriteString(writer, FUniqueName);

            if (!string.IsNullOrEmpty(FDescription))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_Description);
                StreamUtils.WriteString(writer, FDescription);
            }

            if (!string.IsNullOrEmpty(FDefaultFormat))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_DefaultFormat);
                StreamUtils.WriteString(writer, FDefaultFormat);
            }

            if (FIsKPI)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_IsKPI);
                StreamUtils.WriteBoolean(writer, FIsKPI);
            }

            StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_KPIStatusImageIndex);
            StreamUtils.WriteInt32(writer, FKPIStatusImageIndex);

            StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_AggregateFunction);
            StreamUtils.WriteInt16(writer, (short) FAggregateFunction);

            if (CalculatedByRows)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_CalculatedByRows);
                StreamUtils.WriteBoolean(writer, CalculatedByRows);
            }

            if (!string.IsNullOrEmpty(FSourceField))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_SourceField);
                StreamUtils.WriteString(writer, FSourceField);
            }

            StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_FieldType);
            StreamUtils.WriteType(writer, FFieldType);

            StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_DataTable);
            StreamUtils.WriteString(writer, FDataTable);

            if (FVisible)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_Visible);
                StreamUtils.WriteBoolean(writer, FVisible);
            }

            if (!VisibleInTree)
                StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_NotVisibleInTree);

            StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_MeasureID);
            StreamUtils.WriteInt32(writer, fMeasureID);

            StreamUtils.WriteTag(writer, Tags.tgCubeMeasure_EOT);
            /*
                    [NonSerialized]
                    internal object FDataSortedArray; // the object of List<TSortedColumnDataItem> type
                    [NonSerialized]
                    internal RadarCube FCube;
            */
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgCubeMeasure);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgCubeMeasure_DisplayName:
                        FDisplayName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeMeasure_DisplayFolder:
                        FDisplayFolder = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeMeasure_Expression:
                        FExpression = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeMeasure_UniqueName:
                        FUniqueName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeMeasure_Description:
                        FDescription = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeMeasure_DefaultFormat:
                        FDefaultFormat = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeMeasure_IsKPI:
                        FIsKPI = StreamUtils.ReadBoolean(reader);
                        break;
                    case Tags.tgCubeMeasure_KPIStatusImageIndex:
                        FKPIStatusImageIndex = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeMeasure_AggregateFunction:
                        FAggregateFunction = (OlapFunction) StreamUtils.ReadInt16(reader);
                        break;
                    case Tags.tgCubeMeasure_CalculatedByRows:
                        CalculatedByRows = StreamUtils.ReadBoolean(reader);
                        break;
                    case Tags.tgCubeMeasure_SourceField:
                        FSourceField = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeMeasure_FieldType:
                        FFieldType = StreamUtils.ReadType(reader);
                        break;
                    case Tags.tgCubeMeasure_DataTable:
                        FDataTable = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeMeasure_Visible:
                        FVisible = StreamUtils.ReadBoolean(reader);
                        break;
                    case Tags.tgCubeMeasure_NotVisibleInTree:
                        VisibleInTree = false;
                        break;
                    case Tags.tgCubeMeasure_MeasureID:
                        fMeasureID = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeMeasure_EOT:
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