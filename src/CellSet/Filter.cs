using System.IO;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Tools;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.CellSet
{
    /// <summary>Describes the context filter settings for the hierarchy level.</summary>
    /// <remarks>
    ///     <para>
    ///         Context filters can be applied to each hierarchy level separately. End users
    ///         can access these filters through the context menus of Grid cells, or through the
    ///         "Filer" menu of the Hierarchy Editor.
    ///     </para>
    ///     <para>
    ///         The same can be done programmatically by creating an instance of the Filter
    ///         class and then setting the Filter property of the hierarchy level to its new
    ///         instance.
    ///     </para>
    ///     <para>
    ///         Please keep in mind that context filters can be applied only to hierarchies
    ///         in one of the three active areas (rows, columns or pages).
    ///     </para>
    ///     <para></para>
    /// </remarks>
    /// <example>
    ///     <code lang="CS" title="[New Example]">
    /// // this hierarchy must be placed into any of the three active areas (row, columns or pages).
    /// Hierarchy h = tolapGrid1.Dimensions.FindHierarchy("[Date].[Fiscal Year]");
    /// // independent-cultute string representation of the "1-jan-current year" date.
    /// string date = (new DateTime(DateTime.Now.Year, 1, 1)).ToString(CultureInfo.InvariantCulture.DateTimeFormat);
    /// // create an instance of the Filter class
    /// Filter f = new Filter(h.Levels[0], OlapFilterType.ftOnDate, null, OlapFilterCondition.fcNotLess, 
    ///        date, null);
    /// // apply the filter to the level
    /// h.Levels[0].Filter = f;
    /// </code>
    ///     <code lang="VB" title="[New Example]">
    /// ' this hierarchy must be placed into any of the three active areas (row, columns or pages).
    /// Dim h As Hierarchy = tolapGrid1.Dimensions.FindHierarchy("[Date].[Fiscal Year]")
    /// ' independent-cultute string representation of the "1-jan-current year" date.
    /// Dim Date As String = New DateTime(DateTime.Now.Year, 1, 1).ToString(CultureInfo.InvariantCulture.DateTimeFormat)
    /// ' create an instance of the Filter class
    /// Dim f As New Filter(h.Levels.Item(0), OlapFilterType.ftOnDate, Nothing, OlapFilterCondition.fcNotLess, 
    ///     [Date], Nothing)
    /// ' apply the filter to the level
    /// h.Levels.Item(0).Filter = f
    /// </code>
    /// </example>
    public class Filter : IStreamedObject
    {
        internal OlapFilterCondition fCondition;
        private string fFirstValue = "";
        internal string fLevelName;
        internal string fMeasureName;
        private string fSecondFalue = "";

        //private Image fBitmap;

        /// <summary>
        ///     An icon which will be displayed in the context menu.
        /// </summary>
        /// <summary>Creates a new instance of the Filter type.</summary>
        /// <param name="ALevel">The hierarchy level the filter will be applied to.</param>
        /// <param name="filterType">Describes the filter type, i.e. how this filter is applied to the hierarchy members.</param>
        /// <param name="applyTo">The measure this filter is applied to, or null if the filter is not of "value" type.</param>
        /// <param name="condition">The condition used in this filter (equal, less, between, etc.)</param>
        /// <param name="firstValue">The string representation of the first value being used in this filter.</param>
        /// <param name="secondValue">The string representation of the auxiliary (or second) value used in some filter conditions.</param>
        public Filter(Level ALevel, OlapFilterType filterType, Measure applyTo, OlapFilterCondition condition,
            string firstValue, string secondValue)
        {
            Level = ALevel;
            FilterType = filterType;
            if (applyTo != null)
                fMeasureName = applyTo.UniqueName;
            fCondition = condition;
            fFirstValue = firstValue;
            fSecondFalue = secondValue;
        }

        internal Filter(Level level)
        {
            Level = level;
        }

        /// <summary>
        ///     Describes the filter type, i.e. how this filter is applied to the hierarchy members
        ///     (to member captions, dates or measure values).
        /// </summary>
        public OlapFilterType FilterType { get; private set; }

        /// <summary>
        ///     For the MSAS versions only. The unique name of the level in a "Parent-Child"
        ///     hierarchy, for a hierarchy of a different type equals null.
        /// </summary>
        public string MDXLevelName
        {
            get => fLevelName ?? Level.UniqueName;
            set
            {
                if (string.IsNullOrEmpty(value))
                    fLevelName = null;
                else
                    fLevelName = value == Level.UniqueName ? null : value;
            }
        }

        /// <summary>
        ///     The measure the filter is applied to. Used only for context filtering by
        ///     value.
        /// </summary>
        public Measure AppliesTo
        {
            get
            {
                if (string.IsNullOrEmpty(fMeasureName)) return null;
                return Level.Grid.Measures.Find(fMeasureName);
            }
            set
            {
                if (AppliesTo != value)
                {
                    fMeasureName = value == null ? null : value.UniqueName;
                    if (Level.Filter == this)
                        Level.FHierarchy.UpdateFilterState(true);
                }
            }
        }

        /// <summary>The condition used in the filter (equal to, less than, between etc.)</summary>
        public OlapFilterCondition FilterCondition => fCondition;

        /// <summary>The string representation of the first value used in this filter.</summary>
        /// <remarks>For the Date-filters, the data should be assigned in the following format: MM/DD/YYYY.</remarks>
        public string FirstValue
        {
            get => fFirstValue;
            set
            {
                if (fFirstValue != value)
                {
                    fFirstValue = value;
                    if (Level.Filter == this)
                        Level.FHierarchy.UpdateFilterState(true);
                }
            }
        }

        /// <summary>
        ///     The string representation of an auxiliary (or second) value used in some filter
        ///     conditions ("Between", "Not between" and "Top 10" filters).
        /// </summary>
        /// <remarks>For the Date-filters, the data should be assigned in the following format: MM/DD/YYYY.</remarks>
        public string SecondValue
        {
            get => fSecondFalue;
            set
            {
                if (fSecondFalue != value)
                {
                    fSecondFalue = value;
                    if (Level.Filter == this)
                        Level.FHierarchy.UpdateFilterState(true);
                }
            }
        }

        /// <summary>
        ///     The hierarchy level the filter is applied to.
        /// </summary>
        public Level Level { get; }

        /// <summary>Detailed description of the filter.</summary>
        public string Description
        {
            get
            {
                if (FilterType != OlapFilterType.ftOnValue)
                {
                    var _Caption = FilterType == OlapFilterType.ftOnDate ? "rsDate" : "exprt_Caption";

                    return RadarUtils.GetResStr(_Caption) + " " + DoSimpleDescription();
                }
                if (fCondition != OlapFilterCondition.fcFirstTen)
                    return RadarUtils.GetResStr("rsMeasure") + " \"" + AppliesTo.DisplayName + "\" "
                           + DoSimpleDescription();
                var _Top = fSecondFalue.StartsWith("[1].") ? "rsBottom" : "rsTop";

                var s = RadarUtils.GetResStr(_Top) + " " + fFirstValue + " ";

                var _Items = "rsItems";
                if (!string.IsNullOrEmpty(fSecondFalue))
                    if (fSecondFalue.EndsWith(".[1]"))
                        _Items = "rsPercentFromItemsCount";
                    else if (fSecondFalue.EndsWith(".[2]"))
                        _Items = "rsSumma";

                s += RadarUtils.GetResStr(_Items) + " " + RadarUtils.GetResStr("rsIn") +
                     " \"" + AppliesTo.DisplayName + "\" ";
                return s;
            }
        }

        private string DoSimpleDescription()
        {
            var s = RadarUtils.GetResStr("rs" + fCondition) + " \"" +
                    fFirstValue + "\"";
            if (fCondition == OlapFilterCondition.fcBetween || fCondition == OlapFilterCondition.fcNotBetween)

                s += " " + RadarUtils.GetResStr("rsAnd") + " \"" +
                     fSecondFalue + "\"";
            return s;
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgFilter);

            StreamUtils.WriteTag(writer, Tags.tgFilter_Type);
            StreamUtils.WriteInt32(writer, (int) FilterType);

            StreamUtils.WriteTag(writer, Tags.tgFilter_Condition);
            StreamUtils.WriteInt32(writer, (int) fCondition);

            if (!string.IsNullOrEmpty(fMeasureName))
            {
                StreamUtils.WriteTag(writer, Tags.tgFilter_MeasureName);
                StreamUtils.WriteString(writer, fMeasureName);
            }

            if (!string.IsNullOrEmpty(fFirstValue))
            {
                StreamUtils.WriteTag(writer, Tags.tgFilter_FirstValue);
                StreamUtils.WriteString(writer, fFirstValue);
            }

            if (!string.IsNullOrEmpty(fSecondFalue))
            {
                StreamUtils.WriteTag(writer, Tags.tgFilter_SecondValue);
                StreamUtils.WriteString(writer, fSecondFalue);
            }

            if (!string.IsNullOrEmpty(fLevelName))
            {
                StreamUtils.WriteTag(writer, Tags.tgFilter_LevelName);
                StreamUtils.WriteString(writer, fLevelName);
            }

            StreamUtils.WriteTag(writer, Tags.tgFilter_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgFilter);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgFilter_BitmapURL:
                        var dummy = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgFilter_Name:
                        dummy = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgFilter_Type:
                        FilterType = (OlapFilterType) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgFilter_Condition:
                        fCondition = (OlapFilterCondition) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgFilter_MeasureName:
                        fMeasureName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgFilter_FirstValue:
                        fFirstValue = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgFilter_SecondValue:
                        fSecondFalue = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgFilter_LevelName:
                        fLevelName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgFilter_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        #endregion
    }
}