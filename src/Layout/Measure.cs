using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Events;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>An object responsible for measures on the grid level.</summary>
    /// <remarks>
    ///     The corresponding class on the Cube level is called CubeMeasure, and each
    ///     measure in the Grid, if it is collected on the basis of metadata, has reference to the
    ///     appropriate instance of the CubeMeasure class. However, using the
    ///     Measures.AddCalculatedMeasure method, you can create a calculated measure on the Grid
    ///     level, whose value for each Cube cell is calculated not on the fact table data basis,
    ///     but on values of the previously calculated Cube cells.
    /// </remarks>
    public class Measure : IStreamedObject, IChartable
    {
        private Member _member;

        [NonSerialized] internal CubeMeasure FCubeMeasure;

        internal string FDefaultFormat_ = "Currency";
        private bool FDefaultFormatChanged;
        internal string FDescription;

        internal string FDisplayFolder = "";
        internal string FDisplayName;
        internal string FExpression;
        internal MeasureFilter fFilter;
        internal OlapFunction FFunction = OlapFunction.stSum;

        [NonSerialized] internal OlapControl FGrid;

        internal MeasureShowModes FShowModes;
        internal string FUniqueName;

        internal Measure(OlapControl AGrid)
        {
            FGrid = AGrid;
            FShowModes = new MeasureShowModes(this);
            VisibleInTree = true;
        }

        public bool IsActive
        {
            get
            {
                var l = Grid.AxesLayout;
                if (Visible) return true;
                if (l.ColorBackAxisItem == this) return true;
                if (l.fShapeAxisItem == this) return true;
                if (l.fSizeAxisItem == this) return true;
                if (l.fXAxisMeasure == this) return true;
                if (l.fYAxisMeasures.Any(item => item.Contains(this))) return true;
                return false;
            }
        }

        public bool VisibleInChart
        {
            get
            {
                if (Grid.CellsetMode == CellsetMode.cmGrid) return false;

                var l = Grid.AxesLayout;
                if (l.fColorAxisItem == this) return true;
                if (l.fShapeAxisItem == this) return true;
                if (l.fSizeAxisItem == this) return true;
                if (l.fXAxisMeasure == this) return true;
                if (l.fYAxisMeasures.Any(item => item.Contains(this))) return true;
                return false;
            }
        }

        internal MeasureShowMode DefaultMode
        {
            get
            {
                MeasureShowMode result = null;
                foreach (var m in ShowModes)
                    if (m.fVisible)
                    {
                        if (result != null) return null;
                        result = m;
                    }
                return result;
            }
        }

        /// <summary>
        ///     The mask defining the formatting rules for the values of the specified
        ///     measure.
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
        public string DefaultFormat
        {
            get => FCubeMeasure == null || FDefaultFormatChanged && !string.IsNullOrEmpty(FDefaultFormat_)
                ? FDefaultFormat_
                : FCubeMeasure.DefaultFormat;
            set
            {
                FDefaultFormat_ = value;
                FDefaultFormatChanged = FCubeMeasure == null || FCubeMeasure.DefaultFormat != value;
            }
        }

        /// <summary>
        ///     References to the instance of the CubeMeasure class related to the specified
        ///     object.
        /// </summary>
        /// <remarks>
        ///     <para>For calculated measures of the third type is null.</para>
        /// </remarks>
        public CubeMeasure CubeMeasure => FCubeMeasure;

        /// <summary>
        ///     Defines the context filter applied to the measure.
        /// </summary>
        /// <remarks>See the MeasureFilter class description for details.</remarks>
        public MeasureFilter Filter
        {
            get => fFilter;
            set
            {
                if (value != null && value.Equals(fFilter))
                    return;

                if (fFilter != null && fFilter.Equals(value))
                    return;

                fFilter = value;
                Grid.Engine.ClearMeasureData(this);
                if (!Grid.IsUpdating)
                {
                    Grid.CellSet.Rebuild();
                    Grid.EndChange(GridEventType.geFilterAction, this);
                }
            }
        }


        /// <summary>References to the OlapGrid instance that contains the specified measure.</summary>
        public OlapControl Grid => FGrid;

        /// <summary>
        ///     The list of measure display modes. It contains both the standard and custom
        ///     modes.
        /// </summary>
        /// <remarks>
        ///     <para>Is used only in the OlapGrid.OnInitMeasures event handler.</para>
        ///     <para></para>
        /// </remarks>
        public MeasureShowModes ShowModes => FShowModes;

        /// <summary>
        ///     Contains a unique string measure identifier.
        /// </summary>
        public string UniqueName
        {
            get => FUniqueName;
            set => FUniqueName = value;
        }

        /// <summary>
        ///     If True, then the specified measure represents a Key Peformance Indicator. For a
        ///     client to MS Analysis only.
        /// </summary>
        public bool IsKPI => FCubeMeasure == null ? false : FCubeMeasure.FIsKPI;

        /// <summary>A valid MDX-style expression.</summary>
        /// <remarks>
        ///     The value of this property will be used as an MDX expression describing the
        ///     measure in the WITH MEMBER clause of the MDX queries passed to the server.
        /// </remarks>
        /// <example>
        ///     <code lang="CS" title="[New Example]">
        /// 		<![CDATA[
        /// Measure m = OlapAnalysis1.Measures.AddCalculatedMeasure("Calculated");
        /// m.DefaultFormat = "Currency";
        /// m.Expression = "[Measures].[Sales Amount] / [Measures].[Order Count]";]]>
        /// 	</code>
        /// </example>
        public string Expression
        {
            get => FExpression;
            set
            {
                if (FExpression == value)
                    return;

                FExpression = value;
                FGrid.Engine.Clear();
            }
        }

        /// <summary>A visible name of the measure.</summary>
        public string DisplayName
        {
            get => FDisplayName;
            set => FDisplayName = value;
        }

        /// <summary>
        ///     A detailed measure description that appears as a pop-up window (tooltip) when the
        ///     cursor is pointed at the measure name in the Grid.
        /// </summary>
        public string Description
        {
            get => FDescription;
            set => FDescription = value;
        }

        /// <summary>
        ///     The folder name where the specified measure will be placed when its name is
        ///     displayed in the "Cube Structure" window of the Measures subtree.
        /// </summary>
        /// <remarks>
        ///     If this property is left empty, then the measure name will be placed directly
        ///     into the Measures subtree.
        /// </remarks>
        public string DisplayFolder
        {
            get => FDisplayFolder;
            set => FDisplayFolder = value;
        }

        /// <summary>
        ///     Defines the type of the aggregate function for a measure (Sum, Count,
        ///     etc.).
        /// </summary>
        public OlapFunction AggregateFunction
        {
            get => FFunction;
            set => FFunction = value;
        }

        internal bool IsCalculated => FFunction == OlapFunction.stCalculated;

        internal Member Member => _member ?? (_member = GetMeasureMemberInner());

        internal bool FVisible
        {
            get
            {
                if (Member == null)
                    return false;
                return Member.FVisible;
            }
            set
            {
                if (Member == null)
                    return;
                if (Member.FVisible == value)
                    return;
#if DEBUG
                if (DisplayName == "Unit Sales")
                {
                }
#endif
                Member.FVisible = value;
            }
        }

        /// <summary>Defines whether the measure is visible in the Grid.</summary>
        public bool Visible
        {
            get
            {
                if (Member == null)
                    return false;

                return Member.Visible;
            }
            set
            {
                if (Visible == value)
                    return;
                if (Member == null)
                    return;

                Member.FVisible = value;

                if (Grid.Measures.FLevel == null)
                    return;

                if (Grid.Active)
                {
                    if (!Grid.IsUpdating)
                        Grid.CellSet.Rebuild();
                    Grid.EndChange(GridEventType.gePivotAction);
                }
            }
        }

        /// <summary>Defines whether the measure is visible in the Cube Structure Tree.</summary>
        /// <remarks>
        ///     The good place for changing this property is the OlapGrid.OnInitMeasures event handler.
        /// </remarks>
        public bool VisibleInTree { get; set; }

        internal void RestoreAfterSerialization(OlapControl grid)
        {
            FGrid = grid;
            if (FGrid.Cube == null)
                return;
            FCubeMeasure = FGrid.Cube.Measures.Find(UniqueName);
            FDefaultFormatChanged = FCubeMeasure != null && FDefaultFormat_ != FCubeMeasure.DefaultFormat;
        }

        private Member GetMeasureMemberInner()
        {
            if (Grid.Measures.FLevel == null)
                return null;
            foreach (var m in Grid.Measures.FLevel.Members)
                if (m.UniqueName == UniqueName)
                    return m;
            return null;
        }

        public override string ToString()
        {
            return UniqueName;
        }

        internal void InitMeasure(CubeMeasure CubeMeasure)
        {
            FCubeMeasure = CubeMeasure;
            UniqueName = CubeMeasure.UniqueName;
            FExpression = CubeMeasure.Expression;
            FFunction = CubeMeasure.AggregateFunction;
            FDisplayName = CubeMeasure.DisplayName;
            FDescription = CubeMeasure.Description;
            FVisible = CubeMeasure.FVisible;
            FDefaultFormat_ = CubeMeasure.DefaultFormat;

            if (CubeMeasure.FIsKPI) FShowModes.RestoreDefaults();

            FDisplayFolder = CubeMeasure.DisplayFolder;
            VisibleInTree = CubeMeasure.VisibleInTree;
        }

        internal string FormatValue(object Value, string FormatStr)
        {
            return RadarUtils.InternalFormatValue(Value, FormatStr, DefaultFormat,
                Grid.FCurrencyFormatString, Grid.FEmptyDataString);
        }

        private ICubeAddress MergeAddresses(ICubeAddress A, ICubeAddress B)
        {
            if (A == null) return B;
            if (B == null) return A;
            var Result = A.Clone();
            if (B.Measure != null) Result.Measure = B.Measure;
            for (var i = 0; i < B.LevelsCount; i++) Result.AddMember(B.Members(i));
            return Result;
        }

        internal List<Measure> AffectedMeasures()
        {
            if (string.IsNullOrEmpty(Expression))
                return new List<Measure>();

            var res = new List<Measure>();
            var ex = Expression.ToLower();
            foreach (var m in Grid.Measures)
                if (ex.Contains(m.UniqueName.ToLower()) || ex.Contains("[measures].[" + m.DisplayName.ToLower() + "]"))
                    res.Add(m);

            if (res.Count == 0 && Grid.CellsetMode == CellsetMode.cmChart)
            {
                var fake_m = Grid.Measures.FirstOrDefault(x => x.Expression.IsNullOrEmpty());
                if (fake_m != null)
                    res.Add(fake_m);
            }

            return res;
        }

        internal string DoFormatMode(IDataCell Cell, object Value, MeasureShowMode mode, out object OutValue)
        {
            if (mode.Mode == MeasureShowModeType.smNormal || IsKPI)
            {
                OutValue = Value;
                return FormatValue(Value, DefaultFormat);
            }

            var C = Grid.CellSet;
            object V = null;
            OutValue = null;
            ICubeAddress A;
            switch (mode.Mode)
            {
                case MeasureShowModeType.smPercentRowTotal:
                    A = Grid.Engine.CreateCubeAddress();
                    A.Measure = Cell.Address.Measure;
                    var M = C.Cells(C.FixedColumns - 1, Cell.StartRow) as IMemberCell;
                    if (M != null && M.Address != null)
                        A.Merge(M.Address);
                    Grid.Engine.GetCellValue(A, out V);
                    if (RadarUtils.IsNumeric(V) && RadarUtils.IsNumeric(Value) && Convert.ToDouble(V) != 0)
                    {
                        OutValue = Convert.ToDouble(Value) / Convert.ToDouble(V);
                        return FormatValue(OutValue, "Percent");
                    }
                    else
                    {
                        return "";
                    }
                case MeasureShowModeType.smPercentColTotal:
                    A = Grid.Engine.CreateCubeAddress();
                    A.Measure = Cell.Address.Measure;
                    M = C.Cells(Cell.StartColumn, C.FixedRows - 1) as IMemberCell;
                    if (M != null && M.Address != null)
                        A.Merge(M.Address);
                    Grid.Engine.GetCellValue(A, out V);
                    if (RadarUtils.IsNumeric(V) && RadarUtils.IsNumeric(Value) && Convert.ToDouble(V) != 0)
                    {
                        OutValue = Convert.ToDouble(Value) / Convert.ToDouble(V);
                        return FormatValue(OutValue, "Percent");
                    }
                    else
                    {
                        return "";
                    }
                case MeasureShowModeType.smPercentParentRowItem:
                    A = Grid.Engine.CreateCubeAddress();
                    A.Measure = Cell.Address.Measure;
                    M = C.Cells(C.FixedColumns - 1, Cell.StartRow) as IMemberCell;
                    if (M != null) M = M.HierarchyMemberCell;
                    if (M != null) M = M.Parent;
                    if (M != null && M.Address != null) A.Merge(M.Address);
                    M = C.Cells(Cell.StartColumn, C.FixedRows - 1) as IMemberCell;
                    if (M != null) M = M.HierarchyMemberCell;
                    if (M != null && M.Address != null) A.Merge(M.Address);
                    Grid.Engine.GetCellValue(A, out V);
                    if (RadarUtils.IsNumeric(V) && RadarUtils.IsNumeric(Value) && Convert.ToDouble(V) != 0)
                    {
                        OutValue = Convert.ToDouble(Value) / Convert.ToDouble(V);
                        return FormatValue(OutValue, "Percent");
                    }
                    else
                    {
                        return "";
                    }
                case MeasureShowModeType.smPercentParentColItem:
                    A = Grid.Engine.CreateCubeAddress();
                    A.Measure = Cell.Address.Measure;
                    M = C.Cells(C.FixedColumns - 1, Cell.StartRow) as IMemberCell;
                    if (M != null) M = M.HierarchyMemberCell;
                    if (M != null && M.Address != null) A.Merge(M.Address);
                    M = C.Cells(Cell.StartColumn, C.FixedRows - 1) as IMemberCell;
                    if (M != null) M = M.HierarchyMemberCell;
                    if (M != null) M = M.Parent;
                    if (M != null && M.Address != null) A.Merge(M.Address);
                    Grid.Engine.GetCellValue(A, out V);
                    if (RadarUtils.IsNumeric(V) && RadarUtils.IsNumeric(Value) && Convert.ToDouble(V) != 0)
                    {
                        OutValue = Convert.ToDouble(Value) / Convert.ToDouble(V);
                        return FormatValue(OutValue, "Percent");
                    }
                    else
                    {
                        return "";
                    }
                case MeasureShowModeType.smPercentGrandTotal:
                    A = Grid.Engine.CreateCubeAddress();
                    A.Measure = Cell.Address.Measure;
                    Grid.Engine.GetCellValue(A, out V);
                    if (RadarUtils.IsNumeric(V) && RadarUtils.IsNumeric(Value) && Convert.ToDouble(V) != 0)
                    {
                        OutValue = Convert.ToDouble(Value) / Convert.ToDouble(V);
                        return FormatValue(OutValue, "Percent");
                    }
                    else
                    {
                        return "";
                    }
                case MeasureShowModeType.smColumnRank:
                    if (!(Value is IComparable))
                        return "";

                    var mc = Cell.RowMember;
                    if (mc == null) return "";

                    mc = Cell.RowMember.HierarchyMemberCell;
                    if (mc == null) return "";

                    A = Cell.Address.Clone();
                    if (A.Measure == null) return "";
                    A.MeasureMode = A.Measure.ShowModes[0];

                    var Rank = 1;

                    var cmp = Value as IComparable;
                    for (var i = 0; i < mc.SiblingsCount; i++)
                    {
                        var m = mc.Siblings(i).Member;
                        if (m == null) continue;
                        try
                        {
                            A.AddMember(m);
                            object vv;
                            if (FGrid.FEngine.GetCellValue(A, out vv))
                                if (cmp.CompareTo(vv) < 0)
                                    Rank++;
                        }
                        catch
                        {
                            ;
                        }
                    }
                    OutValue = Rank;
                    return Rank.ToString();
                case MeasureShowModeType.smRowRank:
                    if (!(Value is IComparable))
                        return "";

                    mc = Cell.ColumnMember;
                    if (mc == null) return "";

                    mc = Cell.ColumnMember.HierarchyMemberCell;
                    if (mc == null) return "";

                    A = Cell.Address.Clone();
                    if (A.Measure == null) return "";
                    A.MeasureMode = A.Measure.ShowModes[0];

                    Rank = 1;

                    cmp = Value as IComparable;
                    for (var i = 0; i < mc.SiblingsCount; i++)
                    {
                        var m = mc.Siblings(i).Member;
                        if (m == null) continue;
                        try
                        {
                            A.AddMember(m);
                            object vv;
                            if (FGrid.FEngine.GetCellValue(A, out vv))
                                if (cmp.CompareTo(vv) < 0)
                                    Rank++;
                        }
                        catch
                        {
                            ;
                        }
                    }
                    OutValue = Rank;
                    return Rank.ToString();
                case MeasureShowModeType.smSpecifiedByEvent:
                    if (!Grid.EventShowMeasureAssigned)
                        throw new Exception(string.Format(RadarUtils.GetResStr("rssmError"), DisplayName,
                            mode.Caption));

                    var E = new ShowMeasureArgs(Value, mode, Cell);
                    E.fEvaluator = new Evaluator(Grid, Cell.Address.Clone());
                    Grid.EventShowMeasure(E);
                    OutValue = E.ReturnData;
                    return E.fValue;
                default:
                    return "";
            }
        }

        internal void Close()
        {
            Visible = false;
            fFilter = null;
            FGrid = null;
            FShowModes = null;
            UniqueName = null;
            FCubeMeasure = null;
        }


        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgMeasure);

            StreamUtils.WriteTag(writer, Tags.tgMeasure_UniqueName);
            StreamUtils.WriteString(writer, FUniqueName);

            if (FDefaultFormat_ != "Currency" || FDefaultFormatChanged)
            {
                StreamUtils.WriteTag(writer, Tags.tgMeasure_DefaultFormat);
                StreamUtils.WriteString(writer, FDefaultFormat_);
            }

            if (fFilter != null)
                StreamUtils.WriteStreamedObject(writer, fFilter, Tags.tgMeasure_Filter);

            if (!string.IsNullOrEmpty(FDescription))
            {
                StreamUtils.WriteTag(writer, Tags.tgMeasure_Description);
                StreamUtils.WriteString(writer, FDescription);
            }

            if (!string.IsNullOrEmpty(FDisplayFolder))
            {
                StreamUtils.WriteTag(writer, Tags.tgMeasure_DisplayFolder);
                StreamUtils.WriteString(writer, FDisplayFolder);
            }

            if (!string.IsNullOrEmpty(FDisplayName))
            {
                StreamUtils.WriteTag(writer, Tags.tgMeasure_DisplayName);
                StreamUtils.WriteString(writer, FDisplayName);
            }

            if (!string.IsNullOrEmpty(FExpression))
            {
                StreamUtils.WriteTag(writer, Tags.tgMeasure_Expression);
                StreamUtils.WriteString(writer, FExpression);
            }

            if (FFunction != OlapFunction.stSum)
            {
                StreamUtils.WriteTag(writer, Tags.tgMeasure_Function);
                StreamUtils.WriteInt32(writer, (int) FFunction);
            }

            StreamUtils.WriteStreamedObject(writer, FShowModes, Tags.tgMeasure_ShowModes);

            if (FVisible)
                StreamUtils.WriteTag(writer, Tags.tgMeasure_Visible);

            if (!VisibleInTree)
                StreamUtils.WriteTag(writer, Tags.tgMeasure_NotVisibleInTree);

            StreamUtils.WriteTag(writer, Tags.tgMeasure_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgMeasure);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgMeasure_DefaultFormat:
                        FDefaultFormat_ = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgMeasure_Description:
                        FDescription = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgMeasure_DisplayFolder:
                        FDisplayFolder = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgMeasure_DisplayName:
                        FDisplayName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgMeasure_Expression:
                        FExpression = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgMeasure_Function:
                        FFunction = (OlapFunction) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgMeasure_ID:
                        StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgMeasure_ShowModes:
                        StreamUtils.ReadStreamedObject(reader, FShowModes);
                        break;
                    case Tags.tgMeasure_UniqueName:
                        FUniqueName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgMeasure_Visible:
                        FVisible = true;
                        break;
                    case Tags.tgMeasure_NotVisibleInTree:
                        VisibleInTree = false;
                        break;
                    case Tags.tgMeasure_Filter:
                        fFilter = new MeasureFilter(this);
                        StreamUtils.ReadStreamedObject(reader, fFilter);
                        break;
                    case Tags.tgMeasure_EOT:
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