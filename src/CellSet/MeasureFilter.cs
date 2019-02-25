using System.IO;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.CellSet
{
    /// <summary>
    ///     The class describes the context filter settings for the measure.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Context measure filters can be applied to each measure separately. End users
    ///         can access these filters through the context menus of the Grid cells, or by
    ///         clicking the "Filer" icon on the approptiate measure panel.
    ///     </para>
    ///     <para>
    ///         The same can be done programmatically by creating an instance of the
    ///         MeasureFilter class and then setting the Filter property of the measure to its new
    ///         instance.
    ///     </para>
    ///     <para>
    ///         Please keep in mind that measure context filters can be applied to the
    ///         visible measures only.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     For example, let's apply the measure context filter to the "Sales Amount" measure
    ///     so that the Grid shows only values exceed 1000:
    ///     <code lang="CS" title="[New Example]">
    /// // this measure must be visible in the Grid.
    /// Measure m = tolapGrid1.Measures.Find("[Measures].[Sales Amount]");
    /// // create an instance of the MeasureFilter class
    /// MeasureFilter f = new MeasureFilter(m, OlapFilterCondition.fcGreater, 
    ///        "1000", null);
    /// // apply the filter to the measure
    /// m.Filter = f;
    /// </code>
    ///     <code lang="VB" title="[New Example]">
    /// ' this measure must be visible in the Grid.
    /// Dim m As Measure = tolapGrid1.Measures.Find("[Measures].[Sales Amount]")
    /// ' create the instance of the MeasureFilter class
    /// Dim f As New MeasureFilter(m, OlapFilterCondition.fcGreater, "1000", Nothing)
    /// ' apply the filter to the measure
    /// m.Filter = f
    /// </code>
    /// </example>
    public class MeasureFilter : IStreamedObject
    {
        internal OlapFilterCondition fCondition;
        private string fFirstValue = "";
        private MeasureFilterRestriction fRestrictsTo = MeasureFilterRestriction.mfrAggregatedValues;
        private string fSecondFalue = "";

        /// <summary>Creates a new instance of the MeasureFilter type.</summary>
        /// <param name="AMeasure">The measure the filter will be applied to.</param>
        /// <param name="condition">The condition used in this filter (equal, less, between, etc.)</param>
        /// <param name="firstValue">The string representation of the first value being used in this filter.</param>
        /// <param name="secondValue">The string representation of the second value used in some filter conditions.</param>
        public MeasureFilter(Measure AMeasure, OlapFilterCondition condition,
            string firstValue, string secondValue)
        {
            //fBitmap = ABitmap;
            Measure = AMeasure;
            fCondition = condition;
            fFirstValue = firstValue;
            fSecondFalue = secondValue;
        }

        internal MeasureFilter(Measure measure)
        {
            Measure = measure;
        }

        /// <summary>The condition used in this filter (equal to, less than, between etc.)</summary>
        public OlapFilterCondition FilterCondition => fCondition;

        /// <summary>The string representation of the first value used in this filter.</summary>
        public string FirstValue
        {
            get => fFirstValue;
            set
            {
                if (fFirstValue != value)
                {
                    fFirstValue = value;
                    ApplyChange();
                }
            }
        }

        internal double FirstValueAsDouble
        {
            get
            {
                if (string.IsNullOrEmpty(FirstValue))
                    return double.NaN;

                double res;
                if (double.TryParse(FirstValue, out res))
                    return res;
                return double.NaN;
            }
        }

        internal double SecondValueAsDouble
        {
            get
            {
                if (string.IsNullOrEmpty(SecondValue))
                    return double.NaN;

                double res;
                if (double.TryParse(SecondValue, out res))
                    return res;
                return double.NaN;
            }
        }

        /// <summary>
        ///     The string representation of the second value used in some filter conditions
        ///     ("Between" and "Not between" filters).
        /// </summary>
        public string SecondValue
        {
            get => fSecondFalue;
            set
            {
                if (fSecondFalue != value)
                {
                    fSecondFalue = value;
                    ApplyChange();
                }
            }
        }

        /// <summary>
        ///     The measure the filter is applied to.
        /// </summary>
        public Measure Measure { get; }

        /// <summary>Detailed description of the filter.</summary>
        public string Description
        {
            get
            {
                var s = DoSimpleDescription();
                if (RestrictsTo == MeasureFilterRestriction.mfrFactTable)

                    s = RadarUtils.GetResStr("rsFactTableValues") + " " + s;

                return s;
            }
        }

        /// <summary>
        ///     In the Desktop version defines the type of restriction introduced by the filter
        ///     (ether on fact table rows or on aggregated values).
        /// </summary>
        public MeasureFilterRestriction RestrictsTo
        {
            get => fRestrictsTo;
            set
            {
                if (fRestrictsTo == value) return;
                fRestrictsTo = value;
                ApplyChange();
            }
        }

        public override int GetHashCode()
        {
            return FilterCondition.GetHashCode() ^ FirstValue.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj.GetType() != typeof(MeasureFilter))
                return false;

            var mf = obj as MeasureFilter;

            if (mf.Description != Description)
                return false;

            if (mf.FilterCondition != FilterCondition)
                return false;

            if (mf.FirstValue != FirstValue)
                return false;

            if (mf.Measure != Measure)
                return false;

            if (mf.RestrictsTo != RestrictsTo)
                return false;

            if (mf.SecondValue != SecondValue)
                return false;

            return true;
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

        private void ApplyChange()
        {
            if (Measure.Filter == this)
            {
                Measure.Grid.Engine.ClearMeasureData(Measure);
                if (!Measure.Grid.IsUpdating)
                    Measure.Grid.CellSet.Rebuild();
            }
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgMeasureFilter);

            StreamUtils.WriteTag(writer, Tags.tgMeasureFilter_Condition);
            StreamUtils.WriteInt32(writer, (int) fCondition);

            if (!string.IsNullOrEmpty(fFirstValue))
            {
                StreamUtils.WriteTag(writer, Tags.tgMeasureFilter_FirstValue);
                StreamUtils.WriteString(writer, fFirstValue);
            }

            if (!string.IsNullOrEmpty(fSecondFalue))
            {
                StreamUtils.WriteTag(writer, Tags.tgMeasureFilter_SecondValue);
                StreamUtils.WriteString(writer, fSecondFalue);
            }

            if (fRestrictsTo != MeasureFilterRestriction.mfrAggregatedValues)
            {
                StreamUtils.WriteTag(writer, Tags.tgMeasureFilter_RestrictsTo);
                StreamUtils.WriteInt32(writer, (int) fRestrictsTo);
            }

            StreamUtils.WriteTag(writer, Tags.tgMeasureFilter_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgMeasureFilter);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgMeasureFilter_BitmapURL:
                        var dummy = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgMeasureFilter_Condition:
                        fCondition = (OlapFilterCondition) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgMeasureFilter_FirstValue:
                        fFirstValue = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgMeasureFilter_SecondValue:
                        fSecondFalue = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgMeasureFilter_RestrictsTo:
                        fRestrictsTo = (MeasureFilterRestriction) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgMeasureFilter_EOT:
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