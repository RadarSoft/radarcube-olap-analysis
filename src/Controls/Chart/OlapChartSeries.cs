using System.Collections.Generic;
using System.Text;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Controls.Chart
{
    /// <summary>
    ///     Returnes the series displayed in the Chart. Each Chart can contain one or more series.
    /// </summary>
    public class OlapChartSeries : IDescriptionable
    {
        internal Member fColorMember;
        internal List<ChartCellDetails> fDetails = new List<ChartCellDetails>();
        internal Measure fMeasure;
        internal Member fShapeMember;
        internal Member fSizeMember;

        internal OlapChartSeries(Member color, Member size, Member shape, Measure measure, ChartArea area)
        {
            fColorMember = color;
            fSizeMember = size;
            fShapeMember = shape;
            fMeasure = measure;

            var sb = new StringBuilder();
            if (fMeasure != null)
                sb.Append(fMeasure.UniqueName);
            sb.Append("|");
            if (fColorMember != null)
                sb.Append(fColorMember.UniqueName);
            sb.Append("|");
            if (fSizeMember != null)
                sb.Append(fSizeMember.UniqueName);
            sb.Append("|");
            if (fShapeMember != null)
                sb.Append(fShapeMember.UniqueName);

            UniqueName = sb.ToString();

            if (fMeasure != null)
                UniqueNameColor = fMeasure.UniqueName;
            else
                UniqueNameColor = string.Empty;
            Area = area;
        }

        /// <summary>
        ///     The list of series' points.
        /// </summary>
        public List<ChartCellDetails> Data => fDetails;

        /// <summary>
        ///     References to the Chart with the specified series.
        /// </summary>
        public ChartArea Area { get; }

        /// <summary>
        ///     References to the Y axis measure (if any), common for all
        ///     the elements of the specified series.
        /// </summary>
        public Measure YMeasure => fMeasure;

        /// <summary>
        ///     Show only Measures unique name
        /// </summary>
        internal string UniqueName { get; }

        /// <summary>
        ///     Show only Measures unique name (Color properties ONLY !)
        /// </summary>
        internal string UniqueNameColor { get; }

        string IDescriptionable.DisplayName => string.Empty;

        string IDescriptionable.Description => string.Empty;

        string IDescriptionable.UniqueName => UniqueName;
    }
}