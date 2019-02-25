using System.Collections.Generic;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    public class SerializedMeasureGroup
    {
        public string[] Measures;

        internal void Init(MeasureGroup mg)
        {
            Measures = new string[mg.Count];
            for (var i = 0; i < mg.Count; i++)
                Measures[i] = mg[i].UniqueName;
        }

        internal void Restore(OlapControl grid, List<Measure> assignedMeasures)
        {
            MeasureGroup mg = null;
            foreach (var s in Measures)
            {
                var m = grid.Measures.Find(s);
                if (m != null)
                {
                    mg = grid.Pivoting(m, LayoutArea.laRow, mg, null);
                    assignedMeasures.Remove(m);
                }
            }
        }
    }
}