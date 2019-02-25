using System.Collections.Generic;
using System.ComponentModel;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class RCellset
    {
        public RCell[] Cells;
        public int ColCount;

        [DefaultValue(null)] public ClientChartAxis ColorAxis = null;

        [DefaultValue(null)] public ClientMember[] ColorChartMembers = null;

        [DefaultValue(null)] public List<List<ClientMember>> ColumnChartMembers = null;

        public int FixedCols;
        public int FixedRows;

        [DefaultValue(null)] public ClientChartAxis ForeColorAxis = null;

        [DefaultValue(null)] public ClientMember[] ForeColorGridMembers = null;

        [DefaultValue(null)] public string[] Members2DisplayNames = null;

        [DefaultValue(null)] public string[] Members2UniqueNames = null;

        [DefaultValue(null)] public List<List<ClientMember>> RowChartMembers = null;

        public int RowCount;

        [DefaultValue(null)] public ClientChartAxis ShapeAxis = null;

        [DefaultValue(null)] public ClientMember[] ShapeChartMembers = null;

        [DefaultValue(null)] public ClientChartAxis SizeAxis = null;

        [DefaultValue(null)] public ClientMember[] SizeChartMembers = null;

        [DefaultValue(true)] public bool TreeLikeBehavior = true;

        [DefaultValue(null)] public ClientChartAxis XAxis = null;

        [DefaultValue(null)] public ClientChartAxis[] YAxes = null;

        public RCellset()
        {
        }

        public RCellset(CellSet.CellSet cs, int maxTextLenght)
        {
            if (cs == null)
            {
                RowCount = 0;
                ColCount = 0;
                FixedRows = 0;
                FixedCols = 0;
                Cells = new RCell[0];
                return;
            }
            RowCount = cs.PagedRowCount;
            ColCount = cs.PagedColumnCount;
            FixedRows = cs.FixedRows;
            FixedCols = cs.FixedColumns;
            TreeLikeBehavior = cs.Grid.HierarchiesDisplayMode == HierarchiesDisplayMode.TreeLike;

            var c1 = new HashSet<string>();
            //impos = new Dictionary<string, ImagePosition>();
            //images = new Dictionary<string, string>();

            var cc = new List<RCell>();
            for (var i = 0; i < cs.RowCount; i++)
            {
                if (!cs.Grid.CellSet.IsRowVisible(i)) continue;
                for (var j = 0; j < cs.ColumnCount; j++)
                {
                    if (!cs.Grid.CellSet.IsColumnVisible(j)) continue;
                    var c = cs[j, i];
                    if (c.ColSpan < 1 || c.RowSpan < 1)
                        continue;
                    if (c.ColSpan > 1 || c.RowSpan > 1)
                    {
                        var s = string.Format("{0}|{1}", c.StartColumn, c.StartRow);
                        if (!c1.Add(s)) continue;
                    }

#if SL
                    DrawCellEventArgs e = ((RiaOLAPControl)cs.Grid).OnDrawCell(c);

                    RCell cell = new RCell(c, e);
                    if (e!=null && !string.IsNullOrEmpty(e.ImageUri))                        
                    {
                        cell.BackImageUrl = e.ImageUri;
                        cell.ImagePosition = e.ImagePosition;
                       

                        //impos.Add(index.ToString(), e.ImagePosition);
                        //images.Add(index.ToString(), e.ImageUri);
                    }
#else
                    var cell = new RCell(c);
#endif
                    cell.MaxTextLength = maxTextLenght;
                    cc.Add(cell);
                }
            }
            Cells = cc.ToArray();

            cs.Grid.InitClientCellset(this);
        }

        //internal Dictionary<string, ImagePosition> impos = new Dictionary<string, ImagePosition>();
        //internal Dictionary<string, string> images = new Dictionary<string, string>();
    }
}