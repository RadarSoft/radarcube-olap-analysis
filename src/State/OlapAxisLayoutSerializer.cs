using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Events;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.State
{
    /// <summary>Saves/restores the state of the Grid.</summary>
    public abstract class OlapAxisLayoutSerializer : IXmlSerializable
    {
        private const string _signalstr =
                @"<OlapGridSerializer xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">"
            ;

        internal static OlapControl _SOLAPGrid;

        [XmlIgnore] private readonly List<Type> _ExcludeTypes = new List<Type>();

        private AnalysisType _analysisType;
        private bool _AnalysisTypeWasset;

        [XmlAttribute] [DefaultValue(null)] public string ActiveOLAPSlice;

        /// <exclude />
        [Browsable(false)] [EditorBrowsable(EditorBrowsableState.Never)] public SerializedLayout AxesLayout;

        [DefaultValue(null)] public SeriesType[] ChartsType;

        [DefaultValue(null)] public string[] ChartsTypeUniqueNames;

        [EditorBrowsable(EditorBrowsableState.Never)] public string[] ColumnsWidthNames;

        [EditorBrowsable(EditorBrowsableState.Never)] public double[] ColumnsWidths;

        /// <exclude />
        [EditorBrowsable(EditorBrowsableState.Never)] public SerializedICubeAddress[] CommentAddresses;

        /// <exclude />
        [EditorBrowsable(EditorBrowsableState.Never)] public string[] CommentStrings;

        [EditorBrowsable(EditorBrowsableState.Never)] [DefaultValue(OlapControl.__CurrencyFormatString)]
        public string CurrencyFormatString;

        [Browsable(false)] [EditorBrowsable(EditorBrowsableState.Never)] public string Data;

        /// <exclude />
        [EditorBrowsable(EditorBrowsableState.Never)] [DefaultValue(null)] public string EmptyDataString;

        internal OlapControl fGrid;

        /// <exclude />
        [EditorBrowsable(EditorBrowsableState.Never)] public SerializedHierarchy[] Hierarchies;


        [XmlAttribute] [DefaultValue(SizeMode.Increase)] public SizeMode SizeMode;

        protected OlapAxisLayoutSerializer()
        {
            EmptyDataString = string.Empty;
            CurrencyFormatString = OlapControl.__CurrencyFormatString;
            AxesLayout = new SerializedLayout();
            SizeMode = SizeMode.Increase;

            _analysisType = AnalysisType.Grid;
            _AnalysisTypeWasset = false;

            SuppressSaveOLAPSlices = false;
        }

        protected OlapAxisLayoutSerializer(OlapControl grid, bool OnlyGraphicalPart)
            : this()
        {
            // OLAPWINFORM
            fGrid = grid;
            LoadFrom(grid);
        }

        [XmlIgnore]
        internal List<Type> ExcludeTypes => _ExcludeTypes;

        [Obsolete("Use OnBeforSave and OnAfterLoad event")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DefaultValue("")]
        public string StreamData { get; set; }

        internal bool SuppressSaveOLAPSlices { get; set; }

        [DefaultValue(null)]
        public string[] OLAPSlices { get; set; }

        /// <summary>
        ///     Gets and sets the Grid state as an UTF8-encoded xml string.
        /// </summary>
        [XmlIgnore]
        public string XMLString
        {
            get
            {
                var ms = new MemoryStream();
                WriteXML(ms);
                var b = ms.ToArray();
                return Encoding.UTF8.GetString(b);
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                var bytes = Encoding.UTF8.GetBytes(value);
                var st = new MemoryStream(bytes);
                ReadXML(st);
            }
        }

        protected virtual void ReadStartElement(XmlReader reader)
        {
        }

        internal virtual void LoadFrom(OlapControl grid)
        {
            DebugLogging.WriteLine("OlapAxisLayoutSerializer.LoadFrom");

            EmptyDataString = grid.FEmptyDataString;
            CurrencyFormatString = grid.FCurrencyFormatString;

            grid.RefreshChartsType();
            ChartsType = grid.ChartsType;
            TrendSerialize(grid);

            AxesLayout = new SerializedLayout();
            AxesLayout.Init(grid);

            var lh = new List<SerializedHierarchy>();
            foreach (var d in grid.Dimensions)
            foreach (var h in d.Hierarchies)
                lh.Add(new SerializedHierarchy(h));
            Hierarchies = lh.ToArray();

            if (grid.FCellSet.fComments.Count > 0)
            {
                var ca = new List<SerializedICubeAddress>(grid.FCellSet.fComments.Count);
                var cs = new List<string>(grid.FCellSet.fComments.Count);

                foreach (var k in grid.FCellSet.fComments)
                {
                    ca.Add(new SerializedICubeAddress(k.Key));
                    cs.Add(k.Value);
                }

                CommentAddresses = ca.ToArray();
                CommentStrings = cs.ToArray();
            }
            SaveCubeData(grid);
        }


        private void TrendSerialize(OlapControl grid)
        {
        }

        private void TrendDeSerialize(OlapControl grid)
        {
        }

        private void SaveCubeData(OlapControl grid)
        {
            //var ms = new MemoryStream();
            //grid.SaveCompressed(ms, StreamContent.CubeData);
            //ms.Position = 0;
            //var b = new byte[ms.Length];
            //ms.Read(b,  0, (int)ms.Length);

            //_StreamData = Convert.ToBase64String(b, Base64FormattingOptions.None);
        }

        internal virtual void LoadTo(OlapControl aGrid)
        {
            DebugLogging.WriteLine("OlapAxisLayoutSerializer.LoadTo");

            if (!aGrid.Active)
                return;

            aGrid.FEmptyDataString = EmptyDataString;
            aGrid.FCurrencyFormatString = CurrencyFormatString;


            aGrid.BeginUpdate();

            Clear(aGrid);

            if (AxesLayout != null)
            {
                aGrid.FLayout.fMeasureLayout = AxesLayout.MeasureLayout;
                aGrid.FLayout.fMeasurePosition = AxesLayout.MeasurePosition;
                aGrid.FLayout.fHideMeasureIfPossible = AxesLayout.HideMeasureIfPossible;
                aGrid.FLayout.fHideMeasureModesIfPossible = AxesLayout.HideMeasureModesIfPossible;

                aGrid.FCellSet.FSortingDirection = AxesLayout.ValueSortingDirection;
                aGrid.FCellSet.ValueSortedColumn = AxesLayout.ValueSortedColumn;
            }

            var assignedMeasures = aGrid.Measures.Where(item => item.Visible).ToList();

            LoadMeasuresXandY(aGrid, assignedMeasures);
            LoadHierarchies(aGrid, assignedMeasures);

            aGrid.FCellSet.Rebuild();

            LoadMeasures(aGrid);

            assignedMeasures = aGrid.Measures.Where(item => item.Visible).ToList();

            aGrid.FCellSet.Rebuild();

            if (AxesLayout != null)
            {
                aGrid.FCellSet.FSortingDirection = AxesLayout.ValueSortingDirection;
                aGrid.FCellSet.ValueSortedColumn = AxesLayout.ValueSortedColumn;
            }

            foreach (var item in assignedMeasures)
                aGrid.Pivoting(item, LayoutArea.laRow, null, LayoutArea.laNone);
            LoadComments(aGrid);
            LoadColumnsWidth(aGrid);

            aGrid.EndUpdate();
        }

        private void LoadColumnsWidth(OlapControl grid)
        {
        }

        private void LoadComments(OlapControl grid)
        {
            grid.FCellSet.fComments.Clear();
            if (CommentStrings != null)
                for (var i = 0; i < CommentStrings.Length; i++)
                {
                    var a = CommentAddresses[i].GetCubeAddress(grid);
                    grid.FCellSet.fComments.Add(a, CommentStrings[i]);
                }
        }

        private void LoadHierarchies(OlapControl grid, List<Measure> assignedMeasures)
        {
            if (Hierarchies == null)
                return;

            foreach (var H in Hierarchies)
            {
                var h = grid.Dimensions.FindHierarchy(H.UniqueName);
                if (h != null)
                    H.Restore(h);
            }

            foreach (var s in AxesLayout.RowHierarchies)
            {
                var h = grid.Dimensions.FindHierarchy(s);
                if (h != null)
                    grid.PivotingLast(h, LayoutArea.laRow);
            }

            foreach (var s in AxesLayout.ColumnHierarchies)
            {
                var H = grid.Dimensions.FindHierarchy(s);
                if (H != null) grid.PivotingLast(H, LayoutArea.laColumn);
            }
            //if (olap.OlapDocumentMode == OlapDocumentMode.MDI && (olap.AllowShareFilter == false))
            {
                foreach (var s in AxesLayout.PageHierarchies)
                {
                    var H = grid.Dimensions.FindHierarchy(s);
                    if (H != null) grid.PivotingLast(H, LayoutArea.laPage);
                }
            }

            if (AxesLayout.DetailsHierarchies != null)
                foreach (var s in AxesLayout.DetailsHierarchies)
                {
                    var h = grid.Dimensions.FindHierarchy(s);
                    if (h != null) grid.PivotingLast(h, LayoutArea.laDetails);
                }

            if (!string.IsNullOrEmpty(AxesLayout.ColorAxis)) // && grid.CellsetMode == CellsetMode.cmChart)
            {
                var m = grid.Measures.Find(AxesLayout.ColorAxis);
                if (m != null)
                {
                    grid.Pivoting(m, LayoutArea.laColor, null, LayoutArea.laNone);
                    assignedMeasures.Remove(m);
                }
                else
                {
                    var h = grid.Dimensions.FindHierarchy(AxesLayout.ColorAxis);
                    if (h != null)
                        grid.PivotingFirst(h, LayoutArea.laColor);
                }
            }

            if (!string.IsNullOrEmpty(AxesLayout.ColorForeAxis) && grid.CellsetMode == CellsetMode.cmGrid)
            {
                var m = grid.Measures.Find(AxesLayout.ColorForeAxis);
                if (m != null)
                {
                    grid.Pivoting(m, LayoutArea.laColorFore, null, LayoutArea.laNone);
                    assignedMeasures.Remove(m);
                }
                else
                {
                    var h = grid.Dimensions.FindHierarchy(AxesLayout.ColorForeAxis);
                    if (h != null)
                        grid.PivotingFirst(h, LayoutArea.laColorFore);
                }
            }

            if (!string.IsNullOrEmpty(AxesLayout.SizeAxis))
            {
                var m = grid.Measures.Find(AxesLayout.SizeAxis);
                if (m != null)
                {
                    grid.Pivoting(m, LayoutArea.laSize, null, LayoutArea.laNone);
                    assignedMeasures.Remove(m);
                }
                else
                {
                    var h = grid.Dimensions.FindHierarchy(AxesLayout.SizeAxis);
                    if (h != null)
                        grid.PivotingFirst(h, LayoutArea.laSize);
                }
            }
            if (!string.IsNullOrEmpty(AxesLayout.ShapeAxis))
            {
                var h = grid.Dimensions.FindHierarchy(AxesLayout.ShapeAxis);
                if (h != null)
                    grid.PivotingFirst(h, LayoutArea.laShape);
            }

            if (!string.IsNullOrEmpty(AxesLayout.XMeasure))
            {
                var m = grid.Measures.Find(AxesLayout.XMeasure);
                if (m != null)
                    grid.Pivoting(m, LayoutArea.laColumn, null, LayoutArea.laNone);
            }

            if (AxesLayout.YMeasures != null)
            {
                grid.AxesLayout.fYAxisMeasures.Clear();

                foreach (var gm in AxesLayout.YMeasures)
                    gm.Restore(grid, assignedMeasures);
            }

#pragma warning disable 612,618
            if (AxesLayout.OpenendNodes != null)
            {
                var on = new Dictionary<string, PossibleDrillActions>(AxesLayout.OpenendNodes.Length);
                for (var i = 0; i < AxesLayout.OpenendNodes.Length; i++)
                    on.Add(AxesLayout.OpenendNodes[i], AxesLayout.OpenendActions[i]);
#pragma warning restore 612,618
                grid.FCellSet.ApplyOpenedNodes(on);
            }
            if (AxesLayout.Drills != null)
                foreach (var s in AxesLayout.Drills)
                {
                    var da = DrillAction.FromString(grid, s);
                    if (da != null)
                        grid.CellSet.FDrillActions.Add(da);
                }
        }

        private void LoadMeasuresXandY(OlapControl grid, List<Measure> assignedMeasures)
        {
            if (!string.IsNullOrEmpty(AxesLayout.XMeasure))
            {
                var m = grid.Measures.Find(AxesLayout.XMeasure);
                if (m != null)
                {
                    grid.Pivoting(m, LayoutArea.laColumn, null, LayoutArea.laNone);
                    assignedMeasures.Remove(m);
                }
            }

            if (AxesLayout.YMeasures != null)
                foreach (var gm in AxesLayout.YMeasures)
                    gm.Restore(grid, assignedMeasures);
        }

        private void LoadMeasures(OlapControl grid)
        {
            var cMs = new List<Measure>();
            if (AxesLayout != null && AxesLayout.Measures != null)
            {
                for (var i = 0; i < AxesLayout.Measures.Length; i++)
                {
                    var SM = AxesLayout.Measures[i];
                    var m = grid.Measures.Find(SM.UniqueName);

                    if (m == null && !string.IsNullOrEmpty(SM.Expression))
                    {
                        m = grid.Measures.AddCalculatedMeasure(SM.DisplayName, "", "", SM.UniqueName, true);
                        m.Expression = SM.Expression;
                        m.DefaultFormat = SM.DefaultFormat;
                    }

                    if (m != null)
                    {
                        m.Expression = SM.Expression;
                        m.VisibleInTree = SM.VisibleInTree;

                        if (string.IsNullOrEmpty(SM.DefaultFormat))
                            m.FDefaultFormat_ = m.CubeMeasure == null ? "" : m.CubeMeasure.DefaultFormat;
                        else
                            m.FDefaultFormat_ = SM.DefaultFormat;

                        m.Visible = SM.Visible;

                        foreach (var sm in m.ShowModes.ToArray())
                            try
                            {
                                if (sm.Visible)
                                    sm.Visible = false;
                            }
                            catch (Exception e)
                            {
                            }

                        if (SM.ActiveModes != null)
                            foreach (var s in SM.ActiveModes)
                            {
                                var sm = m.ShowModes.Find(s);
                                if (sm != null)
                                    sm.Visible = true;
                            }

                        if (SM.ActiveShowModes != null)
                            foreach (var assm in SM.ActiveShowModes)
                            {
                                var sm = m.ShowModes.Find(assm);
                                if (sm != null)
                                    sm.Visible = true;
                            }

                        if (SM.Intelligences != null)
                            for (var ii = 0; ii < SM.Intelligences.Length; ii++)
                            {
                                var hi = grid.Dimensions.FindHierarchy(SM.IntelligenceParents[ii]);
                                if (hi != null)
                                {
                                    hi.InitHierarchy(0);
                                    foreach (var ti in hi.Intelligence)
                                        if (ti.Expression == SM.Intelligences[ii])
                                        {
                                            ti.AddMeasure(m);
                                            break;
                                        }
                                }
                            }

                        var b = m.ShowModes.Any(sm => sm.fVisible);

                        if (!b)
                            m.ShowModes[0].fVisible = true;

                        if (SM.Filter != null)
                            m.fFilter = new MeasureFilter(m, SM.Filter.Condition, SM.Filter.FirstValue,
                                                SM.Filter.SecondFalue)
                                            {
                                                RestrictsTo = SM.Filter.RestrictsTo
                                            };
                        else
                            m.fFilter = null;

                        m.RestoreAfterSerialization(grid);
                        var mm = grid.Measures.Level.FindMember(SM.UniqueName);
                        grid.Measures.Level.Members.MoveMember(mm, i);
                    }

                    if (m != null && !string.IsNullOrEmpty(m.Expression))
                        cMs.Add(m);
                }
                if (grid.CellsetMode == CellsetMode.cmChart)
                {
                    foreach (var m in grid.Measures)
                    {
                        if (!m.Visible)
                            continue;
                        if (!m.IsActive)
                            grid.Pivoting(m, LayoutArea.laRow, null, LayoutArea.laNone);
                    }
                    grid.ChartsType = ChartsType;
                    TrendDeSerialize(grid);
                }
            }
        }

        private void Clear(OlapControl grid)
        {
            grid.FCellSet.ClearMembers();
            grid.FCellSet.FDrillActions.Clear();

            foreach (var h in grid.FLayout.fColumnAxis.ToArray())
                h.ResetFilter();

            grid.FLayout.fColumnAxis.Clear();
            foreach (var h in grid.FLayout.fRowAxis.ToArray())
                h.ResetFilter();

            grid.FLayout.fRowAxis.Clear();
            foreach (var h in grid.FLayout.fPageAxis.ToArray())
                h.ResetFilter();

            grid.FLayout.fPageAxis.Clear();
            foreach (var h in grid.FLayout.fDetailsAxis.ToArray())
                h.ResetFilter();

            grid.FLayout.fDetailsAxis.Clear();

            var colorBackAxisItem = grid.FLayout.ColorBackAxisItem as Hierarchy;
            if (colorBackAxisItem != null)
                colorBackAxisItem.ResetFilter();

            var colorForeAxisItem = grid.FLayout.fColorForeAxisItem as Hierarchy;
            if (colorForeAxisItem != null)
                colorForeAxisItem.ResetFilter();


            var sizeAxisItem = grid.FLayout.SizeAxisItem as Hierarchy;
            if (sizeAxisItem != null)
                sizeAxisItem.ResetFilter();
            grid.FLayout.SizeAxisItem = null;

            var shapeAxisItem = grid.FLayout.fShapeAxisItem as Hierarchy;
            if (shapeAxisItem != null)
                shapeAxisItem.ResetFilter();

            grid.FLayout.ShapeAxisItem = null;
            grid.FLayout.fXAxisMeasure = null;

            foreach (var m in grid.Measures)
                m.FVisible = false;

            grid.AxesLayout.fYAxisMeasures.Clear();

            if (grid.Measures.Level != null)
                foreach (var m in grid.Measures.Level.Members)
                    m.FVisible = false;
        }


        protected virtual void WriteXMLInner(Stream stream)
        {
            DebugLogging.WriteLine("OlapAxisLayoutSerializer.WriteXMLInner");

            //if (fGrid != null)
            LoadFrom(fGrid);

            fGrid.BeforSave(fGrid, new OnSerializeArgs(this));

            var bf = XmlSerializator.GetXmlSerializer(GetType());
            bf.Serialize(stream, this);
        }

        internal virtual void ReadXMLInner(Stream stream)
        {
            DebugLogging.WriteLine("OlapAxisLayoutSerializer.ReadXMLInner");

            if (fGrid != null)
                fGrid.BeforLoad(fGrid, EventArgs.Empty);

            #region code of old vertions file (web saving)

            // define signal string
            // content of web-versions file:
            // #1: <?xml version="1.0"?>
            // #2: <OlapGridSerializer xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
            // # etc ...
            //

            stream.Position = 0;
            var sr = new StreamReader(stream, Encoding.UTF8);

            stream.Position = 0;
            var alldata = sr.ReadToEnd();

            // its Web-like saved !!!
            //if ((!string.IsNullOrEmpty(lines[1])) && (string.Compare(lines[1], signalstr) == 0))
            if (alldata.Contains(_signalstr))
            {
                stream.Position = 0;
                sr = new StreamReader(stream, Encoding.UTF8);
                alldata = alldata.Replace(_signalstr, @"<OlapAxisLayoutSerializer>");
                alldata = alldata.Replace(@"</OlapGridSerializer>", @"</OlapAxisLayoutSerializer>");

                var ms = new MemoryStream();
                var sw = new StreamWriter(ms);
                sw.Write(alldata);
                sw.Flush();

                var bf = XmlSerializator.GetXmlSerializer(typeof(OlapAxisLayoutSerializer));

#if OLAPWINFORMS
                if (fGrid != null)
                    fGrid.Cursor = System.Windows.Forms.Cursors.WaitCursor;
#endif

                ms.Position = 0;

                var g = (OlapAxisLayoutSerializer) bf.Deserialize(ms);

                if (fGrid != null)
                {
                    g.fGrid = fGrid;
                    g.LoadTo(fGrid);
#if OLAPWINFORMS
                    fGrid.Cursor = System.Windows.Forms.Cursors.Default;
#endif
                    fGrid.AfterLoad(fGrid, new OnSerializeArgs(g));
                }

                return;
            }

            #endregion end of web-xml reading

            _SOLAPGrid = fGrid;

            var bf1 = XmlSerializator.GetXmlSerializer(GetType());

            stream.Flush();
            stream.Position = 0;

            if (fGrid != null)
            {
                var g1 = (OlapGridSerializer) bf1.Deserialize(stream);
                g1.fGrid = fGrid;
                g1.SuppressSaveOLAPSlices = SuppressSaveOLAPSlices;

                fGrid._IsReadXMLProcessing = true;
                if (!fGrid.IsUpdating)
                {
                    fGrid.BeginUpdate();
                    g1.LoadTo(fGrid);
                    fGrid.EndUpdate();
                }
                else
                {
                    g1.LoadTo(fGrid);
                }

                fGrid.AfterLoad(fGrid, new OnSerializeArgs(g1));

                fGrid._IsReadXMLProcessing = false;
            }
            _SOLAPGrid = null;
        }

        /// <summary>Writes the saved Grid state into the stream.</summary>
        public void WriteXML(Stream stream)
        {
            WriteXMLInner(stream);
        }

        /// <summary>Writes the saved Grid state into the file.</summary>
        /// <param name="fileName">The name of the file to write.</param>
        public void WriteXML(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Create))
            {
                WriteXML(fs);
            }
        }

        /// <summary>Restores the saved Grid state from the file.</summary>
        /// <param name="fileName">The name of the file to read.</param>
        public void ReadXML(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                ReadXML(fs);
            }
        }

        /// <summary>Restores the saved Grid state from the stream.</summary>
        public void ReadXML(Stream stream)
        {
            ReadXMLInner(stream);
        }

        #region IXmlSerializable Members

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            ReadStartElement(reader);

            XmlSerializator.DeSerialize(reader, this);

            if (!reader.EOF)
                reader.ReadEndElement();
        }


        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            XmlSerializator.ExcludeTypes.Clear();
            XmlSerializator.ExcludeTypes.AddRange(ExcludeTypes);
            XmlSerializator.Serialize(writer, this);
        }

        #endregion
    }
}