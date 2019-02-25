using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class ClientLayout
    {
        [DefaultValue("")] public string ColorAxisItem = "";

        public string[] ColumnArea;

        [DefaultValue(null)] public string[] ColumnLevels;

        public string[] DetailsArea;
        public ClientDimension[] Dimensions;
        public bool ExpandDimensionsNode;

        [DefaultValue(false)] public bool ExpandMeasuresNode;

        [DefaultValue("")] public string ForeColorAxisItem = "";

        [DefaultValue(true)] public bool HideDimensionNameIfPossible = true;

        public ClientMeasure[] Measures;
        public string[] PageArea;
        public string[] RowArea;

        [DefaultValue(null)] public string[] RowLevels;

        [DefaultValue("")] public string ShapeAxisItem = "";

        [DefaultValue("")] public string SizeAxisItem = "";

        [DefaultValue("")] public string XAxisMeasure = "";

        [DefaultValue(null)] public List<List<string>> YAxisMeasures;

        public ClientLayout()
        {
        }

        internal ClientLayout(AxesLayout layout)
        {
            PageArea = layout.fPageAxis.Select(e => e.UniqueName).ToArray();
            ColumnArea = layout.fColumnAxis.Select(e => e.UniqueName).ToArray();
            RowArea = layout.fRowAxis.Select(e => e.UniqueName).ToArray();
            DetailsArea = layout.fDetailsAxis.Select(e => e.UniqueName).ToArray();
            HideDimensionNameIfPossible = layout.Grid.HideDimensionNameIfPossible;
            ExpandMeasuresNode = layout.Grid.ExpandMeasuresNode;
            ExpandDimensionsNode = layout.Grid.ExpandDimensionsNode;

            var grid = layout.Grid;

            Measures = grid.Measures.Count > 0
                ? grid.Measures.Level.Members.Select(
                    item => new ClientMeasure(grid.Measures.Find(item.UniqueName))).ToArray()
                : new ClientMeasure[0];

            Dimensions = grid.Dimensions.Where(
                item => item.Visible).Select(
                item => new ClientDimension(item, grid)).ToArray();

            if (layout.fRowLevels.Count > 0)
                RowLevels = layout.fRowLevels.Select(item => item.UniqueName).ToArray();

            if (layout.fColumnLevels.Count > 0)
                ColumnLevels = layout.fColumnLevels.Select(item => item.UniqueName).ToArray();

            if (layout.fXAxisMeasure != null)
                XAxisMeasure = layout.fXAxisMeasure.UniqueName;

            if (layout.fColorAxisItem != null)
                ColorAxisItem = layout.fColorAxisItem.UniqueName;

            if (layout.fColorForeAxisItem != null)
                ForeColorAxisItem = layout.fColorForeAxisItem.UniqueName;

            if (layout.fSizeAxisItem != null)
                SizeAxisItem = layout.fSizeAxisItem.UniqueName;

            if (layout.fShapeAxisItem != null)
                ShapeAxisItem = layout.fShapeAxisItem.UniqueName;

            if (layout.fYAxisMeasures.Count > 0)
                YAxisMeasures = layout.fYAxisMeasures.Select(
                    item => item.Select(
                        item2 => item2.UniqueName).ToList()).ToList();
        }
    }
}