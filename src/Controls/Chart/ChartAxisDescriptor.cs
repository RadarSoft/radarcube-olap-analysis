using System;
using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.Controls.Chart
{
    /// <summary>Describes one of the chart's axis.</summary>
    public class ChartAxisDescriptor : IDescriptionable
    {
        internal IDescriptionable fDescriptor = null;
        private readonly string fUniqueName = Guid.NewGuid().ToString();

        /// <summary>
        ///     <para>
        ///         References to an object (a measure or an hierarchy level) that describes the
        ///         specified axis
        ///     </para>
        /// </summary>
        public IDescriptionable DescriptorObject => fDescriptor;

        /// <summary>
        ///     References to the described axis.
        /// </summary>
        public ChartAxis Axis { get; private set; }

        internal void SetAxis(ChartAxis axis)
        {
            Axis = axis;
        }

        #region IDescriptionable Members

        /// <summary>
        ///     Caption of the specified axis. Is the same as the Caption of the object that
        ///     describes the axis.
        /// </summary>
        public string DisplayName => fDescriptor != null ? fDescriptor.DisplayName : string.Empty;

        /// <summary>
        ///     Description of the specified axis. Is the same as the Description of the object
        ///     that describes the axis.
        /// </summary>
        public string Description => fDescriptor != null ? fDescriptor.Description : string.Empty;

        /// <summary>
        ///     The unique name of the specified axis. Is the same as the unique name of the
        ///     object that describes the axis.
        /// </summary>
        public string UniqueName => fDescriptor != null ? fDescriptor.UniqueName : fUniqueName;

        /// <summary>
        ///     <para>References to an object, representing an hierarchy axis.</para>
        /// </summary>
        public ChartAxis AxisData => Axis;

        #endregion
    }
}