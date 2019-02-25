using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>
    ///     Describes both the initial and the current configurations of the Grid pivot
    ///     areas.
    /// </summary>
    ////[Serializable]
    public class AxesLayout : IStreamedObject
    {
        internal IDescriptionable fColorAxisItem;

        internal IDescriptionable fColorForeAxisItem;
        internal TObservableCollection<Hierarchy> fColumnAxis = new TObservableCollection<Hierarchy>();
        internal List<Level> fColumnLevels = new List<Level>();
        internal string fColumnNodes = "";

        internal List<Hierarchy> fDetailsAxis = new List<Hierarchy>();

        [NonSerialized] internal OlapControl fGrid;

        internal bool fHideMeasureIfPossible = true;
        internal bool fHideMeasureModesIfPossible = true;
        internal LayoutArea fMeasureLayout = LayoutArea.laColumn;
        internal MeasurePosition fMeasurePosition = MeasurePosition.mpLast;
        internal TObservableCollection<Hierarchy> fPageAxis = new TObservableCollection<Hierarchy>();
        internal string fPageNodes = "";

        internal TObservableCollection<Hierarchy> fRowAxis = new TObservableCollection<Hierarchy>();
        internal List<Level> fRowLevels = new List<Level>();
        internal string fRowNodes = "";
        internal IDescriptionable fShapeAxisItem;
        internal IDescriptionable fSizeAxisItem;

        internal Measure fXAxisMeasure;

        internal List<MeasureGroup> fYAxisMeasures = new List<MeasureGroup>();

        internal AxesLayout(OlapControl AGrid)
        {
            fGrid = AGrid;
        }

        internal IDescriptionable ColorBackAxisItem
        {
            get => fColorAxisItem;
            set
            {
                if (fColorAxisItem == value)
                    return;

                fColorAxisItem = value;
            }
        }

        internal IDescriptionable ColorForeAxisItem
        {
            get => fColorForeAxisItem;
            set
            {
                if (fColorForeAxisItem == value)
                    return;

                fColorForeAxisItem = value;
            }
        }

        internal IDescriptionable ShapeAxisItem
        {
            get => fShapeAxisItem;
            set
            {
                if (ShapeAxisItem == value)
                    return;

                fShapeAxisItem = value;
            }
        }

        internal IDescriptionable SizeAxisItem
        {
            get => fSizeAxisItem;
            set
            {
                if (SizeAxisItem == value)
                    return;

                fSizeAxisItem = value;
            }
        }

        /// <summary>
        ///     References to the OlapControl instance the specified object belongs
        ///     to.
        /// </summary>
        public OlapControl Grid => fGrid;

        /// <summary>
        ///     The read-only collection of hierarchies in the Row area.
        /// </summary>

        public IList<Hierarchy> RowAxis => fRowAxis;

        /// <summary>
        ///     The read-only collection of hierarchies in the Column area.
        /// </summary>

        public IList<Hierarchy> ColumnAxis => fColumnAxis;

        /// <summary>
        ///     The read-only collection of hierarchies in the Page (filter) area.
        /// </summary>
        public IList<Hierarchy> PageAxis => fPageAxis;

        /// <summary>
        ///     The read-only collection of hierarchies in the Details area.
        /// </summary>

        public IList<Hierarchy> DetailsAxis => fDetailsAxis;

        /// <summary>
        ///     The read-only collection of hierarchies in the Rows area.
        /// </summary>
        /// <remarks>
        /// </remarks>
        [DefaultValue(null)]
        public string RowNodes
        {
            get => fRowNodes;
            set
            {
                fRowNodes = value;
                UpdateAxisByNodes(value, fRowAxis);
            }
        }

        /// <summary>
        ///     The list of hierarchies to be placed in the Column area upon opening the
        ///     Grid.
        /// </summary>
        [DefaultValue(null)]
        public string ColumnNodes
        {
            get => fColumnNodes;
            set
            {
                fColumnNodes = value;
                UpdateAxisByNodes(value, fColumnAxis);
            }
        }

        /// <summary>
        ///     The list of hierarchies to be placed in the Page area upon opening the
        ///     Grid.
        /// </summary>
        [DefaultValue(null)]
        public string PageNodes
        {
            get => fPageNodes;
            set
            {
                fPageNodes = value;
                UpdateAxisByNodes(value, fPageAxis);
            }
        }

        /// <summary>
        ///     The pivot area (Row or Column), where the names of the measures are
        ///     displayed.
        /// </summary>
        [DefaultValue(LayoutArea.laColumn)]
        public LayoutArea MeasureLayout
        {
            get => fMeasureLayout;
            set
            {
                if (fMeasureLayout != value)
                {
                    if (value != LayoutArea.laRow && value != LayoutArea.laColumn)
                        value = LayoutArea.laColumn;

                    fMeasureLayout = value;
                    if (Grid.Active)
                        Grid.CellSet.Rebuild();
                    Grid.EndChange(GridEventType.gePivotAction);
                }
            }
        }

        /// <summary>
        ///     The position of the measure (the first or the last) in the measures list in the
        ///     pivot area.
        /// </summary>
        [DefaultValue(MeasurePosition.mpLast)]
        public MeasurePosition MeasurePosition
        {
            get => fMeasurePosition;
            set
            {
                if (fMeasurePosition == value)
                    return;

                fMeasurePosition = value;
                if (Grid.Active)
                    Grid.CellSet.Rebuild();
                Grid.EndChange(GridEventType.gePivotAction);
            }
        }

        /// <summary>
        ///     If this property is set to True, and the Grid mode shows only visible measures,
        ///     then this measure name is not displayed.
        /// </summary>
        [DefaultValue(true)]
        public bool HideMeasureIfPossible
        {
            get => fHideMeasureIfPossible;
            set
            {
                if (fHideMeasureIfPossible == value) return;
                fHideMeasureIfPossible = value;
                if (Grid.IsUpdating || Grid.CellSet == null) return;
                if (Grid.CellSet.FVisibleMeasures.Count == 1) Grid.CellSet.Rebuild();
            }
        }

        /// <summary>
        ///     If possible, it hides the cells that display mode names, thus saving the screen
        ///     space.
        /// </summary>
        [DefaultValue(true)]
        public bool HideMeasureModesIfPossible
        {
            get => fHideMeasureModesIfPossible;
            set
            {
                if (fHideMeasureModesIfPossible == value) return;
                fHideMeasureModesIfPossible = value;
                if (Grid.IsUpdating || Grid.CellSet == null) return;
                if (Grid.CellSet.FVisibleMeasures.Count == 1) Grid.CellSet.Rebuild();
            }
        }

        internal void CheckExpandedLevels()
        {
            fRowLevels.Clear();

            foreach (var h in fRowAxis)
            //    if (h.Levels != null)
            foreach (var l in h.Levels)
                if (l.Visible)
                    fRowLevels.Add(l);

            fColumnLevels.Clear();

            foreach (var h in fColumnAxis)
                if (h.Levels != null)
                    foreach (var l in h.Levels)
                        if (l.Visible) fColumnLevels.Add(l);
        }

        internal Hierarchy FindHierarchy(string UniqueName)
        {
            for (var i = 0; i < fGrid.Dimensions.Count; i++)
            {
                var Hierarchy = fGrid.Dimensions[i].Hierarchies.Find(UniqueName);
                if (Hierarchy != null) return Hierarchy;
            }
            return null;
        }


        internal void UpdateAxisByNodes(string Nodes, IList<Hierarchy> Axis)
        {
            Axis.Clear();
            var S = Nodes;
            do
            {
                if (S.Contains("<|>"))
                {
                    var S1 = S.Substring(0, S.IndexOf("<|>"));
                    S = S.Substring(S.IndexOf("<|>") + 3);
                    var H = FindHierarchy(S1);
                    if (H != null) Axis.Add(H);
                }
                else
                {
                    var H = FindHierarchy(S);
                    if (H != null)
                        Axis.Add(H);
                    break;
                }
            } while (true);
        }

        internal void LayoutChanged(LayoutArea Area)
        {
        }

        internal string[] CreateStringArray(List<Hierarchy> P)
        {
            var a = new string[P.Count];
            for (var i = 0; i < P.Count; i++) a[i] = P[i].UniqueName;
            return a;
        }

        private void fPageAxis_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    break;
                default:
                {
                    var data = 0;
                }
                    break;
            }
        }

        /// <summary>
        ///     Clears the layout and makes the Grid empty.
        /// </summary>
        public void Clear()
        {
            fGrid.BeginUpdate();
            fRowLevels.Clear();
            fColumnLevels.Clear();

            if (Grid.Dimensions != null)
                foreach (var d in Grid.Dimensions)
                foreach (var h in d.Hierarchies)
                {
                    h.DeleteGroups();
                    h.ResetHierarchy();
                }

            fRowAxis.Clear();
            fColumnAxis.Clear();
            fPageAxis.Clear();
            fDetailsAxis.Clear();
            if (fXAxisMeasure != null)
                fXAxisMeasure = null;

            if (Grid.Measures != null)
                foreach (var m in Grid.Measures)
                    m.fFilter = null;
            fYAxisMeasures.Clear();

            if (ColorBackAxisItem is Hierarchy)
                ((Hierarchy) ColorBackAxisItem).ResetHierarchy();
            if (ColorBackAxisItem is Measure)
                ((Measure) ColorBackAxisItem).fFilter = null;
            ColorBackAxisItem = null;

            if (fColorForeAxisItem is Hierarchy)
                ((Hierarchy) fColorForeAxisItem).ResetHierarchy();
            if (fColorForeAxisItem is Measure)
                ((Measure) fColorForeAxisItem).fFilter = null;
            fColorForeAxisItem = null;

            if (fSizeAxisItem is Hierarchy)
                ((Hierarchy) fSizeAxisItem).ResetHierarchy();
            if (fSizeAxisItem is Measure)
                ((Measure) fSizeAxisItem).fFilter = null;
            SizeAxisItem = null;

            if (fShapeAxisItem is Hierarchy)
                ((Hierarchy) fShapeAxisItem).ResetHierarchy();
            if (fShapeAxisItem is Measure)
                ((Measure) fShapeAxisItem).fFilter = null;
            ShapeAxisItem = null;

            if (fGrid.Measures != null)
            {
                foreach (var m in fGrid.Measures)
                {
                    foreach (var sm in m.ShowModes)
                        sm.fVisible = false;
                    m.ShowModes[0].fVisible = true;
                    m.FVisible = false;
                }

                if (fGrid.Measures.Level != null)
                    foreach (var m in fGrid.Measures.Level.Members)
                    {
                        m.FVisible = false;
                        m.Children.ForEach(item => { item.FVisible = false; });
                    }
            }

            if (fGrid.CellSet != null)
            {
                fGrid.CellSet.ClearMembers();
                fGrid.CellSet.FDrillActions.Clear();
            }

            fGrid.EndUpdate();
        }

        internal LayoutArea ContainInLayoutArea(Hierarchy Hierarchy)
        {
            if (Grid.AxesLayout.ColumnAxis.Contains(Hierarchy))
                return LayoutArea.laColumn;

            if (Grid.AxesLayout.RowAxis.Contains(Hierarchy))
                return LayoutArea.laRow;

            if (Grid.AxesLayout.PageAxis.Contains(Hierarchy))
                return LayoutArea.laPage;

            return LayoutArea.laNone;
        }

        internal IEnumerable<LayoutArea> ContainInModificators(Hierarchy Hierarchy)
        {
            if (Grid.AxesLayout.ColorBackAxisItem == Hierarchy)
                yield return LayoutArea.laColor;

            if (Grid.AxesLayout.fColorForeAxisItem == Hierarchy)
                yield return LayoutArea.laColorFore;

            if (Grid.AxesLayout.fShapeAxisItem == Hierarchy)
                yield return LayoutArea.laShape;

            if (Grid.AxesLayout.fSizeAxisItem == Hierarchy)
                yield return LayoutArea.laSize;

            if (Grid.AxesLayout.fDetailsAxis.Contains(Hierarchy))
                yield return LayoutArea.laDetails;
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgLayout);

            if (!string.IsNullOrEmpty(fRowNodes))
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_RowNodes);
                StreamUtils.WriteString(writer, fRowNodes);
            }

            if (!string.IsNullOrEmpty(fColumnNodes))
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_ColumnNodes);
                StreamUtils.WriteString(writer, fColumnNodes);
            }

            if (!string.IsNullOrEmpty(fPageNodes))
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_PageNodes);
                StreamUtils.WriteString(writer, fPageNodes);
            }

            if (fRowAxis.Count > 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_RowAxis);
                StreamUtils.WriteInt32(writer, fRowAxis.Count);
                foreach (var h in fRowAxis)
                    StreamUtils.WriteString(writer, h.UniqueName);
            }

            if (fColumnAxis.Count > 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_ColumnAxis);
                StreamUtils.WriteInt32(writer, fColumnAxis.Count);
                foreach (var h in fColumnAxis)
                    StreamUtils.WriteString(writer, h.UniqueName);
            }

            if (fPageAxis.Count > 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_PageAxis);
                StreamUtils.WriteInt32(writer, fPageAxis.Count);
                foreach (var h in fPageAxis)
                    StreamUtils.WriteString(writer, h.UniqueName);
            }

            if (MeasureLayout != LayoutArea.laColumn)
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_MeasureLayout);
                StreamUtils.WriteInt32(writer, (int) fMeasureLayout);
            }

            if (fMeasurePosition != MeasurePosition.mpLast)
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_MeasurePosition);
                StreamUtils.WriteInt32(writer, (int) fMeasurePosition);
            }

            if (!fHideMeasureIfPossible)
                StreamUtils.WriteTag(writer, Tags.tgLayout_ShowMeasureIfPossible);

            if (!fHideMeasureModesIfPossible)
                StreamUtils.WriteTag(writer, Tags.tgLayout_ShowMeasureModesIfPossible);

            if (fDetailsAxis.Count > 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_DetailsAxis);
                StreamUtils.WriteInt32(writer, fDetailsAxis.Count);
                foreach (var h in fDetailsAxis)
                    StreamUtils.WriteString(writer, h.UniqueName);
            }

            if (fXAxisMeasure != null)
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_XAxisMeasure);
                StreamUtils.WriteString(writer, fXAxisMeasure.UniqueName);
            }

            if (fYAxisMeasures.Count > 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_YAxisMeasures);
                StreamUtils.WriteInt32(writer, fYAxisMeasures.Count);
                foreach (var g in fYAxisMeasures)
                {
                    StreamUtils.WriteInt32(writer, g.Count);
                    foreach (var m in g)
                        StreamUtils.WriteString(writer, m.UniqueName);
                }
            }

            if (ColorBackAxisItem != null)
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_ColorAxis);
                StreamUtils.WriteString(writer, ColorBackAxisItem.UniqueName);
            }

            if (fColorForeAxisItem != null)
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_ColorForeAxis);
                StreamUtils.WriteString(writer, fColorForeAxisItem.UniqueName);
            }


            if (fSizeAxisItem != null)
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_SizeAxis);
                StreamUtils.WriteString(writer, fSizeAxisItem.UniqueName);
            }

            if (fShapeAxisItem != null)
            {
                StreamUtils.WriteTag(writer, Tags.tgLayout_ShapeAxis);
                StreamUtils.WriteString(writer, fShapeAxisItem.UniqueName);
            }

            StreamUtils.WriteTag(writer, Tags.tgLayout_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgLayout);
            fColumnAxis.Clear();
            fRowAxis.Clear();
            fPageAxis.Clear();
            fDetailsAxis.Clear();
            fXAxisMeasure = null;
            fYAxisMeasures.Clear();
            ColorBackAxisItem = null;
            SizeAxisItem = null;
            ShapeAxisItem = null;
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                int c;
                switch (tag)
                {
                    case Tags.tgLayout_ColumnAxis:
                        c = StreamUtils.ReadInt32(reader);
                        for (var i = 0; i < c; i++)
                        {
                            var h = fGrid.Dimensions.FindHierarchy(StreamUtils.ReadString(reader));
                            if (h != null) fColumnAxis.Add(h);
                        }
                        break;
                    case Tags.tgLayout_RowAxis:
                        c = StreamUtils.ReadInt32(reader);
                        for (var i = 0; i < c; i++)
                        {
                            var h = fGrid.Dimensions.FindHierarchy(StreamUtils.ReadString(reader));
                            if (h != null) fRowAxis.Add(h);
                        }
                        break;
                    case Tags.tgLayout_PageAxis:
                        c = StreamUtils.ReadInt32(reader);
                        for (var i = 0; i < c; i++)
                        {
                            var h = fGrid.Dimensions.FindHierarchy(StreamUtils.ReadString(reader));
                            if (h != null) fPageAxis.Add(h);
                        }
                        break;
                    case Tags.tgLayout_ColumnNodes:
                        fColumnNodes = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgLayout_PageNodes:
                        fPageNodes = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgLayout_RowNodes:
                        fRowNodes = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgLayout_MeasureLayout:
                        fMeasureLayout = (LayoutArea) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgLayout_MeasurePosition:
                        fMeasurePosition = (MeasurePosition) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgLayout_ShowMeasureIfPossible:
                        fHideMeasureIfPossible = false;
                        break;
                    case Tags.tgLayout_ShowMeasureModesIfPossible:
                        fHideMeasureModesIfPossible = false;
                        break;
                    case Tags.tgLayout_DetailsAxis:
                        c = StreamUtils.ReadInt32(reader);
                        for (var i = 0; i < c; i++)
                        {
                            var h = fGrid.Dimensions.FindHierarchy(StreamUtils.ReadString(reader));
                            if (h != null)
                                DetailsAxis.Add(h);
                        }
                        break;
                    case Tags.tgLayout_XAxisMeasure:
                        fXAxisMeasure = fGrid.Measures.Find(StreamUtils.ReadString(reader));
                        break;
                    case Tags.tgLayout_YAxisMeasures:
                        c = StreamUtils.ReadInt32(reader);
                        for (var i = 0; i < c; i++)
                        {
                            var c1 = StreamUtils.ReadInt32(reader);
                            var mg = new MeasureGroup();
                            for (var i1 = 0; i1 < c1; i1++)
                            {
                                var m = fGrid.Measures.Find(StreamUtils.ReadString(reader));
                                ;
                                if (m != null) mg.Add(m);
                            }
                            if (mg.Count > 0) fYAxisMeasures.Add(mg);
                        }
                        break;
                    case Tags.tgLayout_ColorAxis:
                        ColorBackAxisItem = fGrid.FindMeasureOrHierarchy(StreamUtils.ReadString(reader));
                        break;
                    case Tags.tgLayout_ColorForeAxis:
                        fColorForeAxisItem = fGrid.FindMeasureOrHierarchy(StreamUtils.ReadString(reader));
                        break;
                    case Tags.tgLayout_SizeAxis:
                        SizeAxisItem = fGrid.FindMeasureOrHierarchy(StreamUtils.ReadString(reader));
                        break;
                    case Tags.tgLayout_ShapeAxis:
                        ShapeAxisItem = fGrid.FindMeasureOrHierarchy(StreamUtils.ReadString(reader));
                        break;
                    case Tags.tgLayout_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        #endregion
    }
}