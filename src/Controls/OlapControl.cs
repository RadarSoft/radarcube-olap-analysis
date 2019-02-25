using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.ClientAgents;
using RadarSoft.RadarCube.Controls.Analysis;
using RadarSoft.RadarCube.Controls.Chart;
using RadarSoft.RadarCube.Controls.Filter;
using RadarSoft.RadarCube.Controls.Grid;
using RadarSoft.RadarCube.Controls.HeirarchyEditor;
using RadarSoft.RadarCube.Controls.Menu;
using RadarSoft.RadarCube.Controls.Toolbox;
using RadarSoft.RadarCube.Controls.Tree;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Events;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.State;
using RadarSoft.RadarCube.Tools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace RadarSoft.RadarCube.Controls
{
    /// <summary>An abstract class, an ancestor to the OlapGrid control.</summary>
    /// <remarks>
    ///     Contains all properties and methods to work with an OLAP slice, with the
    ///     exception of visualization methods
    /// </remarks>
    public abstract class OlapControl : WebControl, IStreamedObject
    {
        public virtual bool Active { get; set; }

        public virtual string ConnectionString { get; set; }

        /// <summary>
        ///     The width of the Cube Structure Tree (in pixels)
        /// </summary>
        public virtual string StructureTreeWidth { get; set; }


        protected const string _RadarSoftSupport = "helpdesk@radar-soft.net";

        public virtual AnalysisType AnalysisType { get; set; }

        /// <summary>
        ///     The name of the client method that will be used for outputting messages.
        /// </summary>
        public virtual string MessageHandler { get; set; }

        /// <summary>
        ///     The name of the client method that will be used for proceccing errors.
        /// </summary>
        public virtual string ErrorHandler { get; set; }

        /// <summary>
        ///     If True, it is possible to select cells for color conditional formatting and obtaining statistical information.
        /// </summary>
        public virtual bool AllowSelectionFormatting { get; set; }

        /// <summary>
        ///     <para>
        ///         Runs a mail agent and creates an email to the technical support service to
        ///         report a problem with the component. In the address field ("To") the support email
        ///         address will appear.
        ///     </para>
        /// </summary>
        public virtual string SupportEMail { get; set; }

        /// <summary>
        ///     If True, the component is switched to the scrolling mode, in case its size
        ///     exceeds those assigned by the width and the height properties.
        /// </summary>
        /// <remarks>
        ///     If the option is on, it is equivalent to the setting of the CSS style "overflow:
        ///     auto".
        /// </remarks>
        public virtual bool AllowScrolling { get; set; }

        public virtual bool AllowResizing { get; set; }

        /// <summary>Specifies whether the delay pivoting.</summary>
        public virtual bool DelayPivoting { get; set; }

        private PivotingBehavior _pivotingBehavior;

        /// <summary>
        ///     Defines, if the automatic drilling down to the nodes of a new hierarchy pivoted to the active area is performed.
        /// </summary>
        public virtual PivotingBehavior PivotingBehavior
        {
            get => _pivotingBehavior;
            set
            {
                if (_pivotingBehavior == value)
                    return;
                _pivotingBehavior = value;
            }
        }

        /// <summary>
        ///     If true, all the 'Dimensions' filters in the Cube Structure tree will be expanded by default.
        /// </summary>
        public virtual bool ExpandDimensionsNode { get; set; }

        /// <summary>
        ///     If True, then the item appears in cell's context menu. And thus, allows ediding
        ///     the value of the current cell.
        /// </summary>
        public virtual bool AllowEditing { get; set; }

        /// <summary>
        ///     If True, then Grid headers are fixed while scrolling.
        ///     If the control is in the Grid mode and this property is set to False, then automatically activates the possibility
        ///     of resizing cells.
        ///     It's recommended to set this property to False, if fixing of Grid headers is not required, or for some reason
        ///     the Grid is rendered incorrectly on a Web-page.
        /// </summary>
        public virtual bool UseFixedHeaders { get; set; } = true;

        /// <summary>
        ///     Defines mode of pivoting elements in the Cube Structure Tree. If set to False,
        ///     then pivoting is performed by drag&amp;dropping, otherwise by clicking the checkbox of
        ///     the appropriate tree element.
        /// </summary>
        public virtual bool UseCheckboxesInTree { get; set; } = true;

        /// <summary>
        ///     If True, the names of the dimensions with a single hierarchy will be
        ///     hidden.
        /// </summary>
        public virtual bool HideDimensionNameIfPossible { get; set; }

        private HierarchiesDisplayMode _hierarchyDisplayMode;

        public HierarchiesDisplayMode HierarchiesDisplayMode
        {
            get
            {
                if (CellsetMode == CellsetMode.cmChart)
                    return HierarchiesDisplayMode.TreeLike;
                return _hierarchyDisplayMode;
            }
            set
            {
                if (_hierarchyDisplayMode == value)
                    return;

                if (CellsetMode != CellsetMode.cmChart)
                {
                    _hierarchyDisplayMode = value;

                    if (CellSet != null)
                        CellSet.Rebuild();
                }
            }
        }

        internal OlapGridMode FMode = OlapGridMode.gmStandard;

        /// <summary>
        ///     Indicates the Grid operation mode: Standard (navigating the Cube), or Display of
        ///     MDX-queries results mode
        /// </summary>
        /// <remarks>
        ///     <para>The OLAP Grid can operate in two different modes:</para>
        ///     <list type="bullet">
        ///         <item>
        ///             Standard (with support of the Cube navigation, such as pivoting,
        ///             drilling, filtration, etc.)
        ///         </item>
        ///         <item>Display of the MDX-queries results mode .</item>
        ///     </list>
        ///     <para>
        ///         By default, the Grid is launched in the Standard mode and is automatically
        ///         switched to the Display of the MDX-query results mode following the accomplishment
        ///         of the ShowMDXQuery method. You can switch the Grid back to the Standard mode only
        ///         by opening and closing the Cube.
        ///     </para>
        /// </remarks>
        /// <summary>
        ///     <para>
        ///         Indicates the Grid's operation mode: standard (navigation in the Cube), or
        ///         display mode of the MDX-queries results.
        ///     </para>
        /// </summary>
        public virtual OlapGridMode Mode
        {
            get => FMode;
            internal set
            {
                if (Mode == value)
                    return;

                FMode = value;
                if (_ModeUpdater.IsBusy)
                {
                    _ModeUpdater.UpdateEnd -= _ModeUpdater_UpdateEnd;
                    _ModeUpdater.UpdateEnd += _ModeUpdater_UpdateEnd;
                }
                else
                {
                    ModeChanged();
                }
            }
        }

        private bool fAllowDrilling = true;

        /// <summary>Defines whether the drilling option is allowed for users.</summary>
        public virtual bool AllowDrilling
        {
            get => fAllowDrilling;
            set
            {
                if (fAllowDrilling == value)
                    return;

                fAllowDrilling = value;
            }
        }

        /// <summary>Defines whether the filtering option is allowed for users.</summary>
        public virtual bool AllowFiltering { get; set; } = true;

        /// <summary>
        ///     Allows organizing a hierarchy page view in the OLAP Slice table. Switching to
        ///     this mode also allows you to substantially reduce traffic between a web server and a
        ///     client.
        /// </summary>
        /// <remarks>
        ///     By using the Level.PagerSettings property you can tune the hierarchy page view
        ///     mode for any hierarchy level individually.
        /// </remarks>
        public virtual bool AllowPaging { get; set; } = true;

        private int _LinesInPage;
#if DEBUG
        internal const int __MIN_LINES_IN_PAGE = 2;
#else
        internal const int __MIN_LINES_IN_PAGE = 5;
#endif
        internal const int __LINES_IN_PAGE = 10;

        /// <summary>Determines the number of lines in a "hierarchy page" by default.</summary>
        /// <remarks>
        ///     You can switch to the hierarchy page view mode by setting the AllowPaging
        ///     property to <em>True</em>, and tune it for any hierarchy level of any hierarchy
        ///     individually by using the Level.PagerSettings property.
        /// </remarks>
        public virtual int LinesInPage { get; set; }

        /// <summary>Specifies whether the pivot Filter area is shown.</summary>
        public virtual bool FilterAreaVisible { get; set; }

        /// <summary>
        ///     Specifies whether to show the "Cube Structure Tree" and "Pivot Areas" areas
        /// </summary>
        public virtual rsShowAreasOlapGrid ShowAreasMode { get; set; }

        /// <summary>
        ///     Specifies to show the Modification areas.
        /// </summary>
        public virtual bool ShowModificationAreas { get; set; }

        /// <summary>
        ///     Specifies to show the toolbox.
        /// </summary>
        public virtual bool ShowToolbox { get; set; }

        /// <summary>
        ///     Specifies to show the filter grid.
        /// </summary>
        public virtual bool ShowFilterGrid { get; set; }

        /// <summary>
        ///     Specifies to show the Legends container.
        /// </summary>
        public virtual bool ShowLegends { get; set; }

        /// <summary>
        ///     "$#,#.00"
        /// </summary>
        internal const string __CurrencyFormatString = "$#,#.00";

        internal string FCurrencyFormatString = __CurrencyFormatString;

        /// <summary>
        ///     A format string for measures formatted as "Currency". It must comply with the
        ///     rules of NET's "Numeric format strings" described in MSDN.
        /// </summary>
        public virtual string CurrencyFormatString
        {
            get => FCurrencyFormatString;
            set => FCurrencyFormatString = value;
        }

        internal string FEmptyDataString = "";

        public virtual string EmptyCellString
        {
            get => FEmptyDataString;
            set => FEmptyDataString = value;
        }

        private bool fAllowDeferLayout = true;

        /// <summary>
        ///     Defines, whether the user is shown the "Defer layout update" checkbox.
        /// </summary>
        public virtual bool AllowDeferLayout
        {
            get => fAllowDeferLayout;
            set
            {
                if (fAllowDeferLayout == value)
                    return;

                fAllowDeferLayout = value;
            }
        }

        private bool fDeferLayoutUpdate;

        public bool DeferLayoutUpdate
        {
            get => fDeferLayoutUpdate;
            set
            {
                var needRebuild = fDeferLayoutUpdate && !value;
                fDeferLayoutUpdate = value;
                if (needRebuild)
                    FCellSet.Rebuild();
            }
        }

        internal const int __MAXROWSINGRID = 500;
        internal const int __MAXCOLUMNSINGRID = 200;

        /// <summary>
        ///     Sets the maximum number of rows in the Grid. If this value is 0 then an unlimited
        ///     number of rows is allowed.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This limit of rows is set IRRELEVANTLY to paging.<br />
        ///         If the number of rows is limited, then after reaching it the Grid stops populating
        ///         the cellset with new rows. This property is set only in DesignTime or when creating
        ///         a control in the Page_Init method. You should not change the value of this property
        ///         from session to session, as it may cause faults.
        ///     </para>
        /// </remarks>
        public virtual int MaxRowsInGrid { get; set; } = __MAXROWSINGRID;

        /// <summary>
        ///     Sets the maximum number of columns in the Grid. If this value is 0 then an
        ///     unlimited number of columns is allowed.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This limit of columns is set IRRELEVANTLY to paging.<br />
        ///         If the number of columns is limited, then after reaching it the Grid stops
        ///         populating the cellset with new columns. This property is set only in DesignTime or
        ///         when creating a control in the Page_Init method. You should not change the value of
        ///         this property from session to session, as it may cause faults.
        ///     </para>
        /// </remarks>
        public virtual int MaxColumnsInGrid { get; set; } = __MAXCOLUMNSINGRID;


        public virtual int MaxTextLength { get; set; } = 30;

        #region Toolbox

        internal PostbackData postbackData { get; set; } = PostbackData.Nothing;

        internal Settings Settings { get; set; } = Settings.None;

        protected OlapToolboxBase fToolbox;

        internal OlapToolboxBase Toolbox => fToolbox;

        /// <summary>
        ///     Represents the collection of custom toolbox buttons.
        /// </summary>
        public virtual CustomToolboxButtonCollection CustomButtons => Toolbox.CustomButtons;

        /// <summary>
        ///     Contains all necessary properties for "Connect" toolbox button
        /// </summary>
        public virtual ConnectToolboxButton ConnectButton => Toolbox.ConnectButton;

        /// <summary>
        ///     Contains all necessary properties for "Save layout" toolbox button
        /// </summary>
        public virtual SaveLayoutToolboxButton SaveLayoutButton => Toolbox.SaveLayoutButton;

        /// <summary>
        ///     Contains all necessary properties for "Load layout" toolbox button
        /// </summary>
        public virtual LoadLayoutButton LoadLayoutButton => Toolbox.LoadLayoutButton;

        /// <summary>
        ///     Contains all necessary properties for "MDX Query" toolbox button
        /// </summary>
        public virtual MDXQueryButton MDXQueryButton => Toolbox.MDXQueryButton;

        /// <summary>
        ///     Contains all necessary properties for "Add calculated measure" toolbox button
        /// </summary>
        public virtual AddCalculatedMeasureButton AddCalculatedMeasureButton => Toolbox.AddCalculatedMeasureButton;

        /// <summary>
        ///     Contains all necessary properties for "Show all areas" toolbox button
        /// </summary>
        public virtual AllAreasToolboxButton AllAreasButton => Toolbox.AllAreasButton;

        /// <summary>
        ///     Contains all necessary properties for "Clear layout" toolbox button
        /// </summary>
        public virtual ClearLayoutToolboxButton ClearLayoutButton => Toolbox.ClearLayoutButton;

        /// <summary>
        ///     Contains all necessary properties for "Show data area" toolbox button
        /// </summary>
        public virtual DataAreaToolboxButton DataAreaButton => Toolbox.DataAreaButton;

        /// <summary>
        ///     Contains all necessary properties for "Show data and pivot areas" toolbox button
        /// </summary>
        public virtual PivotAreaToolboxButton PivotAreaButton => Toolbox.PivotAreaButton;

        /// <summary>
        ///     Contains all necessary properties for "Zoom out" toolbox button
        /// </summary>
        public virtual ScaleDecreaseButton ZoomOutButton => Toolbox.ZoomOutButton;

        /// <summary>
        ///     Contains all necessary properties for "Zoom in" toolbox button (only for a OLAP Chart).
        /// </summary>
        public virtual ScaleIncreaseButton ZoomInButton => Toolbox.ZoomInButton;

        /// <summary>
        ///     Contains all necessary properties for "Reset zoom to 100%" toolbox button (only for a OLAP Chart).
        /// </summary>
        public virtual ScaleResetButton ResetZoomButton => Toolbox.ResetZoomButton;

        /// <summary>
        ///     Contains all necessary properties for "Switch to the Grid mode" toolbox button.
        /// </summary>
        public virtual ModeButton ModeButton => Toolbox.ModeButton;

        /// <summary>
        ///     Contains all necessary properties for "Switches the delay pivoting" toolbox button.
        /// </summary>
        public virtual DelayPivotingButton DelayPivotingButton => Toolbox.DelayPivotingButton;

        /// <summary>
        ///     Contains all necessary properties for "Switches the possibility of resizing the cells" toolbox button.
        /// </summary>
        public virtual ResizingButton ResizingButton => Toolbox.ResizingButton;

        /// <summary>
        ///     Contains all necessary properties of toolbox button that difine placement of measures.
        /// </summary>
        public virtual MeasurePlaceToolboxButton MeasurePlaceButton => Toolbox.MeasurePlaceButton;

        /// <summary>
        ///     Occurs when a user presses a toolbox button and to override the standard button
        ///     action if it's necessary.
        /// </summary>
        /// <remarks>
        ///     In the event handler, you can define what button is pressed by examining the
        ///     <see cref="ToolboxItemActionArgs.Item">e.Item</see> property, and prevent
        ///     fulfilling of the standard action by setting the <see cref="ToolboxItemActionArgs.Handled">e.Handled</see> flag to
        ///     True.
        /// </remarks>
        /// <example>
        ///     For example, let's override the standard on-click action of the Save button. The
        ///     default on-click action is saving the current state of the Grid and sending it to
        ///     the user in a XML file. We'll make it save the current Grid state to the database:
        ///     <code>
        /// protected void TOLAPToolbox1_ToolboxItemAction(object sender, ToolboxItemActionArgs e)
        /// {
        ///     if (e.Item is TSaveLayoutToolboxButton)
        ///     {
        ///         string xmlstring = OlapAnalysis1.Serializer.XMLString;
        ///         // do something to save the string to the database
        ///         // ...
        ///         e.Handled = true;
        ///     }
        /// }
        /// </code>
        /// </example>
        public virtual event ToolboxItemActionHandler ToolboxItemAction
        {
            add => Toolbox.ToolboxItemAction += value;
            remove => Toolbox.ToolboxItemAction -= value;
        }

        internal virtual bool OnToolboxItemAction(ToolboxItemActionArgs e)
        {
            return Toolbox.OnToolboxItemAction(e);
        }

        #endregion

        internal Dictionary<string, string> callbackExceptionData = null;
        internal ContextMenu mnu_control;
        internal ContextMenu mnu_cf;

        internal LoadLayoutFileDialog fFileDialog;

        protected JsonDialog _filterconditiondlg = null;

        public virtual Skins Skin { get; set; }

        internal HierarchyEditorStyle fHierarchyEditorStyle = new HierarchyEditorStyle();

        public virtual HierarchyEditorStyle HierarchyEditorStyle => fHierarchyEditorStyle;

        internal jQueryTree fSeparatedTree = null;
        internal bool HasSeparatedPivots = false;
        internal jQueryTree FTree;

        internal jQueryTree _FTree => fSeparatedTree == null ? FTree : fSeparatedTree;

        internal Dimensions FDimensions;

#if DEBUG
        internal AxesLayout FLayout { get; set; }

        private CellSet.CellSet _FCellSet;
        internal CellSet.CellSet FCellSet
        {
            get => _FCellSet;
            set
            {
                if (_FCellSet == value)
                    return;

                _FCellSet = value;
            }
        }
#else
        internal CellSet.CellSet FCellSet;
        internal AxesLayout FLayout;
#endif


        internal Engine.Engine FEngine;
        internal Measures fMeasures;

        //private bool _FActive;
        //internal bool FActive
        //{
        //    get { return _FActive; }
        //    set
        //    {
        //        _FActive = value;
        //    }
        //}

        private const string _TempPath = "Temp";
        internal virtual string TempPath => Cube != null ? Path.Combine(Cube.TempPath, _TempPath) : _TempPath;


        internal List<Hierarchy> FFilteredHierarchies;

        /// <summary>
        ///     ones filtered by using the Filters collection
        /// </summary>
        internal List<Level> FFilteredLevels;

        /// <summary>
        ///     Please use IncrementUpdateCounter() and DecrementUpdateCounter() function's !!!
        /// </summary>
        internal int FUpdateCounter;

        internal virtual CellsetMode CellsetMode { get; }


        /// <summary>
        ///     Fires after the Writeback MDX command with its resulta passed as the parameter to
        ///     the event handler.
        /// </summary>
        public virtual event WritebackHandler OnWriteback;

        public delegate void WritebackHandler(object sender, WritebackArgs e);


        /// <summary>
        ///     If True, the 'Measures' node in the Cube Structure Tree will be expanded by
        ///     default.
        /// </summary>
        public virtual bool ExpandMeasuresNode { get; set; }


        public virtual ExportType DrillthroughExportType { get; set; }

        /// <summary>
        ///     Is raised while rendering Pivot panels, and allows changing text displayed on
        ///     them.
        /// </summary>
        public virtual event RenderPivotEvent OnRenderPivotPanel;

        internal bool RiseOnRenderPivotPanel(RenderPivotArgs args)
        {
            if (OnRenderPivotPanel != null)
            {
                OnRenderPivotPanel(this, args);
                return true;
            }

            return false;
        }

        /// <summary>Fires during each callback operation with Grid.</summary>
        public virtual event EventHandler OnCallback;

        internal void HandleOnCallback()
        {
            OnCallback?.Invoke(this, new EventArgs());
        }

        internal string _ClientMassage;

        /// <summary>The name of the client method that will be used for proceccing errors.</summary>
        /// <remarks>
        ///     By default, the are procecced with the control's own means.
        /// </remarks>
        /// <summary>
        ///     Sets the value for the client message.
        /// </summary>
        /// <remarks>
        ///     Can be called in the control's event handlers.
        /// </remarks>
        /// <param name="Message"></param>
        public void SetClientMessage(string Message)
        {
            _ClientMassage = Message;
        }

        internal void InitClientMessage(JsonResponse response)
        {
            response.clientMessage = _ClientMassage;
            response.messageHandler = MessageHandler;
        }

        internal virtual SeriesType[] ChartsType { get; set; }

        internal virtual JsonResponse MakeCallbackResponse()
        {
            throw new NotImplementedException();
        }

        internal StoredImagesProvider images;

        protected virtual void InitStore()
        {
            images = new StoredImagesProvider(this);
        }

        internal virtual string ImageUrl(string resName)
        {
            return images.ImageUrl(resName, GetType(), TempPath);
        }

        internal virtual void InitSessionData()
        {
            var ssd = new SessionStateData();
            ssd.Init(this);
            SessionState.Write(ssd, SessionKey.olapgrid_sessionstate, UniqueID);
        }

        protected bool IsStored => SessionState.KeyExists(SessionKey.olapgrid_sessionstate, UniqueID);

        internal bool IsRestored = false;

        /// <summary>
        ///     Checks whether the Grid state serialized in the previous web-query is restored.
        ///     If not, restores the state. A given method is used by the RadarCube infrastructure and,
        ///     as a rule, there's no need to call the method by yourself.
        /// </summary>
        /// <remarks>Forcibly restores the Grid state saved in the previous session.</remarks>
        public virtual void EnsureStateRestored()
        {
#if !DEBUG
            try
            {
#endif
            SessionStateData ssd = null;

            if (SessionState == null)
                return;

            if (!SessionState.KeyExists(SessionKey.olapgrid_sessionstate, UniqueID))
                return;

            ssd = new SessionStateData();
            SessionState.ReadStreamedObject(SessionKey.olapgrid_sessionstate, ssd, UniqueID);

            if (ssd != null)
                ssd.Restore(this);
#if !DEBUG
            }
            catch (Exception E)
            {
                callbackException = E;
            }
#endif
        }

        private jQueryTreeNode DoFindNode(jQueryTreeNode n, ref int index, int RequestedIndex)
        {
            if (index == RequestedIndex) return n;
            index++;
            for (var i = 0; i < n.ChildNodes.Count; i++)
            {
                var tn = DoFindNode(n.ChildNodes[i], ref index, RequestedIndex);
                if (tn != null) return tn;
            }
            return null;
        }

        internal jQueryTreeNode FindTreeNode(int RequestedIndex, out bool IsMeasure)
        {
            var index = 0;
            IsMeasure = false;
            var hasKPI = false;
            foreach (var m in Measures)
                if (m.IsKPI)
                {
                    hasKPI = true;
                    break;
                }
            for (var i = 0; i < _FTree.Nodes.Count; i++)
            {
                IsMeasure = i == 0 || hasKPI && i == 1;
                var tn = DoFindNode(_FTree.Nodes[i], ref index, RequestedIndex);
                if (tn != null) return tn;
            }
            return null;
        }

        protected string HTMLPrepare(string src)
        {
            return src == null ? "" : src.Replace("\n", "<br />").Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");
        }

        protected string _CallbackResult;

        private Exception _callbackException;

        internal Exception callbackException
        {
            get => _callbackException;
            set
            {
                _callbackException = value;
                if (value is UnauthorizedAccessException)
                    throw new UnauthorizedAccessException(value.Message, value);
            }
        }

        protected OlapControl(HttpContext context, IHostingEnvironment hosting, IMemoryCache cache) : 
            base(context, hosting, cache)
        {
            DebugLogging.WriteLine("OlapControl.ctor(HttpContext)");

            HideDimensionNameIfPossible = true;
            ExpandMeasuresNode = false;
            PivotingBehavior = PivotingBehavior.Excel2010;

            FDimensions = new Dimensions(this);
            fMeasures = new Measures(this);

            FFilteredHierarchies = new List<Hierarchy>();
            FFilteredLevels = new List<Level>();

            InitStore();
        }

        protected OlapControl(IHttpContextAccessor httpContextAccessor, IHostingEnvironment hosting, IMemoryCache cache) 
            : this(httpContextAccessor.HttpContext, hosting, cache)
        {
            DebugLogging.WriteLine("OlapControl.ctor(IHttpContextAccessor)");
        }

        internal struct TreeHelper
        {
            internal jQueryTreeNode Node;
            internal SortedList<string, jQueryTreeNode> SL;
            internal object obj;

            internal TreeHelper(string Caption)
            {
                Node = new jQueryTreeNode(Caption);
                SL = new SortedList<string, jQueryTreeNode>();
                obj = null;
            }
        }

        internal void FillTree()
        {
            if (HideDimensionNameIfPossible)
            {
                var l = new List<string>();
                foreach (var d in Dimensions)
                {
                    if (d.Hierarchies.Count(item => item.Visible) != 1) continue;
                    var s = d.Hierarchies.First(item => item.Visible).DisplayName;
                    if (l.Contains(s))
                    {
                        HideDimensionNameIfPossible = false;
                        break;
                    }
                    l.Add(s);
                }
            }

            _FTree.Nodes.Clear();
            var n = new jQueryTreeNode(RadarUtils.GetResStr("rsMeasures"));
            if (ExpandMeasuresNode) n.Expanded = true;
            var url_measure = ImageUrl("MeasuresRoot.gif");
            // TODO
            var url_expand =
                ""; // ((StoredImagesProvider)images).ImageUrl("TreeView_Default_Expand.gif", Page, typeof(TreeNode), TempPath);
            var url_noexpand =
                ""; //((StoredImagesProvider)images).ImageUrl("TreeView_Default_NoExpand.gif", Page, typeof(TreeNode), TempPath);
            var url_collapse =
                ""; //((StoredImagesProvider)images).ImageUrl("TreeView_Default_Collapse.gif", Page, typeof(TreeNode), TempPath);
            var url_dimension = ImageUrl("FolderDimen.gif");
            var url_measurecalculated = ImageUrl("MeasureCalculated.gif");
            var
                url_folder =
                    ""; //((StoredImagesProvider)images).ImageUrl("TreeView_XP_Explorer_ParentNode.gif", Page, typeof(TreeNode), TempPath);
            var url_hoAttribute = ImageUrl("hoAttribute.gif");
            var url_namedsets = ImageUrl("NamedSets.gif");
            var url_hoParentChild = ImageUrl("hoParentChild.gif");
            var url_hoUserDefined = ImageUrl("hoUserDefined.gif");
            n.ImageUrl = url_measure;
            _FTree.Nodes.Add(n);
            var sl = new SortedList<string, TreeHelper>();
            var slk = new SortedList<string, Measure>();
            foreach (var m in Measures)
            {
                if (!m.VisibleInTree) continue;
                if (m.IsKPI)
                {
                    if (slk.ContainsKey(m.DisplayName)) continue;
                    slk.Add(m.DisplayName, m);
                }
                else
                {
                    if (string.IsNullOrEmpty(m.DisplayFolder)) continue;
                    var folders = m.DisplayFolder.Split(';');
                    foreach (var folder in folders)
                    {
                        if (sl.ContainsKey(folder)) continue;
                        var h = new TreeHelper(folder);
                        if (ExpandMeasuresNode) h.Node.Expanded = true;
                        h.Node.ImageUrl = url_folder;
                        sl.Add(folder, h);
                    }
                }
            }

            if (slk.Count > 0)
            {
                var url_kpi = ImageUrl("KPI.gif");
                var k = new jQueryTreeNode(RadarUtils.GetResStr("rsKPIs"));
                if (ExpandMeasuresNode) k.Expanded = true;
                k.ImageUrl = url_kpi;
                _FTree.Nodes.Add(k);
                foreach (var m in slk.Values)
                {
                    var n1 = new jQueryTreeNode(MarkMeasuresForTree(m.DisplayName, false), m.UniqueName, url_kpi);
                    n1.ToolTip = m.Description;
                    k.ChildNodes.Add(n1);
                    AddCheckbox(n1, m);
                }
            }

            foreach (var H in sl.Values)
            {
                var folders = H.Node.Text.Split('\\');
                if (folders.Length > 1)
                {
                    var parNode = n;
                    for (var ii = 0; ii < folders.Length; ii++)
                    {
                        var folder = folders[ii];
                        var curNode = FindChildNodes(folder, parNode);
                        if (curNode == null)
                        {
                            if (ii == folders.Length - 1)
                            {
                                curNode = H.Node;
                                curNode.Text = folder;
                            }
                            else
                            {
                                curNode = new jQueryTreeNode(folder);
                                curNode.ImageUrl = url_folder;
                            }
                            parNode.ChildNodes.Add(curNode);
                        }
                        parNode = curNode;
                    }
                }
                else
                {
                    n.ChildNodes.Add(H.Node);
                }
            }
            var sl2 = new SortedList<string, jQueryTreeNode>();

            foreach (var m in Measures)
            {
                if (!m.VisibleInTree) continue;
                if (m.IsKPI) continue;
                if (string.IsNullOrEmpty(m.DisplayFolder))
                {
                    var n1 = new jQueryTreeNode(MarkMeasuresForTree(m.DisplayName, false));
                    n1.ImageUrl = m.AggregateFunction == OlapFunction.stCalculated
                        ? url_measurecalculated
                        : url_measure;
                    n1.Value = m.UniqueName;
                    n1.ToolTip = m.Description;
                    sl2.Add(m.DisplayName, n1);
                    AddCheckbox(n1, m);
                }
                else
                {
                    var folders = m.DisplayFolder.Split(';');
                    foreach (var folder in folders)
                    {
                        TreeHelper h;
                        sl.TryGetValue(folder, out h);
                        var n1 = new jQueryTreeNode(MarkMeasuresForTree(m.DisplayName, false));
                        n1.ImageUrl = m.AggregateFunction == OlapFunction.stCalculated
                            ? url_measurecalculated
                            : url_measure;
                        n1.Value = m.UniqueName;
                        n1.ToolTip = m.Description;
                        h.SL.Add(m.DisplayName, n1);
                        AddCheckbox(n1, m);
                    }
                }
            }
            foreach (var H in sl.Values)
            foreach (var tn in H.SL.Values) H.Node.ChildNodes.Add(tn);
            foreach (var tn in sl2.Values) n.ChildNodes.Add(tn);

            var dims = new SortedList<string, Dimension>();
            foreach (var d in Dimensions)
            {
                if (!d.Visible) continue;
                dims.Add(d.DisplayName, d);
            }
            foreach (var d in dims.Values)
            {
                if (HideDimensionNameIfPossible && d.Hierarchies.Count(item => item.Visible) < 2) continue;
                var tnd = new jQueryTreeNode(d.DisplayName, "", url_dimension);

                tnd.ToolTip = d.Description;
                if (ExpandDimensionsNode) tnd.Expanded = true;
                _FTree.Nodes.Add(tnd);

                var df = new SortedList<string, TreeHelper>();
                foreach (var h in d.Hierarchies)
                {
                    if (!h.Visible) continue;
                    if (string.IsNullOrEmpty(h.CubeHierarchy.DisplayFolder)) continue;
                    if (df.ContainsKey(h.CubeHierarchy.DisplayFolder)) continue;
                    var th = new TreeHelper(h.CubeHierarchy.DisplayFolder);
                    if (ExpandDimensionsNode) th.Node.Expanded = true;
                    th.Node.ImageUrl = url_folder;
                    df.Add(h.CubeHierarchy.DisplayFolder, th);
                }


                var sl2_ = new SortedList<string, jQueryTreeNode>();
                foreach (var h in d.Hierarchies)
                {
                    if (!h.Visible) continue;
                    var tn = new jQueryTreeNode(MarkHierarchiesForTree(h.DisplayName, false));
                    AddCheckbox(tn, h);
                    tn.ToolTip = h.Description;
                    if (h.Origin == HierarchyOrigin.hoAttribute) tn.ImageUrl = url_hoAttribute;
                    if (h.Origin == HierarchyOrigin.hoParentChild) tn.ImageUrl = url_hoParentChild;
                    if (h.Origin == HierarchyOrigin.hoUserDefined) tn.ImageUrl = url_hoUserDefined;
                    if (h.Origin == HierarchyOrigin.hoNamedSet) tn.ImageUrl = url_namedsets;
                    tn.Value = h.UniqueName;
                    if (string.IsNullOrEmpty(h.CubeHierarchy.DisplayFolder))
                    {
                        sl2_.Add(h.DisplayName, tn);
                    }
                    else
                    {
                        TreeHelper dfitem;
                        df.TryGetValue(h.CubeHierarchy.DisplayFolder, out dfitem);
                        dfitem.SL.Add(h.DisplayName, tn);
                    }
                }

                foreach (var th in df.Values)
                {
                    tnd.ChildNodes.Add(th.Node);
                    foreach (var tn in th.SL.Values) th.Node.ChildNodes.Add(tn);
                }
                foreach (var tn in sl2_.Values) tnd.ChildNodes.Add(tn);
            }

            if (HideDimensionNameIfPossible)
            {
                var sl3 = new SortedDictionary<string, jQueryTreeNode>();
                foreach (var d in dims.Values)
                {
                    if (d.Hierarchies.Count(item => item.Visible) != 1) continue;
                    var h = d.Hierarchies.First(item => item.Visible);
                    var tn = new jQueryTreeNode(MarkHierarchiesForTree(h.DisplayName, false), h.UniqueName);
                    AddCheckbox(tn, h);
                    tn.ToolTip = h.Description;
                    if (h.Origin == HierarchyOrigin.hoAttribute) tn.ImageUrl = url_hoAttribute;
                    if (h.Origin == HierarchyOrigin.hoParentChild) tn.ImageUrl = url_hoParentChild;
                    if (h.Origin == HierarchyOrigin.hoUserDefined) tn.ImageUrl = url_hoUserDefined;
                    sl3.Add(h.DisplayName, tn);
                }
                foreach (var tn in sl3.Values) _FTree.Nodes.Add(tn);
            }
        }

        private void AddCheckbox(jQueryTreeNode n, Measure m)
        {
            n.Draggable = true;
            if (UseCheckboxesInTree)
            {
                n.ShowCheckBox = true;
                n.Checked = m.Visible;
            }
        }

        private void AddCheckbox(jQueryTreeNode n, Hierarchy h)
        {
            n.Draggable = true;
            if (UseCheckboxesInTree)
            {
                n.ShowCheckBox = true;
                n.Checked = (h.State & HierarchyState.hsActive) == HierarchyState.hsActive;
            }
        }

        private jQueryTreeNode FindChildNodes(string name, jQueryTreeNode parent)
        {
            foreach (var n in parent.ChildNodes)
                if (n.Text == name) return n;
            return null;
        }

        internal string MarkMeasuresForTree(string displayName, bool IsMoveCursor)
        {
            var s = "";
            if (IsMoveCursor)
                s = " style=\"cursor: move; white-space: nowrap; display: block\"";
            return "<span ms" + s + ">" + displayName + "</span>";
        }

        internal string MarkHierarchiesForTree(string displayName, bool IsMoveCursor)
        {
            var s = "";
            if (IsMoveCursor)
                s = " style=\"cursor: move; white-space: nowrap; display: block\"";
            return "<span hr" + s + ">" + displayName + "</span>";
        }

        #region Context menu

        protected GenericMenu mnu = new GenericMenu();

        internal GenericMenu GenericMnu => mnu;

        internal void RenderDataPopup(IDataCell dc)
        {
            if (dc.Address == null) return;
            var receivedArgument = dc.StartColumn.ToString() + '|' + dc.StartRow;
            GenericMenuItem MI = null;
            GenericMenuItem MI1;

            //string url_checked = "";

            //if (IsMvc == false)
            //    url_checked = ResolveUrl(Page.ClientScript.GetWebResourceUrl(typeof(System.Web.UI.WebControls.TreeView),
            //        "WebPartMenu_Check.gif"));

            var m = dc.Address.Measure;
            if (m != null && m.AggregateFunction == OlapFunction.stCalculated && !string.IsNullOrEmpty(m.Expression))
            {
                MI = new GenericMenuItem(GenericMenuActionType.ShowDialog,
                    RadarUtils.GetResStr("rsEditCalcMeasure"), ImageUrl("Calculated_edit.gif"),
                    "editcalculatedmeasure|" + m.UniqueName);
                mnu.Add(MI);

                MI = new GenericMenuItem(GenericMenuActionType.Postback, RadarUtils.GetResStr("rsDeleteCalcMeasure"),
                    ImageUrl("Calculated_delete.gif"), "deletecalculatedmeasure|" + m.UniqueName);
                mnu.Add(MI);
            }

            foreach (var ca in dc.CubeActions)
            {
                if (ca.ActionType == CubeActionType.caURL)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RedirectTo, ca.Caption,
                        string.Empty, ca.Expression);
                    MI.TargetPage = "_blank";
                    mnu.Add(MI);
                }
                if (ca.ActionType == CubeActionType.csDrillthrough || ca.ActionType == CubeActionType.caRowset)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.ShowDialog, ca.Caption, ImageUrl("dt.png"),
                        "dodrillthrough|" + dc.CubeActions.IndexOf(ca) + "|" + dc.StartColumn + "|" + dc.StartRow);
                    mnu.Add(MI);
                }
            }

            mnu.AddSeparator();

            MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                RadarUtils.GetResStr("rspSortAscending"), string.Empty,
                "valuesort|" + dc.StartColumn + "|" +
                (int) ValueSortingDirection.sdAscending);
            mnu.Add(MI);

            MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                RadarUtils.GetResStr("rspSortDescending"),
                string.Empty, "valuesort|" + dc.StartColumn + "|" +
                              (int) ValueSortingDirection.sdDescending);
            mnu.Add(MI);

            mnu.AddSeparator();

            if (dc.RowMember != null)
            {
                var rm = dc.RowMember;
                rm = rm.HierarchyMemberCell;
                if (rm != null && rm.Member != null)
                {
                    string Mdxname = null;
                    if (rm.Member.CubeMember != null && rm.Member.CubeMember.FMDXLevelIndex >= 0)
                        Mdxname = rm.Member.CubeMember.ParentLevel.Hierarchy.FMDXLevelNames[
                            rm.Member.CubeMember.FMDXLevelIndex];
                    FillFilterMenu(mnu, rm.Member.Level, Mdxname, m, "");
                }
            }

            mnu.AddSeparator();

            var ss1 = "javascript:{RadarSoft.$('#" + ClientID + "').data('grid').modifyComment('" + receivedArgument +
                      "','" +
                      RadarUtils.GetResStr("rsCommentCellPrompt") + "','" +
                      dc.Comment + "')}";
            MI = new GenericMenuItem(GenericMenuActionType.RedirectTo,
                RadarUtils.GetResStr("rsCommentCell"),
                ImageUrl("EditComment.gif"), ss1);
            mnu.Add(MI);

            if (m != null)
            {
                MI = new GenericMenuItem(RadarUtils.GetResStr("repShowAs"));
                mnu.Add(MI);

                foreach (var mm in m.ShowModes)
                {
                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        mm.Caption, mm.Visible,
                        "measureshowmode|" + m.UniqueName + "|" + mm.Mode + "|" + mm.Caption);
                    MI.ChildItems.Add(MI1);
                }
                CreateTIList(dc);
            }

            FillDrillthroughMenu(dc, mnu);

            FillWritebackMenu(dc, mnu);
        }

        internal void FillDrillthroughMenu(IDataCell dc, GenericMenu mnu)
        {
            if (Cube.GetProductID() == RadarUtils.rsAspNetDesktop)
            {
                mnu.AddSeparator();

                var MI = new GenericMenuItem(GenericMenuActionType.ShowDialog,
                    RadarUtils.GetResStr("rsDrillthrough"),
                    ImageUrl("dt.png"), "dodrillthrough2|" + dc.StartColumn + "|" + dc.StartRow);
                mnu.Add(MI);
            }
        }

        internal virtual void FillWritebackMenu(IDataCell dc, GenericMenu mnu)
        {
        }

        internal void FillFilterMenu(GenericMenu mnu, Level level, string MDXLevelName,
            Measure measure, string extraCommand)
        {
            var H = level.Hierarchy;
            GenericMenuItem MI, MI1;

            mnu.AddSeparator();

            if (H.AllowFilter)
            {
                if (MDXLevelName == null && level.Hierarchy.Origin == HierarchyOrigin.hoParentChild)
                    if (level.Hierarchy.CubeHierarchy.FMDXLevelNames.Count > 0)
                        MDXLevelName = level.Hierarchy.CubeHierarchy.FMDXLevelNames[0];

                if (H.Filtered || H.FilteredByLevelFilters)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repResetFilter"),
                        ImageUrl("FiltrClear.png"),
                        "resetfilter|" + level.UniqueName);
                    MI.ExtraCommand = extraCommand;
                    mnu.Add(MI);
                }

                OlapFilterCondition[] caption_filters;
                GenericMenuItem MIGroup = null;
                var menuvalue = (int) OlapFilterCondition.fcEqual;
                if (level.Hierarchy.IsDate)
                {
                    caption_filters = new[]
                                      {
                                          OlapFilterCondition.fcEqual,
                                          OlapFilterCondition.fcNotEqual, OlapFilterCondition.fcLess,
                                          OlapFilterCondition.fcNotLess, OlapFilterCondition.fcGreater,
                                          OlapFilterCondition.fcNotGreater, OlapFilterCondition.fcBetween,
                                          OlapFilterCondition.fcNotBetween
                                      };

                    MIGroup = null;
                    menuvalue = (int) OlapFilterCondition.fcEqual;
                    foreach (var fc in caption_filters)
                    {
                        var ff = level.Filter;
                        if (ff == null || ff.FilterCondition != fc || ff.FilterType != OlapFilterType.ftOnDate)
                        {
                            ff = new CellSet.Filter(level, OlapFilterType.ftOnDate, null,
                                fc, "", null);
                            ff.MDXLevelName = MDXLevelName;
                        }
                        if (Cube.IsFilterAllowed(ff))
                        {
                            if (MIGroup == null)
                            {
                                MIGroup = new GenericMenuItem(RadarUtils.GetResStr("rsfcFiltersOnDate"));
                                MIGroup.ImageUrl = ImageUrl("filtr_clock.gif");
                                mnu.Add(MIGroup);

                                MIGroup.IsChecked = level.Filter != null &&
                                                    level.Filter.FilterType == OlapFilterType.ftOnDate;
                            }

                            var mv1 = (int) fc;
                            if (mv1 - menuvalue > 1) MIGroup.ChildItems.AddSeparator();
                            menuvalue = mv1;

                            MI1 = new GenericMenuItem(GenericMenuActionType.ShowDialog,
                                RadarUtils.GetResStr("rs" + ff.FilterCondition) + "...",
                                "",
                                "showfiltersettings|" + level.UniqueName + "|" + OlapFilterType.ftOnDate + "|" + fc);
                            MI1.ExtraCommand = extraCommand;
                            MIGroup.ChildItems.Add(MI1);
                            MI1.IsChecked = level.Filter == ff;
                        }
                    }
                }

                caption_filters = new[]
                                  {
                                      OlapFilterCondition.fcEqual,
                                      OlapFilterCondition.fcNotEqual, OlapFilterCondition.fcStartsWith,
                                      OlapFilterCondition.fcNotStartsWith, OlapFilterCondition.fcEndsWith,
                                      OlapFilterCondition.fcNotEndsWith, OlapFilterCondition.fcContains,
                                      OlapFilterCondition.fcNotContains, OlapFilterCondition.fcLess,
                                      OlapFilterCondition.fcNotLess, OlapFilterCondition.fcGreater,
                                      OlapFilterCondition.fcNotGreater, OlapFilterCondition.fcBetween,
                                      OlapFilterCondition.fcNotBetween
                                  };

                MIGroup = null;
                menuvalue = (int) OlapFilterCondition.fcEqual;
                foreach (var fc in caption_filters)
                {
                    var ff = level.Filter;
                    if (ff == null || ff.FilterCondition != fc || ff.FilterType != OlapFilterType.ftOnCaption)
                    {
                        ff = new CellSet.Filter(level, OlapFilterType.ftOnCaption, null,
                            fc, "", null);
                        ff.MDXLevelName = MDXLevelName;
                    }
                    if (Cube.IsFilterAllowed(ff))
                    {
                        if (MIGroup == null)
                        {
                            MIGroup = new GenericMenuItem(RadarUtils.GetResStr("rsfcFiltersOnCaption"));
                            MIGroup.ImageUrl = ImageUrl("filtr_caption.gif");
                            mnu.Add(MIGroup);

                            MIGroup.IsChecked = level.Filter != null &&
                                                level.Filter.FilterType == OlapFilterType.ftOnCaption;
                        }

                        var mv1 = (int) fc;
                        if (mv1 - menuvalue > 1) MIGroup.ChildItems.AddSeparator();
                        menuvalue = mv1;

                        MI1 = new GenericMenuItem(GenericMenuActionType.ShowDialog,
                            RadarUtils.GetResStr("rs" + ff.FilterCondition) + "...",
                            "", "showfiltersettings|" + level.UniqueName + "|" + OlapFilterType.ftOnCaption + "|" + fc);
                        MI1.ExtraCommand = extraCommand;
                        MIGroup.ChildItems.Add(MI1);
                        MI1.IsChecked = level.Filter == ff;
                    }
                }

                OlapFilterCondition[] value_filters =
                {
                    OlapFilterCondition.fcEqual,
                    OlapFilterCondition.fcNotEqual, OlapFilterCondition.fcLess,
                    OlapFilterCondition.fcNotLess, OlapFilterCondition.fcGreater,
                    OlapFilterCondition.fcNotGreater,
                    OlapFilterCondition.fcBetween,
                    OlapFilterCondition.fcNotBetween,
                    OlapFilterCondition.fcFirstTen
                };
                MIGroup = null;
                menuvalue = (int) OlapFilterCondition.fcEqual;
                foreach (var fc in value_filters)
                {
                    var ff = level.Filter;
                    if (ff == null || ff.FilterCondition != fc || ff.FilterType != OlapFilterType.ftOnValue)
                    {
                        ff = new CellSet.Filter(level, OlapFilterType.ftOnValue, measure,
                            fc, "", null);
                        ff.MDXLevelName = MDXLevelName;
                    }
                    if (Cube.IsFilterAllowed(ff))
                    {
                        if (MIGroup == null)
                        {
                            MIGroup = new GenericMenuItem(RadarUtils.GetResStr("rsfcFiltersOnValue"));
                            MIGroup.ImageUrl = ImageUrl("filtr_calc.gif");
                            mnu.Add(MIGroup);

                            MIGroup.IsChecked = level.Filter != null &&
                                                level.Filter.FilterType == OlapFilterType.ftOnValue;
                        }

                        var mv1 = (int) fc;
                        if (mv1 - menuvalue > 1) MIGroup.ChildItems.AddSeparator();
                        menuvalue = mv1;

                        MI1 = new GenericMenuItem(GenericMenuActionType.ShowDialog,
                            RadarUtils.GetResStr("rs" + ff.FilterCondition) + "...",
                            "", "showfiltersettings|" + level.UniqueName + "|" + OlapFilterType.ftOnValue + "|" + fc);
                        MI1.ExtraCommand = extraCommand;
                        MIGroup.ChildItems.Add(MI1);
                        MI1.IsChecked = level.Filter == ff;
                    }
                }
            }
        }

        private void CreateTIList(IDataCell dc)
        {
            var groups = new Dictionary<string, GenericMenuItem>();
            var a = dc.Address;
            for (var i = 0; i < a.LevelsCount; i++)
            {
                var h = a.Levels(i).Hierarchy;
                for (var j = 0; j < h.Intelligence.Count; j++)
                {
                    var ti = h.Intelligence[j];
                    GenericMenuItem MI;
                    if (!groups.TryGetValue(ti.fIntelligenceGroup, out MI))
                    {
                        MI = new GenericMenuItem(ti.fIntelligenceGroup);
                        groups.Add(ti.fIntelligenceGroup, MI);
                        mnu.Add(MI);
                    }

                    var b = ti.FindShowMode(a.Measure) != null;
                    GenericMenuItem MI1;
                    if (b)
                    {
                        MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                            ti.fDisplayName, "",
                            "remintelligence|" + j + "|" + h.UniqueName + "|" + a.Measure.UniqueName);
                        MI1.IsChecked = true;
                    }
                    else
                    {
                        MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                            ti.fDisplayName, "",
                            "addintelligence|" + j + "|" + h.UniqueName + "|" + a.Measure.UniqueName);
                    }
                    MI.ChildItems.Add(MI1);
                }
            }
        }

        /// <summary>Allows altering the contents of the standard context menus.</summary>
        /// <remarks>
        ///     <para>To handle a click on a context menu item on the server side you need:</para>
        ///     <para>
        ///         1. To assign a unique value to the Value property of the specified menu
        ///         item;
        ///     </para>
        ///     <para>
        ///         2. To write the OnContextMenuClick event handler where required actions will
        ///         be realized.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     For example, export the contents of the Grid into an Excel file when clicking on
        ///     the appropriate context menu item, and send it to a client. That is done with the
        ///     following code:
        ///     <code lang="CS" title="[New Example]">
        /// protected void OlapAnalysis1_OnShowContextMenu(OlapGrid sender, ShowContextMenuEventArgs e)
        /// {
        ///       MenuItem mi = new MenuItem("Export to MS Excel", "ExportExcel", "~/Images/e_xls.gif");
        ///       e.ContextMenu.Items.Add(mi);
        /// }
        ///  
        /// protected void OlapAnalysis1_OnContextMenuClick(OlapGrid sender, ContextMenuClickArgs e)
        /// {
        ///       if (e.MenuItemValue == "ExportExcel")
        ///       {
        ///             Response.ContentType = "APPLICATION/OCTET-STREAM";
        ///             Response.AppendHeader("Content-Disposition", "Attachment; Filename=RadarCubeSlice.xls");
        ///             OlapAnalysis1.ExportToXMLSpreadsheet(Response.OutputStream);
        ///             Response.Flush();
        ///             Response.End();
        ///       }
        /// }
        /// </code>
        ///     <code title="[New Example]">
        /// Protected Sub OlapAnalysis1_OnShowContextMenu(ByVal sender As OlapGrid, ByVal e As ShowContextMenuEventArgs)
        ///       Dim mi As New MenuItem("Export to MS Excel", "ExportExcel", "~/Images/e_xls.gif")
        ///       e.ContextMenu.Items.Add(mi)
        /// End Sub
        ///  
        /// Protected Sub OlapAnalysis1_OnContextMenuClick(ByVal sender As OlapGrid, ByVal e As ContextMenuClickArgs)
        ///       If (e.MenuItemValue Is "ExportExcel") Then
        ///             Response.ContentType = "APPLICATION/OCTET-STREAM"
        ///             Response.AppendHeader("Content-Disposition", "Attachment; Filename=RadarCubeSlice.xls")
        ///             OlapAnalysis1.ExportToXMLSpreadsheet(Response.OutputStream)
        ///             Response.Flush
        ///             Response.End 
        ///       End If
        /// End Sub
        /// </code>
        /// </example>
        public virtual event ShowContextMenuEventHandler OnShowContextMenu;

        internal void HandleOnShowContextMenu(ICell c, IDescriptionable dim, ContextMenu mnu)
        {
            if (OnShowContextMenu != null)
            {
                var E = new ShowContextMenuEventArgs(c, dim, mnu);
                OnShowContextMenu(this, E);
            }
        }

        /// <summary>
        ///     The client side script executed by the callback procedure that updates the OLAP
        ///     Grid data
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The property is typically used for synchronizing various components of the
        ///         web-form while callbacking.
        ///     </para>
        ///     <para>
        ///         For example, RadarCube Grid and Dundas Chart are located on the same Web-page
        ///         which displays the Grid data in a graphical form. To reflect the Grid changes in
        ///         the Chart, you should add the following string to the OnLoad event handler of the
        ///         page:
        ///     </para>
        ///     <para>
        ///         OlapAnalysis1.ClientCallbackFunction =
        ///         Chart1.CallbackManager.GetCallbackMethodReference("", "");
        ///     </para>
        /// </remarks>
        public virtual string ClientCallbackFunction { get; set; }

        internal class KeyIndexComparer : Comparer<KeyValuePair<object, int>>
        {
            public override int Compare(KeyValuePair<object, int> x, KeyValuePair<object, int> y)
            {
                var ic = x.Key as IComparable;
                var i = ic.CompareTo(y.Key);
                if (i != 0) return i;
                if (x.Value < y.Value) return -1;
                if (x.Value > y.Value) return 1;
                return 0;
            }
        }

        internal virtual bool HandlePivotCallback(string eventArgument, ICell cell)
        {
            callbackData = CallbackData.PivotAndData;
            var args = eventArgument.Split('|');

            var list = new HashSet<string>();

            if (args[0] == "deletelevel")
            {
                var lc = cell as ILevelCell;
                IDescriptionable item = lc.Level;
                if (item is Level)
                {
                    var l = (Level) item;
                    if (l.Hierarchy.Levels[0] == l && l.Grid.CellsetMode == CellsetMode.cmGrid)
                    {
                        item = l.Hierarchy;
                    }
                    else
                    {
                        if (l.Grid.CellsetMode == CellsetMode.cmChart)
                        {
                            l.Visible = false;
                        }
                        else
                        {
                            l = l.Hierarchy.Levels[l.Hierarchy.Levels.IndexOf(l) - 1];
                            l.Grid.CellSet.CollapseAllNodes(l);
                        }
                    }
                }

                if (item is Hierarchy)
                {
                    var h = (Hierarchy) item;
                    h.Dimension.Grid.PivotingOut(h,
                        lc.Level.Grid.AxesLayout.ColumnAxis.Contains(lc.Level.Hierarchy)
                            ? LayoutArea.laColumn
                            : LayoutArea.laRow);
                }
                return true;
            }

            if (args[0] == "activatecube")
            {
                Cube.Active = true;
                callbackData = CallbackData.PivotAndData;
                return true;
            }

            if (args[0] == "deactivatecube")
            {
                Cube.Active = false;
                callbackData = CallbackData.Nothing;
                return true;
            }

            if (args[0] == "applyconnection")
            {
                var p = Cube as IMOlapCube;
                if (p != null)
                    p.Activate(args[1], args[2], args[3], args[4]);
                return true;
            }

            if (args[0] == "clearlayout")
            {
                AxesLayout.Clear();
                return true;
            }

            if (args[0] == "hidelevel")
            {
                var l = Dimensions.FindLevel(args[1]);
                if (l != null) l.Visible = false;
                return true;
            }

            if (args[0] == "hidelevelsbelow")
            {
                var l = Dimensions.FindLevel(args[1]);
                if (l != null)
                {
                    var h = l.Hierarchy;
                    if (FLayout.fColorAxisItem == h || FLayout.fSizeAxisItem == h || FLayout.fShapeAxisItem == h ||
                        FLayout.fDetailsAxis.Contains(h))
                    {
                        if (l.Index > 0)
                        {
                            h.Levels[l.Index - 1].Visible = true;
                            for (var i = l.Index + 1; i < l.Hierarchy.Levels.Count; i++)
                                l.Hierarchy.Levels[i].FVisible = false;
                            l.Visible = false;
                        }
                    }
                    else
                    {
                        if (l.Visible)
                        {
                            for (var i = l.Index + 1; i < l.Hierarchy.Levels.Count; i++)
                                l.Hierarchy.Levels[i].FVisible = false;
                            l.Visible = false;
                        }
                    }
                }
                return true;
            }

            if (args[0] == "custommenuaction")
            {
                DoContextMenuClick(args[1], cell);
                return true;
            }

            if (args[0] == "showlevel")
            {
                var l = Dimensions.FindLevel(args[1]);
                if (l != null) l.Visible = true;
                return true;
            }

            if (args[0] == "shownextlevel")
            {
                var l = Dimensions.FindLevel(args[1]);
                if (l != null)
                {
                    if (l.Index < l.Hierarchy.Levels.Count - 1)
                        l.Hierarchy.Levels[l.Index + 1].Visible = true;
                    var h = l.Hierarchy;
                    if (FLayout.fColorAxisItem == h || FLayout.fSizeAxisItem == h || FLayout.fShapeAxisItem == h ||
                        FLayout.fDetailsAxis.Contains(h))
                        l.Visible = false;
                }
                return true;
            }

            if (args[0] == "hidethis")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[1]), Convert.ToInt32(args[2])) as IMemberCell;
                c.Member.Visible = false;
                return true;
            }

            if (args[0] == "hidethis2")
            {
                var h = Dimensions.FindHierarchy(args[1]);
                var m = h.FindMemberByUniqueName(args[2]);
                m.Visible = false;
                return true;
            }

            if (args[0] == "hideexcept")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[1]), Convert.ToInt32(args[2])) as IMemberCell;
                var M = c.Member;
                M.Level.Hierarchy.BeginUpdate();
                M.Level.Hierarchy.UnfetchedMembersVisible = false;
                foreach (var m in M.Level.Hierarchy.Levels[0].Members)
                    m.Visible = false;
                M.Visible = true;
                M.Level.Hierarchy.EndUpdate();
                return true;
            }

            if (args[0] == "hideexcept2")
            {
                var h = Dimensions.FindHierarchy(args[1]);
                var M = h.FindMemberByUniqueName(args[2]);
                h.BeginUpdate();
                h.UnfetchedMembersVisible = false;
                foreach (var m in h.Levels[0].Members)
                    m.Visible = false;
                M.Visible = true;
                h.EndUpdate();
                return true;
            }

            if (args[0] == "hideabove")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[1]), Convert.ToInt32(args[2])) as IMemberCell;
                var M = c.Member;
                M.Level.Hierarchy.BeginUpdate();
                var CS = c.PrevMember;
                while (CS != null)
                {
                    CS.Member.Visible = false;
                    CS = CS.PrevMember;
                }
                M.Level.Hierarchy.EndUpdate();
                return true;
            }

            if (args[0] == "hidebelow")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[1]), Convert.ToInt32(args[2])) as IMemberCell;
                var M = c.Member;
                M.Level.Hierarchy.BeginUpdate();
                var CS = c.NextMember;
                while (CS != null)
                {
                    CS.Member.Visible = false;
                    CS = CS.NextMember;
                }
                M.Level.Hierarchy.EndUpdate();
                return true;
            }

            if (args[0] == "showtop")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[2]), Convert.ToInt32(args[3])) as IMemberCell;
                if (c.Address.Measure == null) return true;
                var _value = args[1];
                if (_value[_value.Length - 1] == '%')
                {
                    _value = _value.Substring(0, _value.Length - 1);
                    var sl = new SortedList<KeyValuePair<object, int>, Member>(new KeyIndexComparer());
                    var l = new List<Member>();
                    double vsum = 0;
                    for (var i = 0; i < c.SiblingsCount; i++)
                    {
                        var mc1 = c.Siblings(i);
                        object v;
                        if (!Engine.GetCellValue(mc1.Address, out v))
                        {
                            l.Add(mc1.Member);
                        }
                        else
                        {
                            sl.Add(new KeyValuePair<object, int>(v, i), mc1.Member);
                            if (!RadarUtils.IsNumeric(v))
                                throw new
                                    Exception(RadarUtils.GetResStr("repWrongDataToFilter"));
                            vsum += Convert.ToDouble(v);
                        }
                    }
                    vsum = vsum * Convert.ToInt32(_value) / 100;
                    var vsum2 = Convert.ToDouble(sl.Keys[sl.Count - 1].Key);
                    c.Member.Level.Hierarchy.BeginUpdate();
                    for (var i = sl.Count - 2; i >= 0; i--)
                    {
                        sl.Values[i].Visible = vsum2 <= vsum;
                        vsum2 += Convert.ToDouble(sl.Keys[i].Key);
                    }
                    foreach (var m in l) m.Visible = false;
                    c.Member.Level.Hierarchy.EndUpdate();
                }
                else
                {
                    var sl = new SortedList<KeyValuePair<object, int>, Member>(new KeyIndexComparer());
                    var l = new List<Member>();
                    for (var i = 0; i < c.SiblingsCount; i++)
                    {
                        var mc1 = c.Siblings(i);
                        object v;
                        if (!Engine.GetCellValue(mc1.Address, out v))
                            l.Add(mc1.Member);
                        else sl.Add(new KeyValuePair<object, int>(v, i), mc1.Member);
                    }
                    c.Member.Level.Hierarchy.BeginUpdate();
                    for (var i = 0; i < sl.Count - Convert.ToInt32(_value); i++) sl.Values[i].Visible = false;
                    for (var i = l.Count - 1; i >= Math.Max(0, Convert.ToInt32(_value) - sl.Count); i--)
                        l[i].Visible = false;
                    c.Member.Level.Hierarchy.EndUpdate();
                }
                return true;
            }

            if (args[0] == "showbottom")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[2]), Convert.ToInt32(args[3])) as IMemberCell;
                if (c.Address == null || c.Address.Measure == null)
                    return true;
                var _value = args[1];
                if (_value[_value.Length - 1] == '%')
                {
                    _value = _value.Substring(0, _value.Length - 1);
                    var sl = new SortedList<KeyValuePair<object, int>, Member>(new KeyIndexComparer());
                    var l = new List<Member>();
                    double vsum = 0;
                    for (var i = 0; i < c.SiblingsCount; i++)
                    {
                        var mc1 = c.Siblings(i);
                        object v;
                        if (!Engine.GetCellValue(mc1.Address, out v))
                        {
                            l.Add(mc1.Member);
                        }
                        else
                        {
                            sl.Add(new KeyValuePair<object, int>(v, i), mc1.Member);
                            if (!RadarUtils.IsNumeric(v))
                                throw new
                                    Exception(RadarUtils.GetResStr("repWrongDataToFilter"));
                            vsum += Convert.ToDouble(v);
                        }
                    }
                    vsum = vsum * Convert.ToInt32(_value) / 100;
                    var vsum2 = Convert.ToDouble(sl.Keys[0].Key);
                    c.Member.Level.Hierarchy.BeginUpdate();
                    for (var i = 1; i < sl.Count; i++)
                    {
                        sl.Values[i].Visible = vsum2 <= vsum;
                        vsum2 += Convert.ToDouble(sl.Keys[i].Key);
                    }
                    foreach (var m in l) m.Visible = false;
                    c.Member.Level.Hierarchy.EndUpdate();
                }
                else
                {
                    var sl = new SortedList<KeyValuePair<object, int>, Member>(new KeyIndexComparer());
                    var l = new List<Member>();
                    for (var i = 0; i < c.SiblingsCount; i++)
                    {
                        var mc1 = c.Siblings(i);
                        object v;
                        if (!Engine.GetCellValue(mc1.Address, out v))
                            l.Add(mc1.Member);
                        else sl.Add(new KeyValuePair<object, int>(v, i), mc1.Member);
                    }
                    c.Member.Level.Hierarchy.BeginUpdate();
                    for (var i = Convert.ToInt32(_value); i < sl.Count; i++) sl.Values[i].Visible = false;
                    foreach (var m in l) m.Visible = false;
                    c.Member.Level.Hierarchy.EndUpdate();
                }
                return true;
            }

            if (args[0] == "hidemeasure")
            {
                var m = Measures[args[1]];
                m.Visible = false;
                return true;
            }

            if (args[0] == "resetfilter")
            {
                var l = Dimensions.FindLevel(args[1]);
                if (l != null)
                {
                    l.Hierarchy.ResetFilter();

                    if (AxesLayout.PageAxis.Contains(l.Hierarchy))
                        PivotingOut(l.Hierarchy, LayoutArea.laPage);
                }
                else
                {
                    var h = Dimensions.FindHierarchy(args[1]);
                    if (h != null)
                    {
                        h.ResetFilter();

                        if (AxesLayout.PageAxis.Contains(h))
                            PivotingOut(h, LayoutArea.laPage);
                    }
                    var m = Measures.Find(args[1]);
                    if (m != null)
                        m.Filter = null;
                    ApplyChanges();
                }
                return true;
            }

            if (args[0] == "pivotingout")
            {
                var m = Measures.Find(args[1]);
                if (m != null)
                {
                    m.Visible = false;
                    return true;
                }
                var h = Dimensions.FindHierarchy(args[1]);
                if (h == null)
                {
                    var l = Dimensions.FindLevel(args[1]);
                    if (l != null) h = l.Hierarchy;
                }
                if (h != null)
                {
                    LayoutArea? la = null;
                    if (args.Length > 2 && args[2].StartsWith("la"))
                        la = RadarUtils.ParseLayoutArea(args[2]);
                    PivotingOut(h, la);
                    return true;
                }
            }

            if (args[0] == "pivoting")
            {
                if (args[1] == null)
                    throw new Exception("args[1] is null");

                if (args[1].StartsWith("node_"))
                {
                    callbackData = CallbackData.Nothing;
                    return false;
                }
                if (Dimensions == null)
                    throw new Exception("Dimensions is null");

                var h = Dimensions.FindHierarchy(args[1]);
                if (h == null)
                {
                    var l = Dimensions.FindLevel(args[1]);
                    if (l != null) h = l.Hierarchy;
                }
                if (h != null)
                {
                    var l = args.Length >= 5 ? RadarUtils.ParseLayoutArea(args[4]) : null;

                    var da = RadarUtils.ParseLayoutArea(args[2], h.CubeHierarchy.Dimension.IsTimeDimension);

                    Pivoting(h, da.HasValue ? da.Value : LayoutArea.laTree, Convert.ToInt16(args[3]), l);
                    if (CalcBehavior(da.HasValue ? da.Value : LayoutArea.laTree) == PivotingBehavior.Excel2010)
                        CellSet.ExpandAllHierarchies(PossibleDrillActions.esNextHierarchy, da == LayoutArea.laColumn,
                            da == LayoutArea.laRow);
                    callbackData = CallbackData.PivotAndData;
                    return true;
                }
                var m = Measures.Find(args[1]);
                if (m == null) return true;
                var to = RadarUtils.ParseLayoutArea2(args[2]);

                var chart = this as OlapChart;
                if (chart != null && chart.AnalysisType == AnalysisType.Chart)
                    switch (to)
                    {
                        case LayoutArea.laShape:
                        case LayoutArea.laDetails:
                            return true;
                    }

                var from = LayoutArea.laNone;
                ;
                if (args.Length > 4)
                    from = RadarUtils.ParseLayoutArea2(args[4]);

                if (CellsetMode == CellsetMode.cmGrid &&
                    to != LayoutArea.laColor && to != LayoutArea.laColorFore &&
                    from != LayoutArea.laColor && from != LayoutArea.laColorFore)
                {
                    if (args[2] == LayoutArea.laTree.ToString() || args[2] == "tree")
                    {
                        m.Visible = false;
                        PivotingOut(m, false, LayoutArea.laRow);
                        callbackData = CallbackData.PivotAndData;
                        return true;
                    }
                    //m.Visible = true;
                    Pivoting(m, LayoutArea.laRow, null, from);

                    if (Measures.Level == null)
                        throw new Exception("Measures.Level is null");
                    var M = Measures.Level.FindMember(m.UniqueName);
                    Member Target = null;
                    if (args[3] != "999")
                    {
                        if (Measures.Level.Members == null)
                            throw new Exception("Measures.Level.Members is null");
                        Target = Measures.Level.Members[Convert.ToInt32(args[3])];
                    }
                    if (Target == null)
                        Target = Measures.Level.Members[Measures.Level.Members.Count - 1];
                    if (M != Target)
                    {
                        if (Target != null)
                        {
                            if (Measures.Level.Members == null)
                                throw new Exception("Measures.Level.Members is null");
                            var mi = Measures.Level.Members.IndexOf(M);
                            var ti = Measures.Level.Members.IndexOf(Target);
                            Measures.Level.Members.Remove(M);
                            if (mi < ti)
                                Measures.Level.Members.Insert(Measures.Level.Members.IndexOf(Target) + 1, M);
                            else
                                Measures.Level.Members.Insert(Measures.Level.Members.IndexOf(Target), M);
                            CellSet.Rebuild();
                        }
                        if (CellSet == null)
                            throw new Exception("Cellset is null");
                        //CellSet.Rebuild();
                        callbackData = CallbackData.PivotAndData;
                    }
                    return true;
                }
                if (from != LayoutArea.laNone)
                    Pivoting(m, to, null, from);
                else
                    Pivoting(m, to, null, null);
                return true;
            }
            if (args[0] == "pivoting2")
            {
                //Level l = Dimensions.FindLevel(args[1]);
                //if (l != null)
                //{
                //    Hierarchy h = l.Hierarchy;
                //    if ((FLayout.fColorAxisItem == h) || (FLayout.fSizeAxisItem == h) || (FLayout.fShapeAxisItem == h) ||
                //        FLayout.fDetailsAxis.Contains(h))
                //    {
                //        if (l.Index > 0)
                //        {
                //            h.Levels[l.Index - 1].Visible = true;
                //            for (int i = l.Index + 1; i < l.Hierarchy.Levels.Count; i++)
                //                l.Hierarchy.Levels[i].FVisible = false;
                //            l.Visible = false;
                //        }
                //    }
                //    else
                //    {
                //        if (l.Visible)
                //        {
                //            for (int i = l.Index + 1; i < l.Hierarchy.Levels.Count; i++)
                //                l.Hierarchy.Levels[i].FVisible = false;
                //            l.Visible = false;
                //        }
                //    }
                //}
                //return true;

                if (args[1] == null)
                    throw new Exception("args[1] is null");

                if (Dimensions == null)
                    throw new Exception("Dimensions is null");
                var h1 = Dimensions.FindHierarchy(args[1]);
                var h2 = Dimensions.FindHierarchy(args[5]);
                if (h1 == null)
                {
                    var l = Dimensions.FindLevel(args[1]);
                    if (l != null) h1 = l.Hierarchy;
                }
                if (h2 == null)
                {
                    var l = Dimensions.FindLevel(args[5]);
                    if (l != null) h2 = l.Hierarchy;
                }
                if (h1 != null && h2 != null)
                {
                    var l = args.Length > 12 ? RadarUtils.ParseLayoutArea(args[10]) : null;
                    if (args[2].StartsWith("la"))
                        Pivoting(h1, RadarUtils.ParseLayoutArea2(args[2]), Convert.ToInt16(args[3]), l);
                    if (args[2] == "row") Pivoting(h1, LayoutArea.laRow, Convert.ToInt16(args[3]), l);
                    if (args[2] == "col") Pivoting(h1, LayoutArea.laColumn, Convert.ToInt16(args[3]), l);
                    if (args[2] == "page") Pivoting(h1, LayoutArea.laPage, Convert.ToInt16(args[3]), l);

                    if (FLayout.fColumnAxis.Count == 0 && args.Length >= 11)
                    {
                        h2 = Dimensions.FindHierarchy(args[9]);
                        if (h2 == null)
                        {
                            var lev = Dimensions.FindLevel(args[9]);
                            if (lev != null) h2 = lev.Hierarchy;
                        }
                    }

                    if (h2 == null)
                    {
                        Pivoting((Hierarchy) FLayout.fColorAxisItem, LayoutArea.laTree, Convert.ToInt16(args[3]), l);
                        return true;
                    }
                    if (args[6].StartsWith("la"))
                        Pivoting(h2, RadarUtils.ParseLayoutArea2(args[6]), Convert.ToInt16(args[7]), l);
                    if (args[6] == "row") Pivoting(h1, LayoutArea.laRow, Convert.ToInt16(args[7]), l);
                    if (args[6] == "col") Pivoting(h1, LayoutArea.laColumn, Convert.ToInt16(args[7]), l);
                    if (args[6] == "page") Pivoting(h1, LayoutArea.laPage, Convert.ToInt16(args[7]), l);

                    callbackData = CallbackData.PivotAndData;
                    return true;
                }

                var l1 = args.Length > 10 ? RadarUtils.ParseLayoutArea(args[10]) : null;

                var m = Measures.Find(args[1]);
                if (m == null) return true;

                if (args.Length > 4)
                {
                    var count = FLayout.fYAxisMeasures.Count;
                    Pivoting(m, RadarUtils.ParseLayoutArea2(args[2]), null,
                        RadarUtils.ParseLayoutArea(args[4]));
                    if (count > FLayout.fYAxisMeasures.Count && FLayout.fYAxisMeasures.Count > 0)
                    {
                        var ll = args.Length > 12 ? RadarUtils.ParseLayoutArea(args[10]) : null;
                        Pivoting(h2, RadarUtils.ParseLayoutArea2(args[6]), Convert.ToInt16(args[7]), ll);

                        return true;
                    }
                    //if (count == FLayout.fYAxisMeasures.Count)
                    //{
                    //    LayoutArea ll = (args.Length > 7) ? (LayoutArea)Enum.Parse(typeof(LayoutArea), args[8]) : LayoutArea.laNone;
                    //    Pivoting(h2, (LayoutArea)Enum.Parse(typeof(LayoutArea), args[6]), Convert.ToInt16(args[7]), ll);

                    //}
                }
                // else
                //  Pivoting(m, (LayoutArea)Enum.Parse(typeof(LayoutArea), args[2]), null, LayoutArea.laNone);

                if (args[6].StartsWith("la"))
                    Pivoting(h2, RadarUtils.ParseLayoutArea2(args[6]), Convert.ToInt16(args[7]), l1);
                if (args[6] == "row") Pivoting(h1, LayoutArea.laRow, Convert.ToInt16(args[7]), l1);
                if (args[6] == "col") Pivoting(h1, LayoutArea.laColumn, Convert.ToInt16(args[7]), l1);
                if (args[6] == "page") Pivoting(h1, LayoutArea.laPage, Convert.ToInt16(args[7]), l1);

                callbackData = CallbackData.PivotAndData;
                return true;
            }

            if (args[0] == "pivoting3")
            {
                if (args[1] == null)
                    throw new Exception("args[1] is null");
                if (args[1].StartsWith("node_"))
                {
                    callbackData = CallbackData.Nothing;
                    return false;
                }
                if (args[1] == "shownextlevel")
                {
                    var l = Dimensions.FindLevel(args[2]);
                    if (l != null)
                        if (l.Index < l.Hierarchy.Levels.Count - 1)
                        {
                            l.Hierarchy.Levels[l.Index + 1].Visible = true;

                            var h1 = l.Hierarchy.Dimension.Hierarchies.Where(item =>
                                                                                 item.DisplayName ==
                                                                                 FLayout.fColumnLevels[
                                                                                         FLayout.fColumnLevels.Count -
                                                                                         1]
                                                                                     .DisplayName).First();

                            Pivoting(h1, LayoutArea.laColor, 999, LayoutArea.laTree);
                        }

                    return true;
                }

                if (args[1] == "hidelevelsbelow")
                {
                    var l = Dimensions.FindLevel(args[2]);
                    if (l != null)
                    {
                        var h1 = l.Hierarchy;
                        if (FLayout.fColorAxisItem == h1 || FLayout.fSizeAxisItem == h1 ||
                            FLayout.fShapeAxisItem == h1 ||
                            FLayout.fDetailsAxis.Contains(h1))
                        {
                            if (l.Index > 0)
                            {
                                h1.Levels[l.Index - 1].Visible = true;
                                for (var i = l.Index + 1; i < l.Hierarchy.Levels.Count; i++)
                                    l.Hierarchy.Levels[i].FVisible = false;
                                l.Visible = false;
                            }
                        }
                        else
                        {
                            if (l.Visible)
                            {
                                for (var i = l.Index + 1; i < l.Hierarchy.Levels.Count; i++)
                                    l.Hierarchy.Levels[i].FVisible = false;
                                l.Visible = false;
                                var visibleLevel = l.Hierarchy.Levels.Where(item => item.Visible).ToList();


                                var h2 = l.Hierarchy.Dimension.Hierarchies.Where(item =>
                                                                                     item.DisplayName ==
                                                                                     l.Hierarchy.Levels
                                                                                         .Where(it => it.Visible)
                                                                                         .ToList()[
                                                                                             visibleLevel.Count - 1]
                                                                                         .DisplayName).First();

                                Pivoting(h2, LayoutArea.laColor, 999, LayoutArea.laTree);
                            }
                        }
                    }
                    return true;
                }

                if (Dimensions == null)
                    throw new Exception("Dimensions is null");
                var h = Dimensions.FindHierarchy(args[1]);
                if (h == null)
                {
                    var l = Dimensions.FindLevel(args[1]);
                    if (l != null) h = l.Hierarchy;
                }


                if (h != null)
                {
                    var l = args.Length > 5 ? RadarUtils.ParseLayoutArea(args[4]) : null;
                    if (args[2].StartsWith("la"))
                    {
                        if (args.Count() == 6)
                        {
                            Pivoting(h, RadarUtils.ParseLayoutArea2(args[2]), Convert.ToInt16(args[3]), l);

                            var h3 = h.Dimension.Hierarchies
                                .Where(item => item.DisplayName ==
                                               h.Levels.Where(lev => lev.Visible).Last().DisplayName).First();
                            Pivoting(h3, LayoutArea.laColor, Convert.ToInt16(args[3]), l);
                        }
                        if (args.Count() == 10)
                        {
                            var h2 = Dimensions.FindHierarchy(args[5]);
                            if (h2 == null)
                            {
                                var l1 = Dimensions.FindLevel(args[5]);
                                if (l1 != null) h2 = l1.Hierarchy;
                            }
                            if (h2 != null)
                                h2 = h2.Dimension.Hierarchies
                                    .Where(item => item.DisplayName ==
                                                   h2.Levels.Where(lev => lev.Visible).Last().DisplayName).First();

                            var la1 = LayoutArea.laColor;
                            Pivoting(h, RadarUtils.ParseLayoutArea2(args[2]), Convert.ToInt16(args[3]), l);
                            if (h2 != null)
                                Pivoting(h2, LayoutArea.laColor, Convert.ToInt16(args[8]), LayoutArea.laTree);
                        }
                    }
                    if (args[2] == "row") Pivoting(h, LayoutArea.laRow, Convert.ToInt16(args[3]), l);
                    if (args[2] == "col") Pivoting(h, LayoutArea.laColumn, Convert.ToInt16(args[3]), l);
                    if (args[2] == "page") Pivoting(h, LayoutArea.laPage, Convert.ToInt16(args[3]), l);

                    callbackData = CallbackData.PivotAndData;
                    return true;
                }
                var m = Measures.Find(args[1]);
                if (m == null) return true;
                if (CellsetMode == CellsetMode.cmGrid)
                {
                    if (args[2] == LayoutArea.laTree.ToString())
                    {
                        m.Visible = false;
                        return true;
                    }
                    if (args[2] == LayoutArea.laRow.ToString())
                    {
                        m.Visible = true;
                        return true;
                    }
                    if (args[2] != "") return true;
                    if (!m.Visible) m.Visible = true;
                    if (Measures.Level == null)
                        throw new Exception("Measures.Level is null");
                    var M = Measures.Level.FindMember(m.UniqueName);
                    Member Target = null;
                    if (args[3] != "999")
                    {
                        if (Measures.Level.Members == null)
                            throw new Exception("Measures.Level.Members is null");
                        Target = Measures.Level.Members[Convert.ToInt32(args[3])];
                    }
                    if (Target == null)
                        Target = Measures.Level.Members[Measures.Level.Members.Count - 1];
                    if (M != Target)
                    {
                        if (Target != null)
                        {
                            if (Measures.Level.Members == null)
                                throw new Exception("Measures.Level.Members is null");
                            var mi = Measures.Level.Members.IndexOf(M);
                            var ti = Measures.Level.Members.IndexOf(Target);
                            Measures.Level.Members.Remove(M);
                            if (mi < ti)
                                Measures.Level.Members.Insert(Measures.Level.Members.IndexOf(Target) + 1, M);
                            else
                                Measures.Level.Members.Insert(Measures.Level.Members.IndexOf(Target), M);
                        }
                        if (CellSet == null)
                            throw new Exception("Cellset is null");
                        CellSet.Rebuild();
                        callbackData = CallbackData.PivotAndData;
                    }
                    return true;
                }
                if (args.Length > 4)
                {
                    Pivoting(m, RadarUtils.ParseLayoutArea2(args[2]), null,
                        RadarUtils.ParseLayoutArea(args[4]));
                    if (args.Count() == 10)
                    {
                        var h2 = Dimensions.FindHierarchy(args[5]);
                        if (h2 == null)
                        {
                            var l1 = Dimensions.FindLevel(args[5]);
                            if (l1 != null) h2 = l1.Hierarchy;
                        }
                        var la1 = LayoutArea.laColor;

                        if (h2 != null)
                            Pivoting(h2, RadarUtils.ParseLayoutArea2(args[7]), Convert.ToInt16(args[8]),
                                RadarUtils.ParseLayoutArea(args[6]));
                    }
                }
                else
                {
                    Pivoting(m, RadarUtils.ParseLayoutArea2(args[2]), null, null);
                }
                return true;
            }

            if (args[0] == "pivotingtogroup")
            {
                if (args[1] == null)
                    throw new Exception("args[1] is null");
                if (Measures == null)
                    throw new Exception("Measures is null");
                var m = Measures.Find(args[1]);
                if (m == null) return true;

                MeasureGroup mg = null;
                var i = Convert.ToInt32(args[2]);
                if (FLayout.fYAxisMeasures.Count > i)
                    mg = FLayout.fYAxisMeasures[i];
                if (args.Length > 4)
                    Pivoting(m, LayoutArea.laRow, mg, RadarUtils.ParseLayoutArea(args[4]));
                else
                    Pivoting(m, LayoutArea.laRow, mg, null);
                return true;
            }

            if (args[0] == "pivotingtogroup2")
            {
                if (args[1] == null)
                    throw new Exception("args[1] is null");
                if (Measures == null)
                    throw new Exception("Measures is null");
                var m = Measures.Find(args[1]);
                if (m == null) return true;

                MeasureGroup mg = null;
                var i = Convert.ToInt32(args[2]);
                var count = FLayout.fYAxisMeasures.Count;
                if (FLayout.fYAxisMeasures.Count > i)
                    mg = FLayout.fYAxisMeasures[i];
                if (args.Length > 4)
                {
                    Pivoting(m, LayoutArea.laRow, mg, RadarUtils.ParseLayoutArea(args[4]));
                    if (count > FLayout.fYAxisMeasures.Count)
                    {
                        var ll = args.Length > 12 ? RadarUtils.ParseLayoutArea(args[10]) : null;
                        var h = Dimensions.FindHierarchy(args[5]);

                        if (h == null)
                        {
                            var l = Dimensions.FindLevel(args[5]);
                            if (l != null) h = l.Hierarchy;
                        }

                        Pivoting(h, RadarUtils.ParseLayoutArea2(args[6]), Convert.ToInt16(args[7]), ll);
                    }

                    if (count < FLayout.fYAxisMeasures.Count)
                    {
                        var ll = args.Length > 12 ? RadarUtils.ParseLayoutArea(args[10]) : null;
                        var h = Dimensions.FindHierarchy(args[9]);

                        if (h == null)
                        {
                            var l = Dimensions.FindLevel(args[9]);
                            if (l != null) h = l.Hierarchy;
                        }

                        Pivoting(h, RadarUtils.ParseLayoutArea2(args[6]), Convert.ToInt16(args[7]), ll);
                    }
                }
                else
                {
                    Pivoting(m, LayoutArea.laRow, mg, null);
                }
                return true;
            }
            if (args[0] == "measposition")
            {
                var mp = MeasurePosition.mpFirst;
                if (args[1] == "false") mp = MeasurePosition.mpLast;
                if (FLayout.MeasurePosition != mp)
                {
                    FLayout.MeasurePosition = mp;
                    FCellSet.Rebuild();
                }
                return true;
            }

            if (args[0] == "measlayout")
            {
                var la = LayoutArea.laColumn;
                if (args[1] == "false") la = LayoutArea.laRow;
                if (FLayout.MeasureLayout != la)
                {
                    FLayout.MeasureLayout = la;
                    FCellSet.Rebuild();
                }
                return true;
            }

            if (args[0] == "applycalcmeasure")
            {
                // 1 - unique name
                // 2 - display name
                // 3 - format
                // 4 - expression
                var m = Measures.Find(args[1]);
                if (m == null)
                {
                    if (string.IsNullOrEmpty(args[2]) ||
                        string.IsNullOrEmpty(args[4]))
                    {
                        callbackData = CallbackData.Nothing;
                        return true;
                    }
                    m = Measures.AddCalculatedMeasure(args[2]);
                }
                m.DefaultFormat = args[3];
                m.Expression = args[4];
                Engine.ClearMeasureData(m);
                m.Visible = true;
                CellSet.Rebuild();

                Pivoting(m, LayoutArea.laRow, null, LayoutArea.laRow);
                callbackData = CallbackData.CubeTree;
                return true;
            }

            if (args[0] == "deletecalculatedmeasure")
            {
                var m = Measures.Find(args[1]);
                if (m == null)
                {
                    callbackData = CallbackData.Nothing;
                    return true;
                }
                Measures.DeleteCalculatedMeasure(m);
                return true;
            }

            if (args[0] == "cmfilter")
            {
                var m = Measures.Find(args[1]);
                if (m == null) return true;

                var fc = (OlapFilterCondition) Enum.Parse(typeof(OlapFilterCondition), args[2]);

                var value1 = args[3];
                var value2 = string.IsNullOrEmpty(args[4]) ? null : args[4];
                if (m.Filter != null && m.Filter.FilterCondition == fc)
                {
                    m.Filter.FirstValue = value1;
                    m.Filter.SecondValue = value2;
                    m.Filter.RestrictsTo = args[5] == "0"
                        ? MeasureFilterRestriction.mfrAggregatedValues
                        : MeasureFilterRestriction.mfrFactTable;
                }
                else
                {
                    var ff = new MeasureFilter(m, fc, value1, value2);
                    ff.RestrictsTo = args[5] == "0"
                        ? MeasureFilterRestriction.mfrAggregatedValues
                        : MeasureFilterRestriction.mfrFactTable;
                    m.Filter = ff;
                }
                return true;
            }

            if (args[0] == "resetcmfilter")
            {
                var m = Measures.Find(args[1]);
                if (m == null) return true;
                m.Filter = null;
                return true;
            }

            if (args[0] == "cfilter")
            {
                var l = Dimensions.FindLevel(args[1]);
                string MDXLevel = null;
                if (l == null)
                {
                    foreach (var d in Dimensions)
                    {
                        if (l != null) break;
                        foreach (var h in d.Hierarchies)
                        {
                            if (l != null) break;
                            foreach (var mdxl in h.CubeHierarchy.FMDXLevelNames)
                                if (args[1] == mdxl)
                                {
                                    l = h.Levels[0];
                                    MDXLevel = args[1];
                                    break;
                                }
                        }
                    }
                    if (l == null) return true;
                }

                var ft = (OlapFilterType) Enum.Parse(typeof(OlapFilterType), args[2]);
                var fc = (OlapFilterCondition) Enum.Parse(typeof(OlapFilterCondition), args[3]);

                Measure ms = null;
                if (ft == OlapFilterType.ftOnValue)
                    ms = Measures.Find(args[5]);
                var value1 = args[4];
                var value2 = string.IsNullOrEmpty(args[6]) ? null : args[6];
                if (ft == OlapFilterType.ftOnDate)
                {
                    var vv = value1;
                    try
                    {
                        var dt = DateTime.Parse(value1, CultureInfo.InvariantCulture.DateTimeFormat);
                        if (!string.IsNullOrEmpty(value2))
                        {
                            vv = value2;
                            dt = DateTime.Parse(value2, CultureInfo.InvariantCulture.DateTimeFormat);
                        }
                    }
                    catch
                    {
                        var vd = "MM/DD/YYYY";
                        _callbackClientErrorString = RadarUtils.GetResStr("rsWrongDateStr",
                            vv, vd);
                        callbackData = CallbackData.ClientError;
                        return true;
                    }
                }
                if (l.Filter != null && l.Filter.FilterCondition == fc && l.Filter.FilterType == ft)
                {
                    if (!FLayout.ColumnAxis.Contains(l.Hierarchy) &&
                        !FLayout.RowAxis.Contains(l.Hierarchy) &&
                        !FLayout.PageAxis.Contains(l.Hierarchy))
                        Pivoting(l.Hierarchy, LayoutArea.laPage, 999);

                    l.Hierarchy.BeginUpdate();
                    if (MDXLevel != null) l.Filter.fLevelName = MDXLevel;
                    l.Filter.FirstValue = value1;
                    l.Filter.SecondValue = value2;
                    if (ms != null) l.Filter.AppliesTo = ms;
                    l.Hierarchy.EndUpdate();
                }
                else
                {
                    if (!FLayout.ColumnAxis.Contains(l.Hierarchy) &&
                        !FLayout.RowAxis.Contains(l.Hierarchy) &&
                        !FLayout.PageAxis.Contains(l.Hierarchy))
                        Pivoting(l.Hierarchy, LayoutArea.laPage, 999);

                    var ff = new CellSet.Filter(l, ft, ms, fc, value1, value2);
                    if (MDXLevel != null) ff.fLevelName = MDXLevel;
                    l.Filter = ff;
                }

                return true;
            }

            if (args[0] == "pivotingtree")
            {
                var m = Measures.Find(args[1]);
                if (m != null)
                {
                    m.Visible = !m.Visible;
                }
                else
                {
                    var h = Dimensions.FindHierarchy(args[1]);
                    if (h == null)
                    {
                        callbackData = CallbackData.Nothing;
                        return true;
                    }
                    if ((h.State & HierarchyState.hsActive) == HierarchyState.hsActive)
                    {
                        PivotingOut(h, null);
                    }
                    else
                    {
                        var area = h.IsDate ? LayoutArea.laColumn : LayoutArea.laRow;
                        PivotingLast(h, area);
                        if (CalcBehavior(area) == PivotingBehavior.Excel2010)
                            CellSet.ExpandAllHierarchies(PossibleDrillActions.esNextHierarchy, h.IsDate, !h.IsDate);
                    }
                }
                return true;
            }

            callbackData = CallbackData.Nothing;
            return false;
        }

        internal struct MembersCallbackData
        {
            public int from;
            public ClientMember[] members;
            public int count;
            public ClientLevel[] levels;
        }

        internal MembersCallbackData memberCallbackData;

        internal void HandleMembersCallback(string eventArgument)
        {
            callbackData = CallbackData.MembersList;
            var args = eventArgument.Split('|');
            var h = Dimensions.FindHierarchy(args[0]);
            memberCallbackData = new MembersCallbackData();
            bool stateChanged;
            var stateChanged2 = false;
            if (h.State == HierarchyState.hsNone)
            {
                stateChanged2 = true;
                h.DefaultInit();
                memberCallbackData.levels = h.Levels.Select(item => new ClientLevel(item)).ToArray();
            }
            var m = string.IsNullOrEmpty(args[1]) ? null : h.FindMemberByUniqueName(args[1]);
            var ms = m == null ? h.Levels[0].Members : m.Children.Count == 0 ? m.NextLevelChildren : m.Children;

            var hasCustom = ms.Any(item => item is CustomMember);
            var members = new List<string>();
            var from = hasCustom ? -1 : Convert.ToInt32(args[2]);
            var count = hasCustom ? -1 : Convert.ToInt32(args[3]) - from + 1;

            var filtered = bool.Parse(args[4]);
            var FilterString = args[5];

            if (!filtered && string.IsNullOrEmpty(FilterString))
            {
                Cube.RetrieveMembersPartial(this, (object) m ?? h.Levels[0], from,
                    count, members, out stateChanged);
                memberCallbackData.from = from;
                if (h.Origin == HierarchyOrigin.hoParentChild)
                {
                    Cube.CheckAreLeaves(ms.ToList());
                    memberCallbackData.members = ms.Select(item => new ClientMember(item)).ToArray();
                    memberCallbackData.count = ms.Count;
                }
                else
                {
                    var ml = members.Select(
                        item => h.FindMemberByUniqueName(item));
                    Cube.CheckAreLeaves(ml.ToList());
                    memberCallbackData.members = ml.Select(item => new ClientMember(item)).ToArray();
                    memberCallbackData.count = m == null
                        ? -1
                        : Cube.RetrieveMembersCount(this, m) +
                          m.Level.FUniqueNamesArray.Values.Count(item => item.Parent == m);
                }
            }
            else
            {
                Cube.RetrieveMembersFiltered(this, h, FilterString, members,
                    out stateChanged, false, filtered);
                memberCallbackData.from = 0;
                var ma = members.ToArray();
                Array.Sort(ma);
                memberCallbackData.members = ms.Select(item => item.UniqueName)
                    .Where(item => Array.BinarySearch(ma, item) >= 0).Select(
                        item => new ClientMember(h.FindMemberByUniqueName(item))).ToArray();
                memberCallbackData.count = memberCallbackData.members.Length;
            }

            if (stateChanged || stateChanged2) ApplyChanges();
        }

        internal virtual bool HandleDataCallback(string eventArgument)
        {
            callbackData = CallbackData.Data;
            var args = eventArgument.Split('|');

            if (args[0] == "showinfos")
            {
                //showallintt
                //hideallintt

                //hideallinreport

                var cl = Cube.Dimensions.FindLevel(args[2]);
                string an = null;
                var a = AttributeDispalyMode.None;

                if (args[1] == "showattrep")
                {
                    an = args[3];
                    a = AttributeDispalyMode.AsColumn;
                }

                if (args[1] == "showatttt")
                {
                    an = args[3];
                    a = AttributeDispalyMode.AsTooltip;
                }

                if (an == null)
                {
                    if (args[1].StartsWith("showall"))
                    {
                        if (args[1] == "showallinreport")
                            a = AttributeDispalyMode.AsColumn;
                        if (args[1] == "showallintt")
                            a = AttributeDispalyMode.AsTooltip;

                        foreach (var ti in cl.InfoAttributes)
                            ti.DisplayMode |= a;
                    }

                    if (args[1].StartsWith("hideall"))
                    {
                        if (args[1] == "hideallinreport")
                            a = AttributeDispalyMode.AsColumn;
                        if (args[1] == "hideallintt")
                            a = AttributeDispalyMode.AsTooltip;

                        foreach (var ti in cl.InfoAttributes)
                            ti.DisplayMode &= ~a;
                    }
                }
                else
                {
                    var ti = cl.InfoAttributes.Find(an);
                    if (ti.DisplayMode.HasFlag(a))
                        ti.DisplayMode &= ~a;
                    else
                        ti.DisplayMode |= a;
                }

                FCellSet.Rebuild();
                return true;
            }

            if (args[0] == "deletecalculatedmember")
            {
                var l = Dimensions.FindLevel(args[1]);
                var m = l != null ? l.FindMember(args[2]) as CalculatedMember : null;
                if (l == null || m == null)
                {
                    callbackData = CallbackData.Nothing;
                    return true;
                }
                l.Hierarchy.DeleteCalculatedMember(m);
                return true;
            }

            if (args[0] == "applycalcmember")
            {
                // 1 - level unique name
                // 2 - member unique name
                // 3 - display name
                // 4 - format
                // 5 - expression
                var l = Dimensions.FindLevel(args[1]);
                if (l == null)
                {
                    callbackData = CallbackData.Nothing;
                    return true;
                }

                var m = l.FindMember(args[2]) as CalculatedMember;

                if (m == null)
                {
                    if (string.IsNullOrEmpty(args[3]) ||
                        string.IsNullOrEmpty(args[5]))
                    {
                        callbackData = CallbackData.Nothing;
                        return true;
                    }
                    m = l.Hierarchy.CreateCalculatedMember(args[3], null, l, null, CustomMemberPosition.cmpLast);
                }
                m.Expression = args[5];

                return true;
            }

            if (args[0] == "groupthis")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[2]), Convert.ToInt32(args[3])) as IMemberCell;
                var gm = c.Member.Level.FindMember(args[1]) as GroupMember;
                c.Member.Level.Hierarchy.MoveToGroup(gm, c.Member);
                return true;
            }

            if (args[0] == "groupall")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[2]), Convert.ToInt32(args[3])) as IMemberCell;
                var gm = c.Member.Level.FindMember(args[1]) as GroupMember;
                var P = new List<Member>(c.Member.Level.Members.Where(x => x.MemberType != MemberType.mtGroup));

                if (P.Count > 0)
                    c.Member.Level.Hierarchy.MoveToGroup(gm, P.ToArray());
                else
                    callbackData = CallbackData.Nothing;
                return true;
            }

            if (args[0] == "groupbelow")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[2]), Convert.ToInt32(args[3])) as IMemberCell;
                var gm = c.Member.Level.FindMember(args[1]) as GroupMember;
                var P = new List<Member>();
                for (var i = c.SiblingsOrder + 1; i < c.SiblingsCount; i++)
                {
                    var c_ = c.Siblings(i);
                    if (c.Member != c_.Member && c_.Member.MemberType != MemberType.mtGroup)
                        P.Add(c_.Member);
                }
                if (P.Count > 0)
                    c.Member.Level.Hierarchy.MoveToGroup(gm, P.ToArray());
                else
                    callbackData = CallbackData.Nothing;
                return true;
            }

            if (args[0] == "groupexcept")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[2]), Convert.ToInt32(args[3])) as IMemberCell;
                var gm = c.Member.Level.FindMember(args[1]) as GroupMember;
                var P = new List<Member>();
                for (var i = 0; i < c.SiblingsCount; i++)
                {
                    var c_ = c.Siblings(i);
                    if (c.Member != c_.Member && c_.Member.MemberType != MemberType.mtGroup)
                        P.Add(c_.Member);
                }
                if (P.Count > 0)
                    c.Member.Level.Hierarchy.MoveToGroup(gm, P.ToArray());
                else
                    callbackData = CallbackData.Nothing;
                return true;
            }

            if (args[0] == "renamegroup")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[1]), Convert.ToInt32(args[2])) as IMemberCell;
                (c.Member as GroupMember).DisplayName = args[3];
                return true;
            }

            if (args[0] == "modifycomment")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[1]), Convert.ToInt32(args[2]));
                var comment = args[3];
                if (c is IDataCell)
                    ((IDataCell) c).Comment = comment;
                if (c is IMemberCell)
                    ((IMemberCell) c).Comment = comment;
                return true;
            }

            if (args[0] == "cleargroup")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[1]), Convert.ToInt32(args[2])) as IMemberCell;
                c.Member.Level.Hierarchy.ClearGroup(c.Member as GroupMember);
                return true;
            }

            if (args[0] == "deletegroup")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[1]), Convert.ToInt32(args[2])) as IMemberCell;
                c.Member.Level.Hierarchy.DeleteGroup(c.Member as GroupMember);
                CellSet.Rebuild();
                return true;
            }

            if (args[0] == "ungroupthis")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[1]), Convert.ToInt32(args[2])) as IMemberCell;
                c.Member.Level.Hierarchy.MoveFromGroup(c.Member);
                return true;
            }

            if (args[0] == "writeback")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[1]), Convert.ToInt32(args[2])) as IDataCell;
                if (OnWriteback == null)
                {
                    c.Writeback(args[3], WritebackMethod.wmEqualAllocation);
                }
                else
                {
                    var E = new WritebackArgs(c, args[3]);
                    OnWriteback(this, E);
                    if (!E.AllowWriteback)
                    {
                        callbackData = CallbackData.Data;
                        return true;
                    }
                    c.Writeback(E.NewValue, E.WritebackMethod);
                }
                return true;
            }

            if (args[0] == "measureshowmode")
            {
                var m = Measures[args[1]];
                var type = (MeasureShowModeType) Enum.Parse(typeof(MeasureShowModeType), args[2]);
                MeasureShowMode mm;
                if (type != MeasureShowModeType.smSpecifiedByEvent)
                    mm = m.ShowModes.Find(type);
                else
                    mm = m.ShowModes.Find(args[3]);

                if (mm != null && (m.ShowModes.CountVisible > 1 || !mm.Visible))
                    mm.Visible = !mm.Visible;
                else
                    callbackData = CallbackData.Nothing;
                return true;
            }

            if (args[0] == "hideempty")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[1]), Convert.ToInt32(args[2]));
                var h = (c as ILevelCell).Level.Hierarchy;
                h.ShowEmptyLines = false;
                return true;
            }

            if (args[0] == "showempty")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[1]), Convert.ToInt32(args[2]));
                var h = (c as ILevelCell).Level.Hierarchy;
                h.ShowEmptyLines = true;
                return true;
            }

            if (args[0] == "drillall")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[2]), Convert.ToInt32(args[3]));
                var l = c as ILevelCell;
                if (args[1] == "l") l.ExpandAllNodes(PossibleDrillActions.esNextLevel);
                if (args[1] == "c") l.CollapseAllNodes();
                if (args[1] == "h") l.ExpandAllNodes(PossibleDrillActions.esNextHierarchy);
                if (args[1] == "p") l.ExpandAllNodes(PossibleDrillActions.esParentChild);
                CellSet.Rebuild();
                return true;
            }

            if (args[0] == "tfic")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[2]), Convert.ToInt32(args[3]));
                Hierarchy h = null;
                if (c is ILevelCell)
                    h = ((ILevelCell) c).Level.Hierarchy;
                if (args[1] == "all") h.TakeFiltersIntoCalculations = false;
                if (args[1] == "visible") h.TakeFiltersIntoCalculations = true;
                CellSet.Rebuild();
                return true;
            }

            if (args[0] == "lsort")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[2]), Convert.ToInt32(args[3]));
                Level l = null;
                if (c is ILevelCell)
                    l = ((ILevelCell) c).Level;
                if (args[1] == "default") l.SortType = MembersSortType.msTypeRelated;
                if (args[1] == "asc") l.SortType = MembersSortType.msNameAsc;
                if (args[1] == "desc") l.SortType = MembersSortType.msNameDesc;
                CellSet.Rebuild();
                return true;
            }

            if (args[0] == "chta")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[2]), Convert.ToInt32(args[3]));
                Hierarchy h = null;
                if (c is ILevelCell)
                    h = ((ILevelCell) c).Level.Hierarchy;
                if (args[1] == "first") h.TotalAppearance = TotalAppearance.taFirst;
                if (args[1] == "last") h.TotalAppearance = TotalAppearance.taLast;
                if (args[1] == "none") h.TotalAppearance = TotalAppearance.taInvisible;
                CellSet.Rebuild();
                return true;
            }

            if (args[0] == "changehidemeasures")
            {
                AxesLayout.HideMeasureIfPossible = !AxesLayout.HideMeasureIfPossible;
                return true;
            }

            if (args[0] == "creategroup")
            {
                var c = CellSet.Cells(Convert.ToInt32(args[1]), Convert.ToInt32(args[2]));
                if (c is ILevelCell)
                {
                    var l = ((ILevelCell) c).Level;
                    l.Hierarchy.CreateGroup(args[3], CustomMemberPosition.cmpGeneralOrder, new Member[0]);
                }
                if (c is IMemberCell)
                {
                    var mc = c as IMemberCell;
                    var l = mc.Member.Level;
                    l.Hierarchy.CreateGroup(args[3], "", mc.Member, true, new Member[0]);
                }
                return true;
            }

            if (args[0] == "valuesort")
            {
                var i = Convert.ToInt32(args[1]);
                if (args.Length < 4)
                {
                    if (i == FCellSet.FValueSortedColumn)
                    {
                        if (FCellSet.FSortingDirection == ValueSortingDirection.sdDescending)
                        {
                            FCellSet.ValueSortingDirection = ValueSortingDirection.sdAscending;
                            return true;
                        }
                        if (FCellSet.FSortingDirection == ValueSortingDirection.sdAscending)
                        {
                            FCellSet.ValueSortedColumn = -1;
                            return true;
                        }
                    }
                    FCellSet.FSortingDirection = ValueSortingDirection.sdDescending;
                    FCellSet.ValueSortedColumn = i;
                }
                else
                {
                    FCellSet.FSortingDirection = (ValueSortingDirection) Convert.ToInt32(args[2]);
                    FCellSet.ValueSortedColumn = i;
                }
                return true;
            }

            if (args[0] == "pagelevel")
            {
                MemberCell mc;
                var icol = Convert.ToInt32(args[2]);
                var irow = Convert.ToInt32(args[3]);
                var lc = CellSet.Cells(icol, irow) as LevelCell;
                if (lc != null)
                {
                    if (args[1] == "all")
                    {
                        if (lc.Level != null)
                        {
                            lc.Level.PagerSettings.AllowPaging = false;
                            CellSet.Rebuild();
                            ApplyChanges();
                        }
                        return true;
                    }
                    if (args[1] == "allenable")
                    {
                        if (lc.Level != null)
                        {
                            lc.Level.PagerSettings.AllowPaging = true;
                            CellSet.Rebuild();
                            ApplyChanges();
                        }
                        return true;
                    }
                }
            }

            if (args[0] == "page")
            {
                MemberCell mc;
                var icol = Convert.ToInt32(args[2]) % FCellSet.ColumnCount;
                var irow = Convert.ToInt32(args[2]) / FCellSet.ColumnCount;
                mc = CellSet.Cells(icol, irow) as MemberCell;

                var page = -1;
                try
                {
                    if (args[1] == "all")
                    {
                        //                        mc.fM.FLevel.FLevel.PagerSettings.LinesInPage = 999999;
                        if (mc.Model.FParent != null)
                        {
                            foreach (var cc in mc.Model.FParent.FChildren)
                                if (!cc.FIsPager && !cc.FIsTotal)
                                {
                                    cc.FLevel.FLevel.PagerSettings.AllowPaging = false;
                                    CellSet.Rebuild();
                                    ApplyChanges();
                                    //                                    cc.PageTo(1);
                                }
                        }
                        else
                        {
                            if (mc.Model.FLevel != null)
                            {
                                mc.Model.FLevel.FLevel.PagerSettings.AllowPaging = false;
                                CellSet.Rebuild();
                                ApplyChanges();
                            }
                        }
                        return true;
                    }
                    if (args[1] != "null")
                    {
                        page = Convert.ToInt32(args[1]);
                        var count = mc.SiblingsCount;
                        var dummy = count % mc.Model.FLevel.FLevel.PagerSettings.LinesInPage;
                        var pages = count / mc.Model.FLevel.FLevel.PagerSettings.LinesInPage + 1;
                        if (page < 1 || page > pages)
                            return true;
                        mc.PageTo(page);
                    }
                }
                catch
                {
                    ;
                }
                return true;
            }

            if (args[0] == "drill")
            {
                int icol;
                int irow;
                if (args.Length < 4 || args[3] == "piemode")
                {
                    icol = Convert.ToInt32(args[2]) % FCellSet.ColumnCount;
                    irow = Convert.ToInt32(args[2]) / FCellSet.ColumnCount;
                }
                else
                {
                    icol = Convert.ToInt32(args[2]);
                    irow = Convert.ToInt32(args[3]);
                }

                var c = CellSet.Cells(icol, irow);
                if (c is IMemberCell)
                {
                    var mc = (IMemberCell) c;
                    if (args[1] == "l") mc.DrillAction(PossibleDrillActions.esNextLevel);
                    if (args[1] == "c") mc.DrillAction(PossibleDrillActions.esCollapsed);
                    if (args[1] == "h") mc.DrillAction(PossibleDrillActions.esNextHierarchy);
                    if (args[1] == "p") mc.DrillAction(PossibleDrillActions.esParentChild);
                }
                if (c is ILevelCell)
                {
                    callbackData = CallbackData.PivotAndData;
                    var lc = (ILevelCell) c;
                    if (args[1] == "l") lc.ExpandAllNodes(PossibleDrillActions.esNextLevel);
                    if (args[1] == "c") lc.CollapseAllNodes();
                    if (args[1] == "h") lc.ExpandAllNodes(PossibleDrillActions.esNextHierarchy);
                    if (args[1] == "p") lc.ExpandAllNodes(PossibleDrillActions.esParentChild);
                }

                if (args.Length > 3 && args[3] == "piemode")
                {
                    var i = FLayout.fColumnLevels.Count - 1;


                    var h1 = FLayout.fColumnLevels[i].Hierarchy.Dimension.Hierarchies.Where(item =>
                                                                                                item.DisplayName ==
                                                                                                FLayout.fColumnLevels[i]
                                                                                                    .DisplayName)
                        .FirstOrDefault();
                    if (h1 != null && FLayout.fColorAxisItem.UniqueName != h1.UniqueName)
                        Pivoting(h1, LayoutArea.laColor, 999, LayoutArea.laTree);
                }
                return true;
            }

            if (args[0] == "drillanywhere")
            {
                int icol;
                int irow;
                if (args.Length < 4 || args[3] == "piemode")
                {
                    icol = Convert.ToInt32(args[2]) % FCellSet.ColumnCount;
                    irow = Convert.ToInt32(args[2]) / FCellSet.ColumnCount;
                }
                else
                {
                    icol = Convert.ToInt32(args[2]);
                    irow = Convert.ToInt32(args[3]);
                }

                var c = CellSet.Cells(icol, irow);

                var mc = c as MemberCell;
                if (mc != null)
                {
                    Level toLevel = null;
                    if (args[1] == "l")
                    {
                        toLevel = mc.Member.Level.Hierarchy.Levels.FirstOrDefault(x => x.UniqueName == args[4]);
                        if (toLevel != null)
                            mc.ExpandNodesAnywhere(PossibleDrillActions.esNextLevel, toLevel);
                        else
                            mc.DrillAction(PossibleDrillActions.esNextLevel);
                    }

                    if (args[1] == "h")
                    {
                        var h = Dimensions.FindHierarchy(args[4]);
                        if (h.Levels[0] != null)
                            mc.ExpandNodesAnywhere(PossibleDrillActions.esNextHierarchy, h.Levels[0]);
                        else
                            mc.DrillAction(PossibleDrillActions.esNextHierarchy);
                    }
                }
                if (c is ILevelCell)
                {
                    if (args.Length < 5 || args[4].IsNullOrEmpty())
                        return false;

                    callbackData = CallbackData.PivotAndData;
                    var lc = (LevelCell) c;
                    Level toLevel = null;
                    if (args[1] == "l")
                    {
                        toLevel = lc.Level.Hierarchy.Levels.FirstOrDefault(x => x.UniqueName == args[4]);
                        if (toLevel != null)
                            lc.ExpandNodesAnywhere(PossibleDrillActions.esNextLevel, toLevel);
                        else
                            lc.ExpandAllNodes(PossibleDrillActions.esNextLevel);
                    }

                    if (args[1] == "h")
                    {
                        var h = Dimensions.FindHierarchy(args[4]);
                        if (h.Levels[0] != null)
                            lc.ExpandNodesAnywhere(PossibleDrillActions.esNextHierarchy, h.Levels[0]);
                        else
                            lc.ExpandAllNodes(PossibleDrillActions.esNextHierarchy);
                    }
                }

                return true;
            }


            if (args[0] == "remintelligence")
            {
                var h = Dimensions.FindHierarchy(args[2]);
                var ti = h.Intelligence[Convert.ToInt32(args[1])];
                var m = Measures.Find(args[3]);
                ti.RemoveMeasure(m);
                FCellSet.Rebuild();
                return true;
            }

            if (args[0] == "addintelligence")
            {
                var h = Dimensions.FindHierarchy(args[2]);
                var ti = h.Intelligence[Convert.ToInt32(args[1])];
                var m = Measures.Find(args[3]);
                ti.AddMeasure(m);
                FCellSet.Rebuild();
                return true;
            }
            //if (args[0] == "customAction")
            //{
            //    RCommonToolboxButton btn;
            //    if (Buttons.fToolItems.TryGetValue(args[1], out btn))
            //    {
            //        GridToolboxItemActionArgs e = new GridToolboxItemActionArgs(btn);
            //        if (OnToolboxItemAction(e))
            //        {
            //            Buttons.callbackResult = e.ResultValue;
            //        }
            //    }
            //    return true;
            //}

            if (callbackData == CallbackData.Data) callbackData = CallbackData.Nothing;
            return false;
        }

        internal void RenderCaptionPopup(ILevelCell lc)
        {
            GenericMenuItem MI;
            if (lc.Level.Measures != null)
            {
                MI = new GenericMenuItem(GenericMenuActionType.ShowDialog,
                    RadarUtils.GetResStr("rsCreateCalcMeasure"),
                    ImageUrl("Calculated_add.gif"), "createcalculatedmeasure");
                mnu.Add(MI);

                MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                    RadarUtils.GetResStr("rspHideMeasuresIfPossible"),
                    AxesLayout.HideMeasureIfPossible,
                    "changehidemeasures");
                mnu.Add(MI);
                return;
            }

            var H = lc.Level.Hierarchy;
            if (!H.AllowPopupOnLevelCaptions) return;
            GenericMenuItem MI1;
            //string separator = images.ImageUrl("Separator.gif", Page);
            var receivedArgument = lc.StartColumn.ToString() + '|' + lc.StartRow;
            if (H != null)
            {
                if (H.AllowRegroup && lc.Level.Index == 0)
                {
                    var s1 = "javascript:{RadarSoft.$('#" + ClientID + "').data('grid').createGroup('" +
                             receivedArgument + "','" +
                             RadarUtils.GetResStr("repEnterGroupName") + "','" +
                             RadarUtils.GetResStr("repNewGroupName") + "')}";
                    MI = new GenericMenuItem(GenericMenuActionType.RedirectTo,
                        RadarUtils.GetResStr("repCreateNewGroup"),
                        ImageUrl("NewGroup.gif"), s1);
                    mnu.Add(MI);
                    mnu.AddSeparator();
                }
                MI = new GenericMenuItem(GenericMenuActionType.ShowDialog,
                    RadarUtils.GetResStr("rsCreateCalcMember"),
                    ImageUrl("Member_add.gif"),
                    "createcalculatedmember|" + lc.Level.UniqueName);
                mnu.Add(MI);
                if (H.AllowChangeTotalAppearance && CellsetMode == CellsetMode.cmGrid)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("rspSbtShowFirst"),
                        H.TotalAppearance == TotalAppearance.taFirst,
                        "chta|first|" + receivedArgument);
                    mnu.Add(MI);
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("rspSbtShowLast"),
                        H.TotalAppearance == TotalAppearance.taLast,
                        "chta|last|" + receivedArgument);
                    mnu.Add(MI);
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("rspSbtDontShow"),
                        H.TotalAppearance == TotalAppearance.taInvisible,
                        "chta|none|" + receivedArgument);
                    mnu.Add(MI);

                    mnu.AddSeparator();
                }
                if (H.AllowResort)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("rspSortByDefault"),
                        lc.Level.SortType == MembersSortType.msTypeRelated,
                        "lsort|default|" + receivedArgument);
                    mnu.Add(MI);
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("rspSortAscending"),
                        lc.Level.SortType == MembersSortType.msNameAsc,
                        "lsort|asc|" + receivedArgument);
                    mnu.Add(MI);
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("rspSortDescending"),
                        lc.Level.SortType == MembersSortType.msNameDesc,
                        "lsort|desc|" + receivedArgument);
                    mnu.Add(MI);
                    mnu.AddSeparator();
                }
                MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                    RadarUtils.GetResStr("rspAggAllMembers"),
                    !H.TakeFiltersIntoCalculations,
                    "tfic|all|" + receivedArgument);
                mnu.Add(MI);
                MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                    RadarUtils.GetResStr("rspAggVisibleMembers"),
                    H.TakeFiltersIntoCalculations, "tfic|visible|" + receivedArgument);
                mnu.Add(MI);
                mnu.AddSeparator();

                if (AllowDrilling)
                    if (CellsetMode == CellsetMode.cmGrid)
                    {
                        MI = new GenericMenuItem(RadarUtils.GetResStr("rsDrillAnywhere"));
                        var level = lc.Level;
                        foreach (var childrenlevel in level.Hierarchy.Levels)
                            if (childrenlevel.Index > level.Index)
                            {
                                MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                                    childrenlevel.DisplayName, "",
                                    "drillanywhere|l|" + receivedArgument + "|" + childrenlevel.UniqueName);
                                MI.ChildItems.Add(MI1);
                            }

                        var ind = FLayout.fColumnAxis.IndexOf(level.Hierarchy) + 1;
                        TObservableCollection<Hierarchy> hiers = null;
                        if (ind > 0 && ind < FLayout.fColumnAxis.Count)
                        {
                            hiers = FLayout.fColumnAxis;
                        }
                        else
                        {
                            ind = FLayout.fRowAxis.IndexOf(level.Hierarchy) + 1;
                            if (ind > 0 && ind < FLayout.fRowAxis.Count)
                                hiers = FLayout.fRowAxis;
                        }

                        if (hiers != null)
                            for (var i = ind; i < hiers.Count; i++)
                            {
                                var h = hiers[i];
                                MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                                    h.DisplayName, "",
                                    "drillanywhere|h|" + receivedArgument + "|" + h.UniqueName);
                                MI.ChildItems.Add(MI1);
                            }

                        mnu.Add(MI);

                        MI = new GenericMenuItem(RadarUtils.GetResStr("repDrillAll"));
                        mnu.Add(MI);

                        MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                            RadarUtils.GetResStr("repDrillAllHierarchy"), "",
                            "drillall|h|" + receivedArgument);
                        MI.ChildItems.Add(MI1);
                        MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                            RadarUtils.GetResStr("repDrillAllLevel"), "",
                            "drillall|l|" + receivedArgument);
                        MI.ChildItems.Add(MI1);
                        MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                            RadarUtils.GetResStr("repDrillAllChildren"), "",
                            "drillall|p|" + receivedArgument);
                        MI.ChildItems.Add(MI1);
                        MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                            RadarUtils.GetResStr("repDrillAllUp"), "",
                            "drillall|c|" + receivedArgument);
                        mnu.Add(MI);
                        mnu.AddSeparator();
                    }
                    else
                    {
                        if (H.Levels.Count > 1)
                        {
                            MI = new GenericMenuItem(RadarUtils.GetResStr("rsLevelsToShow"));
                            mnu.Add(MI);

                            foreach (var l in H.Levels)
                            {
                                MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                                    l.DisplayName, l.Visible,
                                    (l.Visible ? "hidelevel" : "showlevel") + "|" + l.UniqueName);
                                MI.ChildItems.Add(MI1);
                            }
                            mnu.AddSeparator();
                        }

                        var a = lc.PossibleDrillActions;

                        var s = RadarUtils.GetResStr("repDrillAll") + " ";
                        if ((a & PossibleDrillActions.esNextHierarchy) == PossibleDrillActions.esNextHierarchy)
                        {
                            MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                                s + RadarUtils.GetResStr("repDrillAllHierarchy"), "",
                                "drillall|h|" + receivedArgument);
                            mnu.Add(MI1);
                        }
                        if ((a & PossibleDrillActions.esNextLevel) == PossibleDrillActions.esNextLevel)
                        {
                            MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                                s + RadarUtils.GetResStr("repDrillAllLevel"), "",
                                "drillall|l|" + receivedArgument);
                            mnu.Add(MI1);
                        }
                        if ((a & PossibleDrillActions.esParentChild) == PossibleDrillActions.esParentChild)
                        {
                            MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                                s + RadarUtils.GetResStr("repDrillAllChildren"), "",
                                "drillall|p|" + receivedArgument);
                            mnu.Add(MI1);
                        }
                        if ((a & PossibleDrillActions.esCollapsed) == PossibleDrillActions.esCollapsed)
                        {
                            MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                                RadarUtils.GetResStr("repDrillAllUp"), "",
                                "drillall|c|" + receivedArgument);
                            mnu.Add(MI);
                        }
                        mnu.AddSeparator();
                    }

                if (lc.Level != null && lc.Level.CubeLevel != null)
                    FillPropertiesMenu(lc.Level, mnu);

                FillFilterMenu(mnu, lc.Level, null, null, "");

                mnu.AddSeparator();

                MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                    RadarUtils.GetResStr("repShowEmpty"), H.ShowEmptyLines,
                    (H.ShowEmptyLines ? "hideempty|" : "showempty|") + receivedArgument);
                mnu.Add(MI);

                if (AllowPaging && lc.Level.PagerSettings.LinesInPage < lc.Level.CompleteMembersCount)
                {
                    mnu.AddSeparator();

                    MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("rsEnablePaging"), lc.Level.PagerSettings.AllowPaging,
                        (lc.Level.PagerSettings.AllowPaging ? "pagelevel|all|" : "pagelevel|allenable|") +
                        receivedArgument);
                    mnu.Add(MI);
                }
            }
        }

        internal void MakeFilterMenu(string levelName)
        {
            mnu.Clear();
            FillFilterMenu(mnu, Dimensions.FindLevel(levelName), levelName, CellSet.DefaultMeasure, "");
            ConvertGenericMenu(mnu, null);
        }

        internal void MakeMeaserMenu()
        {
            GenericMenuItem MI;
            MI = new GenericMenuItem(GenericMenuActionType.ShowDialog,
                RadarUtils.GetResStr("rsCreateCalcMeasure"),
                ImageUrl("Calculated_add.gif"), "createcalculatedmeasure");
            mnu.Add(MI);

            MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                RadarUtils.GetResStr("rspHideMeasuresIfPossible"),
                AxesLayout.HideMeasureIfPossible,
                "changehidemeasures");
            mnu.Add(MI);

            ConvertGenericMenu(mnu, null);
        }

        internal void MakeMenu(ICell c)
        {
            if (c is ILevelCell)
                RenderCaptionPopup((ILevelCell) c);
            if (c is IDataCell)
                RenderDataPopup((IDataCell) c);
            if (c is IMemberCell)
            {
                var mc = (IMemberCell) c;
                if (mc.Member != null)
                    if (mc.Member.MemberType == MemberType.mtMeasure)
                        RenderMeasureMemberPopup(mc);
                    else if (mc.Member.MemberType != MemberType.mtMeasureMode)
                        RenderMemberPopup(mc);
            }
            if (c is IChartCell)
                RenderChartPopup((IChartCell) c);
            if (mnu.Count > 0)
                if (mnu[mnu.Count - 1].ActionType == GenericMenuActionType.Separator)
                    mnu.RemoveAt(mnu.Count - 1);

            OnFillCustomMenu(mnu, c);

            ConvertGenericMenu(mnu, null);
        }

        internal virtual void MakeLegendMenu(string member)
        {
        }

        protected virtual void MakePivotMenu(IDescriptionable dim)
        {
            var h = dim as Hierarchy;
            GenericMenuItem MI;
            if (h != null)
            {
                var hplace = LayoutArea.laTree;

                if (AxesLayout.fColumnAxis.Contains(h)) hplace = LayoutArea.laColumn;
                if (AxesLayout.fRowAxis.Contains(h)) hplace = LayoutArea.laRow;

                if (hplace != LayoutArea.laColumn)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToCol"), ImageUrl("PivotRow.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laColumn + "|999");
                    GenericMnu.Add(MI);
                }

                if (hplace != LayoutArea.laRow)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToRow"), ImageUrl("PivotColumn.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laRow + "|999");
                    GenericMnu.Add(MI);
                }

                if (AxesLayout.fColorAxisItem != h)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToColor"), ImageUrl("PivotColor.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laColor + "|999");
                    GenericMnu.Add(MI);
                }

                if (AxesLayout.fColorForeAxisItem != h)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToColorFore"), ImageUrl("PivotForeColor.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laColorFore + "|999");
                    GenericMnu.Add(MI);
                }


                if (hplace != LayoutArea.laTree || AxesLayout.fColorForeAxisItem == h || AxesLayout.fColorAxisItem == h)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repRemoveToTree"), ImageUrl("DeleteGroup.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laTree + "|999");
                    GenericMnu.Add(MI);
                }


                if (h.AllowFilter && h.AllowHierarchyEditor && AllowFiltering)
                {
                    GenericMnu.AddSeparator();
                    MI = new GenericMenuItem(GenericMenuActionType.ExecuteFunction,
                        RadarUtils.GetResStr("rsFilter"), ImageUrl("filtr_edit.gif"),
                        "filterDialog('h:" + h.UniqueName + "')");
                    GenericMnu.Add(MI);
                }
            }

            var m = dim as Measure;
            if (m != null)
            {
                var from = "|999|laRow";
                var showDeleteItem = false;

                if (m.Visible == false)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("rep_MoveMeasureToValues"), ImageUrl("PivotRow.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laRow + "|999");
                    GenericMnu.Add(MI);
                }
                else
                {
                    showDeleteItem = true;
                }

                if (AxesLayout.fColorAxisItem != dim)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToColor"), ImageUrl("PivotColor.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laColor + "|999");
                    GenericMnu.Add(MI);
                }
                else
                {
                    showDeleteItem = true;
                    from = "|999|colors";
                }

                if (AxesLayout.fColorForeAxisItem != dim)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repMoveToColorFore"), ImageUrl("PivotForeColor.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laColorFore + "|999");
                    GenericMnu.Add(MI);
                }
                else
                {
                    showDeleteItem = true;
                    from = "|999|colorfore";
                }

                if (showDeleteItem)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                        RadarUtils.GetResStr("repHideMeasure"), ImageUrl("DeleteGroup.gif"),
                        "pivoting|" + dim.UniqueName + "|" + LayoutArea.laTree + from);
                    GenericMnu.Add(MI);
                }

                if (AllowFiltering)
                {
                    GenericMnu.AddSeparator();

                    MI = new GenericMenuItem(GenericMenuActionType.ExecuteFunction,
                        RadarUtils.GetResStr("rsFilter"), ImageUrl("filtr_edit.gif"),
                        "filterDialog('m:" + m.UniqueName + "')");
                    GenericMnu.Add(MI);
                }
            }

            ConvertGenericMenu(GenericMnu, mnu_control);
        }

        internal virtual void OnFillCustomMenu(GenericMenu mnu, ICell cell)
        {
            ;
        }

        internal virtual void ConvertGenericMenu(GenericMenu genericMenu, object realMenu)
        {
            if (realMenu == null) realMenu = mnu_control;
            ContextMenuItem prev = null;
            foreach (var mi in genericMenu)
                prev = DoConvertGenericMenu(realMenu, prev, mi);
        }

        protected virtual ContextMenuItem DoConvertGenericMenu(object parent, ContextMenuItem previousMenu,
            GenericMenuItem item)
        {
            var separator = ImageUrl("Separator.gif");

            var url_checked = "";
            //if (IsMvc == false)
            //    ResolveUrl(Page.ClientScript.GetWebResourceUrl(typeof(TreeView), "WebPartMenu_Check.gif"));

            var ec = string.IsNullOrEmpty(item.ExtraCommand) ? "}" : "; " + item.ExtraCommand + ";}";
            var s = "javascript:{RadarSoft.$('#" + ClientID + "').data('grid').executeMenuAction('***')" + ec;
            var s1 = "javascript:{RadarSoft.$('#" + ClientID + "').data('grid').executeMenuActionAll('***')" + ec;
            var sp = "javascript:{RadarSoft.$('#" + ClientID + "').data('grid').postBack('***')";
            var sdlg = "javascript:{RadarSoft.$('#" + ClientID + "').data('grid').showDialog('***')" + ec;
            var sfunc = "javascript:{RadarSoft.$('#" + ClientID + "').data('grid').***" + ec;

            var mi = new ContextMenuItem(item.Caption);
            mi.ImageUrl = item.ImageUrl;
            mi.Target = item.TargetPage;
            if (item.IsChecked)
                mi.ImageUrl = url_checked;
            switch (item.ActionType)
            {
                case GenericMenuActionType.Nothing:
                    mi.Selectable = false;
                    break;
                case GenericMenuActionType.RedirectTo:
                    mi.NavigateUrl = item.MenuItemValue;
                    break;
                case GenericMenuActionType.RefreshAll:
                    mi.NavigateUrl = s1.Replace("***", item.MenuItemValue);
                    break;
                case GenericMenuActionType.RefreshData:
                    mi.NavigateUrl = s.Replace("***", item.MenuItemValue);
                    break;
                case GenericMenuActionType.Separator:
                    previousMenu.IsSeparator = true;
                    return previousMenu;
                case GenericMenuActionType.ShowDialog:
                    mi.NavigateUrl = sdlg.Replace("***", item.MenuItemValue);
                    break;
                case GenericMenuActionType.ExecuteFunction:
                    mi.NavigateUrl = sfunc.Replace("***", item.MenuItemValue);
                    break;
            }
            if (parent is ContextMenu)
            {
                ((ContextMenu) parent).Items.Add(mi);
            }
            else
            {
                if (parent is ContextMenuItem)
                    ((ContextMenuItem) parent).ChildItems.Add(mi);
            }
            if (item.ChildItems.Count > 0)
            {
                ContextMenuItem prev = null;
                foreach (var gmi in item.ChildItems)
                    prev = DoConvertGenericMenu(mi, prev, gmi);
            }
            return mi;
        }

        protected virtual void UpdateMenuRecursive(ContextMenuItem MI)
        {
            MI.Text = MI.Text;
            foreach (var mi_ in MI.ChildItems)
                UpdateMenuRecursive(mi_);
        }

        private void RenderMeasureMemberPopup(IMemberCell mc)
        {
            GenericMenuItem MI;
            GenericMenuItem MI1;
            var url_resetfilter = ImageUrl("FiltrClear.png");
            var url_filter = ImageUrl("FilterGrid.png");

            var receivedArgument = mc.StartColumn.ToString() + '|' + mc.StartRow;
            var m = Measures[mc.Member.UniqueName];
            if (m.AggregateFunction == OlapFunction.stCalculated && !string.IsNullOrEmpty(m.Expression))
            {
                MI = new GenericMenuItem(GenericMenuActionType.ShowDialog,
                    RadarUtils.GetResStr("rsEditCalcMeasure"),
                    ImageUrl("Calculated_edit.gif"),
                    "editcalculatedmeasure|" + m.UniqueName);
                mnu.Add(MI);

                MI = new GenericMenuItem(GenericMenuActionType.Postback,
                    RadarUtils.GetResStr("rsDeleteCalcMeasure"),
                    ImageUrl("Calculated_delete.gif"),
                    "deletecalculatedmeasure|" + m.UniqueName);
                mnu.Add(MI);
            }

            MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                RadarUtils.GetResStr("repHideMeasure"),
                "", "hidemeasure|" + mc.Member.UniqueName);
            mnu.Add(MI);

            mnu.AddSeparator();

            if (m.Filter != null)
            {
                MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                    RadarUtils.GetResStr("repResetFilter"),
                    url_resetfilter, "resetcmfilter|" + m.UniqueName);
                mnu.Add(MI);
            }

            MI = new GenericMenuItem(GenericMenuActionType.ShowDialog,
                RadarUtils.GetResStr("rsfcFiltersOnValue"),
                url_filter, "showmfiltersettings|" + m.UniqueName);
            mnu.Add(MI);

            mnu.AddSeparator();

            var ss1 = "javascript:{RadarSoft.$('#" + ClientID + "').data('grid').modifyComment('" + receivedArgument +
                      "','" +
                      RadarUtils.GetResStr("rsCommentCellPrompt") + "','" +
                      mc.Comment + "')}";
            MI = new GenericMenuItem(GenericMenuActionType.RedirectTo,
                RadarUtils.GetResStr("rsCommentCell"),
                ImageUrl("EditComment.gif"), ss1);
            mnu.Add(MI);

            MI = new GenericMenuItem(RadarUtils.GetResStr("repShowAs"));
            mnu.Add(MI);

            foreach (var mm in m.ShowModes)
            {
                MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                    mm.Caption, mm.Visible, "measureshowmode|" + m.UniqueName + "|" + mm.Mode + "|" + mm.Caption);
                MI.ChildItems.Add(MI1);
            }
        }

        private void RenderMemberPopup(IMemberCell mc)
        {
            var receivedArgument = mc.StartColumn.ToString() + '|' + mc.StartRow;
            var pda = mc.PossibleDrillActions;
            GenericMenuItem MI = null;
            GenericMenuItem MI1;

            if (mc.Member is CalculatedMember)
            {
                var cc = (CalculatedMember) mc.Member;
                if (!string.IsNullOrEmpty(cc.Expression))
                {
                    MI = new GenericMenuItem(GenericMenuActionType.ShowDialog,
                        RadarUtils.GetResStr("rsEditCalcMember"),
                        ImageUrl("Member_edit.gif"),
                        "editcalculatedmember|" + cc.Level.UniqueName + "|" + cc.UniqueName);
                    mnu.Add(MI);

                    MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("rsDeleteCalcMember"),
                        ImageUrl("Member_delete.gif"),
                        "deletecalculatedmember|" + cc.Level.UniqueName + "|" + cc.UniqueName);
                    mnu.Add(MI);
                }
            }


            foreach (var ca in mc.CubeActions)
                if (ca.ActionType == CubeActionType.caURL)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RedirectTo,
                        ca.Caption, "", ca.Expression);
                    MI.TargetPage = "_blank";
                    mnu.Add(MI);
                }

            if (MI != null) mnu.AddSeparator();

            if (AllowDrilling)
            {
                if (CellsetMode == CellsetMode.cmGrid)
                {
                    MI = new GenericMenuItem(RadarUtils.GetResStr("rsDrillAnywhere"));
                    var level = mc.Member.Level;
                    foreach (var childrenlevel in level.Hierarchy.Levels)
                        if (childrenlevel.Index > level.Index)
                            if (mc.PossibleDrillActions.HasFlag(PossibleDrillActions.esNextLevel))
                            {
                                MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                                    childrenlevel.DisplayName, "",
                                    "drillanywhere|l|" + receivedArgument + "|" +
                                    childrenlevel.UniqueName);
                                MI.ChildItems.Add(MI1);
                            }

                    var ind = FLayout.fColumnAxis.IndexOf(level.Hierarchy) + 1;
                    TObservableCollection<Hierarchy> hiers = null;
                    if (ind > 0 && ind < FLayout.fColumnAxis.Count)
                    {
                        hiers = FLayout.fColumnAxis;
                    }
                    else
                    {
                        ind = FLayout.fRowAxis.IndexOf(level.Hierarchy) + 1;
                        if (ind > 0 && ind < FLayout.fRowAxis.Count)
                            hiers = FLayout.fRowAxis;
                    }

                    if (hiers != null)
                        for (var i = ind; i < hiers.Count; i++)
                        {
                            var h = hiers[i];
                            if (mc.PossibleDrillActions.HasFlag(PossibleDrillActions.esNextHierarchy))
                            {
                                MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                                    h.DisplayName, "",
                                    "drillanywhere|h|" + receivedArgument + "|" + h.UniqueName);
                                MI.ChildItems.Add(MI1);
                            }
                        }

                    mnu.Add(MI);
                }

                if ((PossibleDrillActions.esCollapsed & pda) == PossibleDrillActions.esCollapsed)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("repDrillUp"), "",
                        "drill|c|" + mc.StartColumn + "|" + mc.StartRow);
                    mnu.Add(MI);
                }
                if ((PossibleDrillActions.esParentChild & pda) == PossibleDrillActions.esParentChild)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("repDrillChildren"), "",
                        "drill|p|" + mc.StartColumn + "|" + mc.StartRow);
                    mnu.Add(MI);
                }
                if ((PossibleDrillActions.esNextLevel & pda) == PossibleDrillActions.esNextLevel)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("repDrillLevel"), "",
                        "drill|l|" + mc.StartColumn + "|" + mc.StartRow);
                    if ((PossibleDrillActions.esNextHierarchy & pda) == PossibleDrillActions.esNone)
                        mnu.Add(MI);
                }
                if ((PossibleDrillActions.esNextHierarchy & pda) == PossibleDrillActions.esNextHierarchy)
                {
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("repDrillHierarchy"), "",
                        "drill|h|" + mc.StartColumn + "|" + mc.StartRow);
                    mnu.Add(MI);
                }
                mnu.AddSeparator();
            }

            if (mc.Member.Level.Hierarchy.AllowFilter && AllowFiltering)
            {
                MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                    RadarUtils.GetResStr("repHideMember"),
                    ImageUrl("HideThis.gif"),
                    "hidethis|" + mc.StartColumn + "|" + mc.StartRow);
                mnu.Add(MI);

                MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                    RadarUtils.GetResStr("repHideExcept"),
                    ImageUrl("HideExcept.gif"),
                    "hideexcept|" + mc.StartColumn + "|" + mc.StartRow);
                mnu.Add(MI);

                MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                    RadarUtils.GetResStr("repHideMembersAbove"),
                    ImageUrl("HideAbove.gif"),
                    "hideabove|" + mc.StartColumn + "|" + mc.StartRow);
                mnu.Add(MI);

                MI = new GenericMenuItem(GenericMenuActionType.RefreshAll,
                    RadarUtils.GetResStr("repHideMembersBelow"),
                    ImageUrl("HideBelow.gif"),
                    "hidebelow|" + mc.StartColumn + "|" + mc.StartRow);
                mnu.Add(MI);

                if (!Cube.IsFilterAllowed(new CellSet.Filter(null, OlapFilterType.ftOnValue, null,
                        OlapFilterCondition.fcFirstTen, "0", "0")) && CellsetMode == CellsetMode.cmGrid)
                {
                    MI = new GenericMenuItem(RadarUtils.GetResStr("repShowOnlyTop"));
                    mnu.Add(MI);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "1", "",
                        "showtop|1|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "3", "",
                        "showtop|3|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "5", "",
                        "showtop|5|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "10", "",
                        "showtop|10|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);
                    MI1.ChildItems.AddSeparator();

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "10%", "",
                        "showtop|10%|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "25%", "",
                        "showtop|25%|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "50%", "",
                        "showtop|50%|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "75%", "",
                        "showtop|75%|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RedirectTo,
                        RadarUtils.GetResStr("repOther"), "",
                        "javascript:{RadarSoft.$('#" + ClientID + "').data('grid').customThreshold('" +
                        RadarUtils.GetResStr("repShowOnlyTop") +
                        "','" + RadarUtils.GetResStr("repOtherPrompt") + "','showtop','" + mc.StartRow + "','" +
                        mc.StartColumn + "')}");
                    MI.ChildItems.Add(MI1);

                    mnu.AddSeparator();

                    MI = new GenericMenuItem(RadarUtils.GetResStr("repShowOnlyBottom"));
                    mnu.Add(MI);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "1", "",
                        "showbottom|1|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "3", "",
                        "showbottom|3|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "5", "",
                        "showbottom|5|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "10", "",
                        "showbottom|10|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);
                    MI.ChildItems.AddSeparator();

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "10%", "",
                        "showbottom|10%|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "25%", "",
                        "showbottom|25%|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "50%", "",
                        "showbottom|50%|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshAll, "75%", "",
                        "showbottom|75%|" + mc.StartColumn + "|" + mc.StartRow);
                    MI.ChildItems.Add(MI1);

                    MI1 = new GenericMenuItem(GenericMenuActionType.RedirectTo, RadarUtils.GetResStr("repOther"), "",
                        "javascript:{RadarSoft.$('#" + ClientID + "').data('grid').customThreshold('" +
                        RadarUtils.GetResStr("repShowOnlyTop") +
                        "','" + RadarUtils.GetResStr("repOtherPrompt") + "','showbottom','" + mc.StartRow + "','" +
                        mc.StartColumn + "')}");
                    MI.ChildItems.Add(MI1);
                }

                if (mc.Member != null && mc.Member.Level != null && mc.Member.Level.CubeLevel != null)
                    FillPropertiesMenu(mc.Member.Level, mnu);

                FillFilterMenu(mnu, mc.Member.Level, null, null, "");
            }

            mnu.AddSeparator();

            var ss1 = "javascript:{RadarSoft.$('#" + ClientID + "').data('grid').modifyComment('" + receivedArgument +
                      "','" +
                      RadarUtils.GetResStr("rsCommentCellPrompt") + "','" +
                      mc.Comment + "')}";
            MI = new GenericMenuItem(GenericMenuActionType.RedirectTo,
                RadarUtils.GetResStr("rsCommentCell"),
                ImageUrl("EditComment.gif"), ss1);
            mnu.Add(MI);

            if (mc.Member.Level.Hierarchy.AllowRegroup)
            {
                var P = mc.Member.Level.CreateGroupList(mc.Member.Parent);

                var s1 = "javascript:{RadarSoft.$('#" + ClientID + "').data('grid').createGroup('" + receivedArgument +
                         "','" +
                         RadarUtils.GetResStr("repEnterGroupName") + "','" +
                         RadarUtils.GetResStr("repNewGroupName") + "')}";
                MI = new GenericMenuItem(GenericMenuActionType.RedirectTo,
                    RadarUtils.GetResStr("repCreateNewGroup"),
                    ImageUrl("NewGroup.gif"), s1);
                mnu.Add(MI);

                if (P.Count > 0 && mc.Member.MemberType != MemberType.mtGroup)
                {
                    if (mc.Member.MemberType == MemberType.mtCommon)
                    {
                        MI = new GenericMenuItem(RadarUtils.GetResStr("repMoveMemberToGroup"));
                        MI.ImageUrl = ImageUrl("GroupThis.gif");
                        mnu.Add(MI);

                        foreach (var g in P)
                        {
                            MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                                g.DisplayName, "",
                                "groupthis|" + g.UniqueName + "|" + mc.StartColumn + "|" + mc.StartRow);
                            MI.ChildItems.Add(MI1);
                        }
                    }

                    MI = new GenericMenuItem(RadarUtils.GetResStr("repMoveExceptToGroup"));
                    MI.ImageUrl = ImageUrl("GroupExcept.gif");
                    mnu.Add(MI);

                    foreach (var g in P)
                    {
                        MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                            g.DisplayName, "",
                            "groupexcept|" + g.UniqueName + "|" + mc.StartColumn + "|" + mc.StartRow);
                        MI.ChildItems.Add(MI1);
                    }

                    MI = new GenericMenuItem(RadarUtils.GetResStr("repMoveBelowToGroup"));
                    MI.ImageUrl = ImageUrl("GroupBelowThis.gif");
                    mnu.Add(MI);

                    foreach (var g in P)
                    {
                        MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                            g.DisplayName, "",
                            "groupbelow|" + g.UniqueName + "|" + mc.StartColumn + "|" + mc.StartRow);
                        MI.ChildItems.Add(MI1);
                    }

                    if (mc.Member.MemberType == MemberType.mtCommon)
                    {
                        MI = new GenericMenuItem(RadarUtils.GetResStr("repMoveAllToGroup"));
                        MI.ImageUrl = ImageUrl("GroupAll.gif");
                        mnu.Add(MI);

                        foreach (var g in P)
                        {
                            MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                                g.DisplayName, "",
                                "groupall|" + g.UniqueName + "|" + mc.StartColumn + "|" + mc.StartRow);
                            MI.ChildItems.Add(MI1);
                        }
                    }
                }

                if (mc.Member.MemberType == MemberType.mtGroup)
                {
                    var g = mc.Member as GroupMember;
                    if (g.DeleteableByUser)
                    {
                        MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                            RadarUtils.GetResStr("repDeleteGroup"),
                            ImageUrl("DeleteGroup.gif"),
                            "deletegroup|" + mc.StartColumn + "|" + mc.StartRow);
                        mnu.Add(MI);
                    }
                    MI = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("repClearGroup"),
                        ImageUrl("ClearGroup.gif"),
                        "cleargroup|" + mc.StartColumn + "|" + mc.StartRow);
                    mnu.Add(MI);

                    s1 = "javascript:{RadarSoft.$('#" + ClientID + "').data('grid').renameGroup('" + receivedArgument +
                         "','" +
                         RadarUtils.GetResStr("repEnterGroupName") + "','" +
                         g.DisplayName + "')}";
                    MI = new GenericMenuItem(GenericMenuActionType.RedirectTo,
                        RadarUtils.GetResStr("repRenameGroup"),
                        ImageUrl("RenameGroup.gif"), s1);
                    mnu.Add(MI);
                }

                if (mc.Member.Parent != null && mc.Member.Parent.MemberType == MemberType.mtGroup)
                {
                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("repMoveFromGroup"), "",
                        "ungroupthis|" + mc.StartColumn + "|" + mc.StartRow);
                    mnu.Add(MI1);
                }
            }
        }

        private void RenderChartPopup(IChartCell c)
        {
            var mi = new GenericMenuItem(GenericMenuActionType.ExecuteFunction,
                RadarUtils.GetResStr("mnShowUnderlying"), ImageUrl("show_underlying.png"), "showDetails();");
            mnu.Add(mi);
            mi = new GenericMenuItem(GenericMenuActionType.ExecuteFunction,
                RadarUtils.GetResStr("mnFilterSelection"), ImageUrl("filtr_sel.png"), "filterSelected();");
            mnu.Add(mi);
        }

        private void FillPropertiesMenu(Level level, GenericMenu mnu)
        {
            var ii = level.CubeLevel.InfoAttributes;

            GenericMenuItem MI1, MI;
            if (ii.Count > 0)
            {
                mnu.AddSeparator();

                MI = new GenericMenuItem(RadarUtils.GetResStr("rsShowPropertiesReport"));
                if (ii.Count > 1)
                {
                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("rsShowAllProperties"), "",
                        "showinfos|showallinreport|" + level.UniqueName);
                    MI.ChildItems.Add(MI1);
                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("rsHideAllProperties"), "",
                        "showinfos|hideallinreport|" + level.UniqueName);
                    MI.ChildItems.Add(MI1);

                    MI.ChildItems.AddSeparator();
                }

                foreach (var ia in ii)
                {
                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData, ia.DisplayName,
                        ia.IsDisplayModeAsColumn,
                        "showinfos|showattrep|" + level.UniqueName + "|" + ia.fUniqueName);
                    MI.ChildItems.Add(MI1);
                }

                mnu.Add(MI);

                MI = new GenericMenuItem(RadarUtils.GetResStr("rsShowPropertiesTooltip"));
                if (ii.Count > 1)
                {
                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("rsShowAllProperties"), "",
                        "showinfos|showallintt|" + level.UniqueName);
                    MI.ChildItems.Add(MI1);
                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData,
                        RadarUtils.GetResStr("rsHideAllProperties"), "",
                        "showinfos|hideallintt|" + level.UniqueName);
                    MI.ChildItems.Add(MI1);

                    MI.ChildItems.AddSeparator();
                }

                foreach (var ia in ii)
                {
                    MI1 = new GenericMenuItem(GenericMenuActionType.RefreshData, ia.DisplayName,
                        ia.IsDisplayModeAsTooltip,
                        "showinfos|showatttt|" + level.UniqueName + "|" + ia.DisplayName);
                    MI.ChildItems.Add(MI1);
                }

                mnu.Add(MI);
            }
        }

        #endregion

        internal HierarchyEditor heditor;
        protected int icol = -1;
        protected int irow = -1;
        protected string uid = string.Empty;
        protected string legendId = string.Empty;

        internal virtual void RaiseCallback(string eventArgument, string data)
        {
            RaiseCallbackHandler(eventArgument);
        }

        internal virtual void RaiseCallbackHandler(string eventArgument)
        {
            if (callbackException != null) return;

#if !DEBUG
            try
            {
#endif
            if (HandleDataCallback(eventArgument))
                return;

            var args = eventArgument.Split('|');

            if (args[0] == "upload")
            {
                var xml =
                    "<?xml version=\"1.0\"?><OlapGridSerializer>  <ChartTypes>    <ArrayOfInt xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xsi:nil=\"true\" />  </ChartTypes>  <AxesLayout>    <SerializedLayout xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">      <RowHierarchies>        <string>277fbc4e-0ccb-4307-bd3b-a02511c86913</string>      </RowHierarchies>      <ColumnHierarchies>       <string>eb5d2721-d6da-4e6a-8081-393fcf5cbebd</string>      </ColumnHierarchies>      <PageHierarchies />      <DetailsHierarchies />      <Measures>        <SerializedMeasure>          <ActiveShowModes>            <ShowMode>smNormal</ShowMode>          </ActiveShowModes>          <Intelligences />          <IntelligenceParents />          <UniqueName>c09376b3-40b6-4289-b5d2-0eba776aaf24</UniqueName>          <DefaultFormat>#,###</DefaultFormat>        </SerializedMeasure>      </Measures>      <OpenendNodes />      <OpenendActions />      <Drills />    </SerializedLayout>  </AxesLayout>  <Hierarchies>    <ArrayOfTSerializedHierarchy xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">      <SerializedHierarchy>       <UniqueName>baccd409-9c92-4e21-b362-c8dab08ed670</UniqueName>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>43693ef1-3f70-4cd2-8117-d45f3cc2731d</UniqueName>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>f3038a97-0505-41b1-8d24-751f0beb738a</UniqueName>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>277fbc4e-0ccb-4307-bd3b-a02511c86913</UniqueName>        <Levels>          <SerializedLevel>            <Visible>true</Visible>          </SerializedLevel>          <SerializedLevel />        </Levels>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>8c988e1e-b220-4d94-9f21-1d99f0331522</UniqueName>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>dda182e6-5e2f-476b-ab05-ece9c49acc75</UniqueName>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>70f3d747-c805-4c3a-8e72-9ba594d038a4.[HomePhone]</UniqueName>        <TotalCaption>¬сего</TotalCaption>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>70f3d747-c805-4c3a-8e72-9ba594d038a4.[Country]</UniqueName>        <TotalCaption>¬сего</TotalCaption>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>70f3d747-c805-4c3a-8e72-9ba594d038a4.[PostalCode]</UniqueName>        <TotalCaption>¬сего</TotalCaption>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>70f3d747-c805-4c3a-8e72-9ba594d038a4.[Region]</UniqueName>        <TotalCaption>¬сего</TotalCaption>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>70f3d747-c805-4c3a-8e72-9ba594d038a4.[City]</UniqueName>        <TotalCaption>¬сего</TotalCaption>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>70f3d747-c805-4c3a-8e72-9ba594d038a4.[Address]</UniqueName>        <TotalCaption>¬сего</TotalCaption>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>64afdc15-1d60-40de-aebf-d3f20837c307</UniqueName>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>12ae4b8c-125e-47cf-b62b-ad80d630e3ea.[Address]</UniqueName>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>12ae4b8c-125e-47cf-b62b-ad80d630e3ea.[City]</UniqueName>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>12ae4b8c-125e-47cf-b62b-ad80d630e3ea.[Region]</UniqueName>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>12ae4b8c-125e-47cf-b62b-ad80d630e3ea.[PostalCode]</UniqueName>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>12ae4b8c-125e-47cf-b62b-ad80d630e3ea.[Country]</UniqueName>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>12ae4b8c-125e-47cf-b62b-ad80d630e3ea.[Phone]</UniqueName>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>eb5d2721-d6da-4e6a-8081-393fcf5cbebd</UniqueName>        <Levels>          <SerializedLevel>            <Visible>true</Visible>          </SerializedLevel>          <SerializedLevel />          <SerializedLevel />        </Levels>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>3e493d8a-9638-40a7-bf48-8aa5e0cc9607</UniqueName>        <FormatString>YYYY</FormatString>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>cd77f89e-9a7d-441a-b1a7-5d24e29295a1</UniqueName>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>e1754fe6-c878-4ae4-8115-a9bb23f1aecf</UniqueName>        <FormatString>MMMM</FormatString>      </SerializedHierarchy>      <SerializedHierarchy>        <UniqueName>a87f51c4-5ecb-48a7-ae34-768534f29fbf</UniqueName>      </SerializedHierarchy>    </ArrayOfTSerializedHierarchy>  </Hierarchies>  <CommentAddresses>    <ArrayOfTSerializedCubeAddress xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xsi:nil=\"true\" />  </CommentAddresses>  <CommentStrings>    <ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xsi:nil=\"true\" />  </CommentStrings>  <ColumnsWidthNames>    <ArrayOfString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xsi:nil=\"true\" />  </ColumnsWidthNames>  <ColumnsWidths>    <ArrayOfDouble xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xsi:nil=\"true\" />  </ColumnsWidths>  <ChartsType>    <ArrayOfSeriesType xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xsi:nil=\"true\" />  </ChartsType></OlapGridSerializer>";
                Serializer.XMLString = xml;
                ApplyChanges();
                callbackData = CallbackData.PivotAndData;
                return;
            }

            //if (IsMvc == false)
            //{
            //    if (args[0] == "cancelajax")
            //    {
            //        List<string> ls = (List<string>)Session["OLP_Cancelled"];
            //        if (ls == null) ls = new List<string>();
            //        ls.Add(args[1]);
            //        Session["OLP_Cancelled"] = ls;
            //        callbackData = CallbackData.Nothing;
            //        return;
            //    }
            //}

            if (args[0] == "heditor")
            {
                if (args[1] == "cancel")
                {
                    callbackData = CallbackData.Nothing;
                    SessionState.Delete(SessionKey.olapgrid_heditor, UniqueID);
                    return;
                }
                if (args[1] == "apply")
                {
                    callbackData = CallbackData.PivotAndData;
                    heditor = new HierarchyEditor(this);
                    SessionState.ReadStreamedObject(SessionKey.olapgrid_heditor, heditor, UniqueID);
                    heditor.Restore(this);
                    heditor.Apply();
                    SessionState.Delete(SessionKey.olapgrid_heditor, UniqueID);
                    heditor = null;
                    return;
                }
                if (SessionState.KeyExists(SessionKey.olapgrid_heditor, UniqueID))
                {
                    heditor = new HierarchyEditor(this);
                    SessionState.ReadStreamedObject(SessionKey.olapgrid_heditor, heditor, UniqueID);
                    heditor.Restore(this);
                }
                else
                {
                    heditor = new HierarchyEditor(this);
                }
                callbackData = heditor.ProcessCallback(args);
                //ApplyChanges();
                //EnsureStateRestored();
                SessionState.Write(heditor, SessionKey.olapgrid_heditor, UniqueID);
                return;
            }

            if (args[0] == "createpopup")
            {
                if (args.Length < 4)
                {
                    icol = Convert.ToInt32(args[1]) % FCellSet.ColumnCount;
                    irow = Convert.ToInt32(args[1]) / FCellSet.ColumnCount;
                }
                SessionState.Write(irow, SessionKey.olapgrid_cellcoordrow, UniqueID);
                SessionState.Write(icol, SessionKey.olapgrid_cellcoordcol, UniqueID);
                callbackData = CallbackData.Popup;
                return;
            }

            if (args[0] == "createpopup2")
            {
                uid = args[1];
                callbackData = CallbackData.Popup;
                return;
            }

            if (args[0] == "loadclientlayout" && args.Length > 1 && args[1].IsFill())
                UploadStateFile(args[1]);
            if (args[0] == "changesn")
            {
                if (Toolbox.MDCube == null)
                {
                    _CallbackResult = "e|Not connected to MOlapCube";
                    callbackData = CallbackData.ResultString;
                    return;
                }
                Session.SetString(ClientID + "$server", args[1]);
                string dbs;
                if (!Toolbox.MDCube.GetDatabasesList(args[1], "", out dbs))
                {
                    _CallbackResult = "e|" + string.Format(Toolbox.ConnectButton.LoginWindowSettings.ErrorString, dbs);
                    callbackData = CallbackData.ResultString;
                    return;
                }
                _CallbackResult = "s|" + dbs;
                callbackData = CallbackData.ResultString;
                return;
            }
            if (args[0] == "changedb")
            {
                if (Toolbox.MDCube == null)
                {
                    _CallbackResult = "e|Not connected to MOlapCube";
                    callbackData = CallbackData.ResultString;
                    return;
                }
                var server = Session.GetString(ClientID + "$server") ??
                             Toolbox.ConnectButton.LoginWindowSettings.ServerName;
                Session.SetString(ClientID + "$db", args[1]);
                string cubes;
                if (!Toolbox.MDCube.GetCubesList(server, args[1], "", out cubes))
                {
                    _CallbackResult =
                        "e|" + string.Format(Toolbox.ConnectButton.LoginWindowSettings.ErrorString, cubes);
                    callbackData = CallbackData.ResultString;
                    return;
                }
                _CallbackResult = "s|" + cubes;
                callbackData = CallbackData.ResultString;
                return;
            }

            //if (args[0] == "ajaxactivation")
            //{
            //    if (Cube == null)
            //        throw new NullReferenceException("Cube should be defined");
            //    Cube.ExecuteAjaxActivation();
            //    callbackData = CallbackData.PivotAndData;
            //    return;
            //}

            HandlePivotCallback(eventArgument, null);
#if !DEBUG
            }
            catch (Exception E)
            {
                callbackException = E;
                callbackExceptionData = new Dictionary<string, string>(1);
                callbackExceptionData.Add("eventArgument", eventArgument);
            }
#endif
        }

        internal void UploadStateFile(string state)
        {
            callbackData = CallbackData.PivotAndData;
            Serializer.XMLString = state;
        }

        protected virtual bool HandleToolboxCallback(string eventArgument)
        {
            var args = eventArgument.Split('|');

            var isToolboxAction = false;
            var grid = this as OlapGrid;
            if (grid != null)
            {
                switch (args[0])
                {
                    case "clearlayout":
                        grid.AxesLayout.Clear();
                        isToolboxAction = true;
                        return true;
                    case "showpivotareas":
                        grid.ShowAreasMode = rsShowAreasOlapGrid.rsPivot;
                        isToolboxAction = true;
                        return true;
                    case "showonlydata":
                        grid.ShowAreasMode = rsShowAreasOlapGrid.rsDataOnly;
                        isToolboxAction = true;
                        return true;
                    case "showallareas":
                        grid.ShowAreasMode = rsShowAreasOlapGrid.rsAll;
                        isToolboxAction = true;
                        return true;
                    case "showmodificationareas":
                        grid.ShowModificationAreas = true;
                        isToolboxAction = true;
                        return true;
                    case "hidemodificationareas":
                        grid.ShowModificationAreas = false;
                        isToolboxAction = true;
                        return true;
                }

                if (isToolboxAction && args.Length > 1 && args[1].IsFill())
                    SetChartsTypes(args[1]);
            }

            if (args[0] == "custommenuaction")
            {
                var icol = Convert.ToInt32(args[2]) % FCellSet.ColumnCount;
                var irow = Convert.ToInt32(args[2]) / FCellSet.ColumnCount;
                var c = CellSet.Cells(icol, irow);
                DoContextMenuClick(args[1], c);
                return true;
            }

            callbackData = CallbackData.Nothing;
            if (eventArgument == "")
            {
                if (SessionState == null)
                    throw new Exception("SessionState is null");
                SessionState.Delete(SessionKey.olapgrid_heditor, UniqueID);
                return true;
            }

            if (args[0] == "showfiltersettings")
            {
                Level l = Dimensions.FindLevel(args[1]);
                if (l != null)
                {
                    CellSet.Filter ff;
                    Measure m = null;
                    OlapFilterCondition fc = (OlapFilterCondition)Enum.Parse(typeof(OlapFilterCondition), args[3]);
                    OlapFilterType ft = (OlapFilterType)Enum.Parse(typeof(OlapFilterType), args[2]);
                    if (args.Length > 5)
                        m = Measures.Find(args[4]);
                    if ((l.Filter != null) && (l.Filter.FilterCondition == fc)
                        && (l.Filter.FilterType == ft))
                        ff = l.Filter;
                    else
                        ff = new CellSet.Filter(l, ft, m, fc, "", null);
                    _filterconditiondlg = FilterConditionDialog.MakeHTML(ff);
                    callbackData = CallbackData.FilterSettings;
                    return true;
                }
                callbackData = CallbackData.Nothing;
                return true;
            }

            if (args[0] == "showmfiltersettings")
            {
                Measure m = Measures.Find(args[1]);
                if (m != null)
                {
                    MeasureFilter ff = m.Filter ?? new MeasureFilter(m, OlapFilterCondition.fcGreater, "", null);
                    _filterconditiondlg = FilterConditionDialog.MakeHTML(ff);
                    callbackData = CallbackData.FilterSettings;
                    return true;
                }
                callbackData = CallbackData.Nothing;
                return true;
            }

            if (args[0] == "createcalculatedmember")
            {
                Level l = Dimensions.FindLevel(args[1]);
                if (l == null)
                {
                    callbackData = CallbackData.Nothing;
                    return true;
                }
                _filterconditiondlg = MakeCalculated.MakeHTMLMember(this, l, null);
                callbackData = CallbackData.FilterSettings;
                return true;
            }

            if (args[0] == "editcalculatedmember")
            {
                Level l = Dimensions.FindLevel(args[1]);
                CalculatedMember m = (l != null) ? (l.FindMember(args[2]) as CalculatedMember) : null;
                if ((l == null) || (m == null))
                {
                    callbackData = CallbackData.Nothing;
                    return true;
                }
                _filterconditiondlg = MakeCalculated.MakeHTMLMember(this, l, m);
                callbackData = CallbackData.FilterSettings;
                return true;
            }

            if (args[0] == "createcalculatedmeasure")
            {
                _filterconditiondlg = MakeCalculated.MakeHTML(this, null);
                callbackData = CallbackData.FilterSettings;
                return true;
            }

            if (args[0] == "editcalculatedmeasure")
            {
                Measure m = Measures.Find(args[1]);
                _filterconditiondlg = MakeCalculated.MakeHTML(this, m);
                callbackData = CallbackData.FilterSettings;
                return true;
            }

            //if (args[0] == "dodrillthrough")
            //{
            //    ICell c = CellSet.Cells(Convert.ToInt32(SessionState.ReadObject(TSessionKey.olapgrid_cellcoordcol, UniqueID)),
            //        Convert.ToInt32(SessionState.ReadObject(TSessionKey.olapgrid_cellcoordrow, UniqueID)));
            //    TCubeAction ca = c.CubeActions[Convert.ToInt32(args[1])];
            //    DataSet ds = new DataSet();
            //    DataTable dt = new DataTable();
            //    ds.Tables.Add(dt);
            //    FEngine.Drillthrough(dt, ca.Expression);
            //    DoDrillthrough(ds);
            //    return;
            //}

            if (args[0] == "popupselected")
            {
                RaiseContextMenuClick(args[1]);
                return true;
            }

            if (args[0] == "pivoting")
            {
                if (args[1] == null)
                    throw new Exception("args[1] is null");
                if (args[1].StartsWith("node_"))
                {
                    callbackData = CallbackData.PivotAndData;
                    FillTree();
                    string s = args[1].Substring(args[1].LastIndexOf('t') + 1);
                    if (s[s.Length - 1] == 'i') s = s.Substring(0, s.Length - 1);
                    int index = Convert.ToInt32(s);
                    bool IsMeasure;
                    jQueryTreeNode tn = FindTreeNode(index, out IsMeasure);
                    if (IsMeasure)
                    {
                        if (Measures == null)
                            throw new Exception("Measures is null");
                        Measure m = Measures.Find(tn.Value);
                        m.Visible = true;
                    }
                    else
                    {
                        if (Dimensions == null)
                            throw new Exception("Dimensions is null");
                        Hierarchy h = Dimensions.FindHierarchy(tn.Value);

                        if (args[2] == "row") Pivoting(h, LayoutArea.laRow, Convert.ToInt16(args[3]));
                        if (args[2] == "")
                        {
                            LayoutArea laa = (h.IsDate) ? LayoutArea.laColumn : LayoutArea.laRow;
                            Pivoting(h, laa, Convert.ToInt16(args[3]));
                        }
                        if (args[2] == "col") Pivoting(h, LayoutArea.laColumn, Convert.ToInt16(args[3]));
                        if (args[2] == "page") Pivoting(h, LayoutArea.laPage, Convert.ToInt16(args[3]));
                    }
                    return true;
                }
            }

            if (args[0] == "connectiondialog")
            {
                _filterconditiondlg = Toolbox.MakeConnectionDialog();
                callbackData = CallbackData.FilterSettings;
                return true;
            }

            if (args[0] == "mdxdialog")
            {
                _filterconditiondlg = Toolbox.MakeMDXDialog();
                callbackData = CallbackData.FilterSettings;
                return true;
            }

            if (eventArgument.StartsWith("upload_"))
            {
                var e = new ToolboxItemActionArgs(Toolbox.LoadLayoutButton);
                if (Toolbox.OnToolboxItemAction(e))
                {
                    if (e.Handled) return true;
                }
                //HttpPostedFile f = File;
                //if (f != null)
                //{
                //    if (f.FileName.ToLower().EndsWith(".xml"))
                //        Serializer.ReadXML(f.InputStream);
                //    if (f.FileName.ToLower().EndsWith(".rsdata"))
                //        Load(f.InputStream);
                //}
                return true;
            }

            return false;
        }

        internal void RaiseContextMenuClick(string id)
        {
            var c = CellSet.Cells(Convert.ToInt32(SessionState.ReadObject(SessionKey.olapgrid_cellcoordcol, UniqueID)),
                Convert.ToInt32(SessionState.ReadObject(SessionKey.olapgrid_cellcoordrow, UniqueID)));

            DoContextMenuClick(id, c);
        }

        internal PivotingBehavior CalcBehavior(LayoutArea area)
        {
            if (CellSet == null)
                return PivotingBehavior;

            if (PivotingBehavior == PivotingBehavior.RadarCube)
                return PivotingBehavior.RadarCube;

            if (area == LayoutArea.laColumn)
            {
                var h = AxesLayout.ColumnAxis.LastOrDefault();
                if (h == null) return PivotingBehavior;

                var i = 1;
                foreach (var l in CellSet.FColumnLevels)
                    i *= l.FLevel.CompleteMembersCount;
                i *= h.Levels[0].CompleteMembersCount;

                return i > 1000 ? PivotingBehavior.RadarCube : PivotingBehavior.Excel2010;
            }

            if (area == LayoutArea.laRow)
            {
                var h = AxesLayout.RowAxis.LastOrDefault();
                if (h == null) return PivotingBehavior;

                var i = 1;
                foreach (var l in CellSet.FRowLevels)
                    i *= l.FLevel.CompleteMembersCount;
                i *= h.Levels[0].CompleteMembersCount;

                return i > 1000 ? PivotingBehavior.RadarCube : PivotingBehavior.Excel2010;
            }

            return PivotingBehavior;
        }

        //public void UpdatePagingForLevels(bool allowPaging, int linesInPage)
        //{
        //    if (Dimensions != null)
        //    {
        //        foreach (Dimension d in Dimensions)
        //        {
        //            if (d.Hierarchies != null)
        //            {
        //                foreach (Hierarchy h in d.Hierarchies)
        //                {
        //                    if (h.Levels != null)
        //                    {
        //                        foreach (Level l in h.Levels)
        //                        {
        //                            l.FPagerSettings.AllowPaging = allowPaging;
        //                            l.FPagerSettings.LinesInPage = linesInPage;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        /// <summary>
        ///     Occurs, when an error arises during the building of a cellset.
        /// </summary>
        /// <remarks>
        ///     If this error is not handled (i.e. the e.Handled
        ///     flag is not set to True), the standard dialog window with the information about it will
        ///     appear.
        /// </remarks>
        /// <example>
        ///     The simple TCustomOLAPGrid.Error event handler:
        ///     <code lang="CS">
        /// void Grid_Error(object sender, ExceptionHandlerEventArs e)
        /// {
        ///     MessageBox.Show(e.Exception.Message);
        ///     (sender as OlapControl).Serializer.XMLString = e.LastSuccessfulState;
        ///     e.Handled = true;
        /// }
        /// </code>
        ///     <code lang="VB">
        /// Private Sub Grid_Error(sender As Object, e As ExceptionHandlerEventArs)
        ///     MessageBox.Show(e.Exception.Message)
        ///     TryCast(sender, OlapControl).Serializer.XMLString = e.LastSuccessfulState
        ///     e.Handled = True
        /// End Sub
        /// </code>
        /// </example>
        public event EventHandler<ExceptionHandlerEventArs> Error;

        /// <summary>
        ///     Raises the <see cref="TCustomOLAPControl.Error">Error</see> event.
        /// </summary>
        protected internal virtual bool OnError(Exception ex)
        {
            DebugLogging.WriteLineException("TCustomOLAPGrid.OnError:", ex);
            // OLAPWPF
            if (FCellSet == null)
                return false;

            var e = new ExceptionHandlerEventArs(ex, CellSet.latestState);
            if (Error != null)
                Error(this, e);

            return e.Handled;
        }

        internal virtual void InitChartAreas(RCell rcell, ICell cell)
        {
        }

        internal virtual void InitClientCellset(RCellset rcellset)
        {
        }

        internal IDescriptionable FindMeasureOrHierarchy(string uniqueName)
        {
            IDescriptionable result = Measures.Find(uniqueName);
            if (result == null)
                return Dimensions.FindHierarchy(uniqueName);
            return result;
        }

        internal void CheckMetadata()
        {
            if (!Cube.Active)
                return;
        }

        internal virtual void Close()
        {
            BeginUpdate();

            if (fMeasures != null)
            {
                foreach (var m in fMeasures)
                {
                    m.Visible = false;
                    m.Close();
                }
                fMeasures.Clear();
                fMeasures.FGrid = null;
            }

            fMeasures = null;

            if (FDimensions != null)
            {
                FDimensions.DeleteGroups();
                FDimensions.DeleteCalculatedMembers();

                foreach (var d in FDimensions)
                {
                    foreach (var h in d.Hierarchies)
                        h.ResetHierarchy();
                    d.FGrid = null;
                }
                FDimensions.Clear();
                FDimensions.FGrid = null;
            }
            FDimensions = null;


            if (FEngine != null)
            {
                FEngine.Clear();
                FEngine.FGrid = null;
                FEngine = null;
            }

            if (FCellSet != null)
            {
                FCellSet.Dispose();
                FCellSet = null;
            }

            if (FLayout != null)
            {
                FLayout.Clear();
                FLayout.fGrid = null;
                FLayout = null;
            }

            FilteredHierarchiesClear();
            FFilteredHierarchies = null;

            FilteredLevelsClear();
            FFilteredLevels = null;

            if (_OnGridEventList != null)
            {
                foreach (var i in _OnGridEventList.ToArray())
                    OnGridEvent -= i;
                _OnGridEventList.Clear();
            }
            _OnGridEventList = null;

            EndUpdate();
        }


        internal bool IsDisposed2 { get; } = false;

        internal bool IsAllowMember(ICubeAddress address, Member member, bool isTotal)
        {
            if (OnAllowDisplayMember == null)
                return true;
            var a = new AllowDisplayMemberArgs(address, member, isTotal);
            OnAllowDisplayMember(this, a);
            return a.Allow;
        }

        public Tuple<double, double> GetMeasureRange(ICubeAddress address)
        {
            if (address.Measure == null || address.MeasureMode == null)
                return null;
            var l = FEngine.GetMetaline(address.FLineID)
                .GetLine(address.FHierID, address.Measure, address.MeasureMode);
            return l.GetRange();
        }

        /// <summary>
        ///     References to the auxiliary object containing methods for saving/restoring the
        ///     current OLAP Slice in the XML format.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         An instance of the OlapGridSerializer object returned by this property has
        ///         two useful methods:
        ///     </para>
        ///     <list type="bullet">
        ///         <item>WriteXML, which saves the current Grid state in the XML format.</item>
        ///         <item>ReadXML, which restores the prior saved state.</item>
        ///     </list>
        ///     <para class="xmldocbulletlist">
        ///         For example, you can save the current Grid state in
        ///         a file by using the following code:
        ///     </para>
        ///     <para class="xmldocbulletlist">
        ///         // saves the Grid state in the "Temp" folder of the
        ///         web application
        ///     </para>
        ///     <para class="xmldocbulletlist">
        ///         OlapAnalysis1.Serializer.WriteXML(MapPath("~/Temp/CurrentState.xml"));
        ///     </para>
        ///     <para class="xmldocbulletlist">// restores the prior saved Grid state</para>
        ///     <para class="xmldocbulletlist">
        ///         OlapAnalysis1.Serializer.ReadXML(MapPath("~/Temp/CurrentState.xml"));
        ///     </para>
        /// </remarks>
        /// <example>
        ///     For example, you can save the current Grid state in a file by using the following
        ///     code:
        ///     <code lang="CS">
        /// // saves the Grid state in the "Temp" folder of the web application 
        /// OlapAnalysis1.Serializer.WriteXML(MapPath("~/Temp/CurrentState.xml"));
        ///  
        /// // restores the prior saved Grid state 
        /// OlapAnalysis1.Serializer.ReadXML(MapPath("~/Temp/CurrentState.xml"));
        /// </code>
        /// </example>
        public virtual OlapGridSerializer Serializer => new OlapGridSerializer(this);


        /// <summary>
        ///     Is called for each Grid cell and allows tuning the style of rendering a cell and
        ///     replacing the HTML code in it.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The RenderCellEventArgs.Text property specifies the HTML code displayed in
        ///         the Grid cell instead of the cell text.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     In the OLAP server the ordinal numbers are used instead of the names of months in
        ///     the hierarchy with a unique name "[Month]". In the example below these ordinal
        ///     numbers are replaced with the real months' names.
        ///     <code lang="CS" title="[New Example]">
        /// protected void OlapAnalysis1_OnRenderCell(OlapGrid sender, RenderCellEventArgs e)
        /// {
        ///     // make sure that we work with the cell in the members' field
        ///     if (e.Cell.CellType != CellType.ctMember) return;
        ///  
        ///     //convert the ICell interface to the IMemberCell interface
        ///     IMemberCell m = (IMemberCell)e.Cell;
        ///  
        ///     // make sure that we work with the "[Month]" hierarchy 
        ///     if (m.Member.MemberType == MemberType.mtMeasure) return;
        ///     if (m.Member.Level.Hierarchy.UniqueName != "[Month]") return;
        ///  
        ///     // convert a month index to a number
        ///     int MonthValue = Convert.ToInt32(m.Value);
        ///         
        ///     // have the full name of the specified month for the current culture
        ///     CultureInfo ci = CultureInfo.CurrentCulture;
        ///     string MonthName = ci.DateTimeFormat.GetMonthName(MonthValue);
        ///  
        ///     // assign the obtained value to the "Text" property describing the HTML code
        ///     // which is placed in the cell
        ///     e.Text = MonthName;
        /// }
        /// </code>
        ///     <code lang="VB" title="[New Example]">
        /// Visual Basic
        /// Protected Sub OlapAnalysis1_OnRenderCell(ByVal sender As RadarSoft.RadarCube.Web.OlapGrid, 
        /// ByVal e As RadarSoft.RadarCube.Web.RenderCellEventArgs) 
        /// Handles OlapAnalysis1.OnRenderCell
        ///     ' make sure that we work with the cell in the members' field
        ///     If (e.Cell.CellType = CellType.ctMember) Then
        ///     Dim m As IMemberCell = DirectCast(e.Cell, IMemberCell)
        ///     ' make sure that we work with the "[Month]" hierarchy
        ///     If ((Not m.Member.MemberType = MemberType.mtMeasure) AndAlso (m.Member.Level.Hierarchy.UniqueName Is "[Month]")) Then
        ///     ' convert a month index to a number
        ///     Dim MonthValue As Integer = Convert.ToInt32(m.Value)
        ///     ' have the full name of the specified month for the current 'culture
        ///     Dim MonthName As String = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(MonthValue)
        ///     ' assign the obtained value to the "Text" property describing 'the HTML code which is placed in the cell 
        ///     e.Text = MonthName
        ///     End If
        ///     End If
        /// End Sub
        /// </code>
        /// </example>
        public virtual event RenderCellEventHandler OnRenderCell;

        internal void HandleOnRenderCell(ICell c)
        {
            if (OnRenderCell != null)
            {
                var e = new RenderCellEventArgs(c);
                OnRenderCell(this, e);
            }
        }

        protected OlapFilters fFilter;

        internal virtual OlapFilters Filter
        {
            get => fFilter;
            set => fFilter = value;
        }

        /// <summary>
        ///     Fired upon mouse-click on the custom menu item added through the OnShowContextMenu
        ///     event handler.
        /// </summary>
        /// <remarks>
        ///     See the example at the OnShowContextMenu event description.
        /// </remarks>
        public virtual event ContextMenuClickHandler OnContextMenuClick;

        internal void DoContextMenuClick(string menuid, ICell cell)
        {
            if (OnContextMenuClick == null)
                throw new Exception(
                    "To handle a click on a custom menu item, you need to write an OnContextMenuClick event handler.");

            var E = new ContextMenuClickArgs(
                menuid, cell);
            OnContextMenuClick(this, E);
        }

        protected void SetChartsTypes(string jsonTypes)
        {
            if (jsonTypes.IsFill())
            {
                var chartsTypesObj = JsonConvert.DeserializeObject<ChartTypesJson>(jsonTypes);

                if (chartsTypesObj != null && chartsTypesObj.chartTypes != null)
                {
                    ChartsType = new SeriesType[chartsTypesObj.chartTypes.Length];

                    for (var i = 0; i < chartsTypesObj.chartTypes.Length; i++)
                        ChartsType[i] = (SeriesType) chartsTypesObj.chartTypes[i];
                }
            }
        }

        protected virtual void CreateAxesLayout()
        {
            throw new NotImplementedException();
        }

        public void ClearAxesLayout()
        {
            AxesLayout.Clear();
            if (AnalysisType == AnalysisType.Grid)
                FLayout = new GridAxesLayout(this);
            else
                FLayout = new ChartAxesLayout(this);
        }

        internal void EventInitMeasures()
        {
            if (OnInitMeasures != null)
                OnInitMeasures(this, new EventArgs());
        }

        internal bool isCalculatedState;

        internal void EventCalcMember(CalcMemberArgs E)
        {
            if (OnCalcMember != null)
            {
                isCalculatedState = true;
                OnCalcMember(this, E);
                isCalculatedState = false;
            }
        }

        internal void EventInitHierarchy(Hierarchy h)
        {
            if (OnInitHierarchy != null)
            {
                var E = new EventInitHierarchyArgs();
                E.H = h;
                h.BeginUpdate();
                OnInitHierarchy(this, E);
                h.EndUpdate();
            }
        }

        internal void EventMemberSort(EventMemberSortArgs E)
        {
            OnMemberSort(this, E);
        }

        internal bool EventMemberSortAssigned => OnMemberSort != null;

        internal void EventShowMeasure(ShowMeasureArgs E)
        {
            OnShowMeasure(this, E);
        }

        internal bool EventShowMeasureAssigned => OnShowMeasure != null;

        internal void Clear()
        {
            DebugLogging.WriteLine("OlapControl.Clear()");
            if (FEngine != null)
                FEngine.Clear();
            if (FCellSet != null)
                FCellSet.ClearMembers();
            FCellSet = null;
            FilteredHierarchiesClear();
            FilteredLevelsClear();
            fMeasures.Clear();
            fMeasures.FLevel = null;
            FDimensions.Clear();
            FDimensions.ClearMembers();
            FLayout.Clear();
        }

        internal virtual CellSet.CellSet CreateCellset()
        {
            throw new NotImplementedException();
        }

        internal void UpdateCellSet()
        {
            DebugLogging.WriteLine("OlapControl.UpdateCellSet()");

            if (FCellSet != null)
                FCellSet.Dispose();

            FCellSet = CreateCellset();
            FCellSet.Rebuild();
        }

        internal void UpdateMeasures()
        {
            DebugLogging.WriteLine("OlapControl.UpdateMeasures()");

            if (Active == false)
            {
                fMeasures.Clear();
                fMeasures.ClearCache();
            }

            var measuresChanged = false;
            if (Cube != null && Cube.Active)
                foreach (var CubeMeasure in Cube.Measures)
                {
                    var Measure = fMeasures.Find(CubeMeasure.UniqueName);
                    if (Measure == null)
                    {
                        Measure = new Measure(this);
                        fMeasures.Add(Measure);
                        Measure.InitMeasure(CubeMeasure);
                        measuresChanged = true;
                    }
                    else
                    {
                        Measure.FCubeMeasure = CubeMeasure;
                        Measure.VisibleInTree = CubeMeasure.VisibleInTree;
                    }
                }

            for (var i = fMeasures.Count - 1; i >= 0; i--)
            {
                var Measure = fMeasures[i];
                if (Measure.FFunction == OlapFunction.stCalculated) continue;
                if (Cube.Measures.Find(Measure.UniqueName) == null)
                {
                    fMeasures.Remove(Measure);
                    measuresChanged = true;
                }
            }

            if (measuresChanged)
                fMeasures.RemoveAll(item => item.FFunction == OlapFunction.stCalculated);
        }

        private void UpdateHierarchies(Dimension Dimension)
        {
            DebugLogging.WriteLine("OlapControl.UpdateHierarchies()");

            foreach (var CubeHierarchy in Dimension.CubeDimension.Hierarchies)
            {
                var Hierarchy = Dimension.FHierarchies.Find(CubeHierarchy.UniqueName);
                if (Hierarchy == null)
                {
                    Hierarchy = new Hierarchy(Dimension);
                    Dimension.FHierarchies.Add(Hierarchy);
                    Hierarchy.InitHierarchyProperties(CubeHierarchy);
                }
                else
                {
                    Hierarchy.FCubeHierarchy = CubeHierarchy;
                }
            }

            for (var i = Dimension.FHierarchies.Count - 1; i >= 0; i--)
            {
                var Hierarchy = Dimension.FHierarchies[i];
                if (Dimension.CubeDimension.Hierarchies.Find(Hierarchy.UniqueName) == null)
                    Dimension.FHierarchies.Remove(Hierarchy);
            }
        }

        private void UpdateDimensions()
        {
            DebugLogging.WriteLine("OlapControl.UpdateDimensions()");
     
            foreach (var CubeDimension in Cube.Dimensions)
            {
                var Dimension = FDimensions.Find(CubeDimension.UniqueName);
                if (Dimension == null)
                {
                    Dimension = new Dimension(this);
                    FDimensions.Add(Dimension);
                    Dimension.FUniqueName = CubeDimension.UniqueName;
                }
                Dimension.FCubeDimension = CubeDimension;
                UpdateHierarchies(Dimension);
            }

            for (var i = FDimensions.Count - 1; i >= 0; i--)
            {
                var Dimension = FDimensions[i];
                if (Cube.Dimensions.Find(Dimension.UniqueName) == null) FDimensions.Remove(Dimension);
            }
        }

        internal void UpdateCubeStructures()
        {
            DebugLogging.WriteLine("OlapControl.UpdateCubeStructures()");

            UpdateDimensions();
            UpdateMeasures();
            FLayout.RowNodes = FLayout.fRowNodes;
            FLayout.ColumnNodes = FLayout.fColumnNodes;
            FLayout.PageNodes = FLayout.fPageNodes;

            if (CellSet != null && Cube != null && Cube.Active)
            {
                CellSet.Rebuild();
                CellSet.Grid.EndChange(GridEventType.geChangeCubeStructure);
            }
        }

        internal void DoReconnect()
        {
            DebugLogging.WriteLine("OlapControl.DoReconnect()");

            if (IgnoreReconect)
                return;

            Clear();
            FilteredHierarchiesClear();
            FilteredLevelsClear();
            UpdateCubeStructures();
            UpdateCellSet();
        }

        internal bool IgnoreReconect;

        /// <summary>The event raised at any Grid modification.</summary>
        /// <remarks>
        ///     Can be used for a synchronic change of information in visual or non visual
        ///     components, that use OLAP Grid as data source.
        /// </remarks>
        public event EventHandler<GridEventArgs> OnGridEvent
        {
            add
            {
                _OnGridEvent += value;
                _OnGridEventList.Add(value);
            }
            remove
            {
                _OnGridEvent -= value;
                if (_OnGridEventList != null)
                    _OnGridEventList.Remove(value);
            }
        }

        private event EventHandler<GridEventArgs> _OnGridEvent;
        protected List<EventHandler<GridEventArgs>> _OnGridEventList = new List<EventHandler<GridEventArgs>>();


        /// <summary>
        ///     The method called at the end of any action in the Grid is initiated either by
        ///     user's actions or by a program. It is primarily used by control developers.
        /// </summary>
        /// <remarks>The method raises the OnGridEvent handler</remarks>
        /// <param name="EventType">The type of event</param>
        /// <param name="Data">Additional data describing the action</param>
        public virtual void EndChange(GridEventType EventType, params object[] Data)
        {
            if (_OnGridEvent == null)
                return;
            var E = new GridEventArgs(this, EventType, Data);
            foreach (var d in _OnGridEvent.GetInvocationList())
                (d as EventHandler<GridEventArgs>)(this, E);
        }

        private void GetCellByAddress_RecursiveScan(List<CellsetMember> M, List<Member> P, ref CellsetMember C)
        {
            var k = -1;
            for (var i = 0; i < M.Count; i++)
                if (M[i].FChildren.Count == 0 && P.Contains(M[i].FMember))
                {
                    k = i;
                    break;
                }
            for (var i = 0; i < M.Count; i++)
                if (M[i].FChildren.Count != 0 && P.Contains(M[i].FMember))
                {
                    k = i;
                    break;
                }
            if (k < 0) return;
            if (M[k].FChildren.Count != 0)
                GetCellByAddress_RecursiveScan(M[k].FChildren, P, ref C);
            else
                C = M[k];
        }

        /// <summary>
        ///     Searches for a cell in the current Cellset by its multidimensional address. The
        ///     method is primarily used by the control developers.
        /// </summary>
        /// <param name="Address">The cell's multidimentional address</param>
        /// <param name="Row">The row index of the found cell. -1 if the cell is not found.</param>
        /// <param name="Column">The column index of the found cell. -1 if the cell is not found.</param>
        public void GetCellByAddress(ICubeAddress Address, out int Row, out int Column)
        {
            Column = -1;
            Row = -1;
            if (CellSet == null) return;
            var P = new List<Member>();
            P.Add(null);
            if (Address.Measure != null)
            {
                var M = fMeasures.FLevel.FindMember(Address.Measure.UniqueName);
                P.Add(M);
            }
            for (var i = 0; i < Address.LevelsCount; i++)
            {
                var M = Address.Members(i);
                while (M != null)
                {
                    P.Add(M);
                    M = M.Parent;
                }
            }
            CellsetMember C = null;
            GetCellByAddress_RecursiveScan(FCellSet.FRowMembers, P, ref C);
            var RC = C;
            C = null;
            GetCellByAddress_RecursiveScan(FCellSet.FColumnMembers, P, ref C);
            var CC = C;
            if (RC == null && CC == null) return;
            if (RC == null && CC != null)
            {
                Row = CC.FStartRow;
                Column = CC.FStartColumn;
            }
            if (RC != null && CC == null)
            {
                Row = RC.FStartRow;
                Column = RC.FStartColumn;
            }
            if (RC != null && CC != null)
            {
                Row = RC.FStartRow;
                Column = CC.FStartColumn;
            }
        }

        /// <summary>
        ///     References to the CellSet object representing the current OLAP Cube slice.
        /// </summary>
        /// <remarks>
        ///     If the Grid is not connected to a Cube, or the Cube is not activated, then this
        ///     property equals <em>null</em>.
        /// </remarks>
        public virtual CellSet.CellSet CellSet => FCellSet;

        internal virtual ICellSet CellSetInterface => CellSet;

        internal void SetActive(bool Value)
        {
            DebugLogging.WriteLine("OlapControl.SetActive(Value={0})", Value);

            //if ((!Value) && (Mode == OlapGridMode.gmQueryResult))
            //{
            //    Mode = OlapGridMode.gmStandard;
            //    Dimensions.Clear();
            //    Measures.Clear();
            //    if (FEngine != null)
            //        FEngine.Clear();
            //}

            if (Value)
            {
                FilteredHierarchiesClear();
                FilteredLevelsClear();
                {
                    UpdateCellSet();
                }
            }
            else
            {
                SessionState.Delete(SessionKey.olapgrid_sessionstate, UniqueID);
                Clear();
            }

            EndChange(GridEventType.geActiveChanged);
        }

        private Updater _ModeUpdater;

        internal Updater ModeUpdater
        {
            get
            {
                if (_ModeUpdater == null)
                {
                    _ModeUpdater = new Updater();
                    _ModeUpdater.UpdateEnd += _ModeUpdater_UpdateEnd;
                }
                return _ModeUpdater;
            }
        }

        private void _ModeUpdater_UpdateEnd(object sender, EventArgs e)
        {
            ModeChanged();
            _ModeUpdater.UpdateEnd -= _ModeUpdater_UpdateEnd;
        }

        internal virtual void ModeChanged()
        {
        }

        internal void FilteredHierarchiesClear()
        {
            if (FFilteredHierarchies != null)
                FFilteredHierarchies.Clear();

            else
                FFilteredHierarchies = new List<Hierarchy>();
        }

        internal void FilteredLevelsClear()
        {
            if (FFilteredLevels != null)
            {
                foreach (var level in FFilteredLevels.ToArray())
                    level.Filter = null;
                FFilteredLevels.Clear();
            }
        }


        internal SessionState SessionState
        {
            get
            {
                if (Cube != null)
                    return Cube.SessionState;
                return null;
            }
        }

        /// <summary>
        ///     References to the Engine object containing methods to low-level access
        ///     to the Cube data.
        /// </summary>
        public virtual Engine.Engine Engine => FEngine;

        /// <summary>
        ///     Prevents the unnecessary recalculation of the Cellset during multiple pivoting
        ///     actions.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Don't forget to call the EndUpdate function after fulfilling pivoting actions
        ///         - it will inform the Grid that the current CellSet should be recalculated.
        ///     </para>
        ///     <para>
        ///         The functions BeginUpdate and EndUpdate are designed to speed up pivoting
        ///         actions. If you need to apply a multiple program filter to some hierarchies, you
        ///         should use the Hierarchy.BeginUpdate &amp; EndUpdate functions.
        ///     </para>
        /// </remarks>
        public void BeginUpdate()
        {
            IncrementUpdateCounter();
        }

        internal int IncrementUpdateCounter()
        {
            return FUpdateCounter++;
        }

        /// <summary>
        ///     Call according with Increment only !!!
        /// </summary>
        /// <returns></returns>
        internal int DecrementUpdateCounter()
        {
            return --FUpdateCounter;
        }

        /// <summary>
        ///     Recalculates the CellSet after multiple pivoting actions forestalled by the
        ///     BeginUpdate function.
        /// </summary>
        /// <remarks>Enables recalculation of the Cellset.</remarks>
        public void EndUpdate()
        {
            if (IsUpdating)
            {
                if (DecrementUpdateCounter() == 0 && IsDisposed2 == false)
                {
                    if (FCellSet != null)
                        FCellSet.Rebuild();
                    EndChange(GridEventType.geEndUpdate);
                }
            }
            else
            {
#if DEBUG
                throw new Exception("Unbalanced call of the OlapGrid.EndUpdate method.");
#endif
            }
        }

        //internal bool IsAllowPivoting_Event(Hierarchy h, LayoutArea? from, ref LayoutArea to)
        //{
        //    if (OnBeforePivot == null)
        //        return true;

        //    //#if OLAPWEB
        //    //        internal bool IsAllowPivoting(Hierarchy h, LayoutArea? from, ref LayoutArea to)
        //    //        {
        //    //            if (OnBeforePivot == null) 
        //    //                return true;
        //    //#endif // OLAPWEB

        //    PivotEventArgs E = new PivotEventArgs(h, from, to);
        //    OnBeforePivot(this, E);
        //    to = E.To;
        //    return E.AllowPivoting;
        //}

        internal virtual bool IsAllowPivoting(Hierarchy h, LayoutArea? from, ref LayoutArea to)
        {
            if (from == null)
            {
                foreach (var probe in FLayout.fPageAxis)
                    if (!h.DoAllowPivoting(probe))
                        return false;

                foreach (var probe in FLayout.fRowAxis)
                    if (!h.DoAllowPivoting(probe))
                        return false;

                foreach (var probe in FLayout.fColumnAxis)
                    if (!h.DoAllowPivoting(probe))
                        return false;

                if (FLayout.fColorAxisItem != null)
                    if (!h.DoAllowPivoting(FLayout.fColorAxisItem as Hierarchy))
                        return false;

                if (FLayout.fColorForeAxisItem != null)
                    if (!h.DoAllowPivoting(FLayout.fColorForeAxisItem as Hierarchy))
                        return false;

                if (FLayout.fSizeAxisItem != null)
                    if (!h.DoAllowPivoting(FLayout.fSizeAxisItem as Hierarchy))
                        return false;

                if (FLayout.fShapeAxisItem != null)
                    if (!h.DoAllowPivoting(FLayout.fShapeAxisItem as Hierarchy))
                        return false;

                //if (FLayout.fTooltipAxisItem != null)
                //    if (!h.DoAllowPivoting(FLayout.fTooltipAxisItem as Hierarchy))
                //        return false;
            }
            else
            {
                if (from == LayoutArea.laColor)
                {
                    //foreach (MeasureGroup gm in FLayout.fYAxisMeasures)
                    //{
                    //    if (gm.Count > 1)
                    //    {
                    //        //string s1 =
                    //        //    "It doesn't allow to place the hierarchy on the Color axis if there is at least one graph containing more than one measure series.";
                    //        //System.Windows.Forms.MessageBox.Show(s1, "",
                    //        //                                     System.Windows.Forms.MessageBoxButtons.OK,
                    //        //                                     System.Windows.Forms.MessageBoxIcon.Exclamation);
                    //        return false;
                    //    }
                    //}
                }
            }

            if (OnBeforePivot != null)
            {
                var e = new PivotEventArgs(h, from, to);
                BeforePivot(this, e);
                to = e.To;
                return e.AllowPivoting;
            }
            return true;
        }


        internal virtual bool IsAllowPivoting(Measure m, LayoutArea? from, ref LayoutArea to)
        {
            if (OnBeforePivot != null)
            {
                var e = new PivotEventArgs(m, from, to);
                BeforePivot(this, e);
                to = e.To;
                return e.AllowPivoting;
            }
            return true;
        }

        internal virtual void BeforePivot(object sender, PivotEventArgs e)
        {
            if (OnBeforePivot != null)
                OnBeforePivot(sender, e);
        }

        protected virtual void DoAfterPivoting(Hierarchy h, LayoutArea? from, LayoutArea to)
        {
            if (OnAfterPivot != null)
                AfterPivoting(this, new PivotEventArgs(h, from, to));
        }

        protected virtual void DoAfterPivoting(Measure m, LayoutArea? from, LayoutArea to)
        {
            if (OnAfterPivot != null)
                AfterPivoting(this, new PivotEventArgs(m, from, to));
        }

        internal virtual void AfterPivoting(object sender, PivotEventArgs e)
        {
            if (OnAfterPivot != null)
                OnAfterPivot(sender, e);
        }

        internal void MakeFiltered2List()
        {
            MakeFiltered2List(false);
        }

        internal void MakeFiltered2List(bool allowUnused)
        {
            FFilteredLevels.Clear();

            FFilteredLevels.AddRange(FLayout.fRowAxis
                .Concat(FLayout.fColumnAxis)
                .Concat(FLayout.fPageAxis)
                .Distinct()
                .SelectMany(h => h.Levels)
                .Where(l => l.Filter != null));

            if (allowUnused)
            {
                var levels = Dimensions
                    .Where(x => x != null && x.Hierarchies != null)
                    .SelectMany(d => d.Hierarchies)
                    .Where(x => x != null && x.Levels != null)
                    .SelectMany(h => h.Levels)
                    .Where(l => l.Filter != null)
                    .Distinct()
                    .ToList();


                FFilteredLevels = FFilteredLevels
                    .Concat(levels)
                    .Distinct()
                    .ToList();
            }
        }

        public MeasureGroup Pivoting(Measure Source)
        {
            return Pivoting(Source, LayoutArea.laRow, null, null);
        }

        /// <summary>Places the hierarchy to the desired position in the desired area.</summary>
        /// <remarks>
        ///     <para>
        ///         Compared to PivotingFirst and PivotingLast has an expanded set of parameters,
        ///         which allows placing a hierarchy in the Index position of the DestArea.
        ///     </para>
        ///     <para>
        ///         To remove a hierarchy from the active area, you can also use the PivotingOut
        ///         method.
        ///     </para>
        /// </remarks>
        /// <param name="Source">The pivoted hierarchy.</param>
        /// <param name="DestArea">The area the hierarchy is to be placed in.</param>
        /// <param name="DestPosition">The position in the area (starts from 0) the hierarchy is to be placed in.</param>
        public void Pivoting(Hierarchy Source, LayoutArea DestArea, int DestPosition)
        {
            Pivoting(Source, DestArea, DestPosition, null);
        }


        internal void Pivoting(Hierarchy Source, LayoutArea DestArea, int DestPosition, LayoutArea? from)
        {
            DebugLogging.WriteLine(
                "TCustomOLAPGrid.Pivoting_Inner DestArea={0} DestPosition={1} from={2}", DestArea, DestPosition, from);

            if (DestArea == LayoutArea.laNone || Source == null)
                return;

            if (CellsetMode == CellsetMode.cmGrid && (
                    DestArea == LayoutArea.laDetails ||
                    DestArea == LayoutArea.laShape ||
                    DestArea == LayoutArea.laSize))
                return;

            if (DestArea == LayoutArea.laTree)
            {
                PivotingOut(Source, from);
                return;
            }

            if (from == null)
                from = GetFrom_Inner(Source);

            if (IsAllowPivoting(Source, from, ref DestArea) == false)
                return;

            Source.DefaultInit();

            if (DestArea == LayoutArea.laColumn || DestArea == LayoutArea.laRow)
            {
                var allHidden = true;
                if (Source.Levels != null)
                {
                    if (Source.Levels.Any(l => l.Visible))
                        allHidden = false;

                    if (allHidden)
                        Source.Levels[0].FVisible = true;
                }
            }

            if (
                DestArea == LayoutArea.laColor ||
                //(DestArea == LayoutArea.laToolTip) ||
                DestArea == LayoutArea.laColorFore ||
                DestArea == LayoutArea.laSize ||
                DestArea == LayoutArea.laShape ||
                DestArea == LayoutArea.laDetails)
            {
                //var visiblelevels = Source.Levels.TakeWhile(x => x.Visible);
                //Source.Levels.Skip(1).ForEach(x => x.FVisible);

                var hasVisible = false;
                foreach (var l in Source.Levels)
                    if (l.Visible)
                        if (hasVisible)
                            l.FVisible = false;
                        else
                            hasVisible = true;
                if (!hasVisible)
                    Source.Levels[0].FVisible = true;

                //if (Source.Levels.Any(x => x.Visible) == false)
                //    Source.Levels[0].FVisible = true;
            }

            BeginUpdate();

            try
            {
                var NR = false;
                if (Source.Origin == HierarchyOrigin.hoNamedSet &&
                    !FLayout.fColumnAxis.Contains(Source) &&
                    !FLayout.fRowAxis.Contains(Source) &&
                    !FLayout.fPageAxis.Contains(Source))
                {
                    FEngine.Clear();

                    if (!FFilteredHierarchies.Contains(Source))
                        FFilteredHierarchies.Add(Source);

                    NR = true;
                }

                if (DestArea == LayoutArea.laColumn ||
                    DestArea == LayoutArea.laDetails ||
                    DestArea == LayoutArea.laPage ||
                    DestArea == LayoutArea.laRow)
                {
                    // keep hierarchy on axises on filter creation
                    if (FLayout.fRowAxis.Remove(Source))
                    {
                        FLayout.LayoutChanged(LayoutArea.laRow);
                        NR = true;
                    }
                    if (FLayout.fColumnAxis.Remove(Source))
                    {
                        FLayout.LayoutChanged(LayoutArea.laColumn);
                        NR = true;
                    }
                    if (FLayout.fDetailsAxis.Remove(Source))
                    {
                        FLayout.LayoutChanged(LayoutArea.laDetails);
                        NR = true;
                    }
                    if (FLayout.fPageAxis.Remove(Source))
                        FLayout.LayoutChanged(LayoutArea.laPage);

                    if (NR)
                        FCellSet.OptimizeDrills();

                    if (from == LayoutArea.laRow ||
                        from == LayoutArea.laColumn)
                        foreach (var v in FCellSet.FDrillActions.ToArray())
                            if (v.Members.Any(item => Source.FindMemberByUniqueName(item.UniqueName) != null))
                                FCellSet.FDrillActions.Remove(v);
                }

                IList<Hierarchy> P = null;
                P = GetAxis_byArea(DestArea);

                if (P != null)
                {
                    if (P.Count < DestPosition)
                        DestPosition = Convert.ToInt16(P.Count);
                    P.Insert(DestPosition, Source);
                    FLayout.LayoutChanged(DestArea);

                    if (DestArea == LayoutArea.laDetails)
                        RefreshDetailsProperties();

                    if (from == null && Source.IsUpdating)
                        Source.ApplyDefaultFilter();
                }

                if (DestArea == LayoutArea.laColor)
                    FLayout.ColorBackAxisItem = Source;
                if (DestArea == LayoutArea.laColorFore)
                    FLayout.ColorForeAxisItem = Source;
                if (DestArea == LayoutArea.laSize)
                    FLayout.SizeAxisItem = Source;
                if (DestArea == LayoutArea.laShape)
                    FLayout.ShapeAxisItem = Source;
                //if (DestArea == LayoutArea.laToolTip)
                //    FLayout.TooltipAxisItem = Source;

                FLayout.CheckExpandedLevels();

                if (FCellSet != null)
                    if ((NR || DestArea != LayoutArea.laPage) && !IsUpdating)
                        FCellSet.Rebuild();
                var h = Source;
                if (h != null && CalcBehavior(DestArea) == PivotingBehavior.Excel2010)
                {
                    if (IsReadXMLProcessing == false)
                        FCellSet.ExpandAllHierarchies(PossibleDrillActions.esNextHierarchy, h.IsDate, !h.IsDate);
#if DEBUG
#endif
                }
                EndChange(GridEventType.gePivotAction, Source);
            }
            finally
            {
                EndUpdate();
                DoAfterPivoting(Source, from, DestArea);
            }
        }

        internal virtual void RefreshDetailsProperties()
        {
        }

        protected virtual IList<Hierarchy> GetAxis_byArea(LayoutArea Area)
        {
            switch (Area)
            {
                case LayoutArea.laRow:
                    return FLayout.fRowAxis;
                case LayoutArea.laColumn:
                    return FLayout.fColumnAxis;
                //case LayoutArea.laSize: return FLayout.fSizeAxisItem;
                //case LayoutArea.laShape: return FLayout.fShapeAxisItem;
                case LayoutArea.laDetails:
                    return FLayout.fDetailsAxis;
                case LayoutArea.laTree:
                    return null;

                case LayoutArea.laColorFore:
                    return null;

                case LayoutArea.laPage:
                    return FLayout.fPageAxis;
                case LayoutArea.laNone:
                default:
                    return null;
            }
        }

        private LayoutArea? GetFrom_Inner(Hierarchy Source)
        {
            LayoutArea? from = null;

            if (FLayout.fRowAxis.Contains(Source))
            {
                from = LayoutArea.laRow;
            }
            else
            {
                if (FLayout.fColumnAxis.Contains(Source))
                    from = LayoutArea.laColumn;
                else if (FLayout.fPageAxis.Contains(Source))
                    from = LayoutArea.laPage;
                else if (FLayout.fDetailsAxis.Contains(Source))
                    from = LayoutArea.laDetails;
                else if (FLayout.ColorBackAxisItem == Source)
                    from = LayoutArea.laColor;
                else if (FLayout.fColorForeAxisItem == Source)
                    from = LayoutArea.laColorFore;
                else if (FLayout.fSizeAxisItem == Source)
                    from = LayoutArea.laSize;
                else if (FLayout.fShapeAxisItem == Source)
                    from = LayoutArea.laShape;
                //else if (FLayout.TooltipAxisItem == Source)
                //{
                //    from = LayoutArea.laToolTip;
                //}
            }
            return from;
        }

        /// <summary>
        ///     Rotates the cube, placing the hierarchy to the last position in the desired
        ///     area.
        /// </summary>
        /// <remarks>
        ///     To put a hierarchy in a desired position of any area, use the Pivoting method. To
        ///     remove a hierarchy from the active area, use the PivotingOut method.
        /// </remarks>
        /// <param name="Source">The pivoted hierarchy.</param>
        /// <param name="DestArea">The area the hierarchy is to be placed in.</param>
        public void PivotingLast(Hierarchy Source, LayoutArea DestArea)
        {
            Pivoting(Source, DestArea, 999, null);
        }

        /// <summary>
        ///     Rotates the cube, placing the hierarchy to the first position in the desired
        ///     area.
        /// </summary>
        /// <remarks>
        ///     To put a hierarchy in a desired position of any area, use the Pivoting method. To
        ///     remove a hierarchy from the active area, use the PivotingOut method.
        /// </remarks>
        /// <param name="Source">The pivoted hierarchy.</param>
        /// <param name="DestArea">The area the hierarchy is to be placed in.</param>
        public void PivotingFirst(Hierarchy Source, LayoutArea DestArea)
        {
            Pivoting(Source, DestArea, 0, null);
        }

        /// <summary>Removes the hierarchy from the active area.</summary>
        /// <remarks>
        ///     <para>
        ///         PivotingOut removes all filters that were applied to the hierarchy
        ///         members.
        ///     </para>
        ///     <para>
        ///         To put the hierarchy in the active area or relocate it within this area, you
        ///         should call one of those methods: Pivoting, PivotingFirst or PivotingLast
        ///     </para>
        /// </remarks>
        /// <param name="Source">The hierarchy removed from the active area.</param>
        public void PivotingOut(Hierarchy Source, LayoutArea? from)
        {
            PivotingOut_Inner(Source, from, true);
        }

        private void PivotingOut_Inner(Hierarchy Source, LayoutArea? from, bool preserveFilters)
        {
            DebugLogging.WriteLine("OlapControl.PivotingOut_Inner(Hierarchy={0}) from={1}", Source.DisplayName,
                from);

            if (from == LayoutArea.laTree)
                return;

#if DEBUG
            Source.CheckInit();
#endif
            var _to = LayoutArea.laTree;
            if (!IsAllowPivoting(Source, from, ref _to))
                return;

            if (from == LayoutArea.laTree)
                return;

            if (from == null || from.Value == LayoutArea.laNone)
                from = GetFrom_Inner(Source);
            if (from == null)
                return;

            var hierarchiesCount = 0;
            if (FLayout.fShapeAxisItem == Source) hierarchiesCount++;
            if (FLayout.fSizeAxisItem == Source) hierarchiesCount++;
            if (FLayout.ColorBackAxisItem == Source) hierarchiesCount++;
            if (FLayout.ColorForeAxisItem == Source) hierarchiesCount++;
            if (FLayout.fDetailsAxis.Contains(Source)) hierarchiesCount++;
            if (FLayout.fPageAxis.Contains(Source)) hierarchiesCount++;
            if (FLayout.fColumnAxis.Contains(Source)) hierarchiesCount++;
            if (FLayout.fRowAxis.Contains(Source)) hierarchiesCount++;

            var hierarchiesCountNotFilter = hierarchiesCount;
            if (FLayout.fPageAxis.Contains(Source))
                hierarchiesCountNotFilter--;

            if (hierarchiesCount <= 1)
            {
                foreach (var l in Source.Levels)
                    l.FVisible = false;
                Source.Levels[0].FVisible = true;
            }

            var NR = false;

            {
                if ((Source.FFiltered || Source.FilteredByLevelFilters) && hierarchiesCount <= 1
                    && !preserveFilters)
                {
                    Source.ResetFilter();
                    NR = true;
                }

                if (from == LayoutArea.laNone)
                {
                    if (FLayout.fRowAxis.Remove(Source))
                        NR = true;

                    if (FLayout.fColumnAxis.Remove(Source))
                        NR = true;

                    FLayout.fPageAxis.Remove(Source);

                    if (FLayout.fDetailsAxis.Remove(Source))
                        NR = true;

                    if (FLayout.ColorBackAxisItem == Source)
                    {
                        FLayout.ColorBackAxisItem = null;
                        NR = true;
                    }
                    if (FLayout.ColorForeAxisItem == Source)
                    {
                        FLayout.ColorForeAxisItem = null;
                        NR = true;
                    }
                    if (FLayout.fSizeAxisItem == Source)
                    {
                        FLayout.SizeAxisItem = null;
                        NR = true;
                    }
                    if (FLayout.fShapeAxisItem == Source)
                    {
                        FLayout.ShapeAxisItem = null;
                        NR = true;
                    }
                    //if (FLayout.TooltipAxisItem == Source)
                    //{
                    //    FLayout.TooltipAxisItem = null;
                    //    NR = true;
                    //}
                }
                else
                {
                    NR = true;
                    switch (from)
                    {
                        case LayoutArea.laColorFore:
                            FLayout.fColorForeAxisItem = null;
                            break;
                        case LayoutArea.laColor:
                            FLayout.ColorBackAxisItem = null;
                            break;
                        case LayoutArea.laColumn:
                            FLayout.fColumnAxis.Remove(Source);
                            break;
                        //case LayoutArea.laToolTip:
                        //    //FLayout.fColumnAxis.Remove(Source);
                        //    FLayout.TooltipAxisItem = null;
                        //    break;
                        case LayoutArea.laDetails:
                            FLayout.fDetailsAxis.Remove(Source);
                            break;
                        case LayoutArea.laPage:
                            FLayout.fPageAxis.Remove(Source);
                            if (Source.FFiltered || Source.FilteredByLevelFilters)
                            {
                                BeginUpdate();
                                Source.DoSetFilter(true, false);
                                EndUpdate();
                            }

                            break;
                        case LayoutArea.laRow:
                            FLayout.fRowAxis.Remove(Source);
                            break;
                        case LayoutArea.laSize:
                            FLayout.fSizeAxisItem = null;
                            break;
                        case LayoutArea.laShape:
                            FLayout.ShapeAxisItem = null;
                            break;
                    }
                }
                if (Source.Origin == HierarchyOrigin.hoNamedSet)
                {
                    FEngine.Clear();
                    FFilteredHierarchies.Remove(Source);
                }
                FLayout.CheckExpandedLevels();
                if ((NR || Source.Origin == HierarchyOrigin.hoNamedSet) && !IsUpdating)
                    FCellSet.Rebuild();

                DoAfterPivoting(Source, from, LayoutArea.laTree);
                EndChange(GridEventType.gePivotAction, Source);
            }
        }

        public MeasureGroup Pivoting(Measure Source, LayoutArea DestArea)
        {
            return Pivoting(Source, DestArea, null, null);
        }

        public MeasureGroup Pivoting(Measure Source, LayoutArea DestArea, MeasureGroup destMeasureGroup,
            LayoutArea? from)
        {
            if (!IsAllowPivoting(Source, from, ref DestArea)) return null;
            if (CellsetMode == CellsetMode.cmGrid &&
                DestArea != LayoutArea.laColor && DestArea != LayoutArea.laColorFore &&
                from != LayoutArea.laColor && from != LayoutArea.laColorFore)
            {
#if !SL
                if (this is OlapAnalysis == false)
                {
                    Source.Visible = true;
                    return null;
                }
#endif

                return PivotingY(Source, destMeasureGroup);
            }

            if (DestArea == LayoutArea.laTree)
            {
                PivotingOut(Source, true, from);
                return null;
            }

            MeasureGroup Result = null;
            if (destMeasureGroup != null && destMeasureGroup.Contains(Source))
                return destMeasureGroup;

            BeginUpdate();

            if (from == null)
            {
                if (FLayout.ColorBackAxisItem == Source && from == LayoutArea.laColor)
                    FLayout.ColorBackAxisItem = null;
                if (FLayout.fColorForeAxisItem == Source && from == LayoutArea.laColorFore)
                    FLayout.ColorForeAxisItem = null;
                if (FLayout.fSizeAxisItem == Source && from == LayoutArea.laSize)
                    FLayout.SizeAxisItem = null;
                if (FLayout.fShapeAxisItem == Source && from == LayoutArea.laShape)
                    FLayout.ShapeAxisItem = null;
            }
            if (DestArea == LayoutArea.laColumn || DestArea == LayoutArea.laRow)
            {
                PivotingYOut(Source, false);

                if (FLayout.fXAxisMeasure == Source)
                    FLayout.fXAxisMeasure = null;
            }
            switch (DestArea)
            {
                case LayoutArea.laColumn:
                    FLayout.fXAxisMeasure = Source;
                    Source.FVisible = true;
                    if (Active)
                        CellSet.Rebuild();

                    break;
                case LayoutArea.laRow:
                    Result = PivotingY(Source, destMeasureGroup);
                    break;
                case LayoutArea.laColor:
                    PivotingToColorAxis(Source);
                    break;
                case LayoutArea.laColorFore:
                    PivotingToForeColorAxis(Source);
                    break;
                case LayoutArea.laSize:
                    PivotingToSizeAxis(Source);
                    break;
                case LayoutArea.laShape:
                    PivotingToShapeAxis(Source);
                    break;
            }
            if (CellsetMode == CellsetMode.cmGrid &&
                (DestArea == LayoutArea.laColor || DestArea == LayoutArea.laColorFore) == false)
                Source.FVisible = true;
            EndUpdate();
            EndChange(GridEventType.gePivotAction, Source);

            DoAfterPivoting(Source, from, DestArea);
            return Result;
        }

        public void PivotingOut(Measure measure, LayoutArea from)
        {
            PivotingOut(measure, true, from);
        }

        internal void PivotingOut(Measure measure, bool allowRebuild, LayoutArea? from)
        {
            if (measure != null)
                DebugLogging.WriteLine("OlapControl.PivotingOut_Inner(Measure={0}) allowRebuild={1} from={2}",
                    measure.DisplayName, allowRebuild, from);

            var _to = LayoutArea.laTree;
            if (!IsAllowPivoting(measure, from, ref _to))
                return;

            var b = false;
            if (from == LayoutArea.laRow)
                b = PivotingYOut(measure, allowRebuild);

            if (!b)
            {
                var mY = FLayout.fYAxisMeasures
                    .Any(
                        x => x != null && x.Any(y => measure != null && y.UniqueName == measure.UniqueName));
                var mX = FLayout.fXAxisMeasure == measure;
                var mColor = FLayout.ColorBackAxisItem == measure;
                var mColorFore = FLayout.ColorForeAxisItem == measure;
                var mSize = FLayout.fSizeAxisItem == measure;
                var mShape = FLayout.ShapeAxisItem == measure;

                if (FLayout.fXAxisMeasure == measure && (from == null || from == LayoutArea.laColumn))
                {
                    FLayout.fXAxisMeasure = null;
                    if (Active && allowRebuild) CellSet.Rebuild();
                    if (!mY && !mColor && !mSize && !mShape && CellsetMode == CellsetMode.cmChart)
                        measure.Visible = false;
                }
                if (FLayout.ColorBackAxisItem == measure && (from == null || from == LayoutArea.laColor))
                {
                    FLayout.ColorBackAxisItem = null;
                    if (Active && allowRebuild)
                        CellSet.Rebuild();

                    if (!mY && !mX && !mSize && !mShape && CellsetMode == CellsetMode.cmChart)
                        measure.Visible = false;
                }

                if (FLayout.ColorForeAxisItem == measure && (from == null || from == LayoutArea.laColorFore))
                {
                    FLayout.ColorForeAxisItem = null;
                    if (Active && allowRebuild)
                        CellSet.Rebuild();

                    if (!mY && !mX && !mSize && !mColorFore && !mColor && !mShape && CellsetMode == CellsetMode.cmGrid)
                        measure.Visible = false;
                }

                if (FLayout.fColorForeAxisItem == measure && (from == null || from == LayoutArea.laColorFore))
                {
                    FLayout.fColorForeAxisItem = null;
                    if (Active && allowRebuild)
                        CellSet.Rebuild();
                }

                if (CellsetMode == CellsetMode.cmGrid && measure != null && measure.Visible)
                    mY = true;

                if (FLayout.fSizeAxisItem == measure && (from == null || from == LayoutArea.laSize))
                {
                    FLayout.SizeAxisItem = null;
                    if (Active && allowRebuild) CellSet.Rebuild();

                    if (!mY && !mColor && !mX && !mShape)
                        measure.Visible = false;
                }
                if (FLayout.fShapeAxisItem == measure && (from == null || from == LayoutArea.laShape))
                {
                    FLayout.ShapeAxisItem = null;
                    if (Active && allowRebuild) CellSet.Rebuild();

                    if (!mY && !mColor && !mSize && !mX)
                        measure.Visible = false;
                }
            }
            if (from == null)
                measure.Visible = false;
            EndChange(GridEventType.gePivotAction, measure);

            DoAfterPivoting(measure, from, LayoutArea.laTree);
        }

        private MeasureGroup PivotingY(Measure measure, MeasureGroup area)
        {
            if (measure == null)
                throw  new ArgumentNullException("measure == null!");

            DebugLogging.WriteLine("OlapControl.PivotingY(Measure={0}) area={1}", measure.DisplayName, area);

            if (area == null)
            {
                area = new MeasureGroup();
                FLayout.fYAxisMeasures.Add(area);
            }

            PivotingOut(measure, false, LayoutArea.laPage);

            area.Add(measure);
            measure.Visible = true;
            return area;
        }

        private bool PivotingYOut(Measure measure, bool allowRebuild)
        {
            if (measure != null)
                DebugLogging.WriteLine("OlapControl.PivotingYOut(Measure={0}) allowRebuild={1}",
                    measure.DisplayName, allowRebuild);

            if (CellsetMode == CellsetMode.cmChart)
            {
                foreach (var d in FLayout.fYAxisMeasures)
                foreach (var s in d)
                    if (s == measure)
                    {
                        d.Remove(s);
                        if (d.Count == 0)
                            FLayout.fYAxisMeasures.Remove(d);
                        measure.FVisible = false;
                        if (Active && allowRebuild) CellSet.Rebuild();
                        return true;
                    }
                return false;
            }
            measure.Visible = false;

            //#if DEBUG
            // grid measures also contains in YAxisMeasures, if not removed then unpivoted measures now show after serialization-deserialization
            if (FLayout.fYAxisMeasures != null)
                foreach (var d in FLayout.fYAxisMeasures)
                foreach (var s in d)
                    if (s == measure)
                    {
                        d.Remove(s);
                        if (d.Count == 0)
                            FLayout.fYAxisMeasures.Remove(d);
                        measure.FVisible = false;
                        return true;
                    }

            return true;
        }

        internal virtual void Pivoting_Inner(Measure source, LayoutArea destArea)
        {
            throw new NotImplementedException();
        }

        internal virtual void Pivoting_Inner(Level source, LayoutArea destArea)
        {
            throw new NotImplementedException();
        }

        private void PivotingToColorAxis(IDescriptionable item)
        {
            if (item == null)
            {
                FLayout.ColorBackAxisItem = null;
                if (Active) CellSet.Rebuild();
            }
            if (item is Measure)
            {
                FLayout.ColorBackAxisItem = item;
                if (Active)
                    CellSet.Rebuild();
            }
            if (item is Hierarchy)
                PivotingFirst((Hierarchy) item, LayoutArea.laColor);
        }

        private void PivotingToForeColorAxis(IDescriptionable item)
        {
            if (item == null)
            {
                FLayout.fColorForeAxisItem = null;
                if (Active) CellSet.Rebuild();
            }
            if (item is Measure)
            {
                FLayout.fColorForeAxisItem = item;
                if (Active)
                    CellSet.Rebuild();
            }
        }

        private void PivotingToSizeAxis(IDescriptionable item)
        {
            if (item == null)
            {
                FLayout.SizeAxisItem = null;
                if (Active) CellSet.Rebuild();
            }
            if (item is Measure)
            {
                FLayout.SizeAxisItem = item;
                if (Active) CellSet.Rebuild();
            }
            if (item is Hierarchy)
                PivotingFirst((Hierarchy) item, LayoutArea.laSize);
        }

        private void PivotingToShapeAxis(IDescriptionable item)
        {
            if (item == null)
            {
                FLayout.ShapeAxisItem = null;
                if (Active) CellSet.Rebuild();
            }
            if (item is Measure)
            {
                FLayout.ShapeAxisItem = item;
                if (Active) CellSet.Rebuild();
            }
            if (item is Hierarchy)
                PivotingFirst((Hierarchy) item, LayoutArea.laShape);
        }

        /// <summary>Rotates the Grid swapping rows and columns areas.</summary>
        public void ToggleLayout()
        {
            DebugLogging.WriteLine("OlapControl.ToggleLayout()");

            var P = FLayout.fRowAxis;

            FLayout.fRowAxis = FLayout.fColumnAxis;
            FLayout.fColumnAxis = P;
            FLayout.fMeasureLayout = FLayout.fMeasureLayout == LayoutArea.laRow
                ? LayoutArea.laColumn
                : LayoutArea.laRow;
            var ll = FLayout.fRowLevels;
            FLayout.fRowLevels = FLayout.fColumnLevels;
            FLayout.fColumnLevels = ll;
            if (CellsetMode == CellsetMode.cmChart)
                if (FLayout.fYAxisMeasures.Count == 0 ||
                    FLayout.fYAxisMeasures.Count == 1 && FLayout.fYAxisMeasures[0].Count == 1)
                {
                    var YMeasure = FLayout.fYAxisMeasures.Count == 1 ? FLayout.fYAxisMeasures[0][0] : null;
                    var XMeasure = FLayout.fXAxisMeasure;
                    if (YMeasure != null)
                        Pivoting(YMeasure, LayoutArea.laColumn, null, LayoutArea.laRow);
                    if (XMeasure != null)
                        Pivoting(XMeasure, LayoutArea.laRow, null, LayoutArea.laColumn);
                }
            EndChange(GridEventType.gePivotAction);
            if (FCellSet != null)
                FCellSet.Rebuild();
        }

        /// <summary>
        ///     Searches for a hierarchy member by its name in all initialized Grid
        ///     hierarchies.
        /// </summary>
        /// <returns>The specified member, or null if the specified control does not exist.</returns>
        /// <remarks>
        ///     <para>
        ///         A "hierarchy member name" may either be unique or comprised of Captions from
        ///         the hierarchy name, its parent members names and all the captions of the levels,
        ///         hierarchies and dimensions this member belongs to.
        ///     </para>
        ///     <para>For more details read the Finding a Hierarchy Member article.</para>
        ///     <para>
        ///         For the Desktop version all hierarchies are initialized once the Cube is
        ///         open. For the MS AS version all previously unused hierarchies are uninitialized,
        ///         thus they do not contain any information about thier members or levels. That's why
        ///         searching members in an uninitialized hierarchy is useless. You can initialize a
        ///         hierarchy by calling its InitHierarhy method.
        ///     </para>
        /// </remarks>
        /// <param name="MemberName">The name of the member to be found.</param>
        public Member FindMemberByName(string MemberName)
        {
            foreach (var d in FDimensions)
            foreach (var h in d.FHierarchies)
                if ((HierarchyState.hsInitialized & h.State) == HierarchyState.hsInitialized)
                {
                    foreach (var l in h.Levels)
                    {
                        var Result = l.FindMember(MemberName);
                        if (Result != null)
                            return Result;
                    }
                    var S = '[' + d.DisplayName + "].[" + h.DisplayName + "].";
                    if (MemberName.ToLower().StartsWith(S.ToLower()))
                    {
                        var Result = h.FindMemberByName(MemberName);
                        if (Result != null)
                            return Result;
                    }
                }
            return null;
        }

        /// <summary>
        ///     Indicates if the Grid is in the batch update mode initiated by the
        ///     BeginUpdate method.
        /// </summary>
        internal virtual bool IsUpdating => FUpdateCounter > 0;

        /// <summary>
        ///     Applies a filter to hierarchies in the current Cellset so that only the items of
        ///     the <em>cells</em> list, passed as the parameter, remain visible.
        /// </summary>
        public void FilterPoints(IList<ICubeAddress> cells)
        {
            var filter = new Dictionary<string, List<Member>>();
            foreach (var a in cells)
            foreach (var m in a.FLevelsAndMembers.Values)
            {
                var hname = m.Level.Hierarchy.UniqueName;
                List<Member> lm;
                if (!filter.TryGetValue(hname, out lm))
                {
                    lm = new List<Member>();
                    filter.Add(hname, lm);
                }
                if (!lm.Contains(m)) lm.Add(m);
            }
            BeginUpdate();
            foreach (var f in filter)
            {
                var h = Dimensions.FindHierarchy(f.Key);
                h.BeginUpdate();
                h.DoSetFilter(false);
                foreach (var m in f.Value)
                    m.Visible = true;
                h.EndUpdate();
            }
            EndUpdate();
        }

        internal IEnumerable<Member> MeasuresVisible
        {
            get
            {
                if (Measures == null || Measures.Level == null)
                    yield break;

                foreach (var member in Measures.Level.Members)
                    if (member.Visible)
                        yield return member;
            }
        }

        protected virtual void DoDisposed(object Sender, EventArgs e)
        {
            foreach (var member in MeasuresVisible)
                member.Visible = false;

            Cube = null;
            FEngine = null;
            Measures.Clear();
            Dimensions.Clear();
            FLayout.RowNodes = "";
            FLayout.ColumnNodes = "";
            FLayout.PageNodes = "";
        }

        internal virtual Cube.RadarCube Cube { get; set; }

        /// References to the AxesLayout object defining the state of the OLAP control pivot areas.
        /// </summary>
        public virtual AxesLayout AxesLayout => FLayout;

        /// <summary>References to the measures list on the Grid level.</summary>
        /// <remarks>
        ///     The measures structure, represented by this property, reproduces the the one
        ///     presented by the Measures property of the RadarCube object, but adds information about
        ///     their appearance.
        /// </remarks>
        public Measures Measures => fMeasures;


        //        [Obsolete("Use ChartsType")]
        //        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //        internal int[] ChartTypes
        //        {
        //            get { return FChartTypes; }
        //        }

        /// <summary>References to the Cube dimensions list on the Grid level.</summary>
        /// <remarks>
        ///     The dimensions structure, this property references to, reproduces the dimensions
        ///     structure referenced to by the Dimensions property of the RadarCube object, but adds
        ///     information about appearance of hierarchies, levels and hierarchy members.
        /// </remarks>
        public virtual Dimensions Dimensions => FDimensions;

        /// <summary>The event is raised before any pivot operation in the Grid.</summary>
        public virtual event EventHandler<PivotEventArgs> OnBeforePivot;

        /// <summary>The event is raised after any pivot operation in the Grid.</summary>
        public virtual event EventHandler<PivotEventArgs> OnAfterPivot;

        private CallbackData _callbackData = CallbackData.Nothing;
        internal string _callbackClientErrorString;

        internal CallbackData callbackData
        {
            get => _callbackData;
            set
            {
                if (_callbackData != CallbackData.ClientError)
                    _callbackData = value;
            }
        }

        /// <summary>
        ///     The event is raised at the hierarchy members' sorting if their
        ///     OverrideSortMethods property is set to <em>True</em>.
        /// </summary>
        /// <example>
        ///     <para>
        ///         The <em>Date</em> hierarchy contains three levels: <em>Year</em>,
        ///         <em>Month</em> and <em>Day</em>, the members of the <em>Month</em> hierarchy are
        ///         represented with full names of the months, from <em>January</em> to
        ///         <em>December</em>. We need the descending and ascending sorting to put them in
        ///         proper (not alphabetical) order.
        ///     </para>
        ///     <code lang="CS" description="First of all, write the OnInitHierarchy event handler:">
        /// private List&lt;string&gt; _monthsArray;
        /// protected void OlapAnalysis1_OnInitHierarchy(object Sender, EventInitHierarchyArgs EventArgs)
        /// {
        ///     if (EventArgs.Hierarchy.DisplayName == "Date")
        ///     {
        ///         // set the overriding flag of the sorting event
        ///         EventArgs.Hierarchy.OverrideSortMethods = true;
        ///         // call the sorting method
        ///         EventArgs.Hierarchy.Sort();
        ///         // Fill up the helper array with the string month names
        ///         _monthsArray = new List&lt;string&gt;(12);
        ///         for (int i = 1; i &lt;= 12; i++)
        ///             _monthsArray.Add(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i));
        ///     }
        /// }
        /// </code>
        ///     <code lang="VB" description="First of all, write the OnInitHierarchy event handler:">
        /// Private _monthsArray As List(Of String)
        /// Protected Sub OlapAnalysis1_OnInitHierarchy(ByVal Sender As Object, ByVal EventArgs As EventInitHierarchyArgs)
        ///     If (EventArgs.Hierarchy.DisplayName Is "Date") Then
        ///         ' set the overriding flag of the sorting event
        ///         EventArgs.Hierarchy.OverrideSortMethods = True
        ///         ' call the sorting method
        ///         EventArgs.Hierarchy.Sort
        ///         ' Fill up the helper array with the string month names
        ///         Me._monthsArray = New List(Of String)(12)
        ///         Dim i As Integer = 1
        ///         Do While (i &lt;= 12)
        ///             Me._monthsArray.Add(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i))
        ///             i += 1
        ///         Loop
        ///     End If
        /// End Sub
        /// </code>
        ///     <code lang="CS"
        ///         description="In the OnMemberSort event handler, implement the sorting algorithm for the Month hierarchy level.">
        /// protected void OlapAnalysis1_OnMemberSort(object Sender, EventMemberSortArgs EventArgs)
        /// {
        ///     if ((EventArgs.MemberLow.Level.Hierarchy.DisplayName == "Date") &amp;&amp;
        ///        (EventArgs.MemberLow.Level.DisplayName == "Month"))
        ///     {
        ///         // have indexes for the names of the months
        ///         int i = _monthsArray.IndexOf(EventArgs.MemberLow.DisplayName);
        ///         int j = _monthsArray.IndexOf(EventArgs.MemberHigh.DisplayName);
        ///         // return a value of the comparison operation in the EventArgs.Result variable
        ///         if (EventArgs.SortingMethod == MembersSortType.msNameDesc)
        ///             EventArgs.Result = Math.Sign(j - i);
        ///         else EventArgs.Result = Math.Sign(i - j);
        ///     }
        /// }
        /// </code>
        ///     <code lang="VB"
        ///         description="In the OnMemberSort event handler, implement the sorting algorithm for the Month hierarchy level.">
        /// Protected Sub OlapAnalysis1_OnMemberSort(ByVal Sender As Object, ByVal EventArgs As EventMemberSortArgs)
        ///     If ((EventArgs.MemberLow.Level.Hierarchy.DisplayName Is "Date") AndAlso 
        ///             (EventArgs.MemberLow.Level.DisplayName Is "Month")) Then
        ///         ' have indexes for the names of the months
        ///         Dim i As Integer = Me._monthsArray.IndexOf(EventArgs.MemberLow.DisplayName)
        ///         Dim j As Integer = Me._monthsArray.IndexOf(EventArgs.MemberHigh.DisplayName)
        ///         ' return a value of the comparison operation in the EventArgs.Result variable
        ///         If (EventArgs.SortingMethod = MembersSortType.msNameDesc) Then
        ///             EventArgs.Result = Math.Sign(CInt((j - i)))
        ///         Else
        ///             EventArgs.Result = Math.Sign(CInt((i - j)))
        ///         End If
        ///     End If
        /// End Sub
        /// </code>
        /// </example>
        /// <remarks>
        ///     <para>
        ///         To use your own sorting mode, in the OnInitHierarchy event handler you need
        ///         to set the OverrideSortMethods property of the appropriate hierarchy to
        ///         <em>True</em>, and then handle the OlapGrid.OnMemberSort event implementing
        ///         comparison function you need.
        ///     </para>
        ///     <para></para>
        /// </remarks>
        public virtual event EventHandler<EventMemberSortArgs> OnMemberSort;

        /// <summary>
        ///     Raised at the hierarchy initialization. Can be used for initializing some
        ///     hierarchy properties like TotalAppearance, AllowFilter etc, defining the behavior
        ///     of the hierarchy.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         A hierarhy is initialized by either moving it to the active area or calling the
        ///         <see cref="THierarchy.InitHierarchy">Hierarchy.InitHierarchy</see> method.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <para>
        ///         We need to initialize the <em>Reseller Type</em> hierarchy, create the
        ///         <em>Others</em> group in it and put there the <em>Warehouse</em> member. Also we
        ///         want to disable the page-viewing option for the <em>Month</em> hierarchy.
        ///     </para>
        ///     <code lang="CS">
        /// protected void OlapAnalysis1_OnInitHierarchy(object Sender, EventInitHierarchyArgs EventArgs)
        /// {
        ///    if (EventArgs.Hierarchy.DisplayName == "Month")
        ///       EventArgs.Hierarchy.Levels[0].PagerSettings.AllowPaging = false;
        ///  
        ///    Hierarchy H = EventArgs.Hierarchy;
        ///    if (H.DisplayName == "Reseller Type")
        ///    {
        ///       H.CreateGroup("Others", CustomMemberPosition.cmpFirst,
        ///          H.FindMemberByName("[Reseller].[Reseller Type].[Business Type].[Warehouse]"));
        ///    }
        /// }
        /// </code>
        ///     <code lang="VB">
        /// Protected Sub OlapAnalysis1_OnInitHierarchy(ByVal &lt;&gt;Sender&lt;&gt; As Object, ByVal EventArgs As EventInitHierarchyArgs)
        ///       If (EventArgs.Hierarchy.DisplayName Is "Month") Then
        ///             EventArgs.Hierarchy.Levels.Item(0).PagerSettings.AllowPaging = False
        ///       End If
        ///       Dim H As Hierarchy = EventArgs.Hierarchy
        ///       If (H.DisplayName Is "Reseller Type") Then
        ///             H.CreateGroup("Others", CustomMemberPosition.cmpFirst, New Member() { H.FindMemberByName("[Reseller].[Reseller Type].[Business Type].[Warehouse]") })
        ///       End If
        /// End Sub
        /// </code>
        /// </example>
        public virtual event EventHandler<EventInitHierarchyArgs> OnInitHierarchy;

        /// <summary>
        ///     This event handler calculated the values of the calculated measures (third type)
        ///     and hierarchy members.
        /// </summary>
        /// <remarks>
        ///     For an example see <see cref="TMeasures.AddCalculatedMeasure">Measures.AddCalculatedMeasure</see> Method.
        /// </remarks>
        public virtual event EventHandler<CalcMemberArgs> OnCalcMember;

        /// <summary>
        ///     Allows to hide some hierarchy members from the <em>Cellset</em> without applying
        ///     any filter.
        /// </summary>
        /// <example>
        ///     <code description="Hides all non-grand totals:">
        /// protected void OlapAnalysis1_OnAllowDisplayMember(object sender, AllowDisplayMemberArgs args)
        /// {
        ///     args.Allow = (!args.IsTotal) || (args.Member == null); 
        /// }
        /// </code>
        ///     <code description="Shows only those cells in the Grid whose [Measures].[Sales Amount] value is not null.">
        /// protected void OlapAnalysis1_OnAllowDisplayMember(object sender, AllowDisplayMemberArgs args)
        /// {
        ///     Measure m = OlapAnalysis1.Measures.Find("[Measures].[Sales Amount]");
        ///     ICubeAddress a = args.Address;
        ///     a.Measure = m;
        ///  
        ///     object dummy;
        ///     args.Allow = OlapAnalysis1.Engine.GetCellValue(a, out dummy);
        /// }
        /// </code>
        /// </example>
        public virtual event EventHandler<AllowDisplayMemberArgs> OnAllowDisplayMember;

        /// <summary>
        ///     A handler for this event should implement the function calculating values of the
        ///     previously added custom measure mode.
        /// </summary>
        /// <summary>You can add custom measure show mode in this event handler.</summary>
        /// <remarks>
        ///     <para>
        ///         Use the Measure.ShowModes.Add method to add a user display mode in
        ///         OnInitMeasure event.
        ///     </para>
        ///     <para>For more details see Measure Display Modes.</para>
        /// </remarks>
        public virtual event EventHandler<ShowMeasureArgs> OnShowMeasure;

        /// <summary>
        ///     Fired right after initialization of the measures list as you open the Cube.
        /// </summary>
        /// <remarks>Custom measure view modes can be added only in this event handler.</remarks>
        public virtual event EventHandler OnInitMeasures;

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid);

            WriteStreamedObjectByDerivedClass(writer);
            if (Mode != OlapGridMode.gmStandard)
            {
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_Mode);
                StreamUtils.WriteInt32(writer, (int) Mode);
            }

            StreamUtils.WriteStreamedObject(writer, fMeasures, Tags.tgOLAPGrid_Measures);

            StreamUtils.WriteStreamedObject(writer, FDimensions, Tags.tgOLAPGrid_Dimensions);

            StreamUtils.WriteStreamedObject(writer, FLayout, Tags.tgOLAPGrid_Layout);

            StreamUtils.WriteTypedStreamedObject(writer, FEngine, Tags.tgOLAPGrid_Engine);

            StreamUtils.WriteStreamedObject(writer, FCellSet, Tags.tgOLAPGrid_Cellset);

            if (PivotingBehavior != PivotingBehavior.Excel2010)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_PivotingBehavior);

            if (HierarchiesDisplayMode != HierarchiesDisplayMode.TreeLike)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_HierarchiesDisplayMode);

            if (FFilteredHierarchies != null && FFilteredHierarchies.Count > 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_FilteredHierarchies);
                StreamUtils.WriteInt32(writer, FFilteredHierarchies.Count);
                for (var i = 0; i < FFilteredHierarchies.Count; i++)
                    StreamUtils.WriteString(writer, FFilteredHierarchies[i].UniqueName);
            }
            if (ChartsType != null)
            {
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_ChartsType);
                StreamUtils.WriteInt32(writer, ChartsType.Length);
                foreach (var t in ChartsType)
                    StreamUtils.WriteInt32(writer, (int) t);
            }

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_EOT);
        }

        internal virtual void WriteStreamedObjectByDerivedClass(BinaryWriter writer)
        {
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            FMode = OlapGridMode.gmStandard;
            FilteredHierarchiesClear();

            StreamUtils.CheckTag(reader, Tags.tgOLAPGrid);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgOLAPGrid_Cellset:
                        FCellSet = CreateCellset();
                        ;
                        StreamUtils.ReadStreamedObject(reader, FCellSet);
                        break;
                    case Tags.tgOLAPGrid_Dimensions:
                        FDimensions = new Dimensions(this);
                        StreamUtils.ReadStreamedObject(reader, FDimensions);
                        FDimensions.RestoreAfterSerialization(this);
                        break;
                    case Tags.tgOLAPGrid_Engine:
                        var engine = (Engine.Engine) StreamUtils.ReadTypedStreamedObject(reader, this);
                        engine.RestoreAfterSerialization(this);
                        break;
                    case Tags.tgOLAPGrid_Measures:
                        fMeasures = new Measures(this);
                        StreamUtils.ReadStreamedObject(reader, fMeasures);
                        fMeasures.RestoreAfterSerialization(this);
                        break;
                    case Tags.tgOLAPGrid_Mode:
                        FMode = (OlapGridMode) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgOLAPGrid_FilteredHierarchies:
                        var c = StreamUtils.ReadInt32(reader);
                        for (var i = 0; i < c; i++)
                            FFilteredHierarchies.Add(FDimensions.FindHierarchy(StreamUtils.ReadString(reader)));
                        break;
                    case Tags.tgOLAPGrid_PivotingBehavior:
                        PivotingBehavior = PivotingBehavior.RadarCube;
                        break;
                    case Tags.tgOLAPGrid_HierarchiesDisplayMode:
                        HierarchiesDisplayMode = HierarchiesDisplayMode.TableLike;
                        break;
                    case Tags.tgOLAPGrid_ChartsType:
                        var count = StreamUtils.ReadInt32(reader);
                        ChartsType = new SeriesType[count];
                        for (var i = 0; i < ChartsType.Length; i++)
                            ChartsType[i] = (SeriesType) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgOLAPGrid_EOT:
                        FLayout.CheckExpandedLevels();
                        exit = true;
                        break;
                    default:
                        ReadByDerivedClass(tag, reader);
                        break;
                }
            }
        }

        internal virtual void ReadByDerivedClass(Tags tag, BinaryReader reader)
        {
        }


        private void WriteCommonPart(BinaryWriter writer, StreamContent StreamContent, byte[] UserData)
        {
            StreamUtils.WriteTag(writer, Tags.tgCommonStream);

            if ((StreamContent & StreamContent.CubeData) > 0)
                StreamUtils.WriteStreamedObject(writer, Cube, Tags.tgCommonStream_OLAPCube);


            if ((StreamContent & StreamContent.GridState) > 0)
                StreamUtils.WriteStreamedObject(writer, this, Tags.tgCommonStream_GridState);

            StreamUtils.WriteBinary(writer, UserData, Tags.tgCommonStream_UserData);

            StreamUtils.WriteTag(writer, Tags.tgCommonStream_EOT);
        }

        private void WriteCommonStream(BinaryWriter writer, StreamContent StreamContent, byte[] UserData)
        {
            if (writer == null) return;
            if (!Active)
                throw new Exception(RadarUtils.GetResStr("rsCantSaveInactiveGrid"));

            StreamUtils.WriteURCFHeader(writer, Cube.GetProductID(), Controls.Cube.RadarCube.VersionNumber, 0);

            var pos = writer.BaseStream.Position;
            long L = 0;
            writer.Write(L); // Reserve the place for the size of the block

            WriteCommonPart(writer, StreamContent, UserData);

            var pos2 = writer.BaseStream.Position;
            writer.BaseStream.Seek(pos, SeekOrigin.Begin);
            L = pos2 - pos - sizeof(long);
            writer.Write(L);
            writer.BaseStream.Seek(pos2, SeekOrigin.Begin);
        }

        private void WriteCommonStreamCompr1(BinaryWriter writer, StreamContent StreamContent, byte[] UserData)
        {
            if (writer == null) return;
            if (!Active)
                throw new Exception(RadarUtils.GetResStr("rsCantSaveInactiveGrid"));

            StreamUtils.WriteURCFHeader(writer, Cube.GetProductID(), Controls.Cube.RadarCube.VersionNumber, 1);

            var pos = writer.BaseStream.Position;
            long L = 0;
            writer.Write(L); // Reserve the place for the size of the block

            // Temp memory stream
            using (var tmp = new MemoryStream())
            {
                using (var tmp_writer = new BinaryWriter(tmp))
                {
                    WriteCommonPart(tmp_writer, StreamContent, UserData);
                    var length = Convert.ToInt32(tmp.Length);
                    var b = tmp.ToArray();
                    using (var ms = new MemoryStream())
                    {
                        using (var ds = new DeflateStream(ms, CompressionMode.Compress, true))
                        {
                            ds.Write(b, 0, b.Length);
                        }
                        // Write length of the source data - we'll neeed it on decompression
                        writer.Write(length);
                        length = Convert.ToInt32(ms.Length);
                        writer.Write(length);
                        ms.WriteTo(writer.BaseStream);
                    }
                }
            }

            var pos2 = writer.BaseStream.Position;
            writer.BaseStream.Seek(pos, SeekOrigin.Begin);
            L = pos2 - pos - sizeof(long);
            writer.Write(L);
            writer.BaseStream.Seek(pos2, SeekOrigin.Begin);
        }

        private void ReadURCF(BinaryReader RD, out byte[] UserData)
        {
            var CSP = StreamUtils.GetCommonStreamProperties(RD);
            if (!CSP.Correct)
                throw new Exception(RadarUtils.GetResStr("rsInvalidStreamFormat"));

            if (CSP.ProductID != RadarUtils.rsWinFormsDesktop &&
                CSP.ProductID != RadarUtils.rsAspNetDesktop &&
                CSP.ProductID != RadarUtils.rsWpfDesktop &&
                CSP.ProductID != RadarUtils.rsWinFormsMSAS &&
                CSP.ProductID != RadarUtils.rsAspNetMSAS &&
                CSP.ProductID != RadarUtils.rsWpfMSAS &&
                CSP.ProductID != RadarUtils.rsAspNetCoreXmla)
                throw new Exception(RadarUtils.GetResStr("rsCantReadAnotherProductStream"));
            if (!Cube.StreamVersionSupported(CSP.Version))
                throw new Exception(RadarUtils.GetResStr("rsecStreamVersionError", CSP.Version));
            UserData = null;

            DebugLogging.WriteLine("OlapControl.ReadURCF.(ProductID={0}, Version={1})", CSP.ProductID,
                CSP.Version);

            if (CSP.CompressionMethod == 0)
            {
                // Uncompressed stream
                ReadCommonStream(RD, out UserData);
            }
            else if (CSP.CompressionMethod == 1)
            {
                // Standard compressed stream
                // Restore the length of the uncomressed data
                var length = RD.ReadInt32();
                var le = RD.ReadInt32();
                var buf = new byte[le];
                RD.Read(buf, 0, le);
                var ms = new MemoryStream(buf);
                ms.Position = 0;
                var ds = new DeflateStream(ms, CompressionMode.Decompress);
                var b = new byte[length + 4096];
                var count = StreamUtils.ReadAllBytesFromStream(ds, b);
                var tmp = new MemoryStream(b);
                tmp.Position = 0;
                var reader = new BinaryReader(tmp);
                // Uncompressed stream
                ReadCommonStream(reader, out UserData);
            }
        }

        private void ReadCommonStream(BinaryReader reader, out byte[] UserData)
        {
            StreamUtils.CheckTag(reader, Tags.tgCommonStream);
            var olapCubeFound = false;
            UserData = null;
            MemoryStream XMLGridState = null;
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgCommonStream_OLAPCube:
                        if (Cube.GetProductID() == RadarUtils.rsWinFormsDesktop
                            || Cube.GetProductID() == RadarUtils.rsAspNetDesktop
                            || Cube.GetProductID() == RadarUtils.rsWpfDesktop
                            || Cube.GetProductID() == RadarUtils.rsAspNetMSAS
                        )
                        {
                            olapCubeFound = true;
                            if (Cube.Active) Cube.Active = false;
                            StreamUtils.ReadStreamedObject(reader, Cube);
                        }
                        else
                        {
                            StreamUtils.SkipValue(reader);
                        }
                        break;
                    case Tags.tgCommonStream_GridState:
                        if (!olapCubeFound && !Active)

                            throw new Exception(RadarUtils.GetResStr("rsCantRestoreInactiveGrid"));
                        StreamUtils.ReadStreamedObject(reader, this, Tags.tgCommonStream_GridState);
                        //(this as IStreamedObject).ReadStream(reader, null);
                        break;
                    case Tags.tgCommonStream_UserData:
                        UserData = StreamUtils.ReadBinary(reader);
                        break;
                    case Tags.tgCommonStream_EOT:
                        exit = true;
                        if (olapCubeFound)
                        {
                        }
                        if (XMLGridState != null)
                        {
                            using (XMLGridState)
                            {
                                Serializer.ReadXML(XMLGridState);
                            }
                            XMLGridState = null;
                        }
                        ApplyChanges();

                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        #endregion

        /// <summary>
        ///     Forcibly serializes OlapGrid state for future sessions. A given method is used
        ///     by the RadarCube infrastructure and, as a rule, there's no need to call the method by
        ///     yourself.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         To save the current Grid state into a strim or a file, use the
        ///         OlapGrid.Serializer.WriteXML method.
        ///     </para>
        /// </remarks>
        public virtual void ApplyChanges()
        {
            InitSessionData();
        }

        private void SaveUncompressed(BinaryWriter writer, StreamContent StreamContent, byte[] data)
        {
            WriteCommonStream(writer, StreamContent, data);
        }

        private void SaveCompressed(BinaryWriter writer, StreamContent StreamContent, byte[] data)
        {
            WriteCommonStreamCompr1(writer, StreamContent, data);
        }

        private void Load(BinaryReader reader, out byte[] data)
        {
            data = null;
            BeginUpdate();
            try
            {
                ReadURCF(reader, out data);
            }
            catch (Exception ex)
            {
                ErrorCatchHandler(ex, null);
                return;
            }
            EndUpdate();
        }

        // ------------ Public Save/Restore methods ----------------
        /// <summary>Saves Grid data into a stream or a file in an uncompressed format.</summary>
        /// <remarks>
        ///     <para>
        ///         Saving the Grid data allows you to restore your Grid either from a stream or
        ///         a file rather than from the database.
        ///     </para>
        ///     <para>
        ///         The saved stream can be read by both TOLAPCube.Load and TCustomOLAPGrid.Load
        ///         methods.
        ///     </para>
        ///     <para>
        ///         The saved stream can contain any additional information the user might want
        ///         to save. This may for example be a password, decryption key, or just any data. User
        ///         data may be represented with a byte[] parameter.
        ///     </para>
        ///     <para>Both Grid-specific and Cube-specific information can be saved.</para>
        ///     <para>
        ///         You can also use the SaveCompressed method instead to perform a compression
        ///         on the final stream.
        ///     </para>
        /// </remarks>
        /// <param name="stream">The stream to save the data to.</param>
        /// <param name="StreamContent">Defines what data should be saved to the stream: Grid-specific, Cube-specific or both.</param>
        /// <param name="UserData">Additional user data. If null, no user data is saved.</param>
        public void SaveUncompressed(Stream stream, StreamContent StreamContent, byte[] UserData)
        {
            DebugLogging.WriteLine("OlapControl.SaveUncompressed(stream)");

            if (stream == null) return;
            if (!stream.CanWrite || !stream.CanSeek)
                throw new Exception(RadarUtils.GetResStr("rsCantWriteToStream"));
            var writer = new BinaryWriter(stream);
            try
            {
                SaveUncompressed(writer, StreamContent, UserData);
            }
            finally
            {
                writer = null;
            }
        }

        /// <summary>Saves Grid data into a stream or a file in an uncompressed format.</summary>
        /// <param name="stream">The stream to save the data to.</param>
        /// <param name="StreamContent">Defines what data should be saved to the stream: Grid-specific, Cube-specific or both.</param>
        public void SaveUncompressed(Stream stream, StreamContent StreamContent)
        {
            SaveUncompressed(stream, StreamContent, null);
        }

        /// <summary>Saves Grid data into a stream or a file in an uncompressed format.</summary>
        /// <param name="FileName">The file to save the data to.</param>
        /// <param name="StreamContent">Defines what data should be saved to the file: Grid-specific, Cube-specific or both.</param>
        /// <param name="UserData">Additional user data. If null, no user data is saved.</param>
        public void SaveUncompressed(string FileName, StreamContent StreamContent, byte[] UserData)
        {
            using (var stream = new FileStream(FileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                SaveUncompressed(stream, StreamContent, UserData);
            }
        }

        /// <summary>Saves Grid data into a stream or a file in an uncompressed format.</summary>
        /// <param name="FileName">The file to save the data to.</param>
        /// <param name="StreamContent">Defines what data should be saved to the stream: Grid-specific, Cube-specific or both.</param>
        public void SaveUncompressed(string FileName, StreamContent StreamContent)
        {
            SaveUncompressed(FileName, StreamContent, null);
        }

        /// <summary>Saves Grid data into a stream or a file in a compressed format.</summary>
        /// <remarks>
        ///     <para>
        ///         This method works exactly as SaveUncompressed only the final stream is
        ///         compressed for the sake of saving memory. Compressing may take up a bit more time,
        ///         but it can prove useful in case of slow or remote streams.
        ///     </para>
        ///     <para>
        ///         The saved stream can be read by both TOLAPCube.Load and TCustomOLAPGrid.Load
        ///         methods.
        ///     </para>
        /// </remarks>
        /// <param name="stream">The stream to save the data to.</param>
        /// <param name="StreamContent">Defines what data should be saved to the stream: Grid-specific, Cube-specific or both.</param>
        /// <param name="UserData">Additional user data. If null, no user data is saved.</param>
        public void SaveCompressed(Stream stream, StreamContent StreamContent, byte[] UserData)
        {
            DebugLogging.WriteLine("OlapControl.SaveCompressed(stream)");

            if (stream == null) return;
            if (!stream.CanWrite || !stream.CanSeek)
                throw new Exception(RadarUtils.GetResStr("rsCantWriteToStream"));
            var writer = new BinaryWriter(stream);
            try
            {
                SaveCompressed(writer, StreamContent, UserData);
            }
            finally
            {
                writer = null;
            }
        }

        /// <summary>Saves Grid data into a stream or file in a compressed format.</summary>
        /// <param name="stream">The stream to save the data to.</param>
        /// <param name="StreamContent">Defines what data should be saved to the stream: Grid-specific, Cube-specific or both.</param>
        public void SaveCompressed(Stream stream, StreamContent StreamContent)
        {
            SaveCompressed(stream, StreamContent, null);
        }

        /// <summary>Saves Grid data into a stream or file in a compressed format</summary>
        /// <param name="FileName">The file to save the data to.</param>
        /// <param name="StreamContent">Defines what data should be saved to the stream: Grid-specific, Cube-specific or both.</param>
        /// <param name="UserData">Additional user data. If null, no user data is saved.</param>
        public void SaveCompressed(string FileName, StreamContent StreamContent, byte[] UserData)
        {
            using (var stream = new FileStream(FileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                SaveCompressed(stream, StreamContent, UserData);
            }
        }

        /// <summary>Saves Grid data into a stream or file in a compressed format.</summary>
        /// <param name="FileName">The file to save the data to.</param>
        /// <param name="StreamContent">Defines what data should be saved to the stream: Grid-specific, Cube-specific or both.</param>
        public void SaveCompressed(string FileName, StreamContent StreamContent)
        {
            SaveCompressed(FileName, StreamContent, null);
        }

        /// <summary>Loads Grid data from a stream or a file.</summary>
        /// <remarks>
        ///     <para>
        ///         It doesn't matter if the stream is compressed or not - the method can read
        ///         both.
        ///     </para>
        ///     <para>Saved user data, if any, is returned with the UserData parameter.</para>
        ///     <para>
        ///         The method may read both Cube-specific and Grid-specific data saved by either
        ///         SaveCompressed or SaveUncompressed methods called either from TOLAPCube or
        ///         TCustomOLAPGrid controls. If the stream contains Cube data then the current Cube
        ///         gets reactivated with the new Cube data. If the stream contains a Grid state then
        ///         it is applied to the current Grid.
        ///     </para>
        /// </remarks>
        /// <param name="stream">The stream to load the data from.</param>
        /// <param name="UserData">Returns the additional user data if there is any in the stream.</param>
        public void Load(Stream stream, out byte[] UserData)
        {
            DebugLogging.WriteLine("OlapControl.Load(stream)");

            var reader = new BinaryReader(stream);
            Load(reader, out UserData);
            reader = null;
        }

        /// <summary>Loads Grid data from a stream or a file.</summary>
        /// <param name="stream">The stream to load the data from.</param>
        public void Load(Stream stream)
        {
            byte[] data;
            Load(stream, out data);
            data = null;
        }

        /// <summary>
        ///     Loads Grid data from a stream or a file.
        /// </summary>
        /// <param name="FileName">The file to load the data from.</param>
        /// <param name="UserData">Returns the additional user data if there is any in the stream.</param>
        public void Load(string FileName, out byte[] UserData)
        {
            using (var stream = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Load(stream, out UserData);
            }
        }

        /// <summary>
        ///     Loads Grid data from a stream or a file.
        /// </summary>
        /// <param name="FileName">The file to load the data from.</param>
        public void Load(string FileName)
        {
            byte[] data;
            Load(FileName, out data);
            data = null;
        }

        /// <summary>
        ///     Allows to control the avaliability of drilling operations for the end user in each cell of the OLAP-slice.
        /// </summary>
        public virtual event EventHandler<AllowDrillActionEventArgs> AllowDrillAction;

        protected internal virtual PossibleDrillActions OnAllowDrillAction(IMemberCell cell,
            PossibleDrillActions actions)
        {
            if (AllowDrillAction != null)
            {
                var E = new AllowDrillActionEventArgs(cell, actions);
                AllowDrillAction(this, E);
                return E.Actions;
            }
            return actions;
        }


        internal virtual void RefreshChartsType()
        {
        }


        internal void ErrorCatchHandler(Exception ex, string latestState)
        {
            var handled = OnError(ex);
            FUpdateCounter = 0;
            if (!handled)
            {
                if (!string.IsNullOrEmpty(latestState))
                    Serializer.XMLString = latestState;
                callbackException = ex;
            }
        }


        private ResizeColumnMode _ResizeColumnMode;

        internal ResizeColumnMode ResizeColumnMode
        {
            get => _ResizeColumnMode;
            set
            {
                if (_ResizeColumnMode == value)
                    return;

                _ResizeColumnMode = value;

                //DebugLogging.WriteLine("OlapControl.ResizeColumnMode={0}", ResizeColumnMode.ToString());
            }
        }

        internal bool _IsReadXMLProcessing;

        internal bool IsReadXMLProcessing
        {
            get => _IsReadXMLProcessing;
            set => _IsReadXMLProcessing = value;
        }

        internal virtual bool IsEval => true;

        internal void UpdateLevelsPageState()
        {
            if (Dimensions == null)
                return;

            foreach (var d in Dimensions)
                if (d.Hierarchies != null)
                    foreach (var h in d.Hierarchies)
                        if (h.Levels != null)
                            foreach (var l in h.Levels)
                            {
                                l.PagerSettings.AllowPaging = AllowPaging;
                                l.PagerSettings.LinesInPage = LinesInPage;
                            }
        }

        internal void InvalidateLevelsPageState()
        {
            if (Dimensions == null)
                return;

            if (CellSet != null)
                CellSet.ScrolledNodes.Clear();

            foreach (var d in Dimensions)
                if (d.Hierarchies != null)
                    foreach (var h in d.Hierarchies)
                        if (h.Levels != null)
                            foreach (var l in h.Levels)
                                l.PagerSettings.InvalidatePagingState();

            //

            //foreach (Dimension d in Dimensions)
            //{
            //    if (d.Hierarchies != null)
            //        foreach (Hierarchy h in d.Hierarchies)
            //        {
            //            if (h.Levels != null)
            //                foreach (Level l in h.Levels)
            //                {
            //                    l.PagerSettings.AllowPaging = allowpaging;
            //                    l.PagerSettings.LinesInPage = lineinpages;
            //                }
            //        }
            //}

            //
        }

        private Updater _Updater;

        internal Updater UpdaterInitialize
        {
            get
            {
                if (_Updater == null)
                    _Updater = new Updater();
                return _Updater;
            }
        }

        internal bool IsInitializeProcessed => UpdaterInitialize.IsBusy;

        internal virtual int CutLength { get; set; }

        internal string CutLineOfText(string AText)
        {
            string res;
            if (RadarUtils.CutText(AText, CutLength, out res))
                return res;
            return AText;
        }

        public event DataConverterHandler DataConverter;

        public virtual void OnDataConverter(object sender, DataConverterHandlerArgs e)
        {
            if (DataConverter != null)
                DataConverter(sender, e);
        }

        internal LayoutArea GetArea(string item)
        {
            var l = AxesLayout;

            if (l.ColorBackAxisItem != null && item == l.ColorBackAxisItem.UniqueName) return LayoutArea.laColor;
            if (l.ColorForeAxisItem != null && item == l.ColorForeAxisItem.UniqueName) return LayoutArea.laColorFore;

            if (l.fColumnAxis.Select(x => x.UniqueName).Contains(item)) return LayoutArea.laColumn;
            if (l.fColumnLevels != null && l.fColumnLevels.Select(x => x.UniqueName).Contains(item))
                return LayoutArea.laColumn;

            if (l.fDetailsAxis.Select(x => x.UniqueName).Contains(item)) return LayoutArea.laDetails;
            if (l.fPageAxis.Select(x => x.UniqueName).Contains(item)) return LayoutArea.laPage;
            if (l.fRowAxis.Select(x => x.UniqueName).Contains(item)) return LayoutArea.laRow;
            if (l.fRowLevels != null && l.fRowLevels.Select(x => x.UniqueName).Contains(item))
                return LayoutArea.laRow;
            if (l.fShapeAxisItem != null && l.fShapeAxisItem.UniqueName == item) return LayoutArea.laShape;
            if (l.fSizeAxisItem != null && l.fSizeAxisItem.UniqueName == item) return LayoutArea.laSize;
            if (l.fXAxisMeasure != null && l.fXAxisMeasure.UniqueName == item) return LayoutArea.laColumn;
            if (l.fYAxisMeasures.Any(i => i.Select(x => x.UniqueName).Contains(item))) return LayoutArea.laRow;

            var ms = Measures.FirstOrDefault(x => x.UniqueName == item);
            if (ms != null)
                if (ms.IsActive) return LayoutArea.laRow;

            return LayoutArea.laNone;
        }

        public event OnSerializeHandler OnBeforSave;
        public event EventHandler OnAfterSave;

        public event EventHandler OnBeforLoad;
        public event OnSerializeHandler OnAfterLoad;

        protected internal virtual void BeforSave(object sender, OnSerializeArgs e)
        {
            if (OnBeforSave != null)
                OnBeforSave(this, e);
        }

        protected internal virtual void AfterSave(object sender, EventArgs e)
        {
            if (OnAfterSave != null)
                OnAfterSave(this, e);
        }

        protected internal virtual void BeforLoad(object sender, EventArgs e)
        {
            if (OnBeforLoad != null)
                OnBeforLoad(this, e);
        }

        protected internal virtual void AfterLoad(object sender, OnSerializeArgs e)
        {
            if (OnAfterLoad != null)
                OnAfterLoad(this, e);
        }

        public void AddResourceStrings(string fileName, CultureInfo culture)
        {
            var filePath = MapPath(fileName);
            RadarUtils.AddResourceStrings(filePath, culture);
        }

        public void AddResourceStrings(string fileName)
        {
            AddResourceStrings(fileName, CultureInfo.CurrentCulture);
        }
    }
}