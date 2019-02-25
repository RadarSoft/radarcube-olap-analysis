using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.Controls.Chart
{
    /// <summary>
    ///     Represents the Chart's axis.
    /// </summary>
    public class ChartAxis
    {
        private CellSet.CellSet fCellset;
        internal ChartAxisDescriptor fDescriptor;
        internal double fMax = double.MinValue;
        internal double fMin = double.MaxValue;

        internal ChartAxis(CellSet.CellSet cellset, ChartAxisFormat format, ChartAxisDescriptor descriptor)
        {
            fCellset = cellset;
            Format = format;
            fDescriptor = descriptor;
            descriptor.SetAxis(this);
        }

        /// <summary>
        ///     Returnes the format of the axis (discrete or continuous).
        /// </summary>
        /// <remarks>
        ///     Axis type is defined by its' descriptor: if the descriptor is a hierarchy level, the axis
        ///     is discrete, if it's a measure, the axis will be continuous.
        /// </remarks>
        public ChartAxisFormat Format { get; }

        /// <summary>
        ///     For the continuous axis returns the value of its' maximal element.
        /// </summary>
        public double Max => fMax;

        /// <summary>
        ///     For the continuous axis returns the value of its' minimal element.
        /// </summary>
        public double Min => fMin;

        /// <summary>
        ///     References to the axis descriptor.
        /// </summary>
        public ChartAxisDescriptor Descriptor => fDescriptor;
    }
}