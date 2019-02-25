using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Http;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.CellSet.Md;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Engine;
using RadarSoft.RadarCube.Engine.Md;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Events;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;
using RadarSoft.XmlaClient;
using RadarSoft.XmlaClient.Metadata;
using Action = System.Action;
using Dimension = RadarSoft.XmlaClient.Metadata.Dimension;
using Hierarchy = RadarSoft.XmlaClient.Metadata.Hierarchy;
using HierarchyOrigin = RadarSoft.RadarCube.Enums.HierarchyOrigin;
using Level = RadarSoft.XmlaClient.Metadata.Level;
using Measure = RadarSoft.XmlaClient.Metadata.Measure;
using Member = RadarSoft.RadarCube.Layout.Member;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace RadarSoft.RadarCube.Controls.Cube.Md
{
    /// <summary>
    ///     Serves as a data source for visual OLAP conrols like OLAP Grid or OLAP Chart, should be connected either to MS
    ///     Analysis
    ///     OLAP server or local cube files.
    /// </summary>
    /// <remarks>
    ///     <para>The OLAP Grid can work in two different modes:</para>
    ///     <list type="bullet">
    ///         <item></item>
    ///         <item>
    ///             Standard (supporting cube navigation such as pivoting, drilling,
    ///             filtration, etc.)
    ///         </item>
    ///         <item>Display mode of the MDX-queries results.</item>
    ///     </list>
    ///     <para>
    ///         By default, the Grid is launched in a standard mode and is automatically
    ///         switched to the display mode of the MDX-query results if the ShowMDXQuery method is
    ///         called. You can switch the Grid back to the standard mode only by opening and
    ///         closing the cube.
    ///     </para>
    ///     <para>The Grid operation mode is defined by the Mode property.</para>
    ///     <para>
    ///         Operating the display mode of the MDX-queries results implies some
    ///         restrictions:
    ///     </para>
    ///     <list type="bullet">
    ///         <item></item>
    ///         <item>
    ///             Standard Cube navigation features (drilling, pivoting, filtration,
    ///             sorting and grouping) are not available. Calling the corresponding RadarCube
    ///             API methods leads to unpredictable results.
    ///         </item>
    ///         <item>Measure display modes are not available.</item>
    ///         <item>
    ///             Only the dimensions, hierarchies and measures mentioned in an MDX-query
    ///             are listed in Dimensions and Measures.
    ///         </item>
    ///         <item>Cube Structure Tree and Pivot Ares are never displayed.</item>
    ///     </list>
    ///     <para>
    ///         Note that the MOlapCube.ShowMDXQuery method is designed for fulfilling the MDX
    ///         queries and returns a Cellset as a result. To execute the MDX-scripts in the
    ///         current session, use the MOlapCube.Connection property.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code lang="CS" title="[New Example]">
    /// protected void Page_Load(object sender, EventArgs e)
    /// {
    ///     OlapCube1.Active = true;
    ///     if (!IsPostBack)
    ///         OlapCube1.ShowMDXQuery(OlapAnalysis1,
    ///         "SELECT  {[Measures].[Internet Sales Amount]} on COLUMNS, " +
    ///         "{[Date].[Fiscal].[Fiscal Year].MEMBERS} ON ROWS FROM [Adventure Works]");
    /// }
    /// </code>
    /// </example>
    [Description(
        "To be used along with ADOMD.NET 9. Serves as a data source for OlapGrid and OlapChart, should be connected either to MS Analysis OLAP server or local cube files.")]
    public class MOlapCube : RadarCube, IMOlapCube
    {
        /// <summary>
        ///     25000
        /// </summary>
        internal const int __MDXCELLSETTHRESHOLD = 25000;

        internal const int __MAX_ATTEMPT = 3;
        internal const int __MAX_ATTEMPT_LIMIT = 32;

        protected const string _DefaultConnectionString = "";

        private int _maxCountAttempt = __MAX_ATTEMPT;

        private readonly bool _needActive = false;


        private string _WorkingDirectory;

        protected internal XmlaConnection FMDConnection;


        private bool IsRestored;

        protected internal RadarCellset rcs;

        /// <summary>
        ///     Initializes a new instance of the MOlapCube class
        /// </summary>
        public MOlapCube(HttpContext context, IHostingEnvironment hosting, IMemoryCache cache) :
            base(context, hosting, cache)
        {
            DebugLogging.WriteLine("MOlapCube.ctor()");

            IgnoreDefaultMember = true;
            FMDConnection = new XmlaConnection();
            FMDConnection.ShowHiddenObjects = true;
        }

        internal override string PublicName => RadarUtils.GetMSASPublicName();

        /// <summary>
        ///     Defines the highest possible amount of rows to return by MDX-queries in order to prevent the web server from
        ///     overload
        /// </summary>
        [Category("Data")]
        [DefaultValue(__MDXCELLSETTHRESHOLD)]
        [Description(
            "Defines the highest possible amount of rows to return by MDX-queries in order to prevent the web server from overload.")]
        public int MDXCellsetThreshold
        {
            get
            {
                object o = Session.GetInt32("MDXCellsetThreshold");
                return o == null ? __MDXCELLSETTHRESHOLD : Convert.ToInt32(o);
            }
            set
            {
                if (value == __MDXCELLSETTHRESHOLD)
                    Session.Remove("MDXCellsetThreshold");
                else
                    Session.SetInt32("MDXCellsetThreshold", value);
            }
        }

        /// <summary>
        ///     True, if the limit of the number of cells returned by Cellset that is specified in the MDXCellsetThreshhold, is
        ///     exceeded.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MDXCellsetThresholdReached { get; internal set; } = false;

        /// <summary>
        ///     Maximum number of connection attempt
        /// </summary>
        [DefaultValue(__MAX_ATTEMPT)]
        public int MaxCountAttempt
        {
            get => _maxCountAttempt;
            set
            {
                if (value == _maxCountAttempt)
                    return;

                if (value > __MAX_ATTEMPT_LIMIT)
                    value = __MAX_ATTEMPT_LIMIT;
                if (value < 1)
                    value = 1;

                _maxCountAttempt = value;
            }
        }

        internal override string WorkingDirectory
        {
            get
            {
                if (_WorkingDirectory.IsFill())
                    return _WorkingDirectory;

                _WorkingDirectory = MapPath(Path.Combine(TempPath, "Temp", ID));
                return _WorkingDirectory;
            }
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DefaultValue(false)]
        public bool ForceDrillthroughActions { get; set; }

        /// <summary>
        ///     AS2000
        /// </summary>
        protected internal bool Is2000
        {
            get
            {
                EnsureConnected();
                return false;
            }
        }

        internal ServerVersion ServerVersion
        {
            get
            {
                EnsureConnected();

                // AS2005, file msolap80.dl
                if (Is2000)
                    return ServerVersion.AS2000;

                // AS2005, file msolap90.dl
                if (DisableSubcubeFeatures || FMDConnection.ServerVersion.StartsWith("9"))
                    return ServerVersion.AS2005;

                // AS2005, file msolap.dl
                if (DisableSubcubeFeatures || FMDConnection.ServerVersion.StartsWith("7"))
                    return ServerVersion.AS7;

                return ServerVersion.Unknown;
            }
        }

        /// <summary>
        ///     Gets or sets the connection string to open an OLAP server connection.
        /// </summary>
        [Category("Data")]
        [DefaultValue(_DefaultConnectionString)]
        [Description("Gets or sets the connection string to open an OLAP server connection.")]
        public override string ConnectionString
        {
            get => Session.GetString(GetPropertySessionName("ConnectionString")) == null
                ? _DefaultConnectionString
                : Session.GetString(GetPropertySessionName("ConnectionString"));
            set
            {
                if (value == ConnectionString) return;
                if (FMDConnection.State == ConnectionState.Open)
                    FMDConnection.Close(false);
                if (value == _DefaultConnectionString)
                    Session.Remove(GetPropertySessionName("ConnectionString"));
                else
                    Session.SetString(GetPropertySessionName("ConnectionString"), value);
                FMDConnection.ConnectionString = value;
            }
        }

        internal override TimeSpan CacheMinTimeout
        {
            get
            {
                if (_CacheMinTimeout.Equals(TimeSpan.Zero))
                {
                    var minTimeSpan = new TimeSpan(0, 0, 30, 0);

                    //if (Page != null)
                    //{
                    //    TimeSpan sessionTimeSpan = TimeSpan.FromMinutes(Session.Timeout);
                    //    _CacheMinTimeout = TimeSpan.Compare(minTimeSpan, sessionTimeSpan) >= 0 ? minTimeSpan : sessionTimeSpan;
                    //}
                    //else
                    _CacheMinTimeout = minTimeSpan;
                }

                return _CacheMinTimeout;
            }
        }

        /// <summary>
        ///     Reference to the AdomdConnection object which provides an access to the OLAP MS AS server
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Allows fulfilling various MDX queries to the OLAP server and
        ///         creating/deleting objects of the session level on the MS AS server for MOlapCube to
        ///         work with.
        ///     </para>
        ///     <para>
        ///         Before you use the property, call the EnsureConnected() method to make sure
        ///         that the connection to the server was not established in the current
        ///         session.
        ///     </para>
        /// </remarks>
        [Browsable(false)]
        internal XmlaConnection Connection => FMDConnection;

        /// <summary>Name of the current OLAP Cube.</summary>
        [Category("Data")]
        [DefaultValue("")]
        [Description("Name of the current OLAP Cube.")]
        public override string CubeName
        {
            get => Session.GetString("CubeName") == null ? "" : Session.GetString("CubeName");
            set
            {
                if (value == null)
                    Session.Remove("CubeName");
                else
                    Session.SetString("CubeName", value);
            }
        }

        /// <summary>
        ///     Allows the Grid to use the OLAP server cell color formatting option.
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Allows the Grid to use the OLAP server cell color formatting option.")]
        public bool UseOlapServerColorFormatting
        {
            get => Session.GetString("UseOlapServerColorFormatting") != null;
            set
            {
                if (value)
                    Session.SetString("UseOlapServerColorFormatting", "true");
                else
                    Session.Remove("UseOlapServerColorFormatting");
            }
        }

        /// <summary>
        ///     Allows the Grid to use the OLAP server cell font formatting option.
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Allows the Grid to use the OLAP server cell font formatting option.")]
        public bool UseOlapServerFontFormatting
        {
            get => Session.GetString("UseOlapServerFontFormatting") != null;
            set
            {
                if (value)
                    Session.SetString("UseOlapServerFontFormatting", "true");
                else
                    Session.Remove("UseOlapServerFontFormatting");
            }
        }

        /// <summary>Allows the use of the MDX subcube expressions.</summary>
        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Defines whether the MDX subcube expressions are allowed.")]
        public bool DisableSubcubeFeatures
        {
            get => Session.GetString("DisableSubcubeFeatures") != null;
            set
            {
                if (value)
                    Session.SetString("DisableSubcubeFeatures", "true");
                else
                    Session.Remove("DisableSubcubeFeatures");
            }
        }

        /// <summary>
        ///     The current OLAP Cube.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal CubeDef ActiveCube
        {
            get
            {
                EnsureConnected();
                return FMDConnection.Cubes.Find(CubeName);
            }
            set
            {
                if (value != null) CubeName = value.Caption;
                else CubeName = "";
                RetrieveMetadata();
            }
        }

        /// <summary>Indicates whether the Cube is active.</summary>
        [Category("Data")]
        [DefaultValue(false)]
        [Description("Indicates whether the cube is active.")]
        public override bool Active
        {
            get => base.Active;
            set
            {
                if (Active == value)
                    return;

                DebugLogging.WriteLine("MOlapCube.Active_SET(value={0})", value);
                base.Active = value;
            }
        }

        /// <summary>
        ///     The list of OLAP Cubes avaliable in the current session provided by the MS
        ///     Analysis server
        /// </summary>
        [Browsable(false)]
        internal CubeCollection Cubes
        {
            get
            {
                EnsureConnected();
                return FMDConnection.Cubes;
            }
        }

        /// <summary>
        ///     If true ignores the MSAS DefaultMember setting for the hierarchy without filtering it while pivoting.
        /// </summary>
        [Category("Behavior")]
        [Description(
            "If true ignores the MSAS DefaultMember setting for the hierarchy without filtering it while pivoting.")]
        [DefaultValue(true)]
        public bool IgnoreDefaultMember { get; set; }

        //protected override void RenderContents(HtmlTextWriter writer)
        //{
        //    base.RenderContents(writer);
        //    if (DesignMode)
        //        CheckForNewVersions();
        //}
        /// <summary>
        ///     Enables a server control to perform a final clean up before it is released from
        ///     memory.
        /// </summary>
        public override void Dispose()
        {
            if (FMDConnection.State == ConnectionState.Open) FMDConnection.Close(false);
            base.Dispose();
            FMDConnection = null;
        }

        internal T ExecuteMDX_Inner<T>(string query, Func<XmlaCommand, T> aGetResult)
            where T : class
        {
            return ExecuteMDX_Inner(query, aGetResult, false);
        }


        private string GetPropertySessionName(string propertyName)
        {
            return propertyName + "_" + ID;
        }

        internal T ExecuteMDX_Inner<T>(string query, Func<XmlaCommand, T> aGetResult, bool isDataOnly)
            where T : class
        {
            var res = default(T);
            var countattempt = 0;
            try
            {
                while (countattempt++ < MaxCountAttempt)
                {
                    EnsureConnected();
                    try
                    {
                        res = TryExecuteMDX_Inner(query, aGetResult, isDataOnly);
                    }
                    catch (FormatException e1)
                    {
                        if (countattempt > __MAX_ATTEMPT)
                            throw e1;
                        DebugLogging.WriteLine(
                            "MOlapCube.ExecuteMDXCellset() FormatException happen! countattempt={0}. throw rejected!",
                            countattempt.ToString());
                        continue;
                    }
                    if (res != null)
                        break;
                }
            }
            catch (FormatException fE)
            {
                throw new Exception(fE.Message + "\nQuery: " + query, fE);
            }
            catch (Exception e)
            {
                DebugLogging.WriteLine("MOlapCube.ExecuteMDXCellset(throw new Exception)");
                throw new Exception(e.Message + "\nQuery: " + query, e);
            }

            return res;
        }

        private T TryExecuteMDX_Inner<T>(string query, Func<XmlaCommand, T> aGetResult, bool isDataOnly)
        {
            var d1 = DateTime.Now;
            var cmd = new XmlaCommand(query, FMDConnection);
            DebugLogging.WriteLine("MOlapCube.ExecuteMDXCellset:query={0}", query);

            //if (isDataOnly)
            //    cmd.Properties.Add("Content", "Data");

            var res = aGetResult(cmd);

            var execTime = DateTime.Now - d1;
            if (OnQuery != null)
                OnQuery(this, new QueryArgs(query, execTime));

            return res;
        }

        internal XmlaClient.Metadata.CellSet ExecuteMDXCellset(string query, bool isDataOnly)
        {
            return ExecuteMDX_Inner(query, x => x.ExecuteCellSet(), isDataOnly);
        }

        internal XmlReader ExecuteXMLACellset(string query, bool isDataOnly)
        {
            return ExecuteMDX_Inner(query, x => x.ExecuteXmlReader(), isDataOnly);
        }

        internal void ExecuteMDXCommand(string query)
        {
            ExecuteMDX_Inner<object>(query, x =>
                                            {
                                                x.ExecuteNonQuery();
                                                return null;
                                            });
        }

        internal DbDataReader ExecuteMDXReader(string query)
        {
            return ExecuteMDX_Inner(query, x => x.ExecuteReader());
        }

        protected void OnUnload()
        {
            //if (FMDConnection.State == ConnectionState.Open) FMDConnection.Close(true);
            //if ((!DesignMode) && (Page != null) && _initCompleted)
            //{
            //    ImmanentCubeMSAS ic = new ImmanentCubeMSAS();
            //    ic.FromCube(this);

            //    string pathToCachDependencyFile = SessionState.WorkingDirectoryName + TempDirectory.CacheDependencyFile;
            //    if (File.Exists(pathToCachDependencyFile))
            //        Page.Cache.Insert(SessionState.WorkingDirectoryName + UniqueID, ic,
            //            new System.Web.Caching.CacheDependency(pathToCachDependencyFile));
            //    else
            //    {
            //        Page.Cache.Remove(SessionState.WorkingDirectoryName + UniqueID);
            //        Page.Cache.Add(SessionState.WorkingDirectoryName + UniqueID, ic, null, System.Web.Caching.Cache.NoAbsoluteExpiration,
            //           CacheMinTimeout, System.Web.Caching.CacheItemPriority.AboveNormal, null);
            //    }
            //}
            //base.OnUnload(e);
        }


        internal override string GetProductID()
        {
            return RadarUtils.rsAspNetCoreXmla;
        }

        internal List<CubeAction> RetrieveActions(ICubeAddress Address, OlapControl FGrid)
        {
            if (Address == null || Address.Measure == null)
                return new List<CubeAction>();

            StringBuilder SUBCUBE = null;
            var subcubeCounter = 0;

            var memlist = new List<string>();
            memlist.Add(Address.Measure.UniqueName);

            for (var i = 0; i < Address.LevelsCount; i++)
                if (memlist.Contains(Address.Members(i).UniqueName) == false)
                    memlist.Add(Address.Members(i).UniqueName);

            for (var i = 0; i < FGrid.FFilteredHierarchies.Count; i++)
            {
                var h = FGrid.FFilteredHierarchies[i];
                var m = Address.GetMemberByHierarchy(h) ?? h.RetrieveFilteredMember();
                if (m != null && m.Filtered) continue;
                var single = m != null;
                var set = "";
                if (m != null)
                {
                    var mm = new HashSet<Member>();
                    mm.Add(m);
                    CreateVisibleSet(FGrid.FFilteredHierarchies[i],
                        out single, out set, mm, false);
                }
                if (!single)
                    if (Is2000)
                    {
                        return new List<CubeAction>();
                    }
                    else
                    {
                        if (SUBCUBE == null)
                            SUBCUBE = new StringBuilder("SELECT ");
                        if (subcubeCounter > 0)
                        {
                            SUBCUBE.Append(",");
                            SUBCUBE.AppendLine();
                        }
                        SUBCUBE.Append("{");
                        SUBCUBE.Append(DoSubcubeFilter(h, null));
                        SUBCUBE.Append("} ON ");
                        SUBCUBE.Append(subcubeCounter++);
                        continue;
                    }
                if (!string.IsNullOrEmpty(set))
                    if (memlist.Any(mem => set.Contains(mem)) == false)
                        memlist.Add(set);
            }

            var sb = new StringBuilder();
            sb.AppendFormat("({0})", string.Join(",", memlist.ToArray()));

            string subcubeExpression = null;
            if (!Is2000)
                if (SUBCUBE != null)
                {
                    SUBCUBE.AppendLine();
                    SUBCUBE.Append("FROM ");
                    SUBCUBE.Append(GetContextFilterSubcube(FGrid));
                    subcubeExpression = "(" + SUBCUBE + ")";
                }
                else
                {
                    subcubeExpression = GetContextFilterSubcube(FGrid);
                }

            try
            {
                EnsureConnected();
                return ParseActions(
                    Address,
                    FGrid,
                    Connection.GetActions(CubeName, sb.ToString(), CoordinateType.Cell),
                    subcubeExpression);
            }
            catch
            {
                return new List<CubeAction>();
            }
        }

        internal override List<CubeAction> RetrieveActions(IDataCell cell)
        {
            return RetrieveActions(cell.Address, cell.CellSet.Grid);
        }

        internal override List<CubeAction> RetrieveActions(IMemberCell cell)
        {
            try
            {
                EnsureConnected();

                return ParseActions(
                    cell.Address,
                    cell.CellSet.Grid,
                    Connection.GetActions(CubeName, cell.Member.UniqueName, CoordinateType.Member),
                    null);
            }
            catch
            {
                return new List<CubeAction>();
            }
        }

        internal string GetContextFilterSubcube(OlapControl FGrid)
        {
            var SUBCUBE2 = new StringBuilder();
            foreach (var l in FGrid.FFilteredLevels)
            {
                if (l.Filter.FilterType == OlapFilterType.ftOnDate && !l.Hierarchy.IsDate)
                    throw new InvalidEnumArgumentException(
                        "Cannot apply the Date context filter to a non-Date hierarchy.");
                if (l.Filter.FilterType == OlapFilterType.ftOnCaption)
                    switch (l.Filter.FilterCondition)
                    {
                        case OlapFilterCondition.fcEqual:
                            SUBCUBE2.Append("(SELECT FILTER(" + l.Filter.MDXLevelName + ".MEMBERS, (" +
                                            l.Hierarchy.UniqueName +
                                            ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\")=\"" +
                                            l.Filter.FirstValue + "\")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotEqual:
                            SUBCUBE2.Append("(SELECT FILTER(" + l.Filter.MDXLevelName + ".MEMBERS, (" +
                                            l.Hierarchy.UniqueName +
                                            ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\")<>\"" +
                                            l.Filter.FirstValue + "\")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcStartsWith:
                            SUBCUBE2.Append("(SELECT FILTER(" + l.Filter.MDXLevelName + ".MEMBERS, (LEFT(" +
                                            l.Hierarchy.UniqueName + ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\"), " +
                                            l.Filter.FirstValue.Length + ")=\"" +
                                            l.Filter.FirstValue + "\")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotStartsWith:
                            SUBCUBE2.Append("(SELECT FILTER(" + l.Filter.MDXLevelName + ".MEMBERS, (LEFT(" +
                                            l.Hierarchy.UniqueName + ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\"), " +
                                            l.Filter.FirstValue.Length + ")<>\"" +
                                            l.Filter.FirstValue + "\")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcEndsWith:
                            SUBCUBE2.Append("(SELECT FILTER(" + l.Filter.MDXLevelName + ".MEMBERS, (RIGHT(" +
                                            l.Hierarchy.UniqueName + ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\"), " +
                                            l.Filter.FirstValue.Length + ")=\"" +
                                            l.Filter.FirstValue + "\")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotEndsWith:
                            SUBCUBE2.Append("(SELECT FILTER(" + l.Filter.MDXLevelName + ".MEMBERS, (RIGHT(" +
                                            l.Hierarchy.UniqueName + ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\"), " +
                                            l.Filter.FirstValue.Length + ")<>\"" +
                                            l.Filter.FirstValue + "\")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcContains:
                            SUBCUBE2.Append("(SELECT FILTER(" + l.Filter.MDXLevelName + ".MEMBERS, (INSTR(1," +
                                            l.Hierarchy.UniqueName +
                                            ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\"), \"" + l.Filter.FirstValue +
                                            "\")>0)) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotContains:
                            SUBCUBE2.Append("(SELECT FILTER(" + l.Filter.MDXLevelName + ".MEMBERS, (INSTR(1," +
                                            l.Hierarchy.UniqueName +
                                            ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\"), \"" + l.Filter.FirstValue +
                                            "\")=0)) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcLess:
                            SUBCUBE2.Append("(SELECT FILTER(" + l.Filter.MDXLevelName + ".MEMBERS, (" +
                                            l.Hierarchy.UniqueName +
                                            ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\")<\"" +
                                            l.Filter.FirstValue + "\")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotLess:
                            SUBCUBE2.Append("(SELECT FILTER(" + l.Filter.MDXLevelName + ".MEMBERS, (" +
                                            l.Hierarchy.UniqueName +
                                            ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\")>=\"" +
                                            l.Filter.FirstValue + "\")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcGreater:
                            SUBCUBE2.Append("(SELECT FILTER(" + l.Filter.MDXLevelName + ".MEMBERS, (" +
                                            l.Hierarchy.UniqueName +
                                            ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\")>\"" +
                                            l.Filter.FirstValue + "\")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotGreater:
                            SUBCUBE2.Append("(SELECT FILTER(" + l.Filter.MDXLevelName + ".MEMBERS, (" +
                                            l.Hierarchy.UniqueName +
                                            ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\")<=\"" +
                                            l.Filter.FirstValue + "\")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcBetween:
                            SUBCUBE2.Append("(SELECT FILTER(" + l.Filter.MDXLevelName + ".MEMBERS, (" +
                                            l.Hierarchy.UniqueName +
                                            ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\")>=\"" +
                                            l.Filter.FirstValue + "\" AND " + l.Hierarchy.UniqueName +
                                            ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\")<=\"" +
                                            l.Filter.SecondValue + "\")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotBetween:
                            SUBCUBE2.Append("(SELECT FILTER(" + l.Filter.MDXLevelName + ".MEMBERS, (" +
                                            l.Hierarchy.UniqueName +
                                            ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\")<\"" +
                                            l.Filter.FirstValue + "\" OR " + l.Hierarchy.UniqueName +
                                            ".CURRENTMEMBER.PROPERTIES(\"MEMBER_CAPTION\")>\"" +
                                            l.Filter.SecondValue + "\")) ON COLUMNS FROM ");
                            break;
                    }
                if (l.Filter.FilterType == OlapFilterType.ftOnDate)
                {
                    var firstdate = "";
                    try
                    {
                        var dt = DateTime.Parse(l.Filter.FirstValue, CultureInfo.InvariantCulture.DateTimeFormat);
                        firstdate = "CDATE(\"" + dt.ToString("yyyy-MM-dd") + "\")";
                    }
                    catch
                    {
                        ;
                    }
                    var seconddate = "";
                    try
                    {
                        var dt = DateTime.Parse(l.Filter.SecondValue, CultureInfo.InvariantCulture.DateTimeFormat);
                        seconddate = "CDATE(\"" + dt.ToString("yyyy-MM-dd") + "\")";
                    }
                    catch
                    {
                        ;
                    }
                    var filteredLevel = l.Filter.MDXLevelName;
                    var currentdate = "CDATE(" + l.Hierarchy.UniqueName + ".CURRENTMEMBER.MEMBERVALUE)";
                    switch (l.Filter.FilterCondition)
                    {
                        case OlapFilterCondition.fcEqual:
                            SUBCUBE2.Append("(SELECT FILTER(" + filteredLevel + ".MEMBERS, (" +
                                            currentdate + "=" +
                                            firstdate + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotEqual:
                            SUBCUBE2.Append("(SELECT FILTER(" + filteredLevel + ".MEMBERS, (" +
                                            currentdate + "<>" +
                                            firstdate + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcLess:
                            SUBCUBE2.Append("(SELECT FILTER(" + filteredLevel + ".MEMBERS, (" +
                                            currentdate + "<" +
                                            firstdate + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotLess:
                            SUBCUBE2.Append("(SELECT FILTER(" + filteredLevel + ".MEMBERS, (" +
                                            currentdate + ">=" +
                                            firstdate + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcGreater:
                            SUBCUBE2.Append("(SELECT FILTER(" + filteredLevel + ".MEMBERS, (" +
                                            currentdate + ">" +
                                            firstdate + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotGreater:
                            SUBCUBE2.Append("(SELECT FILTER(" + filteredLevel + ".MEMBERS, (" +
                                            currentdate + "<=" +
                                            firstdate + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcBetween:
                            SUBCUBE2.Append("(SELECT FILTER(" + filteredLevel + ".MEMBERS, (" +
                                            currentdate + ">=" +
                                            firstdate + " AND " + currentdate + "<=" +
                                            seconddate + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotBetween:
                            SUBCUBE2.Append("(SELECT FILTER(" + filteredLevel + ".MEMBERS, (" +
                                            currentdate + "<" +
                                            firstdate + " OR " + currentdate + ">" +
                                            seconddate + ")) ON COLUMNS FROM ");
                            break;
                    }
                }
                if (l.Filter.FilterType == OlapFilterType.ftOnValue)
                {
                    if (l.Filter.AppliesTo == null)
                        throw new Exception("The value filter applied to the '" + l.DisplayName +
                                            "' level has no defined measure.");
                    switch (l.Filter.FilterCondition)
                    {
                        case OlapFilterCondition.fcEqual:
                            SUBCUBE2.Append("(SELECT FILTER(HIERARCHIZE(" + l.Filter.MDXLevelName + ".MEMBERS), (" +
                                            l.Filter.AppliesTo.UniqueName + "=" +
                                            CorrectNumberString(l.Filter.FirstValue, FGrid) + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotEqual:
                            SUBCUBE2.Append("(SELECT FILTER(HIERARCHIZE(" + l.Filter.MDXLevelName + ".MEMBERS), (" +
                                            l.Filter.AppliesTo.UniqueName + "<>" +
                                            CorrectNumberString(l.Filter.FirstValue, FGrid) + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcLess:
                            SUBCUBE2.Append("(SELECT FILTER(HIERARCHIZE(" + l.Filter.MDXLevelName + ".MEMBERS), (" +
                                            l.Filter.AppliesTo.UniqueName + "<" +
                                            CorrectNumberString(l.Filter.FirstValue, FGrid) + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotLess:
                            SUBCUBE2.Append("(SELECT FILTER(HIERARCHIZE(" + l.Filter.MDXLevelName + ".MEMBERS), (" +
                                            l.Filter.AppliesTo.UniqueName + ">=" +
                                            CorrectNumberString(l.Filter.FirstValue, FGrid) + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcGreater:
                            SUBCUBE2.Append("(SELECT FILTER(HIERARCHIZE(" + l.Filter.MDXLevelName + ".MEMBERS), (" +
                                            l.Filter.AppliesTo.UniqueName + ">" +
                                            CorrectNumberString(l.Filter.FirstValue, FGrid) + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotGreater:
                            SUBCUBE2.Append("(SELECT FILTER(HIERARCHIZE(" + l.Filter.MDXLevelName + ".MEMBERS), (" +
                                            l.Filter.AppliesTo.UniqueName + "<=" +
                                            CorrectNumberString(l.Filter.FirstValue, FGrid) + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcBetween:
                            SUBCUBE2.Append("(SELECT FILTER(HIERARCHIZE(" + l.Filter.MDXLevelName + ".MEMBERS), (" +
                                            l.Filter.AppliesTo.UniqueName + ">=" +
                                            CorrectNumberString(l.Filter.FirstValue, FGrid) + " AND " +
                                            l.Filter.AppliesTo.UniqueName + "<=" +
                                            CorrectNumberString(l.Filter.SecondValue, FGrid) + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcNotBetween:
                            SUBCUBE2.Append("(SELECT FILTER(HIERARCHIZE(" + l.Filter.MDXLevelName + ".MEMBERS), (" +
                                            l.Filter.AppliesTo.UniqueName + "<" +
                                            CorrectNumberString(l.Filter.FirstValue, FGrid) + " OR " +
                                            l.Filter.AppliesTo.UniqueName + ">" +
                                            CorrectNumberString(l.Filter.SecondValue, FGrid) + ")) ON COLUMNS FROM ");
                            break;
                        case OlapFilterCondition.fcFirstTen:
                            var isFirstLevel = l.Index == 0;
                            if (l.Hierarchy.Origin == HierarchyOrigin.hoParentChild &&
                                l.Hierarchy.CubeHierarchy.FMDXLevelNames[0] != l.Filter.MDXLevelName)
                                isFirstLevel = false;
                            if (isFirstLevel)
                            {
                                SUBCUBE2.Append("(SELECT ");
                                switch (l.Filter.SecondValue)
                                {
                                    case "[0].[0]":
                                        SUBCUBE2.Append("TOPCOUNT"); // max items
                                        break;
                                    case "[1].[0]":
                                        SUBCUBE2.Append("BOTTOMCOUNT"); // min items
                                        break;
                                    case "[0].[1]":
                                        SUBCUBE2.Append("TOPPERCENT"); // max %
                                        break;
                                    case "[1].[1]":
                                        SUBCUBE2.Append("BOTTOMPERCENT"); // min %
                                        break;
                                    case "[0].[2]":
                                        SUBCUBE2.Append("TOPSUM"); // max sum
                                        break;
                                    case "[1].[2]":
                                        SUBCUBE2.Append("BOTTOMSUM"); // min sum
                                        break;
                                }
                                SUBCUBE2.Append("(");
                                SUBCUBE2.Append(l.Filter.MDXLevelName);
                                SUBCUBE2.Append(".ALLMEMBERS, ");
                                SUBCUBE2.Append(l.Filter.FirstValue);
                                SUBCUBE2.Append(", ");
                                SUBCUBE2.Append(l.Filter.AppliesTo.UniqueName);
                                SUBCUBE2.Append(") ON COLUMNS FROM ");
                            }
                            else
                            {
                                string prevname;
                                if (l.Index > 0)
                                {
                                    prevname = l.Hierarchy.Levels[l.Index - 1].UniqueName;
                                }
                                else
                                {
                                    var ls = l.Hierarchy.CubeHierarchy.FMDXLevelNames;
                                    prevname = ls[ls.IndexOf(l.Filter.MDXLevelName) - 1];
                                }
                                SUBCUBE2.Append("(SELECT GENERATE(HIERARCHIZE(");
                                SUBCUBE2.Append(prevname);
                                SUBCUBE2.Append(".MEMBERS) AS [RS_FILTERSET_X],");
                                switch (l.Filter.SecondValue)
                                {
                                    case "[0].[0]":
                                        SUBCUBE2.Append("TOPCOUNT(FILTER"); // max items
                                        break;
                                    case "[1].[0]":
                                        SUBCUBE2.Append("BOTTOMCOUNT(FILTER"); // min items
                                        break;
                                    case "[0].[1]":
                                        SUBCUBE2.Append("TOPPERCENT"); // max %
                                        break;
                                    case "[1].[1]":
                                        SUBCUBE2.Append("BOTTOMPERCENT"); // min %
                                        break;
                                    case "[0].[2]":
                                        SUBCUBE2.Append("TOPSUM"); // max sum
                                        break;
                                    case "[1].[2]":
                                        SUBCUBE2.Append("BOTTOMSUM"); // min sum
                                        break;
                                }
                                SUBCUBE2.Append(
                                    "(EXCEPT(DRILLDOWNLEVEL([RS_FILTERSET_X].CURRENT AS [RS_FILTERSET_H], , 0), [RS_FILTERSET_H]), ");
                                if (l.Filter.SecondValue.EndsWith(".[0]"))
                                {
                                    SUBCUBE2.Append("NOT ISEMPTY(");
                                    SUBCUBE2.Append(l.Filter.AppliesTo.UniqueName);
                                    SUBCUBE2.Append(")), ");
                                }
                                SUBCUBE2.Append(l.Filter.FirstValue);
                                SUBCUBE2.Append(", ");
                                SUBCUBE2.Append(l.Filter.AppliesTo.UniqueName);
                                SUBCUBE2.Append(")) ON COLUMNS FROM ");
                            }
                            break;
                    }
                }
            }
            SUBCUBE2.Append(ApplySubcubeFilter());
            for (var i = 0; i < FGrid.FFilteredLevels.Count; i++)
                SUBCUBE2.Append(")");
            return SUBCUBE2.ToString();
        }

        private string CorrectNumberString(string probe, OlapControl FGrid)
        {
            var s = probe.Replace(" ", "");
            double d;
            var b = double.TryParse(s, out d);
            if (!b)
                try
                {
                    b = double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out d);
                }
                catch
                {
                    ;
                }
            if (!b)
                try
                {
                    b = double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out d);
                }
                catch
                {
                    ;
                }
            if (!b)
                try
                {
                    b = double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentUICulture, out d);
                }
                catch
                {
                    ;
                }
            if (!b)
                try
                {
                    b = double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d);
                }
                catch
                {
                    ;
                }
            if (b)
                return d.ToString(CultureInfo.InvariantCulture.NumberFormat);
            return probe;
        }

        private string ApplySubcubeExpression(string mdx, string subcubeExpression)
        {
            if (string.IsNullOrEmpty(subcubeExpression)) return mdx;
            return mdx.Replace("[" + CubeName + "]", subcubeExpression);
        }

        private List<CubeAction> ParseActions(ICubeAddress Address, OlapControl FGrid, ActionCollection actions,
            string subcubeExpression)
        {
            var useDrillthrough = true;
            var ll = new List<Layout.Level>(Address.LevelsCount);
            var lh = new List<Layout.Hierarchy>(Address.LevelsCount);
            for (var i = 0; i < Address.LevelsCount; i++)
            {
                ll.Add(Address.Levels(i));
                lh.Add(Address.Levels(i).Hierarchy);
                if (!ForceDrillthroughActions && Address.Members(i).Filtered)
                {
                    useDrillthrough = false;
                    break;
                }
            }

            if (useDrillthrough && !ForceDrillthroughActions)
                foreach (var h in FGrid.FFilteredHierarchies)
                    if (!lh.Contains(h) && h.RetrieveFilteredMember() == null)
                    {
                        useDrillthrough = false;
                        break;
                    }

            if (useDrillthrough && !ForceDrillthroughActions)
                foreach (var l in FGrid.FFilteredLevels)
                    if (!lh.Contains(l.Hierarchy))
                    {
                        useDrillthrough = false;
                        break;
                    }
                    else
                    {
                        var m = Address.GetMemberByHierarchy(l.Hierarchy);
                        if (l.Index > m.Level.Index)
                        {
                            useDrillthrough = false;
                            break;
                        }
                    }

            var result = new List<CubeAction>();
            try
            {
                foreach (var action in actions)
                {
                    var type = CubeActionType.caStatement;
                    if (action.ActionType.HasFlag(ActionType.DataSet))
                        type = CubeActionType.caDataSet;

                    if (action.ActionType.HasFlag(ActionType.Proprietary))
                        type = CubeActionType.caProprietary;

                    if (action.ActionType.HasFlag(ActionType.Rowset))
                        type = CubeActionType.caRowset;

                    if (action.ActionType.HasFlag(ActionType.Statement))
                        type = CubeActionType.caStatement;

                    if (action.ActionType.HasFlag(ActionType.Url))
                        type = CubeActionType.caURL;

                    if (action.ActionType.HasFlag(ActionType.Drillthrough))
                        type = CubeActionType.csDrillthrough;

                    var a = new CubeAction(type, action.Name,
                        action.Caption, action.Description,
                        ApplySubcubeExpression(action.Content, subcubeExpression),
                        action.Application);

                    if (!useDrillthrough && (a.ActionType == CubeActionType.csDrillthrough ||
                                             a.ActionType == CubeActionType.caRowset)) continue;
                    result.Add(a);
                }
            }
            catch
            {
                ;
            }
            return result;
        }

        /// <summary>
        ///     This method supports the RadarCube infrastructure and is not intended to be used directly from your code
        /// </summary>
        /// <param name="Grid"></param>
        /// <returns></returns>
        protected override Engine.Engine CreateEngine(OlapControl Grid)
        {
            return new MdEngine(this, Grid);
        }

        private Dimension FindDimension(CubeDef c, CubeDimension d)
        {
            return c.Dimensions.FirstOrDefault(D => D.UniqueName == d.UniqueName);
        }

        protected Measure FindMeasure(CubeDef c, string name)
        {
            foreach (var M in c.Measures)
                if (M.UniqueName == name) return M;
            return null;
        }

        //internal void DoOnQuery(string query)
        //{
        //    if (OnQuery == null) return;
        //    QueryArgs E = new QueryArgs(query);
        //    OnQuery(this, E);
        //}

        /// <summary>
        ///     Called after accomplishing an MDX query to the server. It allows getting the text
        ///     of the query.
        /// </summary>
        [Category("Data")]
        [Description(
            "Called before accomplishing of any MDX query to the server. It allows getting the text of the requested query")]
        public event EventHandler<QueryArgs> OnQuery;

        /// <summary>
        ///     Allows defining an MDX subcube expression to filter the Cube data (MSAS 2005
        ///     only).
        /// </summary>
        /// <remarks>
        ///     <para>Allows preliminary filtering of the Cube data.</para>
        ///     <para>
        ///         In the MDX clause the Cube name is replaced with Select expression assigned
        ///         to the e.SubcubeExpression.
        ///     </para>
        ///     <para>
        ///         For example, let's assign the following expression to the e.SubcubeExpression
        ///         property to restrict the cube data to the Year 2003:
        ///     </para>
        ///     <para>
        ///         e.SubcubeExpression = "select {[Date].[Calendar].[Calendar Year].&amp;[2003]}
        ///         on 0 from [Adventure Works]";
        ///     </para>
        ///     <para>As a result, all the MDX-queries to be sent will look like this:</para>
        ///     <para>SELECT ...</para>
        ///     <para>
        ///         FROM (select {[Date].[Calendar].[Calendar Year].&amp;[2003]} on 0 from
        ///         [Adventure Works])
        ///     </para>
        ///     <para>WHERE ...</para>
        ///     <para>
        ///         It's recommended to avoid changing the subcube expression after opening the Cube
        ///         (no matter in this session or earlier) to prevent the incorrect Grid behavior.
        ///     </para>
        /// </remarks>
        [Category("Data")]
        [Description("Allows defining a subcube expression to filter the cube data (MSAS 2005 only).")]
        public event EventHandler<SubcubeFilterArgs> SubcubeFilter;

        internal string ApplySubcubeFilter()
        {
            if (Is2000) return "[" + CubeName + "]";
            var subcube = "";
            if (SubcubeFilter != null)
            {
                var E = new SubcubeFilterArgs();
                SubcubeFilter(this, E);
                subcube = E.SubcubeExpression;
            }
            return string.IsNullOrEmpty(subcube) ? "[" + CubeName + "]" : "(" + subcube + ")";
        }

        private Hierarchy FindHierarchy(Dimension d, CubeHierarchy h)
        {
            return FindHierarchy(d, h.UniqueName);
        }

        private Hierarchy FindHierarchy(Dimension d, string uniqueName)
        {
            return d.Hierarchies.FirstOrDefault(H => H.UniqueName == uniqueName);
        }

        private Level FindLevel(Hierarchy h, string uniqueName)
        {
            return h.Levels.FirstOrDefault(l => l.UniqueName == uniqueName);
        }

        internal override void ShowCurrentHint(out string CurrentHint, out string CurrentHintLink,
            out string CurrentTip, out string CurrentTipLink)
        {
            CurrentTip = "";
            CurrentTipLink = "";
            CurrentHint = "";
            CurrentHintLink = "";
            try
            {
                if (Connection.State != ConnectionState.Open)
                    Connection.Open();
            }
            catch
            {
                CurrentHint =
                    "The connection parameters to the MS Analysis server or local file are not defined properly. Make use of a MOlapCube.ConnectionString property editor.";
                return;
            }
            if (string.IsNullOrEmpty(CubeName))
            {
                CurrentHint =
                    "A cube name is not determined to fetch the data from it. Choose a cube name by assigning the MOlapCube.CubeName property.";
                return;
            }
            if (Active == false)
                CurrentHint = "To open a cube, set its Active property to True.";
        }

        /// <summary>Displays the results of an MDX-query to the MS Analysis server in the Grid</summary>
        /// <remarks>
        ///     <para>The OLAP Grid can work in two different modes:</para>
        ///     <list type="bullet">
        ///         <item></item>
        ///         <item>
        ///             Standard (supporting cube navigation such as pivoting, drilling,
        ///             filtration, etc.)
        ///         </item>
        ///         <item>Display mode of the MDX-queries results.</item>
        ///     </list>
        ///     <para>
        ///         By default, the Grid is launched in a standard mode and is automatically
        ///         switched to the display mode of the MDX-query results if the ShowMDXQuery method is
        ///         called. You can switch the Grid back to the standard mode only by opening and
        ///         closing the cube.
        ///     </para>
        ///     <para>The Grid operation mode is defined by the Mode property.</para>
        ///     <para>
        ///         Operating the display mode of the MDX-queries results implies some
        ///         restrictions:
        ///     </para>
        ///     <list type="bullet">
        ///         <item></item>
        ///         <item>
        ///             Standard Cube navigation features (drilling, pivoting, filtration,
        ///             sorting and grouping) are not available. Calling the corresponding RadarCube
        ///             API methods leads to unpredictable results.
        ///         </item>
        ///         <item>Measure display modes are not available.</item>
        ///         <item>
        ///             Only the dimensions, hierarchies and measures mentioned in an MDX-query
        ///             are listed in Dimensions and Measures.
        ///         </item>
        ///         <item>Cube Structure Tree and Pivot Ares are never displayed.</item>
        ///     </list>
        ///     <para>
        ///         Note that the MOlapCube.ShowMDXQuery method is designed for fulfilling the MDX
        ///         queries and returns a Cellset as a result. To execute the MDX-scripts in the
        ///         current session, use the MOlapCube.Connection property.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <code lang="CS" title="[New Example]">
        /// protected void Page_Load(object sender, EventArgs e)
        /// {
        ///     OlapCube1.Active = true;
        ///     if (!IsPostBack)
        ///         OlapCube1.ShowMDXQuery(OlapAnalysis1,
        ///         "SELECT  {[Measures].[Internet Sales Amount]} on COLUMNS, " +
        ///         "{[Date].[Fiscal].[Fiscal Year].MEMBERS} ON ROWS FROM [Adventure Works]");
        /// }
        /// </code>
        /// </example>
        /// <param name="grid">The Grid where an MDX-query result will be displayed</param>
        /// <param name="MDXQuery">Text of an MDX-query</param>
        public void ShowMDXQuery(OlapControl grid, string MDXQuery)
        {
            EnsureConnected();
            var cs = ExecuteMDXCellset(MDXQuery, true);
            if (cs.Axes.Count > 2)
                throw new Exception("Results cannot be displayed for cellsets with more than two axes.");
            grid.ModeUpdater.BeginUpdate();

            //grid.OnApplyTemplate();
            //grid.OLAPDockPanel.SuspendLayout();
            grid.SetActive(false);

            grid.BeginUpdate();

            ClearMembers();

            grid.Cube = this;
            grid.Engine.Clear();
            grid.Engine.FLevelsList.Clear();

            var ld = new Dictionary<string, Dimension>();
            var lh = new Dictionary<string, Hierarchy>();
            foreach (var a in cs.Axes)
            {
                foreach (Hierarchy h in a.Set.Hierarchies)
                {
                    if (Is2000)
                    {
                        if (h.UniqueName == "Measures") continue;
                        var s = h.UniqueName;
                        if (!lh.ContainsKey(s))
                            lh.Add(s, h);
                    }
                    else
                    {
                        var d = h.ParentDimension;
                        if (d.DimensionType == DimensionTypeEnum.Measure) continue;
                        var s = d.UniqueName;
                        if (!ld.ContainsKey(s))
                            ld.Add(s, d);
                        s = h.UniqueName;
                        if (!lh.ContainsKey(s))
                            lh.Add(s, h);
                    }
                }
            }
            var LH = new Dictionary<string, CubeHierarchy>();
            var LM = new Dictionary<string, CubeMeasure>();

            rcs = new RadarCellset();
            var members = new Dictionary<string, CubeMember>();
            var islevelmeasure = new Dictionary<string, bool>();

            for (var i = 0; i < cs.Axes.Count; i++)
            {
                string[,] curr = null;
                if (i == 0)
                {
                    rcs.columnHierarchies = new string[cs.Axes[0].Set.Hierarchies.Count];
                    for (var i1 = 0; i1 < cs.Axes[0].Set.Hierarchies.Count; i1++)
                        rcs.columnHierarchies[i1] = cs.Axes[0].Set.Hierarchies[i1].UniqueName;
                    rcs.column = new string[cs.Axes[0].Set.Hierarchies.Count, cs.Axes[0].Set.Tuples.Count];
                    curr = rcs.column;
                }
                if (i == 1)
                {
                    rcs.rowHierarchies = new string[cs.Axes[1].Set.Hierarchies.Count];
                    for (var i1 = 0; i1 < cs.Axes[1].Set.Hierarchies.Count; i1++)
                        rcs.rowHierarchies[i1] = cs.Axes[1].Set.Hierarchies[i1].UniqueName;
                    rcs.row = new string[cs.Axes[1].Set.Hierarchies.Count, cs.Axes[1].Set.Tuples.Count];
                    curr = rcs.row;
                }
                for (var j = 0; j < cs.Axes[i].Set.Tuples.Count; j++)
                {
                    var t = cs.Axes[i].Set.Tuples[j];
                    for (var k = 0; k < t.Members.Count; k++)
                    {
                        var m = t.Members[k];
                        curr[k, j] = m.UniqueName;
                        var s = m.UniqueName;
                        bool b;
                        if (!islevelmeasure.TryGetValue(m.LevelName, out b))
                        {
                            if (Is2000)
                                b = m.UniqueName.StartsWith("[Measures]");
                            else
                                b = m.ParentLevel.ParentHierarchy.ParentDimension.DimensionType ==
                                    DimensionTypeEnum.Measure;
                            islevelmeasure.Add(m.LevelName, b);
                        }
                        if (b)
                        {
                            if (!LM.ContainsKey(s))
                            {
                                var M = new CubeMeasure();
                                M.Init(this);
                                M.Description = m.Description;
                                M.DisplayName = m.Caption;
                                M.UniqueName = m.UniqueName;
                                Measures.Add(M);
                                LM.Add(s, M);
                            }
                        }
                        else
                        {
                            if (!members.ContainsKey(s))
                            {
                                CubeHierarchy H = null;

                                H = Dimensions.FindHierarchy(m.ParentLevel.ParentHierarchy.UniqueName);
                                if (H.Levels.Count == 0)
                                    RetrieveLevels(H, grid);

                                var M = new CubeMember(
                                    H, H.Levels[0], m.Caption, m.Description,
                                    m.UniqueName, m.Name, false, "");
                                H.Levels[0].Members.Add(M);
                                members.Add(m.UniqueName, M);
                            }
                        }
                    }
                }
            }
            if (Measures.Count == 0)
                foreach (var m in cs.FilterAxis.Positions[0].Members)
                {
                    bool b;
                    var s = m.UniqueName;
                    if (Is2000)
                        b = m.UniqueName.StartsWith("[Measures]");
                    else
                        b = m.ParentLevel.ParentHierarchy.ParentDimension.DimensionType == DimensionTypeEnum.Measure;
                    if (b)
                        if (!LM.ContainsKey(s))
                        {
                            var M = new CubeMeasure();
                            M.Init(this);
                            M.Description = m.Description;
                            M.DisplayName = m.Caption;
                            M.UniqueName = m.UniqueName;
                            Measures.Add(M);
                            LM.Add(s, M);
                            break;
                        }
                }
            MapChanged();
            foreach (var d in grid.Dimensions)
            foreach (var h in d.Hierarchies)
            {
                h.AllowFilter = false;
                h.AllowHierarchyEditor = false;
                h.AllowRegroup = false;
                h.AllowResort = false;
                h.AllowSwapMembers = false;

                if (h.CubeHierarchy.Levels.Count == 0)
                    RetrieveLevels(h.CubeHierarchy, grid);

                h.CubeHierarchy.Levels[0].FMembersCount = h.CubeHierarchy.Levels[0].FUniqueNamesArray.Count;
                h.InitHierarchy(0);
                h.Levels[0].CreateNewMembers();
            }
            grid.Measures.FLevel = null;
            grid.Measures.InitMeasures();
            rcs.data = new object[cs.Cells.Count];
            rcs.formattedData = new string[cs.Cells.Count];
            for (var i = 0; i < cs.Cells.Count; i++)
            {
                var c = new WrappedCell(cs.Cells[i]);
                rcs.data[i] = c.Value;
                rcs.formattedData[i] = c.FormattedValue;
            }
            RestoreQueryResult(grid);
            grid.Mode = OlapGridMode.gmQueryResult;
            grid.EndChange(GridEventType.geChangeCubeStructure);
            //grid.SetActive(true);

            //grid.UpdaterApplyTemplate_BeginUpdate();

            grid.EndChange(GridEventType.geEndUpdate);
            grid.EndUpdate();

            //grid.OLAPDockPanel.ResumeLayout();
            grid.ModeUpdater.EndUpdate();

            //#if OLAPWEB == false            
//            grid.UpdateUIGrid(false);
//#endif
        }

        internal override void RestoreQueryData(int index, out object Value, out CellFormattingProperties Formatting)
        {
            Formatting = new CellFormattingProperties();
            try
            {
                Value = rcs.data[index];
                Formatting.FormattedValue = rcs.formattedData[index];
            }
            catch
            {
                Value = "#error";
                Formatting.FormattedValue = "#error";
            }
        }

        internal override void RestoreQueryResult(OlapControl grid)
        {
            if (grid.CellSet == null)
                grid.FCellSet = grid.CreateCellset();
            var cs = grid.CellSet;
            cs.ClearMembers();
            cs.FFixedColumns = Math.Max(rcs.rowHierarchies.Length, 1);
            cs.FFixedRows = Math.Max(rcs.columnHierarchies.Length, 1);
            cs.SetRowCount(cs.FFixedRows + rcs.row.Length);
            cs.SetColumnCount(cs.FFixedColumns + rcs.column.Length);

            var hasMeasures = false;

            foreach (var s in rcs.rowHierarchies)
            {
                var H = grid.Dimensions.FindHierarchy(s);
                if (H == null)
                {
                    cs.FRowLevels.Add(new CellsetLevel(grid.Measures.Level));
                    hasMeasures = true;
                }
                else
                {
                    cs.FRowLevels.Add(new CellsetLevel(H.Levels[0]));
                }
            }

            foreach (var s in rcs.columnHierarchies)
            {
                var H = grid.Dimensions.FindHierarchy(s);
                if (H == null)
                {
                    cs.FColumnLevels.Add(new CellsetLevel(grid.Measures.Level));
                    hasMeasures = true;
                }
                else
                {
                    cs.FColumnLevels.Add(new CellsetLevel(H.Levels[0]));
                }
            }

            if (cs.FGrid.Measures.Count == 1 && !hasMeasures)
            {
                cs.FDefaultMeasure = cs.FGrid.Measures[0];
                cs.FVisibleMeasures.Add(cs.FDefaultMeasure);
            }
            var i = 0;
            //int _members = 0;
            while (i < rcs.row.GetLength(1))
            {
                var M = cs.FRowLevels[0].FLevel.FindMember(rcs.row[0, i]);
                //TODO
                var cm = new CellsetMember(M, null, cs.FRowLevels[0], true);
                cs.FRowMembers.Add(cm);
                //_members++;
                var j = 1;
                if (rcs.row.GetLength(0) > 1)
                    j = DoFillCellsetMembers(cs, cm, i, 1, rcs.row, true);
                i += j;
            }
            //if ((grid.AllowPaging) && (_members > grid.LinesInPage))
            //{
            //    CellsetMember cm = new CellsetMember(null, null, cs.FRowLevels[0], true);
            //    cm.FIsPager = true;
            //    cs.FRowMembers.Add(cm);
            //}

            i = 0;
            //_members = 0;
            while (i < rcs.column.GetLength(1))
            {
                var M = cs.FColumnLevels[0].FLevel.FindMember(rcs.column[0, i]);
                //TODO
                var cm = new CellsetMember(M, null, cs.FColumnLevels[0], false);
                cs.FColumnMembers.Add(cm);
                var j = 1;
                if (rcs.column.GetLength(0) > 1)
                    j = DoFillCellsetMembers(cs, cm, i, 1, rcs.column, false);
                i += j;
            }
            //if ((grid.AllowPaging) && (_members > grid.LinesInPage))
            //{
            //    CellsetMember cm = new CellsetMember(null, null, cs.FColumnLevels[0], false);
            //    cm.FIsPager = true;
            //    cs.FColumnMembers.Add(cm);
            //}

            if (cs.FColumnMembers.Count == 0 && cs.FRowMembers.Count > 0)
            {
                //TODO
                var dummy = new CellsetMember(null, null, null, false);
                cs.FColumnMembers.Add(dummy);
            }
            if (cs.FRowMembers.Count == 0 && cs.FColumnMembers.Count > 0)
            {
                //TODO
                var dummy = new CellsetMember(null, null, null, true);
                cs.FRowMembers.Add(dummy);
            }

            cs.CreateSpans();
        }

        private int DoFillCellsetMembers(CellSet.CellSet cs, CellsetMember parent, int from, int column,
            string[,] array, bool isRow)
        {
            var csl = isRow ? cs.FRowLevels[column] : cs.FColumnLevels[column];
            var Parent = array[column - 1, from];
            var i = from;
            //int _members = 0;
            //try
            //{
            while (i < array.GetLength(1))
            {
                if (array[column - 1, i] != Parent) return i - from;
                var M = csl.FLevel.FindMember(array[column, i]);
                //TODO
                var cm = new CellsetMember(M, parent, csl, isRow);
                //_members++;
                if (parent != null) parent.FChildren.Add(cm);
                var j = 1;
                if (column + 1 < array.GetLength(0))
                    j = DoFillCellsetMembers(cs, cm, i, column + 1, array, isRow);
                i += j;
            }
            return Math.Max(i - from, 1);
            //}
            //finally
            //{
            //    if (cs.FGrid.AllowPaging && (cs.FGrid.LinesInPage < _members))
            //    {
            //        CellsetMember cm = new CellsetMember(null, parent, csl, isRow);
            //        if (parent != null) parent.FChildren.Add(cm);
            //        cm.FIsPager = true;
            //    }
            //}
        }

        internal override bool IsFilterAllowed(CellSet.Filter filter)
        {
            EnsureConnected();

            if (Is2000)
                return false;

            return true;
        }

        internal override object RetrieveInfoAttribute(CubeMember m, string attrubuteName)
        {
            EnsureConnected();

            var cube = ActiveCube;
            var d = FindDimension(cube, m.FHierarchy.Dimension);
            var h = FindHierarchy(d, m.FHierarchy);

            Level l;
            if (m.FHierarchy.Origin == HierarchyOrigin.hoParentChild)
                l = FindLevel(h, m.FHierarchy.FMDXLevelNames[m.FMDXLevelIndex]);
            else
                l = FindLevel(h, m.ParentLevel.UniqueName);

            var mm = l.GetMembers(0, 1, new MemberFilter("MEMBER_UNIQUE_NAME", m.UniqueName))[0];

            mm.FetchAllProperties();

            var mp = mm.Properties.Find(attrubuteName);
            if (mp == null)
                return null;
            return mp.Value;
        }

        internal override int RetrieveMembersCount(OlapControl grid, object source)
        {
            EnsureConnected();

            var sb = new StringBuilder("WITH MEMBER [Measures].X AS '");
            if (source is Layout.Level)
            {
                var l = (Layout.Level) source;
                if (l.FUniqueNamesArray.Count > 0)
                    return l.Members.Count;
                if (l.Hierarchy.Origin == HierarchyOrigin.hoNamedSet)
                    return l.CompleteMembersCount;
                if (Is2000 || SubcubeFilter == null)
                {
                    if (l.Hierarchy.Origin != HierarchyOrigin.hoParentChild)
                        sb.Append(l.UniqueName.Replace("'", "''"));
                    else
                        sb.Append(l.Hierarchy.CubeHierarchy.FMDXLevelNames[0].Replace("'", "''"));
                    sb.Append(".ALLMEMBERS.COUNT'");
                }
                else // subcube workaround
                {
                    sb = new StringBuilder("WITH SET StaticSet AS EXISTING ");
                    if (l.Hierarchy.Origin != HierarchyOrigin.hoParentChild)
                        sb.Append(l.UniqueName.Replace("'", "''"));
                    else
                        sb.Append(l.Hierarchy.CubeHierarchy.FMDXLevelNames[0].Replace("'", "''"));
                    sb.Append(".MEMBERS");
                    sb.AppendLine(" MEMBER [Measures].X AS Count(StaticSet) ");
                }
            }
            if (source is Member)
            {
                var m = (Member) source;
                if (m.FMemberType != MemberType.mtCommon)
                    return m.FNextLevelChildren.Count + m.FChildren.Count;
                if (m.CubeMember != null &&
                    m.CubeMember.FChildrenCount > 0)
                    return m.CubeMember.FChildrenCount;
                sb.Append(m.UniqueName.Replace("'", "''"));
                sb.Append(".CHILDREN.COUNT'");
            }

            if (source is CubeMember)
            {
                var m = (CubeMember) source;
                if (m.FChildrenCount > 0)
                    return m.FChildrenCount;
                sb.Append(m.UniqueName.Replace("'", "''"));
                sb.Append(".CHILDREN.COUNT'");
            }

            sb.AppendLine();
            sb.Append("SELECT {X} ON 0");
            sb.AppendLine();

            sb.Append("FROM ");
            sb.Append(ApplySubcubeFilter());


            var css = ExecuteMDXCellset(sb.ToString(), false);
            if (css.Cells.Count < 1) return -1;
            var Result = Convert.ToInt32(css.Cells[0].Value);
            if (source is Member)
            {
                var m = (Member) source;
                if (m.CubeMember != null) m.CubeMember.FChildrenCount = Result;
            }
            return Result;
        }

        internal override int[] RetrieveMembersCount(OlapControl grid, Member[] members)
        {
            EnsureConnected();
            var Result = new int[members.Length];

            var sb = new StringBuilder("WITH MEMBER [Measures].RadarX___ AS '");
            sb.Append(members[0].Level.Hierarchy.UniqueName);
            sb.AppendLine(".CURRENTMEMBER.CHILDREN.COUNT'");
            sb.AppendLine("SELECT {[Measures].RadarX___} ON 0,");
            sb.Append("{");
            var j = 0;
            for (var i = 0; i < members.Length; i++)
            {
                var m = members[i];
                if (m.FMemberType != MemberType.mtCommon) continue;
                if (j++ > 0) sb.Append(",");
                sb.Append(m.UniqueName);
            }
            var counts = new Dictionary<string, int>(j);
            if (j > 0)
            {
                sb.AppendLine("} ON 1 ");
                sb.Append("FROM ");
                sb.Append(ApplySubcubeFilter());

                var css = ExecuteMDXCellset(sb.ToString(), false);
                for (var i = 0; i < css.Cells.Count; i++)
                    counts.Add(css.Axes[1].Set.Tuples[i].Members[0].UniqueName,
                        Convert.ToInt32(css.Cells[i].Value));
            }

            for (var i = 0; i < members.Length; i++)
            {
                var m = members[i];
                if (!counts.TryGetValue(m.UniqueName, out Result[i]))
                    Result[i] = RetrieveMembersCount(grid, m);
            }
            return Result;
        }

        internal void RetrieveMembersCount3_Inner(OlapControl grid, IEnumerable<CubeMember> members)
        {
            if (!members.Any())
                return;

            EnsureConnected();

            var h = members.Select(m => m.ParentLevel.Hierarchy).FirstOrDefault();

            var sb = new StringBuilder("WITH MEMBER [Measures].RadarX___ AS '");
            sb.Append(h.UniqueName);
            sb.AppendLine(".CURRENTMEMBER.CHILDREN.COUNT'");
            sb.AppendLine("SELECT {[Measures].RadarX___} ON 0,");
            sb.Append("{");
            var j = 0;
            foreach (var m in members)
            {
                if (j++ > 0) sb.Append(",");
                sb.Append(m.UniqueName);
            }
            var counts = new Dictionary<string, int>(j);
            if (j > 0)
            {
                sb.AppendLine("} ON 1 ");
                sb.Append("FROM ");
                sb.Append(ApplySubcubeFilter());

                var css = ExecuteMDXCellset(sb.ToString(), false);
                for (var i = 0; i < css.Cells.Count; i++)
                    counts.Add(css.Axes[1].Set.Tuples[i].Members[0].UniqueName, Convert.ToInt32(css.Cells[i].Value));
            }

            foreach (var m in members)
                if (!counts.TryGetValue(m.UniqueName, out m.FChildrenCount))
                    m.FChildrenCount = RetrieveMembersCount(grid, m);
        }

        internal override void RetrieveMembersCount3(OlapControl grid, ICollection<CubeMember> members)
        {
            if (members.Count == 0)
                return;

            var m_pc = members.Where(x => x.Hierarchy.Origin == HierarchyOrigin.hoParentChild);
            var m_notpc = members.Where(x => x.Hierarchy.Origin != HierarchyOrigin.hoParentChild);

            RetrieveMembersCount3_Inner(grid, m_notpc.ToList());

            m_pc.ForEach(x => RetrieveMembersCount3_Inner(grid, new[] {x}));
        }

        internal override void RetrieveMembersFiltered(OlapControl grid, Layout.Hierarchy hierarchy, string filter,
            List<string> list, out bool hasNewMembers, bool exactMatching, bool VisibleOnly)
        {
            hasNewMembers = false;
            EnsureConnected();

            var sb = new StringBuilder("SELECT {} ON 0,");
            sb.AppendLine();

            var ff = filter.Replace("\r", "").Split(new string[1] {"\n"}, StringSplitOptions.RemoveEmptyEntries);

            sb.Append("{HIERARCHIZE(GENERATE(");
            if (ff.Length > 0)
                sb.Append("FILTER(");
            sb.Append(hierarchy.UniqueName);
            sb.Append(".ALLMEMBERS, ");

            if (exactMatching)
                for (var i = 0; i < ff.Length; i++)
                {
                    sb.Append(hierarchy.UniqueName);
                    sb.Append(".CURRENTMEMBER.NAME = \"");
                    sb.Append(ff[i]);
                    sb.Append("\"");
                    if (i < ff.Length - 1)
                        sb.Append(" OR ");
                }
            else
                for (var i = 0; i < ff.Length; i++)
                {
                    sb.Append("InStr(");
                    sb.Append(hierarchy.UniqueName);
                    sb.Append(".CURRENTMEMBER.NAME, \"");
                    sb.Append(ff[i]);
                    sb.Append("\") > 0");
                    if (i < ff.Length - 1)
                        sb.Append(" OR ");
                }

            if (ff.Length > 0)
                sb.Append("), ");
            sb.Append("ASCENDANTS(");

            sb.Append(hierarchy.UniqueName);
            sb.Append(".CURRENTMEMBER)))} DIMENSION PROPERTIES PARENT_UNIQUE_NAME, MEMBER_TYPE ON 1");
            sb.AppendLine();
            sb.Append("FROM ");
            sb.Append(ApplySubcubeFilter());

            var css = ExecuteMDXCellset(sb.ToString(), false);

            foreach (var p in css.Axes[1].Positions)
            {
                var m = p.Members[0];
                if (m.Type == MemberTypeEnum.All) continue;
                //if (m.UniqueName.EndsWith(".UNKNOWNMEMBER")) continue;
                list.Add(m.UniqueName);
                var H = hierarchy.CubeHierarchy;
                var M = H.FindMemberByUniqueName(m.UniqueName);
                if (M == null)
                {
                    var L = H.Origin == HierarchyOrigin.hoParentChild ? H.Levels[0] : H.FindLevel(m.LevelName);
                    var M1 = new CubeMember(H, L, m.Caption, m.Description, m.UniqueName, m.Name, false, m.LevelName);
                    if (m.Type == MemberTypeEnum.Formula)
                        M1.IsMDXCalculated = true;
                    hasNewMembers = true;
                    CubeMember parent = null;
                    var parentname = (string) m.MemberProperties[0].Value;
                    if (parentname != null)
                        parent = H.FindMemberByUniqueName(parentname);
                    if (parent == null || parent.FParentLevel != L) L.Members.Add(M1);
                    if (parent != null)
                        if (parent.FParentLevel == L)
                            SetCubeMemberParent(M1, parent);
                        else
                            SetCubeMemberParent(H, H.Levels.IndexOf(L), H.Levels.IndexOf(parent.FParentLevel),
                                M1.UniqueName, parent.UniqueName);
                }
            }
            foreach (var ll in hierarchy.Levels)
                ll.CreateNewMembers();
            if (VisibleOnly && hierarchy.Filtered)
            {
                var ss = list.ToArray();
                list.Clear();
                foreach (var s in ss)
                {
                    var m = hierarchy.FindMemberByUniqueName(s);
                    if (m.Visible) list.Add(s);
                }
            }
        }

        internal override void CheckAreLeaves(List<Member> members)
        {
            if (members == null || members.Count == 0)
                return;

            if (members[0].Level.Hierarchy.Origin == HierarchyOrigin.hoParentChild)
                DoCheckAreLeaves(members);

            if (members[0].Level.Hierarchy.Origin == HierarchyOrigin.hoUserDefined)
            {
                var h = members[0].Level.Hierarchy;
                var l = h.Levels[h.Levels.Count - 1];
                var newmembers = new List<Member>();
                foreach (var m in members)
                {
                    if (m.Level == l || m.CubeMember != null && m.CubeMember.fIsLeaf != null)
                        continue;
                    newmembers.Add(m);
                }

                DoCheckAreLeaves(newmembers);
            }
        }

        private void DoCheckAreLeaves(List<Member> members)
        {
            if (members.Count == 0) return;
            EnsureConnected();

            var sb = new StringBuilder("WITH MEMBER [Measures].[MyIsLeaf__] AS ");
            sb.AppendLine();
            sb.Append("'IIF(ISLEAF(");
            sb.Append(members[0].FLevel.FHierarchy.UniqueName);
            sb.Append(".CURRENTMEMBER), 1, NULL)' ");
            sb.AppendLine();
            sb.Append("SELECT {[Measures].[MyIsLeaf__]} ON 0, ");
            sb.Append("NON EMPTY {");
            if (members[0].Level.CubeLevel.FMembersCount - 3 < members.Count)
            {
                sb.Append(members[0].Level.UniqueName + ".ALLMEMBERS");
            }
            else
            {
                var b = false;
                foreach (var m in members)
                {
                    if (m.CubeMember.fIsLeaf != null) continue;
                    if (b) sb.Append(",");
                    sb.Append(m.UniqueName);
                    b = true;
                }
            }
            sb.Append("} ON 1 ");
            sb.AppendLine();
            sb.Append("FROM ");
            sb.Append(ApplySubcubeFilter());

            var css = ExecuteMDXCellset(sb.ToString(), false);

            Debug.WriteLine("members.Count=" + members.Count);

            var j = 0;
//#if DEBUG
//            int get_tuple = 0;
//#endif

            for (var i = 0; i < members.Count; i++)
            {
                var m = members[i];


                if (j < css.Cells.Count && m.UniqueName == css.Axes[1].Set.Tuples[j].Members[0].UniqueName)
                {
                    m.CubeMember.fIsLeaf = css.Cells[j].Value.ToString() == "1";
                    j++;
                }
                else
                {
                    if (m.CubeMember != null)
                        m.CubeMember.fIsLeaf = false;
                }
            }
#if DEBUG

            Debug.WriteLine("Tuples getted: " + j);
#endif
        }

        internal override void RetrieveMembers(OlapControl grid, object source)
        {
            if (source is CubeLevel cl)
                source = grid.Dimensions.FindLevel(cl.UniqueName);

            var l = source as Layout.Level;
            
            EnsureConnected();

            if (l != null)
            {
                var metaH = ActiveCube.Dimensions.FindHierarchy(l.Hierarchy.UniqueName);
                var metaL = metaH.FindLevel(l.UniqueName);
                var cubelevel = l.CubeLevel;
                var H = cubelevel.Hierarchy;
                CubeMember Parent = null;
                var LIndex = H.Levels.IndexOf(cubelevel);
                var PIndex = -1;
                var currentRank = -1;
                foreach (var m in metaL.GetMembers())
                {
                    if (m.Type == MemberTypeEnum.All) continue;
                    //if (m.UniqueName.EndsWith(".UNKNOWNMEMBER")) continue;
                    CubeMember M;
                    if (cubelevel.FUniqueNamesArray.TryGetValue(m.UniqueName, out M))
                    {
                        M.FRank = currentRank++;
                        continue;
                    }
                    M = new CubeMember(H, cubelevel, m.Caption, m.Description, m.UniqueName, m.Name, false,
                        m.LevelName);
                    if (m.Type == MemberTypeEnum.Formula)
                        M.IsMDXCalculated = true;
                    M.FRank = currentRank++;
                    cubelevel.Members.Add(M);
                    var o = m.MemberProperties.Find("PARENT_UNIQUE_NAME");
                    if (o != null && o.Value != null)
                    {
                        var pun = o.Value.ToString();
                        if (Parent == null || Parent.UniqueName != pun)
                        {
                            Parent = H.FindMemberByUniqueName(pun);
                            if (Parent != null)
                                PIndex = H.Levels.IndexOf(Parent.ParentLevel);
                            else
                                PIndex = -1;
                        }
                        if (Parent != null)
                        {
                            M.FParent = Parent;
                            if (M.FParentLevel == Parent.FParentLevel)
                                Parent.Children.Add(M);
                            else
                                SetCubeMemberParent(H, LIndex, PIndex, M.UniqueName, Parent.UniqueName);
                        }
                    }

                    var mdxlevel = M.MDXLevel;
                    if (mdxlevel != null)
                        mdxlevel.IncrementMembersCount();
                }
                l.CubeLevel.FMembersCount = l.CubeLevel.Members.Count;
                l.CreateNewMembers();
            }
            if (source is Member)
            {
                var m_ = (Member)source;
                var l_ = m_.Level;
                var h = l_.Hierarchy;
                var L = l_.CubeLevel;
                var H = L.Hierarchy;
                var ls = new SortedList<string, int>(H.Levels.Count);
                for (var i = 0; i < H.Levels.Count; i++)
                    ls.Add(H.Levels[i].UniqueName, i);
                int PIndex;
                var LIndex = 0;
                var templevelname = "?";
                CubeLevel templevel = null;
                ls.TryGetValue(L.UniqueName, out PIndex);
                var ls_ = new SortedList<int, Layout.Level>(1);

                var metaH = ActiveCube.Dimensions.FindHierarchy(l_.Hierarchy.UniqueName);
                var metaL = metaH.FindLevel(l_.UniqueName);

                foreach (var m in metaL.GetMembers())
                {
                    if (m.Type == MemberTypeEnum.All) continue;

                    if (m.LevelName != templevelname)
                    {
                        templevelname = m.LevelName;
                        if (!ls.TryGetValue(templevelname, out LIndex))
                            LIndex = 0;
                        templevel = H.Levels[LIndex];
                        if (!ls_.ContainsKey(LIndex))
                            ls_.Add(LIndex, h.Levels[LIndex]);
                    }
                    if (templevel.FUniqueNamesArray.ContainsKey(m.UniqueName)) continue;
                    var M = new CubeMember(H, templevel, m.Caption, m.Description, m.UniqueName, m.Name, false,
                        m.LevelName);
                    if (m.Type == MemberTypeEnum.Formula)
                        M.IsMDXCalculated = true;
                    if (LIndex == PIndex)
                    {
                        SetCubeMemberParent(M, m_.CubeMember);
                    }
                    else
                    {
                        SetCubeMemberParent(H, LIndex, PIndex, M.UniqueName, m_.UniqueName);
                        templevel.Members.Add(M);
                    }
                }
                foreach (var ll in ls_.Values) ll.CreateNewMembers();
            }
        }

        internal override void RetrieveMembersPartial(OlapControl grid, object source, int from, int count,
            List<string> list, out bool hasNewMembers)
        {
            var cl = source as CubeLevel;
            if (cl != null)
                source = grid.Dimensions.FindLevel(cl.UniqueName);

            var l = source as Layout.Level;
            if (l != null)
            {
                if (l.FUniqueNamesArray.Count > 0)
                {
                    hasNewMembers = false;
                    if (list == null)
                        return;
                    var to = Math.Min(from + count, l.Members.Count);
                    for (var i = from; i < to; i++)
                        list.Add(l.Members[i].UniqueName);
                    return;
                }
                if (from + count == l.Members.Count)
                {
                    hasNewMembers = false;
                    if (list == null)
                        return;
                    var to = Math.Min(from + count, l.Members.Count);
                    for (var i = from; i < to; i++)
                        list.Add(l.Members[i].UniqueName);
                    return;
                }
            }
            EnsureConnected();
            hasNewMembers = false;

            var sb = new StringBuilder("SELECT {} ON 0,");
            sb.AppendLine();
            if (l != null)
            {
                if (count >= 0)
                    if (from == 0)
                        sb.Append("HEAD(");
                    else
                        sb.Append("SUBSET(");
                else
                    sb.Append("{");
                if (l.Hierarchy.Origin != HierarchyOrigin.hoParentChild)
                {
                    sb.Append(l.UniqueName);
                }
                else
                {
                    var cubelevel = l.Hierarchy.CubeHierarchy.Levels[0];
                    var mdxlevel = cubelevel._MDXLevels.FirstOrDefault(x => !x.Isfullfetched);

                    if (mdxlevel != null)
                        sb.Append(mdxlevel.UniqueName);
                    else
                        sb.Append(l.Hierarchy.UniqueName);
                }

                if (l.Hierarchy.Origin != HierarchyOrigin.hoNamedSet)
                    sb.Append(".ALLMEMBERS");

                if (count >= 0)
                {
                    sb.Append(",");
                    if (from > 0)
                    {
                        sb.Append(from);
                        sb.Append(",");
                    }
                    sb.Append(count);
                    sb.Append(")");
                }
                else
                {
                    sb.Append("}");
                }
            }
            if (source is Member)
            {
                var m = (Member) source;
                if (m.FMemberType != MemberType.mtCommon)
                {
                    hasNewMembers = false;
                    for (var i = from; i < from + count; i++)
                        list.Add(m.FChildren[i].UniqueName);
                    return;
                }
                if (count >= 0)
                    if (from == 0)
                        sb.Append("HEAD(");
                    else
                        sb.Append("SUBSET(");
                sb.Append(m.UniqueName);
                sb.Append(".CHILDREN");
                if (count >= 0)
                {
                    sb.Append(",");
                    if (from > 0)
                    {
                        sb.Append(from);
                        sb.Append(",");
                    }
                    sb.Append(count);
                    sb.Append(")");
                }
            }
            sb.Append(" DIMENSION PROPERTIES MEMBER_TYPE ");

            if (l != null && (l.Index > 0 || l.Hierarchy.Origin == HierarchyOrigin.hoParentChild))
                sb.Append(", PARENT_UNIQUE_NAME ");

            sb.Append("ON 1");
            sb.AppendLine();
            sb.Append("FROM ");
            sb.Append(ApplySubcubeFilter());

            var css = ExecuteMDXCellset(sb.ToString(), false);

            if (l != null)
            {
                //Level l = (Level)source;
                var h = l.Hierarchy;
                var cubelevel = l.CubeLevel;
                var H = cubelevel.Hierarchy;
                CubeMember Parent = null;
                var LIndex = H.Levels.IndexOf(cubelevel);
                var PIndex = -1;
                var currentRank = from;
                foreach (var p in css.Axes[1].Positions)
                {
                    var m = p.Members[0];
                    if (m.Type == MemberTypeEnum.All) continue;
                    //if (m.UniqueName.EndsWith(".UNKNOWNMEMBER")) continue;
                    if (list != null) list.Add(m.UniqueName);
                    CubeMember M;
                    if (cubelevel.FUniqueNamesArray.TryGetValue(m.UniqueName, out M))
                    {
                        M.FRank = currentRank++;
                        continue;
                    }
                    M = new CubeMember(H, cubelevel, m.Caption, m.Description, m.UniqueName, m.Name, false,
                        m.LevelName);
                    if (m.Type == MemberTypeEnum.Formula)
                        M.IsMDXCalculated = true;
                    M.FRank = currentRank++;
                    hasNewMembers = true;
                    cubelevel.Members.Add(M);
                    var o = m.MemberProperties.Find("PARENT_UNIQUE_NAME");
                    if (o != null && o.Value != null)
                    {
                        var pun = o.Value.ToString();
                        if (Parent == null || Parent.UniqueName != pun)
                        {
                            Parent = H.FindMemberByUniqueName(pun);
                            if (Parent != null)
                                PIndex = H.Levels.IndexOf(Parent.ParentLevel);
                            else
                                PIndex = -1;
                        }
                        if (Parent != null)
                        {
                            M.FParent = Parent;
                            if (M.FParentLevel == Parent.FParentLevel)
                                Parent.Children.Add(M);
                            else
                                SetCubeMemberParent(H, LIndex, PIndex, M.UniqueName, Parent.UniqueName);
                        }
                    }

                    var mdxlevel = M.MDXLevel;
                    if (mdxlevel != null)
                        mdxlevel.IncrementMembersCount();
                }
                if (from == 0 && count == -1 && l.CubeLevel.FMembersCount < l.CubeLevel.Members.Count)
                    l.CubeLevel.FMembersCount = l.CubeLevel.Members.Count;
                l.CreateNewMembers();
            }
            if (source is Member)
            {
                var m_ = (Member) source;
                var l_ = m_.Level;
                var h = l_.Hierarchy;
                var L = l_.CubeLevel;
                var H = L.Hierarchy;
                var ls = new SortedList<string, int>(H.Levels.Count);
                for (var i = 0; i < H.Levels.Count; i++)
                    ls.Add(H.Levels[i].UniqueName, i);
                int PIndex;
                var LIndex = 0;
                var templevelname = "?";
                CubeLevel templevel = null;
                ls.TryGetValue(L.UniqueName, out PIndex);
                var ls_ = new SortedList<int, Layout.Level>(1);
                foreach (var p in css.Axes[1].Positions)
                {
                    var m = p.Members[0];
                    if (m.Type == MemberTypeEnum.All) continue;
                    //if (m.UniqueName.EndsWith(".UNKNOWNMEMBER")) continue;
                    if (list != null) list.Add(m.UniqueName);
                    if (m.LevelName != templevelname)
                    {
                        templevelname = m.LevelName;
                        if (!ls.TryGetValue(templevelname, out LIndex))
                            LIndex = 0;
                        templevel = H.Levels[LIndex];
                        if (!ls_.ContainsKey(LIndex))
                            ls_.Add(LIndex, h.Levels[LIndex]);
                    }
                    if (templevel.FUniqueNamesArray.ContainsKey(m.UniqueName)) continue;
                    var M = new CubeMember(H, templevel, m.Caption, m.Description, m.UniqueName, m.Name, false,
                        m.LevelName);
                    if (m.Type == MemberTypeEnum.Formula)
                        M.IsMDXCalculated = true;
                    hasNewMembers = true;
                    if (LIndex == PIndex)
                    {
                        SetCubeMemberParent(M, m_.CubeMember);
                    }
                    else
                    {
                        SetCubeMemberParent(H, LIndex, PIndex, M.UniqueName, m_.UniqueName);
                        templevel.Members.Add(M);
                    }
                }
                foreach (var ll in ls_.Values) ll.CreateNewMembers();
            }
        }

        internal override bool HasMemberChildren(Member m)
        {
            if (m.Children.Count > 0)
                return true;
            return m.CubeMember == null
                ? base.HasMemberChildren(m)
                : m.CubeMember.fIsLeaf == false &&
                  m.Level.Hierarchy.Origin == HierarchyOrigin.hoParentChild;
        }

        internal void CreateVisibleSet(Layout.Hierarchy h, out bool SingleVisible, out string Set, Member Restriction,
            bool ForVisualTotals)
        {
            var l = new HashSet<Member>();
            if (Restriction != null)
                l.Add(Restriction);
            CreateVisibleSet(h, out SingleVisible, out Set, l, ForVisualTotals);
        }

        internal void CreateVisibleSet(Layout.Hierarchy h, out bool SingleVisible, out string Set,
            HashSet<Member> Restriction, bool ForVisualTotals)
        {
            var VisibleCount = 0;
            if (Restriction == null) Restriction = new HashSet<Member>();
            var v = new StringBuilder();
            if (Restriction.Count > 0)
                if (Restriction.First().Level.Hierarchy == h &&
                    Restriction.All(item => item != null && item.Visible && !item.Filtered))
                {
                    SingleVisible = Restriction.Count == 1;
                    foreach (var M in Restriction)
                    {
                        if (v.Length > 0) v.Append(',');
                        v.Append(M.UniqueName);
                    }
                    Set = v.ToString();
                    return;
                }
            var unv = new StringBuilder();
            DoCreateVisibleSet(h, null, v, unv, ref VisibleCount, Restriction);

            SingleVisible = false;
            bool allFetched;
            if (h.Origin == HierarchyOrigin.hoParentChild)
                allFetched = h.CubeHierarchy.Levels[0].FFirstLevelMembersCount ==
                             h.CubeHierarchy.Levels[0].Members.Count;
            else
                allFetched = h.CubeHierarchy.Levels[0].IsFullyFetched;
            if (allFetched && VisibleCount == 1)
                SingleVisible = true;
            if (!allFetched && VisibleCount == 1 && !h.UnfetchedMembersVisible)
                SingleVisible = true;
            if (SingleVisible)
            {
                if (h.HasManyLevels)
                    Set = ForVisualTotals ? "HIERARCHIZE(ASCENDANTS(" + v + "))" : v.ToString();
                else
                    Set = ForVisualTotals ? "HIERARCHIZE({" + v + "})" : v.ToString();
                return;
            }

            if (h.UnfetchedMembersVisible)
            {
                if (h.HasManyLevels)
                    if (ForVisualTotals)
                        Set = "DESCENDANTS(" + h.Levels[0].UniqueName + ".ALLMEMBERS) - DESCENDANTS({" +
                              unv + "})";
                    else
                        Set = "DESCENDANTS(" + h.Levels[0].UniqueName + ".MEMBERS,,LEAVES) - DESCENDANTS({" +
                              unv + "},,LEAVES)";
                else
                    Set = h.Levels[0].UniqueName + ".ALLMEMBERS - {" + unv + "}";
            }
            else
            {
                if (ForVisualTotals)
                {
                    if (h.HasManyLevels)
                        Set = "HIERARCHIZE(GENERATE({" + v + "}, ASCENDANTS(" + h.UniqueName + ".CURRENTMEMBER)))";
                    else
                        Set = "DISTINCT({" + v + "})";
                }
                else
                {
                    if (h.HasManyLevels)
                        Set = "DISTINCT(DESCENDANTS({" + v + "},,LEAVES))";
                    else
                        Set = "DISTINCT({" + v + "})";
                }
            }
        }

        //private void DoCreateVisibleSet(Hierarchy h, Member startMember,
        //    StringBuilder Visibles, StringBuilder Unvisibles, ref int VisibleCount, Member Restriction)
        //{
        //    List<Member> l = new List<Member>(1);
        //    if (Restriction != null)
        //        l.Add(Restriction);
        //    DoCreateVisibleSet(h, startMember, Visibles, Unvisibles, ref VisibleCount, l);
        //}

        private void DoCreateVisibleSet(Layout.Hierarchy h, Member startMember,
            StringBuilder Visibles, StringBuilder Unvisibles, ref int VisibleCount, HashSet<Member> Restriction)
        {
            if (Restriction == null) Restriction = new HashSet<Member>();
            Members ms = null;
            if (startMember == null)
            {
                ms = h.Levels[0].Members;
            }
            else
            {
                if (startMember.FChildren.Count > 0) ms = startMember.FChildren;
                if (startMember.FNextLevelChildren.Count > 0) ms = startMember.FNextLevelChildren;
            }
            if (ms == null) // startMember is leaf
            {
                if (startMember.Visible && (Restriction.Count == 0 || Restriction.Contains(startMember)))
                {
                    if (startMember.CubeMember == null || !startMember.CubeMember.IsMDXCalculated)
                    {
                        if (Visibles.Length != 0) Visibles.Append(",");
                        Visibles.Append(startMember.UniqueName);
                        VisibleCount++;
                    }
                }
                else
                {
                    if (Unvisibles.Length != 0) Unvisibles.Append(",");
                    Unvisibles.Append(startMember.UniqueName);
                }
                return;
            }

            foreach (var m in ms)
            {
                if (m is CalculatedMember) continue;
                if (m is GroupMember)
                {
                    if (m.Children.Count > 0)
                        if (Restriction.Contains(m))
                            DoCreateVisibleSet(h, m, Visibles, Unvisibles, ref VisibleCount, null);
                        else
                            DoCreateVisibleSet(h, m, Visibles, Unvisibles, ref VisibleCount, Restriction);
                    continue;
                }
                if (!m.Visible || Restriction.Count > 0 && !Restriction.Contains(m))
                {
                    if (Unvisibles.Length != 0) Unvisibles.Append(",");
                    Unvisibles.Append(m.UniqueName);
                }
                else
                {
                    if (!m.Filtered && (Restriction.Count == 0 || Restriction.Contains(m)))
                    {
                        if (m.CubeMember == null || !m.CubeMember.IsMDXCalculated)
                        {
                            if (Visibles.Length != 0) Visibles.Append(",");
                            Visibles.Append(m.UniqueName);
                            VisibleCount++;
                        }
                    }
                    else
                    {
                        if (Restriction.Contains(m))
                            DoCreateVisibleSet(h, m, Visibles, Unvisibles, ref VisibleCount, null);
                        else
                            DoCreateVisibleSet(h, m, Visibles, Unvisibles, ref VisibleCount, Restriction);
                    }
                }
            }
        }

        /// <summary>
        ///     <para>
        ///         Establishes connection to the OLAP server, if it hasn't already been
        ///         established in the current session.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Connection to the OLAP server may not be established if there is enough data
        ///         fetched from the previous sessions to display the current OLAP slice.
        ///     </para>
        /// </remarks>
        public void EnsureConnected()
        {
            if (FMDConnection.State == ConnectionState.Closed)
            {
                SessionStateData ssd = null;
                if (SessionState.KeyExists(SessionKey.mdcube_sessionstate, UniqueID))
                {
                    ssd = new SessionStateData();
                    SessionState.ReadStreamedObject(SessionKey.mdcube_sessionstate, ssd, UniqueID);
                    FMDConnection.SessionID = ssd.MDXSessionID;
                }
                FMDConnection.ConnectionString = ApplyLocalization(ConnectionString);

                TryOpenConnection(() => FMDConnection.Open(), e =>
                                                              {
                                                                  FMDConnection.SessionID = null;
                                                                  FMDConnection.Open();
                                                              });
            }
        }

        internal static void TryOpenConnection(Action connectionopen, Action<Exception> catchexception)
        {
            var countattempt = 0;
            while (countattempt++ < __MAX_ATTEMPT)
                try
                {
                    connectionopen();
                    break;
                }
                catch (Exception e)
                {
                    if (countattempt > __MAX_ATTEMPT - 1)
                    {
                        catchexception(e);
                        break;
                    }
                    DebugLogging.WriteLine(
                        "MOlapCube.TryOpenConnection() Exception happen! countattempt={0}. throw rejected!",
                        countattempt.ToString());
                }
        }

        internal override void RetrieveAscendants(Layout.Hierarchy hierarchy, IEnumerable<string> members)
        {
            var sb = new StringBuilder();
            foreach (var s in members)
            {
                if (hierarchy.FindMemberByUniqueName(s) != null) continue;
                if (sb.Length > 0) sb.Append(",");
                sb.Append(s);
            }
            if (sb.Length == 0) return;

            string query;
            if (hierarchy.Origin == HierarchyOrigin.hoNamedSet)
                query = "SELECT {} ON 0, {" + sb + "} DIMENSION PROPERTIES PARENT_UNIQUE_NAME, MEMBER_TYPE ON 1 from " +
                        ApplySubcubeFilter();
            else
                query = "SELECT {} ON 0, Hierarchize(Distinct(Generate({" + sb +
                        "}, ASCENDANTS(" + hierarchy.UniqueName +
                        ".currentmember)))) DIMENSION PROPERTIES PARENT_UNIQUE_NAME, MEMBER_TYPE ON 1 from " +
                        ApplySubcubeFilter();
            EnsureConnected();
            var css = ExecuteMDXCellset(query, false);

            foreach (var p in css.Axes[1].Positions)
            {
                var m = p.Members[0];
                if (m.Type == MemberTypeEnum.All) continue;
                //if (m.UniqueName.EndsWith(".UNKNOWNMEMBER")) continue;
                var H = hierarchy.CubeHierarchy;
                var M = H.FindMemberByUniqueName(m.UniqueName);
                if (M == null)
                {
                    var L = H.FindLevel(m.LevelName);
                    if (L == null)
                        if (H.FMDXLevelNames.Contains(m.LevelName))
                            L = H.Levels[0];
                    var M1 = new CubeMember(H, L, m.Caption,
                        m.Description, m.UniqueName, m.Name, false, m.LevelName);
                    if (m.Type == MemberTypeEnum.Formula)
                        M1.IsMDXCalculated = true;
                    CubeMember parent = null;
                    var parentname = (string) m.MemberProperties[0].Value;
                    if (parentname != null)
                        parent = H.FindMemberByUniqueName(parentname);
                    if (parent == null || parent.FParentLevel != L) L.Members.Add(M1);
                    if (parent != null)
                        if (parent.FParentLevel == L)
                            SetCubeMemberParent(M1, parent);
                        else
                            SetCubeMemberParent(H, H.Levels.IndexOf(L), H.Levels.IndexOf(parent.FParentLevel),
                                M1.UniqueName, parent.UniqueName);
                }
            }
            foreach (var ll in hierarchy.Levels)
                ll.CreateNewMembers();
        }

        internal override void RetrieveDescendants(OlapControl grid, List<CubeMember> members, CubeLevel level)
        {
            DoRetrieveDescendants(grid, members, level);
            //CubeHierarchy h = level.Hierarchy;
            //List<CubeMember>[] mtr = new List<CubeMember>[h.Levels.Count];
            //for (int i = 0; i < mtr.Length; i++)
            //    mtr[i] = new List<CubeMember>();

            //DoRetrieveChildrenCount(member, level, mtr, h.Levels.IndexOf(member.ParentLevel));

            //for (int i = 0; i < h.Levels.Count; i++)
            //{
            //    if (mtr[i].Count > 0)
            //    {
            //        RetrieveMembersCount3(grid, mtr[i]);
            //        mtr[i].Clear();
            //    }
            //}

            //DoRetrieveDescendantsCheckMembers(member, level, mtr, h.Levels.IndexOf(member.ParentLevel));

            //for (int i = 0; i < h.Levels.Count; i++)
            //{
            //    if (mtr[i].Count > 0)
            //    {
            //        DoRetrieveDescendants(grid, mtr[i], level);
            //    }
            //}
        }

        private void DoRetrieveChildrenCount(CubeMember member, CubeLevel level, List<CubeMember>[] mtr, int idx)
        {
            if (member.FChildrenCount < 0)
            {
                mtr[idx].Add(member);
            }
            else
            {
                if (level.Hierarchy.Origin == HierarchyOrigin.hoParentChild)
                {
                    foreach (var m in member.Children)
                        DoRetrieveChildrenCount(m, level, mtr, idx);
                }
                else
                {
                    if (level.Hierarchy.Levels[idx + 1] != level)
                        foreach (var m in member.NextLevelChildren)
                            DoRetrieveChildrenCount(m, level, mtr, idx + 1);
                }
            }
        }

        private void DoRetrieveDescendants(OlapControl grid, List<CubeMember> members, CubeLevel level)
        {
            var sb = new StringBuilder();
            foreach (var m in members)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append(m.UniqueName);
            }
            if (sb.Length == 0) return;

            string query;
            if (level.Hierarchy.Origin == HierarchyOrigin.hoParentChild)
                query = "SELECT {} ON 0, Hierarchize(Generate({" + sb +
                        "}, " + level.Hierarchy.UniqueName +
                        ".currentmember.children)) DIMENSION PROPERTIES PARENT_UNIQUE_NAME, MEMBER_TYPE ON 1 from " +
                        ApplySubcubeFilter();
            else
                query = "SELECT {} ON 0, Hierarchize(Generate({" + sb +
                        "}, DESCENDANTS(" + level.Hierarchy.UniqueName + ".currentmember, " + level.UniqueName +
                        ", SELF_AND_BEFORE))) DIMENSION PROPERTIES PARENT_UNIQUE_NAME, MEMBER_TYPE ON 1 from " +
                        ApplySubcubeFilter();
            EnsureConnected();
            var css = ExecuteMDXCellset(query, false);

            foreach (var p in css.Axes[1].Positions)
            {
                var m = p.Members[0];
                if (m.Type == MemberTypeEnum.All) continue;
                //if (m.UniqueName.EndsWith(".UNKNOWNMEMBER")) continue;
                var H = level.Hierarchy;
                var M = H.FindMemberByUniqueName(m.UniqueName);
                if (M == null)
                {
                    var L = H.FindLevel(m.LevelName);
                    if (L == null)
                        if (H.FMDXLevelNames.Contains(m.LevelName))
                            L = H.Levels[0];
                    var M1 = new CubeMember(H, L, m.Caption, m.Description, m.UniqueName, m.Name, false, m.LevelName);
                    if (m.Type == MemberTypeEnum.Formula)
                        M1.IsMDXCalculated = true;
                    CubeMember parent = null;
                    var parentname = (string) m.MemberProperties.Find("PARENT_UNIQUE_NAME").Value;
                    if (parentname != null)
                        parent = H.FindMemberByUniqueName(parentname);
                    if (parent == null || parent.FParentLevel != L) L.Members.Add(M1);
                    if (parent != null)
                        if (parent.FParentLevel == L)
                            SetCubeMemberParent(M1, parent);
                        else
                            SetCubeMemberParent(H, H.Levels.IndexOf(L), H.Levels.IndexOf(parent.FParentLevel),
                                M1.UniqueName, parent.UniqueName);
                }
            }
            foreach (var ll in grid.Dimensions.FindHierarchy(level.Hierarchy.UniqueName).Levels)
                ll.CreateNewMembers();
            foreach (var m in members)
                m.FChildrenCount = m.Children.Count + m.NextLevelChildren.Count;
        }

        private void DoRetrieveDescendantsCheckMembers(CubeMember member, CubeLevel level, List<CubeMember>[] mtr,
            int idx)
        {
            if (level.Hierarchy.Origin == HierarchyOrigin.hoUserDefined)
                if (member.FChildrenCount != member.NextLevelChildren.Count)
                {
                    mtr[idx].Add(member);
                }
                else
                {
                    if (level.Hierarchy.Levels[idx + 1] != level)
                        foreach (var m in member.NextLevelChildren)
                            DoRetrieveDescendantsCheckMembers(m, level, mtr, idx + 1);
                }
            if (level.Hierarchy.Origin == HierarchyOrigin.hoParentChild)
                if (member.FChildrenCount != member.Children.Count)
                    mtr[idx].Add(member);
                else
                    foreach (var m in member.Children)
                        DoRetrieveDescendantsCheckMembers(m, level, mtr, idx);
        }

        internal override void ConvertParentChildToMultilevel()
        {
            foreach (var d in Dimensions)
            foreach (var h in d.Hierarchies)
                if (h.Origin == HierarchyOrigin.hoParentChild)
                    h.FOrigin = HierarchyOrigin.hoUserDefined;
        }

        internal override List<CubeAction> RetrieveChartCellActions(ICubeAddress Address,
            Layout.Measure XMeasure, Layout.Measure YMeasure)
        {
            var a = Address.Clone();
            var Result = new List<CubeAction>();
            if (XMeasure != null)
            {
                a.Measure = XMeasure;
                Result.AddRange(RetrieveActions(a, XMeasure.Grid));
            }
            if (YMeasure != null)
            {
                a.Measure = YMeasure;
                Result.AddRange(RetrieveActions(a, YMeasure.Grid));
            }
            return Result;
        }

        private void RetrieveInfoAttributes(Level l, CubeLevel cl)
        {
            var ls = new List<string>(cl.InfoAttributes.Count);
            foreach (var aa in cl.InfoAttributes)
                ls.Add(aa.DisplayName);
            foreach (var lp in l.LevelProperties)
            {
                var pair = ExtractAttributePair(l, cl, lp);
                if (pair != null && !MemberPairs.ContainsKey(pair))
                    MemberPairs.Add(pair, new Dictionary<int, int>());

                if (ls.Contains(lp.Caption)) continue;
                var i = new InfoAttribute {DisplayName = lp.Caption};
                cl.InfoAttributes.Add(i);
            }
        }

        private static string ExtractAttributePair(Level l, CubeLevel cl, LevelProperty lp)
        {
            var s = cl.Hierarchy.Dimension.UniqueName + lp.UniqueName.Substring(l.UniqueName.Length);
            var h = cl.Hierarchy.Dimension.Hierarchies.Find(s);
            if (h == null || h.Origin != HierarchyOrigin.hoAttribute) return null;
            return cl.UniqueName + "|" + s;
        }

        /// <summary>
        ///     This method supports the RadarCube infrastructure and is not intended to be used directly from your code
        /// </summary>
        /// <param name="AHierarchy"></param>
        /// <param name="LevelsCount"></param>
        protected internal override void RetrieveLevels(CubeHierarchy AHierarchy, OlapControl grid)
        {
            EnsureConnected();

            var ac = ActiveCube;

            if (ac == null)
                return;

            if (AHierarchy.Origin == HierarchyOrigin.hoNamedSet)
            {
                var L = new CubeLevel(AHierarchy, null, AHierarchy.DisplayName, AHierarchy.Description,
                    AHierarchy.UniqueName);
                AHierarchy.Levels.Add(L);
                RegisterLevel(L);
                //bool dummy;
                //RetrieveMembersPartial(grid, L, 0, -1, null, out dummy);
                //L.FMembersCount = L.Members.Count;
                return;
            }

            var d_ = FindDimension(ac, AHierarchy.Dimension);
            if (d_ == null)
                throw new Exception(RadarUtils.GetResStr("rsDimensionNotFound", AHierarchy.Dimension.UniqueName));

            var h_ = FindHierarchy(d_, AHierarchy);
            var isLevelHierarchy = false;
            if (h_ == null)
            {
                if (Is2000 && AHierarchy.Origin == HierarchyOrigin.hoAttribute)
                {
                    h_ = FindHierarchy(d_, AHierarchy.FBaseNamedSetHierarchies);
                    isLevelHierarchy = true;
                }
                if (h_ == null)
                    throw new Exception(RadarUtils.GetResStr("rsHierarchyNotFound", AHierarchy.UniqueName));
            }
            if (AHierarchy.Origin != HierarchyOrigin.hoParentChild)
            {
                if (isLevelHierarchy)
                {
                    var l = FindLevel(h_, AHierarchy.UniqueName);
                    var cubelevel = new CubeLevel(AHierarchy, null, l.Caption, l.Description, l.UniqueName);
                    RetrieveInfoAttributes(l, cubelevel);
                    switch (l.LevelType)
                    {
                        case LevelTypeEnum.TimeDays:
                            cubelevel.FBIMembersType = BIMembersType.ltTimeDayOfMonth;
                            break;
                        case LevelTypeEnum.TimeHalfYears:
                            cubelevel.FBIMembersType = BIMembersType.ltTimeHalfYear;
                            break;
                        case LevelTypeEnum.TimeHours:
                            cubelevel.FBIMembersType = BIMembersType.ltTimeHour;
                            break;
                        case LevelTypeEnum.TimeMinutes:
                            cubelevel.FBIMembersType = BIMembersType.ltTimeMinute;
                            break;
                        case LevelTypeEnum.TimeMonths:
                            cubelevel.FBIMembersType = BIMembersType.ltTimeMonthLong;
                            break;
                        case LevelTypeEnum.TimeQuarters:
                            cubelevel.FBIMembersType = BIMembersType.ltTimeQuarter;
                            break;
                        case LevelTypeEnum.TimeSeconds:
                            cubelevel.FBIMembersType = BIMembersType.ltTimeSecond;
                            break;
                        case LevelTypeEnum.TimeWeeks:
                            cubelevel.FBIMembersType = BIMembersType.ltTimeWeekOfYear;
                            break;
                        case LevelTypeEnum.TimeYears:
                            cubelevel.FBIMembersType = BIMembersType.ltTimeYear;
                            break;
                    }
                    cubelevel.MDXLevelIndex = 0;
                    AHierarchy.Levels.Add(cubelevel);
                    RegisterLevel(cubelevel);
                    cubelevel.FMembersCount = GetLevelMembersCount(l.UniqueName);
                    //foreach (LevelProperty lp in l.LevelProperties)
                    //    L.FInfoAttributes.Add(lp.Caption);
                }
                else // if (isLevelHierarchy)
                {
                    for (var i = 0; i < h_.Levels.Count; i++)
                    {
                        var l = h_.Levels[i];
                        if (l.LevelType == LevelTypeEnum.All)
                            continue;

                        var L = new CubeLevel(AHierarchy, null, l.Caption, l.Description, l.UniqueName);
                        RetrieveInfoAttributes(l, L);
                        switch (l.LevelType)
                        {
                            case LevelTypeEnum.TimeDays:
                                L.FBIMembersType = BIMembersType.ltTimeDayOfMonth;
                                break;
                            case LevelTypeEnum.TimeHalfYears:
                                L.FBIMembersType = BIMembersType.ltTimeHalfYear;
                                break;
                            case LevelTypeEnum.TimeHours:
                                L.FBIMembersType = BIMembersType.ltTimeHour;
                                break;
                            case LevelTypeEnum.TimeMinutes:
                                L.FBIMembersType = BIMembersType.ltTimeMinute;
                                break;
                            case LevelTypeEnum.TimeMonths:
                                L.FBIMembersType = BIMembersType.ltTimeMonthLong;
                                break;
                            case LevelTypeEnum.TimeQuarters:
                                L.FBIMembersType = BIMembersType.ltTimeQuarter;
                                break;
                            case LevelTypeEnum.TimeSeconds:
                                L.FBIMembersType = BIMembersType.ltTimeSecond;
                                break;
                            case LevelTypeEnum.TimeWeeks:
                                L.FBIMembersType = BIMembersType.ltTimeWeekOfYear;
                                break;
                            case LevelTypeEnum.TimeYears:
                                L.FBIMembersType = BIMembersType.ltTimeYear;
                                break;
                        }
                        L.MDXLevelIndex = i;
                        //int LIndex = AHierarchy.Levels.Count;
                        AHierarchy.Levels.Add(L);
                        RegisterLevel(L);
                        L.FMembersCount = GetLevelMembersCount(l.UniqueName);
                        //foreach (LevelProperty lp in l.LevelProperties)
                        //    L.FInfoAttributes.Add(lp.Caption);
                    }
                }
            }
            else // if (AHierarchy.Origin != HierarchyOrigin.hoParentChild)
            {
                var cubelevel = new CubeLevel(AHierarchy, null);
                AHierarchy.Levels.Add(cubelevel);
                RegisterLevel(cubelevel);

                // cubelevel.FCubeHierarchy == AHierarchy; // its parent-hierarchy

                cubelevel._MDXLevels = new List<MDXLevel>();

                for (var i = 0; i < h_.Levels.Count; i++)
                {
                    var level = h_.Levels[i];
                    if (level.LevelType == LevelTypeEnum.All)
                        continue;

                    if (cubelevel.FMembersCount < 0)
                        cubelevel.FMembersCount = 0;

                    RetrieveInfoAttributes(level, cubelevel);

                    var mlevel = new MDXLevel(level.UniqueName, GetLevelMembersCount(level.UniqueName));

                    //mlevel._memberscount = GetLevelMembersCount(level.UniqueName);
                    mlevel._isfullfetched = false;

                    cubelevel.FMembersCount += mlevel._memberscount;
                    cubelevel._MDXLevels.Add(mlevel);
                }

                if (cubelevel.FFirstLevelMembersCount == 0 && cubelevel._MDXLevels.Count > 0)
                    cubelevel.FFirstLevelMembersCount = cubelevel._MDXLevels[0]._memberscount;

                AHierarchy.FMDXLevelNames = cubelevel._MDXLevels.Select(x => x.UniqueName).ToList();
            }
        }

        private int GetLevelMembersCount(string name)
        {
            EnsureConnected();
            var sb = new StringBuilder();
            if (SubcubeFilter == null)
            {
                sb.Append("WITH MEMBER [Measures].[RetrieveLevelMembersCount___] AS ");
                sb.AppendLine();
                sb.Append("'");
                sb.Append(name);
                sb.Append(".ALLMEMBERS.COUNT'");
            }
            else
            {
                sb.Append("WITH SET StaticSet___ as existing ");
                sb.Append(name);
                sb.AppendLine();
                sb.Append("MEMBER [Measures].[RetrieveLevelMembersCount___] AS ");
                sb.AppendLine();
                sb.Append("COUNT(StaticSet___)");
            }
            sb.AppendLine();
            sb.Append("SELECT {[Measures].[RetrieveLevelMembersCount___]} ON 0");
            sb.AppendLine();
            sb.Append("FROM ");
            sb.Append(ApplySubcubeFilter());
            var css = ExecuteMDXCellset(sb.ToString(), true);

            return Convert.ToInt32(css.Cells[0].Value);
        }

        private string ApplyLocalization(string connectionString)
        {
            //setting en-us locale
            return connectionString + ";LocaleIdentifier=" + 1033;

            //if (connectionString.ToLower().Contains("localeidentifier"))
            //    return connectionString;

            //CultureInfo ci = DefinePreferredLang();

            //if (!String.IsNullOrEmpty(fLanguageCode))
            //    ci = new CultureInfo(fLanguageCode);
            //return connectionString + ";LocaleIdentifier=" + ci.LCID.ToString();
        }

        internal string ApplyCellProperties()
        {
            if (!UseOlapServerColorFormatting && !UseOlapServerFontFormatting) return "";
            var sb = new StringBuilder(" CELL PROPERTIES VALUE, FORMATTED_VALUE");
            if (UseOlapServerColorFormatting)
                sb.Append(", BACK_COLOR, FORE_COLOR");
            if (UseOlapServerColorFormatting && UseOlapServerFontFormatting) sb.Append(", ");
            if (UseOlapServerFontFormatting)
                sb.Append("FONT_FLAGS, FONT_NAME, FONT_SIZE");
            return sb.ToString();
        }

        /// <summary>
        ///     Handles the Load event
        /// </summary>
        protected void OnLoad()
        {
            try
            {
                //CheckForNewVersions();
                RestoreCube();
                //string oldConnectionString = ConnectionString;
                //string newConnectionString = RadarCube.SubstitutePathToConnectionString(oldConnectionString, Page.MapPath("~/App_Data"));
                //if (!string.Equals(oldConnectionString, newConnectionString))
                //    ConnectionString = newConnectionString;
                if (_needActive) Active = true;
            }
            catch (Exception E)
            {
                if (FEngineList.Count > 0)
                    FEngineList[0].FGrid.callbackException = E;
                else
                    fCallbackException = E;
            }
        }

        internal string MakeMeasureName(MeasureShowMode mode)
        {
            return mode.Measure.IsKPI ? "KPIValue(\"" + mode.Measure.UniqueName + "\")" : mode.Measure.UniqueName;
        }


        protected internal override void RetrieveMetadata()
        {
            EnsureConnected();
            var cube = ActiveCube;
            if (cube == null)
                if (CubeName != "")
                    throw new Exception(string.Format("The cube \"{0}\" isn't found", CubeName));
                else
                    throw new Exception(string.Format("The CubeName property isn't defined for the {0} control.", ID));

            Dimensions.ClearMembers();
            Dimensions.Clear();
            Measures.Clear();

            var mgroups = new Dictionary<string, string>();
            if (!Is2000)
                foreach (var mgroup in ActiveCube.MeasureGroups)
                    mgroups.Add(mgroup.Name, mgroup.Caption);

            foreach (var m in cube.Measures)
            {
                var o = m.Properties.Find("MEASURE_IS_VISIBLE");
                if (o == null || o.Value != null && Convert.ToBoolean(o.Value))
                {
                    var M = new CubeMeasure();
                    M.Init(this);
                    M.Description = m.Description;
                    M.DisplayName = m.Caption;
                    M.UniqueName = m.UniqueName;
                    o = m.Properties.Find("MEASUREGROUP_NAME");
                    M.DisplayFolder = o != null && o.Value != null ? o.Value.ToString() : "";
                    if (M.DisplayFolder != "" && mgroups.Count > 0)
                    {
                        string mc;
                        if (mgroups.TryGetValue(M.DisplayFolder, out mc))
                            M.DisplayFolder = mc;
                    }
                    o = m.Properties.Find("MEASURE_DISPLAY_FOLDER");
                    var fn = o != null && o.Value != null ? o.Value.ToString() : "";
                    if (!string.IsNullOrEmpty(M.DisplayFolder) && !string.IsNullOrEmpty(fn))
                        M.DisplayFolder += "\\";
                    if (!string.IsNullOrEmpty(fn))
                        M.DisplayFolder += fn;

                    o = m.Properties.Find("DEFAULT_FORMAT_STRING");
                    M.DefaultFormat = o != null && o.Value != null ? o.Value.ToString() : "Standard";
                    M.AggregateFunction = OlapFunction.stInherited;
                    o = m.Properties.Find("MEASURE_AGGREGATOR");
                    if (o != null && o.Value != null)
                        switch (Convert.ToInt32(o.Value))
                        {
                            case 1:
                                M.AggregateFunction = OlapFunction.stSum;
                                break;
                            case 2:
                                M.AggregateFunction = OlapFunction.stCount;
                                break;
                            case 3:
                                M.AggregateFunction = OlapFunction.stMin;
                                break;
                            case 4:
                                M.AggregateFunction = OlapFunction.stMax;
                                break;
                            case 5:
                                M.AggregateFunction = OlapFunction.stAverage;
                                break;
                            case 6:
                                M.AggregateFunction = OlapFunction.stVariance;
                                break;
                            case 7:
                                M.AggregateFunction = OlapFunction.stStdDev;
                                break;
                        }
                    Measures.Add(M);
                }
            }

            if (!Is2000)
                foreach (var k in cube.Kpis)
                {
                    var M = new CubeMeasure();
                    M.Init(this);
                    M.Description = k.Description;
                    M.DisplayName = k.Caption;
                    M.UniqueName = k.Name;
                    M.DisplayFolder = k.DisplayFolder;
                    M.AggregateFunction = OlapFunction.stInherited;
                    M.FIsKPI = true;
                    if (k.StatusGraphic == "Traffic light") M.FKPIStatusImageIndex = 0;
                    if (k.StatusGraphic == "Road signs") M.FKPIStatusImageIndex = 1;
                    if (k.StatusGraphic == "Gauge") M.FKPIStatusImageIndex = 2;
                    if (k.StatusGraphic == "Reversed gauge") M.FKPIStatusImageIndex = 3;
                    if (k.StatusGraphic == "Thermometer") M.FKPIStatusImageIndex = 4;
                    if (k.StatusGraphic == "Cylinder") M.FKPIStatusImageIndex = 5;
                    if (k.StatusGraphic == "Faces") M.FKPIStatusImageIndex = 6;
                    if (k.StatusGraphic == "Variance arrow") M.FKPIStatusImageIndex = 7;

                    if (k.TrendGraphic == "Standard Arrow") M.FKPITrendImageIndex = 0;
                    if (k.TrendGraphic == "Status Arrow - Ascending") M.FKPITrendImageIndex = 1;
                    if (k.TrendGraphic == "Status Arrow - Descending") M.FKPITrendImageIndex = 2;
                    if (k.TrendGraphic == "Faces") M.FKPITrendImageIndex = 3;

                    var o = k.Properties.Find("KPI_VALUE");
                    if (o != null && o.Value != null)
                    {
                        var m = FindMeasure(cube, o.Value.ToString());
                        if (m != null)
                        {
                            var o1 = m.Properties.Find("DEFAULT_FORMAT_STRING");
                            M.DefaultFormat = o1 != null && o1.Value != null ? o1.Value.ToString() : "";
                        }
                    }
                    Measures.Add(M);
                }
            foreach (Dimension d in cube.Dimensions)
            {
                var o = d.Properties.Find("DIMENSION_IS_VISIBLE");
                if (o == null || o.Value != null && Convert.ToBoolean(o.Value))
                {
                    if (d.DimensionType == DimensionTypeEnum.Measure) continue;
                    var D = new CubeDimension();
                    D.Init(this);
                    D.Description = d.Description;
                    if (d.DimensionType == DimensionTypeEnum.Time)
                        D.fDimensionType = DimensionType.dtTime;
                    D.DisplayName = d.Caption;
                    D.UniqueName = d.UniqueName;
                    Dimensions.Add(D);
                    foreach (Hierarchy h in d.Hierarchies)
                    {
                        var ho = h.Properties.Find("HIERARCHY_ORIGIN");
                        if (ho != null)
                            if (Convert.ToInt32(ho.Value) >= 4)
                                D.FKeyHierarchyName = h.UniqueName;
                        o = h.Properties.Find("HIERARCHY_IS_VISIBLE");
                        if (o == null || o.Value != null && Convert.ToBoolean(o.Value))
                        {
                            var H = new CubeHierarchy();
                            H.Init(D);
                            H.DisplayName = h.Caption;
                            H.Description = h.Description;
                            H.UniqueName = h.UniqueName;
                            if (!IgnoreDefaultMember)
                                H.DefaultMember = h.DefaultMember;
                            o = h.Properties.Find("HIERARCHY_DISPLAY_FOLDER");
                            H.DisplayFolder = o != null && o.Value != null ? o.Value.ToString() : "";
                            var s = h.DefaultMember;
                            if (h.HierarchyOrigin == XmlaClient.Metadata.HierarchyOrigin.AttributeHierarchy)
                                H.Origin = HierarchyOrigin.hoAttribute;
                            if (h.HierarchyOrigin == XmlaClient.Metadata.HierarchyOrigin.ParentChildHierarchy)
                                H.Origin = HierarchyOrigin.hoParentChild;
                            if (h.HierarchyOrigin == XmlaClient.Metadata.HierarchyOrigin.UserHierarchy)
                                H.Origin = HierarchyOrigin.hoUserDefined;

                            D.Hierarchies.Add(H);
                        }
                    }
                }
            }
            foreach (var n in cube.NamedSets)
            {
                var dims = n.Properties.Find("DIMENSIONS").Value.ToString().Split(',');
                var d = new List<CubeDimension>(dims.Length);
                foreach (var dim in dims) // array of dimension unique names
                {
                    var H = Dimensions.FindHierarchy(dim);
                    if (H == null) continue;
                    var D = H.Dimension;
                    if (d.Contains(D)) continue;
                    d.Add(D);
                    H = new CubeHierarchy();
                    H.Init(D);
                    H.UniqueName = "[" + n.Name + "]";
                    H.FBaseNamedSetHierarchies = n.Properties.Find("DIMENSIONS").Value.ToString();
                    H.DisplayName = n.Name;
                    var o = n.Properties.Find("SET_CAPTION");
                    if (o != null && o.Value != null)
                        H.DisplayName = o.Value.ToString();
                    H.Description = n.Description;
                    H.Origin = HierarchyOrigin.hoNamedSet;
                    o = n.Properties.Find("SET_DISPLAY_FOLDER");
                    if (o != null && o.Value != null)
                        H.DisplayFolder = o.Value.ToString();
                    D.Hierarchies.Add(H);
                }
            }
            MapChanged();
        }

        protected internal override void InternalSetActive(bool value)
        {
            if (!value)
            {
                var el = FEngineList.ToArray();
                foreach (var eng in el)
                    eng.FGrid.EnsureStateRestored();

                SessionState.Delete(SessionKey.mdcube_sessionstate, UniqueID);

                rcs = null;
                MapChanged();
            }

            if (value)
            {
                PrepareWorkingDirectory();
                RetrieveMetadata();
            }
            else
            {
                EnsureConnected();
                FMDConnection.Close();
            }

            base.InternalSetActive(value);
        }

        internal string MakeIntelligenceMember(Line ALine, StringBuilder WITH)
        {
            var measureName = MakeMeasureName(ALine.fMode);
            var h = ALine.fMode.LinkedIntelligence.fParent;
            var b = false;
            foreach (var l in ALine.fM.fLevels)
                if (l.Hierarchy == h)
                {
                    b = true;
                    break;
                }
            if (!b) // return if the intelligence hierarchy is not appears in the line
                return measureName;

            var name = "[Measures].[" + Guid.NewGuid() + "]";
            if (WITH.Length == 0) WITH.Append("WITH ");
            WITH.Append("MEMBER ");
            WITH.Append(name);
            WITH.Append(" AS '");
            string aggregator;
            switch (ALine.Measure.AggregateFunction)
            {
                case OlapFunction.stSum:
                    aggregator = "SUM";
                    break;
                case OlapFunction.stCount:
                    aggregator = "SUM";
                    break;
                case OlapFunction.stMin:
                    aggregator = "MIN";
                    break;
                case OlapFunction.stMax:
                    aggregator = "MAX";
                    break;
                case OlapFunction.stAverage:
                    aggregator = "AVG";
                    break;
                case OlapFunction.stVariance:
                    aggregator = "VAR";
                    break;
                case OlapFunction.stStdDev:
                    aggregator = "STDEV";
                    break;
                default:
                    aggregator = "AGGREGATE";
                    break;
            }
            var s = ALine.fMode.LinkedIntelligence.Expression.Replace("{1}", measureName.Replace("'", "''")).Replace(
                "{2}",
                aggregator);
            WITH.Append(s);
            WITH.Append("',FORMAT_STRING='");
            WITH.Append(ALine.Measure.DefaultFormat);
            WITH.Append("'");
            WITH.AppendLine();
            return name;
        }

        internal void MakeCalculatedMeasure(Layout.Measure M, StringBuilder WITH)
        {
            if (WITH.Length == 0) WITH.Append("WITH ");
            WITH.Append("MEMBER ");
            WITH.Append(M.UniqueName);
            WITH.Append(" AS '");
            WITH.Append(M.Expression);
            WITH.Append("'");
            if (!string.IsNullOrEmpty(M.DefaultFormat))
            {
                WITH.Append(",FORMAT_STRING='");
                WITH.Append(M.DefaultFormat);
                WITH.Append("'");
            }
            WITH.AppendLine();
        }

        internal void MakeMeasureFilter(Layout.Measure M, StringBuilder WITH, out string measureName)
        {
            measureName = "[Measures].[" + Guid.NewGuid() + "]";
            if (WITH.Length == 0) WITH.Append("WITH ");
            WITH.Append("MEMBER ");
            WITH.Append(measureName);
            WITH.Append(" AS 'IIF(");
            WITH.Append(M.UniqueName);
            var cnd = "";
            switch (M.Filter.FilterCondition)
            {
                case OlapFilterCondition.fcBetween:
                    cnd = ">=";
                    break;
                case OlapFilterCondition.fcEqual:
                    cnd = "=";
                    break;
                case OlapFilterCondition.fcGreater:
                    cnd = ">";
                    break;
                case OlapFilterCondition.fcLess:
                    cnd = "<";
                    break;
                case OlapFilterCondition.fcNotBetween:
                    cnd = "<";
                    break;
                case OlapFilterCondition.fcNotEqual:
                    cnd = "<>";
                    break;
                case OlapFilterCondition.fcNotGreater:
                    cnd = "<=";
                    break;
                case OlapFilterCondition.fcNotLess:
                    cnd = ">=";
                    break;
            }
            WITH.Append(cnd);

            WITH.Append(CorrectNumberString(M.Filter.FirstValue, M.Grid));

            if (M.Filter.FilterCondition == OlapFilterCondition.fcBetween)
            {
                WITH.Append(" AND ");
                WITH.Append(M.UniqueName);
                WITH.Append("<=");

                WITH.Append(CorrectNumberString(M.Filter.SecondValue, M.Grid));
            }
            if (M.Filter.FilterCondition == OlapFilterCondition.fcNotBetween)
            {
                WITH.Append(" OR ");
                WITH.Append(M.UniqueName);
                WITH.Append(">");
                WITH.Append(CorrectNumberString(M.Filter.SecondValue, M.Grid));
            }
            WITH.Append(", ");
            WITH.Append(M.UniqueName);
            WITH.Append(", null)',FORMAT_STRING='");
            WITH.Append(M.DefaultFormat);
            WITH.Append("'");
            WITH.AppendLine();
        }

        internal void MakeVisualTotals(Layout.Hierarchy H, StringBuilder WITH)
        {
            if (WITH.Length == 0) WITH.Append("WITH ");
            WITH.Append("SET [");
            WITH.Append(Guid.NewGuid());
            WITH.Append("] AS 'VISUALTOTALS({");
            bool b;
            string set;
            CreateVisibleSet(H, out b, out set, new HashSet<Member>(), true);
            WITH.Append(set.Replace("'", "''"));
            WITH.Append("})'");
            WITH.AppendLine();
        }

        internal string MakeCrossjoin(Layout.Level L, StringBuilder WITH, HashSet<Member> Restriction, int Depth)
        {
            DebugLogging.WriteLine("MOlapCube.MakeCrossjoin()_START");

            var restriction_members = new List<Member>(Restriction);

            var res = new StringBuilder("{");
            List<Member> ms;
            if (restriction_members == null || restriction_members.Count == 0)
            {
                ms = L.Members;
            }
            else
            {
                ms = new List<Member>();
                foreach (var m in restriction_members)
                {
                    ms.AddRange(m.FChildren);
                    ms.AddRange(m.NextLevelChildren);
                }
            }

            var groups = new List<GroupMember>(1);
            var calcmembers = new List<CalculatedMember>();
            var gnames = "";
            var cnames = "";
            foreach (var m in ms)
            {
                if (m is GroupMember)
                    if (((GroupMember) m).Children.Count > 0 && m.Visible)
                        groups.Add((GroupMember) m);
                if (m is CalculatedMember)
                    if (!string.IsNullOrEmpty(((CalculatedMember) m).Expression))
                        calcmembers.Add((CalculatedMember) m);
            }
            var group_members = restriction_members.Where(item => item is GroupMember).ToList();
            restriction_members.RemoveAll(item => group_members.Contains(item) ||
                                                  group_members.FirstOrDefault(g => g.Children.Contains(item)) != null);
            if (group_members.Count > 0 && restriction_members.Count > 0)
                throw new Exception("Partial grouping isn't supported");

            foreach (var m in group_members)
                restriction_members.AddRange(m.Children);

            if (groups.Count > 0)
                gnames = "," + MakeGroups(L.Hierarchy, WITH, groups);
            if (calcmembers.Count > 0)
                cnames = "," + MakeCalculatedMembers(L.Hierarchy, WITH, calcmembers);

            var sb = new StringBuilder();
            if (group_members.Any())
            {
                foreach (var m in restriction_members)
                {
                    if (sb.Length > 0) sb.Append(",");
                    sb.Append(m.UniqueName);
                }
                res.Append(sb);
            }
            else
            {
                if (restriction_members != null && restriction_members.Count > 0)
                {
                    foreach (var m in restriction_members)
                    {
                        if (sb.Length > 0) sb.Append(",");
                        sb.Append(m.UniqueName);
                    }

                    if (L.Index > restriction_members[0].Level.Index ||
                        L.Hierarchy.Origin == HierarchyOrigin.hoParentChild
                        && Depth > restriction_members[0].Depth)
                        if (restriction_members[0].Level == L)
                            res.Append("Generate({" + sb +
                                       "}, " + L.Hierarchy.UniqueName + ".currentmember.children)");
                        else
                            res.Append("Generate({" + sb +
                                       "}, DESCENDANTS(" + L.Hierarchy.UniqueName + ".currentmember, " + L.UniqueName +
                                       "))");
                    else
                        res.Append(sb);
                }
                else
                {
                    if (L.Hierarchy.Origin == HierarchyOrigin.hoParentChild)
                        res.Append(L.Hierarchy.CubeHierarchy.FMDXLevelNames[0]);
                    else
                        res.Append(L.UniqueName);
                    if (L.Hierarchy.Origin != HierarchyOrigin.hoNamedSet)
                        res.Append(".ALLMEMBERS");
                }
            }
            res.Append(gnames);
            res.Append(cnames);
            res.Append("}");

            return res.ToString();
        }

        internal string MakeGroups(Layout.Hierarchy H, StringBuilder WITH, List<GroupMember> groups)
        {
            var GROUPS = new StringBuilder();
            foreach (var gm in groups)
            {
                var ms = new List<Member>();
                gm.PopulateListOfMembers(ms);
                if (ms.Count == 0) continue;
                if (WITH.Length == 0) WITH.Append("WITH ");
                WITH.Append("MEMBER ");
                WITH.Append(gm.UniqueName);
                WITH.Append(" AS 'AGGREGATE({");
                var b = false;
                foreach (var m in ms)
                {
                    if (b) WITH.Append(",");
                    b = true;
                    WITH.Append(m.UniqueName.Replace("'", "''"));
                }
                WITH.Append("})'");
                WITH.AppendLine();
                if (GROUPS.Length > 0) GROUPS.Append(",");
                GROUPS.Append(gm.UniqueName);
            }
            return GROUPS.ToString();
        }

        internal string MakeCalculatedMembers(Layout.Hierarchy H, StringBuilder WITH, List<CalculatedMember> members)
        {
            var MEMBERS = new StringBuilder();
            foreach (var gm in members)
            {
                if (WITH.Length == 0) WITH.Append("WITH ");
                WITH.Append("MEMBER ");
                WITH.Append(gm.UniqueName);
                WITH.Append(" AS '");
                WITH.Append(gm.Expression);
                WITH.Append("'");
                WITH.AppendLine();
                if (MEMBERS.Length > 0) MEMBERS.Append(",");
                MEMBERS.Append(gm.UniqueName);
            }
            return MEMBERS.ToString();
        }

        private void CreateAllInvisible(Layout.Hierarchy H, Member Restriction,
            List<string> usedLevels, List<string> usedFilters)
        {
            StringBuilder[] filters;
            if (H.Origin == HierarchyOrigin.hoParentChild)
                filters = new StringBuilder[H.CubeHierarchy.FMDXLevelNames.Count];
            else
                filters = new StringBuilder[H.Levels.Count];
            for (var i = 0; i < H.Levels.Count; i++)
            {
                var l = H.Levels[i];
                foreach (var m in l.FStaticMembers)
                    if (m.Filtered)
                    {
                        if (Restriction != null)
                            if (!Restriction.IsAncestorFor(m)) continue;
                        var parent = m.Parent;
                        while (parent != null && parent is GroupMember)
                            parent = parent.Parent;
                        if (parent != null && !parent.Visible) continue;
                        var filterIndex = H.Origin == HierarchyOrigin.hoParentChild ? m.CubeMember.FMDXLevelIndex : i;
                        if (filters[filterIndex] != null)
                            filters[filterIndex].Append(",");
                        else
                            filters[filterIndex] = new StringBuilder();
                        filters[filterIndex].Append(m.UniqueName);
                    }
            }
            for (var i = 0; i < filters.Length; i++)
            {
                if (filters[i] == null) continue;
                usedLevels.Add(H.Origin == HierarchyOrigin.hoParentChild
                    ? H.CubeHierarchy.FMDXLevelNames[i]
                    : H.Levels[i].UniqueName);
                usedFilters.Add(filters[i].ToString());
            }
        }

        private string CreateAllVisible(Layout.Hierarchy H, Member Restriction, List<string> usedLevels)
        {
            var b = false;
            var sb = new StringBuilder();
            foreach (var l in H.Levels)
            foreach (var m in l.FStaticMembers)
                if (m.Visible && !m.Filtered && (m.CubeMember == null || !m.CubeMember.IsMDXCalculated))
                {
                    if (Restriction != null)
                        if (!Restriction.IsAncestorFor(m)) continue;
                    var parent = m.Parent;
                    while (parent != null && parent is GroupMember)
                        parent = parent.Parent;
                    if (parent != null && parent.Visible && !parent.Filtered) continue;
                    if (b)
                        sb.Append(",");
                    b = true;
                    sb.Append(m.UniqueName);
                    if (H.Origin != HierarchyOrigin.hoParentChild)
                    {
                        if (!usedLevels.Contains(l.UniqueName)) usedLevels.Add(l.UniqueName);
                    }
                    else
                    {
                        var lname = H.CubeHierarchy.FMDXLevelNames[m.CubeMember.FMDXLevelIndex];
                        if (!usedLevels.Contains(lname)) usedLevels.Add(lname);
                    }
                }
            return sb.ToString();
        }

        internal string DoSubcubeFilter(Layout.Hierarchy H, Member Restriction)
        {
            var sb = new StringBuilder("");
            if (H.Origin == HierarchyOrigin.hoNamedSet)
            {
                sb.Append("{");
                if (H.FFiltered)
                {
                    var b = false;
                    foreach (var m in H.Levels[0].Members)
                    {
                        if (!m.Visible) continue;
                        if (b) sb.Append(", ");
                        sb.Append(m.UniqueName);
                        b = true;
                    }
                }
                else
                {
                    sb.Append(H.UniqueName);
                }
                sb.Append("}");
                return sb.ToString();
            }

            if (H.UnfetchedMembersVisible && !H.Levels[0].FCubeLevel.IsFullyFetched)
            {
                var usedLevels = new List<string>();
                var usedFilters = new List<string>();
                CreateAllInvisible(H, Restriction, usedLevels, usedFilters);
                for (var i = 0; i < usedLevels.Count; i++)
                {
                    var s = usedLevels[i];
                    if (sb.Length > 0)
                    {
                        sb.Append(",");
                        sb.AppendLine();
                    }
                    if (H.HasManyLevels)
                    {
                        if (Restriction == null)
                        {
                            sb.Append("{");
                            sb.Append(s);
                            sb.Append(".ALLMEMBERS - DESCENDANTS({");
                        }
                        else
                        {
                            sb.Append("{DESCENDANTS(");
                            sb.Append(Restriction.UniqueName);
                            sb.Append(",");
                            sb.Append(s);
                            sb.Append(") - DESCENDANTS({");
                        }
                        sb.Append(usedFilters[i]);
                        sb.Append("},");
                        sb.Append(s);
                        sb.Append(")}");
                    }
                    else
                    {
                        //                        sb.Append("{");
                        if (Restriction == null)
                        {
                            sb.Append(s);
                            sb.Append(".ALLMEMBERS - {");
                            sb.Append(usedFilters[i]);
                            sb.Append("}");
                        }
                        else
                        {
                            sb.Append(Restriction.UniqueName);
                        }
                        //                        sb.Append("}");
                    }
                }
                return sb.ToString();
            }
            else
            {
                var usedLevels = new List<string>();
                var filter = CreateAllVisible(H, Restriction, usedLevels);
                return "{" + filter + "}";
            }
        }

        internal void MakeWhere(Layout.Hierarchy H, StringBuilder WITH, StringBuilder WHERE,
            HashSet<Member> Restriction, out bool single)
        {
            if (Restriction == null) Restriction = new HashSet<Member>();
            if (Restriction.Any(item => item.MemberType == MemberType.mtGroup) ||
                H.Filtered && Restriction.Count > 0 && Restriction.All(item => item.Visible && !item.Filtered))
            {
                single = false;
                return;
            }
            string set;
            CreateVisibleSet(H, out single, out set, Restriction, false);

            if (!single)
            {
                var MName = H.UniqueName + ".[" + Guid.NewGuid() + "]";
                if (WITH.Length == 0) WITH.Append("WITH ");
                WITH.Append("MEMBER ");
                WITH.Append(MName);
                WITH.Append(" AS 'AGGREGATE({");
                WITH.Append(set.Replace("'", "''"));
                WITH.Append("})'");
                WITH.AppendLine();

                if (WHERE.ToString() != "")
                    WHERE.Append(",");
                else
                    WHERE.Append("WHERE (");
                WHERE.Append(MName);
            }
            else
            {
                if (WHERE.ToString() != "")
                    WHERE.Append(",");
                else
                    WHERE.Append("WHERE (");
                WHERE.Append(set);
            }
        }

        /// <summary>
        ///     Raises the Control.PreRender event
        /// </summary>
        protected void OnPreRender()
        {
            InitSessionData(null);
        }

        protected internal override bool RestoreCube()
        {
            //if (IsRestored)
            //    return true;

            if (!SessionState.KeyExists(SessionKey.mdcube_sessionstate, UniqueID))
                return false;
            var ssd = new SessionStateData();
            SessionState.ReadStreamedObject(SessionKey.mdcube_sessionstate, ssd, UniqueID);
            ssd.Restore(this);
            return true;
        }

        internal override void InitSessionData(OlapControl grid)
        {
            if (FEngineList.Count > 0 && grid == null) return;
            if (Active)
            {
                var ssd = new SessionStateData();
                ssd.Init(this);
                SessionState.Write(ssd, SessionKey.mdcube_sessionstate, UniqueID);
            }
        }

        internal override void _DoWriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgOLAPCube);

            StreamUtils.WriteStreamedObject(writer, Measures, Tags.tgOLAPCube_Measures);

            StreamUtils.WriteStreamedObject(writer, frcDimensions, Tags.tgOLAPCube_Dimensions);

            StreamUtils.WriteTag(writer, Tags.tgOLAPCube_Active);
            StreamUtils.WriteBoolean(writer, Active);

            if (FLevelsList.Count > 0)
            {
                StreamUtils.WriteTag(writer, Tags.tgOLAPCube_LevelsList);
                StreamUtils.WriteInt32(writer, FLevelsList.Count);
                foreach (var l in FLevelsList)
                    if (l == null)
                        StreamUtils.WriteString(writer, "");
                    else
                        StreamUtils.WriteString(writer, l.UniqueName);
            }

            StreamUtils.WriteTag(writer, Tags.tgOLAPCube_EOT);
        }

        internal override void _DoReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgOLAPCube);
            frcDimensions.Clear();
            Measures.Clear();
            FLevelsList.Clear();
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgOLAPCube_Active:
                        FActive = StreamUtils.ReadBoolean(reader);
                        break;
                    case Tags.tgOLAPCube_Dimensions:
                        StreamUtils.ReadStreamedObject(reader, frcDimensions);
                        break;
                    case Tags.tgOLAPCube_Measures:
                        StreamUtils.ReadStreamedObject(reader, Measures);
                        break;
                    case Tags.tgOLAPCube_LevelsList:
                        var c = StreamUtils.ReadInt32(reader);
                        for (var i = 0; i < c; i++)
                        {
                            var s = StreamUtils.ReadString(reader);
                            FLevelsList.Add(frcDimensions.FindLevel(s));
                        }
                        break;
                    case Tags.tgOLAPCube_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        [Serializable]
        protected internal class RadarCellset
        {
            internal string[,] column = new string[0, 0];
            internal string[] columnHierarchies = new string[0];
            internal object[] data;
            internal string[] formattedData;
            internal string[,] row = new string[0, 0];
            internal string[] rowHierarchies = new string[0];
        }

        [Serializable]
        protected internal class SessionStateData : IStreamedObject
        {
            public string MDXSessionID;
            private MemoryStream ms;

            internal void Init(MOlapCube cube)
            {
                var root = new RadarCubeRoot();
                root.cube = cube;
                root.ConnectionString = cube.ConnectionString;
                root.rcs = cube.rcs;
                root.CubeName = cube.CubeName;
                MDXSessionID = cube.FMDConnection.SessionID;
                root.SerializeMembers();
                ms = root.ms;
            }

            public void Restore(MOlapCube cube)
            {
                if (cube.IsRestored) return;
                cube.IsRestored = true;
                ms.Position = 0;

                var root = new RadarCubeRoot();
                root.ms = ms;
                root.cube = cube;
                root.DeserializeMembers();
                cube.rcs = root.rcs;
                //foreach (CubeMeasure measure in cube.Measures) measure.FCube = cube;
                //foreach (CubeDimension dim in cube.Dimensions) dim.FCube = cube;
                //cube.ConnectionString = root.ConnectionString;
                //cube.CubeName = root.CubeName;
            }

            [Serializable]
            internal struct RadarCubeRoot : IStreamedObject
            {
                internal string ConnectionString;
                internal string CubeName;
                internal RadarCellset rcs;
                internal MemoryStream ms;
                [NonSerialized] internal MOlapCube cube;

                internal void SerializeMembers()
                {
                    ms = new MemoryStream();
                    var writer = new BinaryWriter(ms);
                    (this as IStreamedObject).WriteStream(writer, null);
                }

                internal void DeserializeMembers()
                {
                    ms.Position = 0;
                    var reader = new BinaryReader(ms);
                    (this as IStreamedObject).ReadStream(reader, null);
                    ms = null;
                }

                #region IStreamedObject Members

                void IStreamedObject.ReadStream(BinaryReader reader, object options)
                {
                    StreamUtils.CheckTag(reader, Tags.tgOLAPCube);
                    cube.frcDimensions.Clear();
                    cube.Measures.Clear();
                    cube.FLevelsList.Clear();
                    for (var exit = false; !exit;)
                    {
                        var tag = StreamUtils.ReadTag(reader);
                        switch (tag)
                        {
                            case Tags.tgOLAPCube_Active:
                                cube.FActive = StreamUtils.ReadBoolean(reader);
                                break;
                            case Tags.tgOLAPCube_Dimensions:
                                StreamUtils.ReadStreamedObject(reader, cube.frcDimensions);
                                break;
                            case Tags.tgOLAPCube_Measures:
                                StreamUtils.ReadStreamedObject(reader, cube.Measures);
                                break;
                            case Tags.tgOLAPCube_LevelsList:
                                var c = StreamUtils.ReadInt32(reader);
                                for (var i = 0; i < c; i++)
                                {
                                    var s = StreamUtils.ReadString(reader);
                                    cube.FLevelsList.Add(cube.frcDimensions.FindLevel(s));
                                }
                                break;
                            case Tags.tgOLAPCube_EOT:
                                exit = true;
                                break;
                            default:
                                StreamUtils.SkipValue(reader);
                                break;
                        }
                    }
                }

                void IStreamedObject.WriteStream(BinaryWriter writer, object options)
                {
                    StreamUtils.WriteTag(writer, Tags.tgOLAPCube);

                    StreamUtils.WriteStreamedObject(writer, cube.Measures, Tags.tgOLAPCube_Measures);

                    StreamUtils.WriteStreamedObject(writer, cube.frcDimensions, Tags.tgOLAPCube_Dimensions);

                    StreamUtils.WriteTag(writer, Tags.tgOLAPCube_Active);
                    StreamUtils.WriteBoolean(writer, cube.Active);

                    if (cube.FLevelsList.Count > 0)
                    {
                        StreamUtils.WriteTag(writer, Tags.tgOLAPCube_LevelsList);
                        StreamUtils.WriteInt32(writer, cube.FLevelsList.Count);
                        foreach (var l in cube.FLevelsList)
                            if (l == null)
                                StreamUtils.WriteString(writer, "");
                            else
                                StreamUtils.WriteString(writer, l.UniqueName);
                    }

                    StreamUtils.WriteTag(writer, Tags.tgOLAPCube_EOT);
                }

                #endregion
            }

            #region IStreamedObject Members

            void IStreamedObject.WriteStream(BinaryWriter writer, object options)
            {
                StreamUtils.WriteTag(writer, Tags.tgMDCubeSessionState);

                StreamUtils.WriteStream(writer, ms, Tags.tgMDCubeSessionState_Stream);

                StreamUtils.WriteTag(writer, Tags.tgMDCubeSessionState_MDXSessionID);
                StreamUtils.WriteString(writer, MDXSessionID);

                StreamUtils.WriteTag(writer, Tags.tgMDCubeSessionState_EOT);
            }

            void IStreamedObject.ReadStream(BinaryReader reader, object options)
            {
                StreamUtils.CheckTag(reader, Tags.tgMDCubeSessionState);
                for (var exit = false; !exit;)
                {
                    var tag = StreamUtils.ReadTag(reader);
                    switch (tag)
                    {
                        case Tags.tgMDCubeSessionState_Stream:
                            ms = new MemoryStream();
                            StreamUtils.ReadStream(reader, ms);
                            break;
                        case Tags.tgMDCubeSessionState_MDXSessionID:
                            MDXSessionID = StreamUtils.ReadString(reader);
                            break;
                        case Tags.tgMDCubeSessionState_EOT:
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

        #region IMSASConnectionInfoProvider Members

        private string MakeConnectionString(string server, string database, string restOfConnectionString)
        {
            var sb = new StringBuilder("Provider=MSOLAP;");
            sb.Append("Data Source=\"");
            sb.Append(server.Trim('\'', '"'));
            sb.Append("\";");

            if (!string.IsNullOrEmpty(database))
            {
                sb.Append("Catalog=");
                sb.Append(database);
                sb.Append(";");
            }
            if (!string.IsNullOrEmpty(restOfConnectionString)) sb.Append(restOfConnectionString);
            return sb.ToString();
        }

        void IMOlapCube.GetCurrentStatus(out string server, out string database, out string cube)
        {
            server = "";
            database = "";
            var str = ConnectionString.Split(';');
            foreach (var s in str)
            {
                var s1 = s.ToLower().Trim();
                if (s1.StartsWith("data source"))
                {
                    var q = s.Split('=');
                    var sname = q[1].Trim(' ', '"', '\'');
                    server = sname;
                }
                if (s1.StartsWith("datasourcelocation"))
                {
                    var q = s.Split('=');
                    var sname = q[1].Trim(' ', '"', '\'');
                    server = sname;
                }
                if (s1.StartsWith("location"))
                {
                    var q = s.Split('=');
                    var sname = q[1].Trim(' ', '"', '\'');
                    server = sname;
                }
                if (s1.StartsWith("catalog"))
                {
                    var q = s.Split('=');
                    database = q[1].Trim(' ', '"', '\'');
                }
                if (s1.StartsWith("initial catalog"))
                {
                    var q = s.Split('=');
                    database = q[1].Trim(' ', '"', '\'');
                }
                if (s1.StartsWith("database"))
                {
                    var q = s.Split('=');
                    database = q[1].Trim(' ', '"', '\'');
                }
            }
            if (string.IsNullOrEmpty(database))
                try
                {
                    EnsureConnected();
                    database = FMDConnection.Database;
                }
                catch
                {
                    ;
                }

            cube = CubeName;
        }

        string[] IMOlapCube.GetServersList()
        {
            var servers = new List<string>();
            try
            {
                //DbProviderFactory factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
                //if (factory.CanCreateDataSourceEnumerator)
                //{
                //    foreach (DataRow row in factory.CreateDataSourceEnumerator().GetDataSources().Rows)
                //    {
                //        string sname = row["ServerName"].ToString();
                //        if (!servers.Contains(sname)) servers.Add(sname);
                //    }
                //}
                return servers.ToArray();
            }
            catch
            {
                return new string[0];
            }
        }

        bool IMOlapCube.GetDatabasesList(string server, string restOfConnectionString, out string result)
        {
            var _result = new StringBuilder();
            try
            {
                //AdomdConnection conn = new AdomdConnection(MakeConnectionString(server, null, restOfConnectionString));
                //conn.Open();
                //DataSet ds = conn.GetSchemaDataSet(AdomdSchemaGuid.Catalogs, null);
                //conn.Close();
                //foreach (DataRow dr in ds.Tables[0].Rows)
                //{
                //    _result.Append(dr.ItemArray[0].ToString());
                //    if (dr != ds.Tables[0].Rows[ds.Tables[0].Rows.Count - 1])
                //        _result.Append("|");
                //}
                result = _result.ToString();
                return true;
            }
            catch (Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        bool IMOlapCube.GetCubesList(string server, string database, string restOfConnectionString, out string result)
        {
            var _result = new StringBuilder();
            try
            {
                var conn = new XmlaConnection(MakeConnectionString(server, database, restOfConnectionString));
                conn.Open();
                foreach (var cube in conn.Cubes)
                    _result.Append(cube.Name);
                conn.Close();
                result = _result.ToString();
                return true;
            }
            catch (Exception e)
            {
                result = e.Message;
                return false;
            }
        }

        string IMOlapCube.Activate(string server, string database, string cube, string restOfConnectionString)
        {
            try
            {
                if (Active) Active = false;
                if (Connection.State == ConnectionState.Open)
                    Connection.Close();

                ConnectionString = MakeConnectionString(server, database, restOfConnectionString);
                CubeName = cube;
                Active = true;
                return "";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        void IMOlapCube.ExecuteMDX(string query, OlapControl grid)
        {
            ShowMDXQuery(grid, query);
        }

        #endregion
    }
}