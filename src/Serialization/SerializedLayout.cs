using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    public class SerializedLayout
    {
        [DefaultValue("")] public string ColorAxis = "";

        [DefaultValue("")] public string ColorForeAxis = "";

        public string[] ColumnHierarchies;

        //public string[] ShapeHierarchies;
        public string[] DetailsHierarchies;

        public string[] Drills;

        [DefaultValue(true)] public bool HideMeasureIfPossible;

        [DefaultValue(true)] public bool HideMeasureModesIfPossible;

        [DefaultValue(LayoutArea.laColumn)] public LayoutArea MeasureLayout;

        [DefaultValue(MeasurePosition.mpLast)] public MeasurePosition MeasurePosition;

        public SerializedMeasure[] Measures;
        public PossibleDrillActions[] OpenendActions;
        public string[] OpenendNodes;
        public string[] PageHierarchies;
        public string[] RowHierarchies;

        [DefaultValue("")] public string ShapeAxis = "";

        [DefaultValue("")] public string SizeAxis = "";

        [DefaultValue(-1)] public int ValueSortedColumn = -1;

        [DefaultValue(ValueSortingDirection.sdAscending)]
        public ValueSortingDirection ValueSortingDirection = ValueSortingDirection.sdAscending;

        [DefaultValue("")] public string XMeasure = "";

        public SerializedMeasureGroup[] YMeasures;

        public SerializedLayout()
        {
            MeasureLayout = LayoutArea.laColumn;
            MeasurePosition = MeasurePosition.mpLast;
            HideMeasureModesIfPossible = true;
            HideMeasureIfPossible = true;
        }

        [DefaultValue(null)]
        public SerializedGradient SelectedBackColor { get; set; }

        [DefaultValue(null)]
        public SerializedGradient SelectedForeColor { get; set; }

        [DefaultValue(null)]
        public SerializedColoredMember[] ColoredMembersBack { get; set; }

        [DefaultValue(null)]
        public SerializedColoredMember[] ColoredMembersFore { get; set; }

        [XmlAttribute]
        [DefaultValue(null)]
        public string SelectedColorBackUniqueName { get; set; }

        [XmlAttribute]
        [DefaultValue(null)]
        public string SelectedColorForeUniqueName { get; set; }

        internal void Init(OlapControl grid)
        {
            if (!grid.Active)
                throw new Exception(RadarUtils.GetResStr("rsCantRestoreInactiveGrid"));

            var l = grid.FLayout;

            if (l.ColorBackAxisItem != null)
                ColorAxis = l.ColorBackAxisItem.UniqueName;

            if (l.fColorForeAxisItem != null)
                ColorForeAxis = l.fColorForeAxisItem.UniqueName;

            if (l.fSizeAxisItem != null)
                SizeAxis = l.fSizeAxisItem.UniqueName;

            if (l.fShapeAxisItem != null)
                ShapeAxis = l.fShapeAxisItem.UniqueName;

            if (l.fXAxisMeasure != null)
                XMeasure = l.fXAxisMeasure.UniqueName;

            if (l.fYAxisMeasures.Count > 0)
            {
                YMeasures = new SerializedMeasureGroup[l.fYAxisMeasures.Count];
                for (var i = 0; i < l.fYAxisMeasures.Count; i++)
                {
                    var g = new SerializedMeasureGroup();
                    YMeasures[i] = g;
                    g.Init(l.fYAxisMeasures[i]);
                }
            }

            RowHierarchies = new string[l.fRowAxis.Count];
            for (var i = 0; i < l.fRowAxis.Count; i++)
                RowHierarchies[i] = l.fRowAxis[i].UniqueName;

            ColumnHierarchies = new string[l.fColumnAxis.Count];
            for (var i = 0; i < l.fColumnAxis.Count; i++)
                ColumnHierarchies[i] = l.fColumnAxis[i].UniqueName;

            PageHierarchies = new string[l.fPageAxis.Count];
            for (var i = 0; i < l.fPageAxis.Count; i++)
                PageHierarchies[i] = l.fPageAxis[i].UniqueName;


            //ShapeHierarchies = new string[l.fShapeAxisItem.Count];
            //for (int i = 0; i < l.fShapeAxisItem.Count; i++)
            //{
            //    ShapeHierarchies[i] = l.fShapeAxisItem[i].UniqueName;
            //}

            DetailsHierarchies = new string[l.fDetailsAxis.Count];
            for (var i = 0; i < l.fDetailsAxis.Count; i++)
                DetailsHierarchies[i] = l.fDetailsAxis[i].UniqueName;

            var lm = new List<SerializedMeasure>();
            if (grid.Measures.Level != null)
                foreach (var m in grid.Measures.Level.Members)
                {
                    var measure = grid.Measures.Find(m.UniqueName);
                    if (measure.Visible || measure.Expression.IsFill())
                        lm.Add(new SerializedMeasure(measure));
                }
            Measures = lm.ToArray();

            MeasureLayout = l.fMeasureLayout;
            MeasurePosition = l.fMeasurePosition;
            HideMeasureIfPossible = l.fHideMeasureIfPossible;
            HideMeasureModesIfPossible = l.fHideMeasureModesIfPossible;

            OpenendNodes = new string[0];
            OpenendActions = new PossibleDrillActions[0];

            Drills = grid.CellSet.FDrillActions.Select(item => item.ToString()).ToArray();

            ValueSortedColumn = grid.FCellSet.ValueSortedColumn;
            if (ValueSortedColumn >= 0)
                ValueSortingDirection = grid.FCellSet.ValueSortingDirection;
        }
    }
}