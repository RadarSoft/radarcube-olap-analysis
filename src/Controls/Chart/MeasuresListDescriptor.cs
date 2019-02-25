using System.Collections.Generic;
using System.Text;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Controls.Chart
{
    /// <summary>The Y-axis descriptor that contains several measures.</summary>
    public class MeasuresListDescriptor : List<Measure>, IDescriptionable
    {
        internal MeasuresListDescriptor(IEnumerable<Measure> measures)
            : base(measures)
        {
        }

        internal Dictionary<string, Measure> DisplayNameDetailed
        {
            get
            {
                var result = new Dictionary<string, Measure>(Count);
                foreach (var m in this)
                {
                    var s = m.ShowModes[0].Visible
                        ? m.DisplayName
                        : m.DisplayName + ": " + m.ShowModes.FirstVisibleMode.Caption;
                    result.Add(s, m);
                }
                return result;
            }
        }

        #region IDescriptionable Members

        /// <exclude />
        public string DisplayName
        {
            get
            {
                if (Count == 0) return string.Empty;
                var sb = new StringBuilder();
                foreach (var m in this)
                    if (m.ShowModes[0].Visible)
                        sb.AppendLine(m.DisplayName);
                    else
                        sb.AppendLine(m.DisplayName + ": " + m.ShowModes.FirstVisibleMode.Caption);
                return sb.ToString();
            }
        }

        /// <exclude />
        public string Description
        {
            get
            {
                if (Count == 0) return string.Empty;
                var sb = new StringBuilder();
                foreach (var m in this)
                    if (!string.IsNullOrEmpty(m.Description))
                        sb.AppendLine(m.DisplayName + ": " + m.Description);
                return sb.ToString();
            }
        }

        /// <exclude />
        public string UniqueName
        {
            get
            {
                if (Count == 0) return string.Empty;
                var sb = new StringBuilder();
                foreach (var m in this)
                    sb.AppendLine(m.UniqueName + "|" + m.ShowModes.FirstVisibleMode.Caption);
                return sb.ToString();
            }
        }

        #endregion
    }
}