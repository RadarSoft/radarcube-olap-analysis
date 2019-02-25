using System.Collections.Generic;
using System.ComponentModel;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class ClientMeasure
    {
        [DefaultValue("")] public string Description = "";

        public string DisplayName;

        [DefaultValue(null)] public ClientMeasureFilter Filter;

        [DefaultValue("")] public string Group = "";

        [DefaultValue(false)] public bool IsCalculated;

        [DefaultValue(false)] public bool IsKPI;

        [DefaultValue(false)] public bool IsVisible;

        [DefaultValue(null)] public ClientMeasureMode[] Modes;

        public string UniqueName;

        [DefaultValue(true)] public bool VisibleInTree = true;

        public ClientMeasure()
        {
        }

        internal ClientMeasure(Measure m)
        {
            DisplayName = m.DisplayName;
            UniqueName = m.UniqueName;
            VisibleInTree = m.VisibleInTree;
            IsKPI = m.IsKPI;
            Group = m.DisplayFolder;
            Description = m.Description;
            if (m.Filter != null)
                Filter = new ClientMeasureFilter(m.Filter);
            IsCalculated = m.AggregateFunction == OlapFunction.stCalculated;
            IsVisible = m.Visible || m.VisibleInChart;
            if (IsVisible)
            {
                var l = new List<ClientMeasureMode>();
                for (var i = 0; i < m.ShowModes.Count; i++)
                {
                    var sm = m.ShowModes[i];
                    if (sm.Visible)
                        l.Add(new ClientMeasureMode(sm, i == 0));
                }
                Modes = l.ToArray();
            }
        }
    }
}