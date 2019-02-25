using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Events;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.State;
using RadarSoft.RadarCube.Tools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace RadarSoft.RadarCube.Controls.Cube
{
    /// <summary>
    ///     An abstract ancestor class for components, the data sources, for
    ///     OlapGrid.
    /// </summary>
    /// <remarks>
    ///     <remarks>
    ///         <para>
    ///             Never create an instance of this class. Instead create instances of the
    ///             successor classes:
    ///         </para>
    ///         <list type="bullet">
    ///             <item>
    ///                 <see cref="TOLAPCube">TOLAPCube</see> for direct access to database tables
    ///                 that store the information for the OLAP cube (Desktop OLAP).
    ///             </item>
    ///             <item>
    ///                 <see cref="OlapCube">MOlapCube</see> for access maintained by the MS Analysis
    ///                 server of versions 2000 or 2005 (OLAP client to MS AS) to the already
    ///                 existing OLAP cubes.
    ///             </item>
    ///         </list>
    ///     </remarks>
    public abstract class RadarCube : WebControl, IStreamedObject
    {
        protected bool IsMvc { get; set; }

        /// <summary>
        ///     http://www.radar-soft.com/
        /// </summary>
        internal const string __RADARSOFT = @"http://www.radar-soft.com/";

        /// <summary>
        ///     http://www.radar-soft.com/Styles/Style.css
        /// </summary>
        internal const string __RADARSOFT_CSS = @"http://www.radar-soft.com/Styles/Style.css";

        /// <summary>
        ///     https://secure.radar-soft.com
        /// </summary>
        internal const string __SECURE_RADARSOFT = @"https://secure.radar-soft.com";

        internal const string __RADARSOFT_DOWNLOAD = @"https://www.radar-soft.com/products/radarcube-asp-net-mvc/demo";

        /// <summary>
        ///     http://www.radar-soft.com/support/GetDescriptions.aspx?product={0}&versionFrom={1}&versionTo={2}
        /// </summary>
        internal const string __RADARSOFT_SUPPORT_DESCRIPTION =
            @"http://www.radar-soft.com/support/GetDescriptions.aspx?product={0}&versionFrom={1}&versionTo={2}";

        /// <summary>
        ///     https://radar-soft.com/api/lastversion/{ID}
        ///     ID  |   Product Name
        ///     ----|------------------------------
        ///     534 |	ASP.NET Web Forms RadarCube
        ///     539 |	ASP.NET MVC RadarCube
        ///     xxx |	ASP.NET Silverlight RadarCube
        ///     541 |	Windows Forms RadarCube
        ///     544 |	WPF RadarCube
        ///     545 |	VCL HierCube
        ///     546 |	VCL RadarCube
        /// </summary>
        /// <summary>
        ///     http://www.radar-soft.com/support/redirect.aspx?way={0}
        /// </summary>
        internal const string __RADARSOFT_SUPPORT_REDIRECT = @"http://www.radar-soft.com/support/redirect.aspx?way={0}";

        /// <summary>
        ///     http://www.radar-soft.com/products/RadarCubeWPF.xml
        /// </summary>
        internal const string __RADARSOFT_WPF_XML = @"http://www.radar-soft.com/products/RadarCubeWPF.xml";

        internal static int VersionNumber => GetVersionNumber();

        private static int GetVersionNumber()
        {
            var v = typeof(RadarCube).GetTypeInfo().Assembly.GetName().Version;
#if DEBUG
            if (v.Major > 9)
                throw new ArgumentOutOfRangeException("Major");
            if (v.Minor > 99)
                throw new ArgumentOutOfRangeException("Minor");
            if (v.Build > 9)
                throw new ArgumentOutOfRangeException("Build");
            if (v.Revision > 9)
                throw new ArgumentOutOfRangeException("Revision");
#endif
            return GetVersionAsNumber(v);
        }

        internal static int GetVersionAsNumber(Version v)
        {
            return v.Major * 10000 + v.Minor * 100 + v.Build * 10 + v.Revision * 1;
        }

        protected TimeSpan _CacheMinTimeout = TimeSpan.Zero;

        /// <summary>
        ///     Gets the minimum data hold time in the cache.
        /// </summary>
        internal virtual TimeSpan CacheMinTimeout => _CacheMinTimeout;

        /// <summary>
        ///     for report use
        /// </summary>
        [Browsable(false)]
        [DefaultValue(false)]
        public bool RunTimeMode { get; set; }

        //#if TIMELIMITED
        //internal static DateTime _ExpireDate = new DateTime(2009, 12, 13);
        //#endif
        internal static int fVersionNumber = 111;

        internal Dictionary<string, Dictionary<int, int>> MemberPairs = new Dictionary<string, Dictionary<int, int>>();

        internal virtual void SetAdditionalActive(bool value)
        {
        }


        private string _TempPath;

        /// <summary>Defines a virtual path to the Temp folder.</summary>
        [Category("Behavior")]
        [DefaultValue("")]
        [Description("Defines a virtual path to the Temp folder.")]
        public string TempPath
        {
            get
            {
                if (_TempPath == null)
                    return string.Empty;

                return _TempPath;
            }
            set => _TempPath = value;
        }

        //This property can be used for setting of MvcMySessionState.WorkingDirectoryNamed 
        //to change directory of the OLAP control storage.
        [Browsable(false)]
        public string WorkingDirectoryForTest { get; set; }

        internal abstract string WorkingDirectory { get; }

        internal void PrepareWorkingDirectory()
        {
            TempDirectory.ClearExpiredSessionsData(Path.Combine(WorkingDirectory, SessionState.SessionDirName),
                Convert.ToInt32(CacheMinTimeout.TotalMinutes));
            TempDirectory.ClearExpiredSessionsData(WorkingDirectory, Convert.ToInt32(CacheMinTimeout.TotalMinutes));
        }

        internal virtual void DoPreLoad()
        {
        }

        protected SessionState fSessionState;

        internal SessionState SessionState
        {
            get
            {
                if (fSessionState == null)
                    InitSession(this);
                return fSessionState;
            }
        }

        protected virtual void InitSession(RadarCube cube)
        {
            fSessionState = new SessionState(cube);
        }

        internal CubeMeasures frcMeasures;
        internal CubeDimensions frcDimensions;

        internal virtual bool StreamVersionSupported(int Version)
        {
            return true;
        }

        internal abstract string GetProductID();
        internal abstract void RestoreQueryResult(OlapControl grid);

        internal virtual void RestoreQueryData(int index, out object Value, out CellFormattingProperties Formatting)
        {
            Value = null;
            Formatting = new CellFormattingProperties();
        }

        internal abstract void InitSessionData(OlapControl grid);

        internal virtual void RestoreCubeState()
        {
        }

        protected internal abstract bool RestoreCube();

        //internal TAggregateEvent fOnAggregate;

        internal Engine.Engine GetEngine(OlapControl Grid)
        {
            var Result = CreateEngine(Grid);
            FEngineList.Add(Result);
            if (fCallbackException != null) Grid.callbackException = fCallbackException;
            return Result;
        }

        internal List<WeakReference> engineList;

        private static Engine.Engine[] EmptyEngines
        {
            get
            {
                if (emptyEngines == null)
                    emptyEngines = new Engine.Engine[0];
                return emptyEngines;
            }
        }

        internal Engine.Engine[] GetEngineArray()
        {
            if (engineList == null || engineList.Count <= 0)
                return EmptyEngines;

            lock (engineList)
            {
                var array = new Engine.Engine[engineList.Count];
                if (array.Length <= 0)
                    return EmptyEngines;
                var index = array.Length;
                while (index > 0)
                {
                    index--;
                    array[index] = (Engine.Engine) engineList[index].Target;
                    if (engineList[index].Target == null || !engineList[index].IsAlive)
                        engineList.RemoveAt(index);
                }
                if (array.Length != engineList.Count)
                {
                    if (engineList.Count <= 0)
                        return EmptyEngines;
                    var oldArray = array;
                    array = new Engine.Engine[engineList.Count];
                    for (var i = 0; i < array.Length; i++)
                        array[i] = (Engine.Engine) engineList[i].Target;
                    GC.KeepAlive(oldArray);
                }
                return array;
            }
        }

        internal bool FActive { get; set; }

        internal List<Engine.Engine> FEngineList = new List<Engine.Engine>();

        private static Engine.Engine[] emptyEngines;

        internal List<CubeLevel> FLevelsList = new List<CubeLevel>();

        internal virtual bool IsFilterAllowed(CellSet.Filter filter)
        {
            return false;
        }

        internal void RegisterLevel(CubeLevel Level)
        {
            if (Level.ID >= 0)
                return;

            Level.fID = FLevelsList.Count;
            FLevelsList.Add(Level);
        }

        protected abstract Engine.Engine CreateEngine(OlapControl Grid);

        protected internal virtual void RetrieveLevels(CubeHierarchy AHierarchy, OlapControl grid)
        {
        }

        internal virtual void RetrieveAscendants(Hierarchy hierarchy, IEnumerable<string> members)
        {
        }

        internal virtual void RetrieveDescendants(OlapControl grid, List<CubeMember> members, CubeLevel level)
        {
        }

        internal virtual void CheckAreLeaves(List<Member> members)
        {
            foreach (var m in members)
            {
                var M = m.CubeMember;
                if (M != null)
                    M.fIsLeaf = M.Children.Count + M.NextLevelChildren.Count == 0;
            }
        }

        protected internal virtual void RetrieveMetadata()
        {
        }

        internal abstract int RetrieveMembersCount(OlapControl grid, object source);

        internal virtual object RetrieveInfoAttribute(CubeMember m, string attrubuteName)
        {
            return null;
        }

        internal virtual int[] RetrieveMembersCount(OlapControl grid, Member[] members)
        {
            var Result = new int[members.Length];
            for (var i = 0; i < members.Length; i++)
                Result[i] = RetrieveMembersCount(grid, members[i]);
            return Result;
        }

        internal virtual void RetrieveMembersCount3(OlapControl grid, ICollection<CubeMember> members)
        {
        }

        protected internal void SetCubeMemberParent(CubeLevel L, string MemberUniqueName,
            string ParentUniqueName)
        {
            var CubeMember = L.FindMemberByUniqueName(MemberUniqueName);
            var M = L.FindMemberByUniqueName(ParentUniqueName);
            if (M == null) return;
            M.Children.Add(CubeMember);
            CubeMember.FParent = M;
            if (M.NextLevelChildren.Count > 0 && M.FChildren.Count > 0)
                throw new Exception(string.Format(RadarUtils.GetResStr("rsHierarchyNotFlat"), M.fDisplayName));
        }

        internal abstract void ConvertParentChildToMultilevel();

        internal bool HasChartConnection
        {
            get
            {
                foreach (var e in FEngineList)
                    if (e.FGrid.CellsetMode == CellsetMode.cmChart) return true;

                return false;
            }
        }

        protected internal void SetCubeMemberParent(CubeMember CubeMember,
            CubeMember Parent)
        {
            Parent.Children.Add(CubeMember);
            CubeMember.FParent = Parent;
            if (Parent.NextLevelChildren.Count > 0 && Parent.FChildren.Count > 0)
                throw new Exception(string.Format(RadarUtils.GetResStr("rsHierarchyNotFlat"), Parent.fDisplayName));
        }

        internal abstract void RetrieveMembers(OlapControl grid, object source);

        internal abstract void RetrieveMembersPartial(OlapControl grid, object source, int from, int count,
            List<string> list, out bool hasNewMembers);

        internal abstract void RetrieveMembersFiltered(OlapControl grid, Hierarchy hierarchy, string filter,
            List<string> list, out bool hasNewMembers, bool exactMatching, bool VisibleOnly);

        internal virtual bool HasMemberChildren(Member m)
        {
            return m.Children.Count > 0;
        }

        internal virtual List<CubeAction> RetrieveActions(IDataCell cell)
        {
            return new List<CubeAction>();
        }

        internal virtual List<CubeAction> RetrieveActions(IMemberCell cell)
        {
            return new List<CubeAction>();
        }

        internal virtual List<CubeAction> RetrieveChartCellActions(ICubeAddress Address,
            Measure XMeasure, Measure YMeasure)
        {
            return new List<CubeAction>();
        }

        internal abstract string PublicName { get; }

        internal void DoRetrieveMembers3(OlapControl grid, CubeLevel level)
        {
            var h = level.Hierarchy;
            var idx = h.Levels.IndexOf(level);
            for (var i = 0; i <= idx; i++)
            {
                level = h.Levels[i];
                if (level.FMembersCount < 0)
                    RetrieveMembersCount(grid, level);
                if (!level.IsFullyFetched)
                {
                    bool dummy;
                    RetrieveMembersPartial(grid, level, 0, -1, null, out dummy);
                }
            }
        }

        internal void CheckForNewVersions()
        {
        }

        internal abstract void ShowCurrentHint(out string CurrentHint, out string CurrentHintLink,
            out string CurrentTip, out string CurrntTipLink);

        internal void SetCubeMemberParent(CubeHierarchy H, int LIndex, int PLIndex,
            string MemberUniqueName, string ParentUniqueName)
        {
            if (LIndex == 0) return;
            var CubeMember = H.Levels[LIndex].FindMemberByUniqueName(MemberUniqueName);
            CubeMember M;
            if (LIndex - PLIndex == 1)
            {
                M = H.Levels[PLIndex].FindMemberByUniqueName(ParentUniqueName);
                if (M == null) return;
            }
            else
            {
                H.Levels[LIndex - 1].FUniqueNamesArray.TryGetValue(H.Levels[LIndex - 1].FUniqueName
                                                                   + ".Ragged", out M);
                if (M == null)
                {
                    M = new CubeMember(H, H.Levels[LIndex - 1], "", "", H.Levels[LIndex - 1].FUniqueName + ".Ragged",
                        "", true, "");

                    H.Levels[LIndex - 1].Members.Add(M);
                    if (LIndex > 2) SetCubeMemberParent(H, LIndex - 1, PLIndex, M.UniqueName, ParentUniqueName);
                }
            }
            CubeMember.FParent = M;
            if (M != null)
            {
                M.NextLevelChildren.Add(CubeMember);
                if (M.NextLevelChildren.Count > 0 && M.FChildren.Count > 0)
                    throw new Exception(string.Format(RadarUtils.GetResStr("rsHierarchyNotFlat"), M.fDisplayName));
            }
        }

        internal void MapChanged()
        {
            DebugLogging.WriteLine("RadarCube.MapChanged()");
            foreach (var e in FEngineList)
                if (e.FGrid != null)
                    e.FGrid.UpdateCubeStructures();
        }

        internal void RebuildAllGrids()
        {
            DebugLogging.WriteLine("RadarCube.RebuildAllGrids()");
            foreach (var e in FEngineList)
                e.FGrid.DoReconnect();
        }

        internal void ClearMembers()
        {
            frcDimensions.ClearMembers();
            FLevelsList.Clear();
        }

        /// <summary>Clears the temporary directory created for the Cube to operate.</summary>
        public virtual void ClearTempDirectory()
        {
            if (RunTimeMode)
                TempDirectory.SafeDirectoryDelete(WorkingDirectory);
            else
                TempDirectory.SafeDirectoryDelete(SessionState.WorkingDirectoryName);
        }

        /// <summary>
        ///     The list of cube measures.
        /// </summary>
        /// <seealso cref="TCubeMeasures">CubeMeasures Class</seealso>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual CubeMeasures Measures => frcMeasures;

        /// <summary>
        ///     The list of cube dimensions.
        /// </summary>
        /// <seealso cref="TCubeDimensions">CubeDimensions Class</seealso>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual CubeDimensions Dimensions => frcDimensions;

        protected internal virtual void DoUnload()
        {
        }

        internal Exception fCallbackException;
        internal bool _initCompleted = false;


        /// <summary>Indicates whether the Cube is active at the moment.</summary>
        /// <remarks>
        ///     <para>
        ///         Set Active to <em>True</em> to activate the cube. It can be done both in
        ///         DesignTime and in Runtime.
        ///     </para>
        ///     <para>
        ///         It should be pointed out that data is fetched in the PreLoad event, thus you
        ///         cannot reach the Cube data in the Init page event handler.
        ///     </para>
        ///     <para>
        ///         If fetching data takes up too much time, you can activate the Cube in the
        ///         "deferred" mode. In this case you'll see a web page with an empty Cube while data
        ///         fetching is initiated by an AJAX-query. See the ActivateAjax method for the
        ///         details.
        ///     </para>
        /// </remarks>
        [Category("Data")]
        [DefaultValue(false)]
        [Description("Indicates whether the cube is active at the moment.")]
        public virtual bool Active
        {
            get => FActive;
            set => InternalSetActive(value);
        }

        protected internal virtual void InternalSetActive(bool value)
        {
            FActive = value;

            foreach (var e in FEngineList)
                e.SetActive(value);
            if (!value)
            {
                FLevelsList.Clear();
                frcDimensions.ClearMembers();
                FLevelsList.Clear();
                MemberPairs.Clear();
            }
        }

        public RadarCube(HttpContext context, IHostingEnvironment hosting, IMemoryCache cache) :
            base(context, hosting, cache)
        {
            frcDimensions = new CubeDimensions(this);
            frcMeasures = new CubeMeasures(this);
        }

        internal virtual void _DoWriteStream(BinaryWriter writer, object options)
        {
        }

        internal virtual void _DoReadStream(BinaryReader reader, object options)
        {
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            _DoWriteStream(writer, options);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            _DoReadStream(reader, options);
        }

        #endregion

        //private bool _fShowProgress = true;

        //internal bool ShowProgress
        //{

        //    get { return _fShowProgress; }
        //    set { _fShowProgress = value; }
        //}
        public static string SubstitutePathToConnectionString(string connectionString, string directory)
        {
            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(directory))
                return connectionString;
            var dataSourceMatch =
                new Regex(@"Data\ Source\=[^\;]*").Match(connectionString);
            if (dataSourceMatch.Success && dataSourceMatch.Length > "Data Source=".Length)
            {
                if (!directory.EndsWith("\\"))
                    directory += "\\";
                var file = dataSourceMatch.Value.Substring("Data Source=".Length);
                var fileDir = string.Empty;
                try
                {
                    fileDir = Path.GetDirectoryName(file);
                }
                catch
                {
                    fileDir = string.Empty;
                }
                if (!string.IsNullOrEmpty(file) && string.IsNullOrEmpty(fileDir) &&
                    !file.Contains('"') && !file.Contains('\\') && !file.Contains('/') &&
                    File.Exists(directory + file))
                    connectionString =
                        connectionString.Substring(0, dataSourceMatch.Index) +
                        "Data Source=" + directory + file +
                        connectionString.Substring(dataSourceMatch.Index + dataSourceMatch.Length);
            }
            return connectionString;
            //
            //DataConnectionException dataConnectionException = e as DataConnectionException;
            //if (dataConnectionException != null)
            //{
            //    string oldCS = dataConnectionException.DataConnection.ConnectionString;
            //    try
            //    {
            //        string[] connect = dataConnectionException.DataConnection.ConnectionString.Split(';');
            //        string provider = connect[0];
            //        string fullname = connect[1].Split('=')[1];
            //        string filename = fullname;
            //        if (fullname.Contains('\\'))
            //            filename = fullname.Split('\\')[fullname.Split('\\').Count() - 1];
            //        string newvaluename = MacrosesBuilder.CurrentAssemblyPath + "\\" + filename;
            //        if (File.Exists(newvaluename) == false)
            //            throw new FileNotFoundException(newvaluename);
            //        string ConnectionString = provider + ";" + connect[1].Split('=')[0] + "=" + newvaluename;
            //        DataConnection dc = dataConnectionException.DataConnection;
            //        dc.ConnectionString = ConnectionString;
            //        dc.Open();
            //        // restored ConnectionString                    
            //    }
            //    catch (Exception e1)
            //    {
            //        Xml.OnXmlErrorSecond(e1);
            //    }
            //    finally
            //    {
            //        dataConnectionException.DataConnection.ConnectionString = oldCS;
            //    }
            //}
            //
        }

        private readonly Updater _UpdaterInitialize = new Updater();

        /// <summary>
        ///     Updater.IsBusy
        /// </summary>
        internal bool IsInitBusy => _UpdaterInitialize.IsBusy;

        public virtual string ConnectionString { get; set; }
        public virtual string CubeName { get; set; }

        public event EventHandler Disposed;

        public event EventHandler<ExceptionHandlerEventArs> Error;

        /// <summary>
        ///     Raises the <see cref="TRadarCube.Error">Error</see> event.
        /// </summary>
        protected internal virtual bool OnError(Exception ex)
        {
            DebugLogging.WriteLineException("RadarCube.OnError:", ex);
            var e = new ExceptionHandlerEventArs(ex, null);
            if (Error != null)
                Error(this, e);

            return e.Handled;
        }

        internal void ErrorCatchHandler(Exception ex)
        {
            var handled = OnError(ex);
            //this.FUpdateCounter = 0;
            if (!handled)
                fCallbackException = ex;
        }
    }
}