using System;
using System.Collections.Generic;
using System.Linq;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>
    ///     Represents a group of measures combined in one chart.
    /// </summary>
    public class MeasureGroup : List<Measure>, IChartable
    {
        public MeasureGroup()
        {
            DebugLogging.WriteLine("MeasureGroup.ctor");
        }

        string IDescriptionable.DisplayName
        {
            get { return RadarUtils.Join(", ", this.Select(x => x.DisplayName).ToList()); }
        }

        string IDescriptionable.Description
        {
            get
            {
                return RadarUtils.Join(Environment.NewLine, this
                    .Select(x => x.Description)
                    .ToList());
            }
        }

        string IDescriptionable.UniqueName
        {
            get
            {
                return RadarUtils.Join('|', this
                    .Select(x => x.UniqueName)
                    .ToList());
            }
        }

        internal bool IsEqualByContent(MeasureGroup tMeasureGroup)
        {
            return ((IDescriptionable) this).UniqueName == ((IDescriptionable) tMeasureGroup).UniqueName;
        }
    }
}