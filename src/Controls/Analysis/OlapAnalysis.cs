using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.ClientAgents;
using RadarSoft.RadarCube.Controls.Chart;
using RadarSoft.RadarCube.Controls.Filter;
using RadarSoft.RadarCube.Controls.Menu;
using RadarSoft.RadarCube.Controls.PropertyGrid;
using RadarSoft.RadarCube.Controls.Toolbox;
using RadarSoft.RadarCube.Controls.Tree;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace RadarSoft.RadarCube.Controls.Analysis
{
    public class OlapAnalysis : OlapChart
    {
        private AnalysisType _analysisType = AnalysisType.Grid;

        private bool _delayPivoting;

        //public virtual string ExportController { get; set; }

        //public virtual string ExportAction { get; set; }

        internal bool _needInitialization;

        private string _SupportEMail = "";

        private Cube.RadarCube FCube;
        //public OlapAnalysis(string ID)
        //{
        //    if (ID.IsNullOrEmpty())
        //        throw new ArgumentException("OlapAnalysis.ID shouldn't be null or empty", "ID");
        //    this.ID = ID;
        //    fToolbox = new OlapToolbox();
        //    fFilter = new OlapFilters();
        //}

        public OlapAnalysis(HttpContext context, IHostingEnvironment hosting, IMemoryCache cache)
            : base(context, hosting, cache)
        {
            fToolbox = new OlapToolbox(context, hosting);
            fFilter = new OlapFilters(context, hosting);
        }

        public override AnalysisType AnalysisType
        {
            get => _analysisType;
            set
            {
                var oldValue = _analysisType;

                _analysisType = value;

                if (value != oldValue && Cube.Active)
                    UpdateCellSet();
            }
        }

        public override bool FilterAreaVisible { get; set; } = true;

        public override rsShowAreasOlapGrid ShowAreasMode { get; set; } = rsShowAreasOlapGrid.rsAll;

        public override bool ShowModificationAreas { get; set; } = true;

        public override bool ShowLegends { get; set; } = true;

        public override bool ShowToolbox { get; set; } = true;

        public override bool ShowFilterGrid { get; set; } = true;

        public override bool AllowScrolling { get; set; } = true;

        public override string StructureTreeWidth { get; set; } = "100%";

        public override string SupportEMail
        {
            get
            {
                if (_SupportEMail.IsNullOrEmpty())
                    return _RadarSoftSupport;

                return _SupportEMail;
            }
            set => _SupportEMail = value;
        }

        public override bool DelayPivoting
        {
            get => _delayPivoting;
            set
            {
                var oldValue = _delayPivoting;
                _delayPivoting = value;

                if (value != oldValue)
                    UpdateCellSet();
            }
        }

        public override bool UseFixedHeaders { get; set; } = true;

        internal override Cube.RadarCube Cube
        {
            get => FCube;
            set
            {
                if (FCube == value)
                    return;

                FCube = value;

                if (CellsetMode == CellsetMode.cmChart && FCube.Active)
                    FCube.ConvertParentChildToMultilevel();

                UpdateCubeStructures();
                FEngine = FCube.GetEngine(this);
            }
        }

        public string ExportController { get; set; }

        public string ExportAction { get; set; }

        public string CallbackController { get; set; }

        public string CallbackAction { get; set; }

        public bool IsSettingsEditable { get; set; } = true;

        internal bool IsCallback { get; set; }

        public event EventHandler InitOlap;

        internal void HandleInitOlap()
        {
            InitOlap(this, new EventArgs());
        }

        private string GetPropertySessionName(string propertyName)
        {
            return propertyName + "_" + ID;
        }

        public virtual void SetDefaultToolboxSettings()
        {
            Toolbox.SetDefaultSettings();
        }

        protected override void InitStore()
        {
            images = new StoredImagesProvider(this);
        }

        internal override string ImageUrl(string resName)
        {
            return images.ImageUrl(resName, typeof(OlapAnalysis), TempPath);
        }

        protected override void CreateChildControls()
        {
            FTree = new jQueryTree(Context, Hosting);
            mnu_control = new ContextMenu(Context, Hosting) {ID = "olapgrid_menu"};
            mnu_cf = new ContextMenu(Context, Hosting) {ID = "olapgrid_menuCF"};
        }

        protected void InitChildrenControls()
        {
            fFilter.Width = "100%";
            fFilter.Grid = this;
            fFilter.ID = "filter_" + ID;
            Toolbox.Width = "100%";
            Toolbox.ID = "toolbox_" + ID;
            Toolbox.OlapControl = this;
        }

        public FileStreamResult DoExport(string olapexportarg)
        {
            return ExecuteRequest(exportParam: olapexportarg);
        }


        public override void EnsureStateRestored()
        {
            Cube.RestoreCubeState();
            base.EnsureStateRestored();
        }

        //internal void RaisePostback(string args)
        //{
        //    if (HandlePivotCallback(args, null))
        //        return;

        //    Toolbox.RaisePostback(args);
        //}

        internal override void RaiseCallback(string eventArgument, string data)
        {
            if (callbackException != null) return;
            try
            {
                var args = eventArgument.Split('|');

                if (args[0] == "loading")
                {
                    callbackData = CallbackData.Loading;
                    return;
                }

                if (args[0] == "giveolapanalysis" && IsSettingsEditable)
                {
                    callbackData = CallbackData.Settings;
                    Settings = Settings.OlapAnalysis;
                    return;
                }

                if (args[0] == "saveolapanalysis" && IsSettingsEditable)
                {
                    var values = JsonConvert.DeserializeObject<OlapAnalysisValues>(data);
                    values.RootObject = this;
                    values.Write();
                    ApplyChanges();
                    callbackData = CallbackData.Toolbox;
                    postbackData = PostbackData.OlapGridContainer | PostbackData.FilterGrid;
                    _needInitialization = true;
                    return;
                }

                if (args[0] == "givecustombuttons" && IsSettingsEditable)
                {
                    callbackData = CallbackData.Settings;
                    Settings = Settings.CustomButtons;
                    return;
                }

                if (args[0] == "savecustombuttons" && IsSettingsEditable)
                {
                    var values = JsonConvert.DeserializeObject<CustomButtonValues[]>(data);
                    CustomButtons.Clear();
                    foreach (var val in values)
                    {
                        val.RootObject = new CustomToolboxButton();
                        val.Write();
                        CustomButtons.Add((CustomToolboxButton) val.RootObject);
                    }
                    ApplyChanges();
                    callbackData = CallbackData.Toolbox;
                    postbackData = PostbackData.Toolbox;
                    return;
                }

                if (args[0] == "givetoolboxbuttons" && IsSettingsEditable)
                {
                    callbackData = CallbackData.Settings;
                    Settings = Settings.ToolboxButtons;
                    return;
                }

                if (args[0] == "savetoolboxbuttons" && IsSettingsEditable)
                {
                    var values = JsonConvert.DeserializeObject<ToolboxButtonValues[]>(data);

                    ((OlapToolbox) Toolbox).SortToolItems(values.Select(x => x.ButtonID));

                    foreach (var val in values)
                    {
                        val.RootObject = Toolbox.fToolItems[val.ButtonID];
                        val.Write();
                    }

                    ApplyChanges();
                    callbackData = CallbackData.Toolbox;
                    postbackData = PostbackData.Toolbox;
                    return;
                }


                //if (args[0] == "cancelajax")
                //{
                //    List<string> ls = (List<string>)Session.("OLP_Cancelled");
                //    if (ls == null) ls = new List<string>();
                //    ls.Add(args[1]);
                //    Session["OLP_Cancelled"] = ls;
                //    callbackData = CallbackData.Nothing;
                //    return;
                //}

                if (args[0] == "changesn")
                {
                    callbackData = CallbackData.ResultString;
                    if (Toolbox.MDCube == null)
                    {
                        _CallbackResult = "e|Not connected to MOlapCube";
                        return;
                    }
                    Session.SetString(ClientID + "$server", args[1]);
                    string dbs;
                    if (!Toolbox.MDCube.GetDatabasesList(args[1], "", out dbs))
                    {
                        _CallbackResult =
                            "e|" + string.Format(Toolbox.ConnectButton.LoginWindowSettings.ErrorString, dbs);
                        return;
                    }
                    _CallbackResult = "s|" + dbs;
                    return;
                }

                if (args[0] == "changedb")
                {
                    callbackData = CallbackData.ResultString;
                    if (Toolbox.MDCube == null)
                    {
                        _CallbackResult = "e|Not connected to MOlapCube";
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
                        return;
                    }
                    _CallbackResult = "s|" + cubes;
                    return;
                }

                if (Toolbox.RaiseCallback(eventArgument))
                    return;

                if (HandleToolboxCallback(eventArgument))
                    return;

                if (callbackData == CallbackData.Nothing)
                    base.RaiseCallback(eventArgument, data);
            }
            catch (Exception E)
            {
                callbackException = E;
                callbackExceptionData = new Dictionary<string, string>(1);
                callbackExceptionData.Add("eventArgument", eventArgument);
            }

        }

        protected JsonResponse MakeLoadingResponse()
        {
            var res = new JsonResponse();
            var writer = new HtmlTextWriter();
            Render(writer);
            res.data = writer.ToString();

            JsonSettings settings = null;
            var jsonSettings = "";
            switch (AnalysisType)
            {
                case AnalysisType.Grid:
                    settings = new MvcJsonSettings(this);
                    settings.InitControlData(CellSet, this);
                    jsonSettings = JsonConvert.SerializeObject(settings);
                    break;
                case AnalysisType.Chart:
                    settings = new MvcChartJsonSettings(this);
                    settings.InitControlData(CellSet, this);
                    jsonSettings = JsonConvert.SerializeObject(settings);
                    break;
            }
            res.settings = settings;
            return res;
        }

        protected JsonResponse MakeSettingsResponse()
        {
            var title = "";
            var saveAction = "";
            //string innerHtml = "<div style=\"max-height: 400px; overflow-y: scroll\">";
            var innerHtml = "";
            var settings = new List<PropertyGridSettings>();
            var res = new JsonResponse();
            var dlg = new JsonDialog();
            var bts = new List<JsonDialogButton>();
            PropertyGridSettings setting = null;
            var settingsJson = "";
            switch (Settings)
            {
                case Settings.None:
                    break;
                case Settings.OlapAnalysis:
                    dlg.target = "options";
                    setting = new OlapAnalysisSettings();
                    setting.Initialize(this);
                    settings.Add(setting);
                    title = "Olap Analysis settings";
                    saveAction = "saveolapanalysis";
                    innerHtml += "<div id=\"rc_property_grid\"></div>";
                    break;
                case Settings.CustomButtons:
                    dlg.target = "custombuttons";
                    title = "Custom toolbox buttons settings";
                    saveAction = "savecustombuttons";
                    innerHtml += "<div id=\"rs-toolboxbuttons\">";
                    foreach (var button in CustomButtons)
                    {
                        setting = new CustomButtonSettings();
                        ((CustomButtonSettings) setting).CustomButton = button;
                        setting.Initialize(this);
                        setting.showButtons = false;
                        settings.Add(setting);
                        innerHtml += RenderPGButtonSettings("Button #" + button.ButtonID, button.ButtonID, true, true);
                    }
                    innerHtml += "</div>";
                    setting = new CustomButtonSettings();
                    ((CustomButtonSettings) setting).CustomButton = new CustomToolboxButton();
                    setting.Initialize(this);
                    settingsJson = JsonConvert.SerializeObject(setting);
                    bts.Add(new JsonDialogButton
                            {
                                text = "Add button",
                                code = "RadarSoft.$('#" + ClientID + "').data('grid').addCustomButton(this, '" +
                                       settingsJson + "');"
                            });
                    break;
                case Settings.ToolboxButtons:
                    dlg.target = "toolboxbuttons";
                    title = "Toolbox buttons settings";
                    saveAction = "savetoolboxbuttons";
                    innerHtml += "<div id=\"rs-toolboxbuttons\">";

                    foreach (var button in Toolbox.fToolItems.Values)
                    {
                        setting = new ToolboxButtonSettings();
                        ((ToolboxButtonSettings) setting).Button = button;
                        setting.Initialize(this);
                        setting.showButtons = false;
                        settings.Add(setting);
                        innerHtml += RenderPGButtonSettings(GetPropertyName(button), button.ButtonID, true, false);
                    }
                    innerHtml += "</div>";
                    break;
                default:
                    break;
            }
            //innerHtml += "</div>";
            dlg.title = title;
            dlg.data = innerHtml;
            dlg.json = settings.ToArray();
            bts.Add(new JsonDialogButton
                    {
                        text = RadarUtils.GetResStr("rsApply"),
                        code = "RadarSoft.$('#" + ClientID + "').data('grid').applySettings(this, '" + saveAction +
                               "'); " +
                               "RadarSoft.$(this).dialog('close');"
                    });
            bts.Add(new JsonDialogButton
                    {
                        text = RadarUtils.GetResStr("rsCancel"),
                        code = "RadarSoft.$(this).dialog('close')"
                    });
            dlg.buttons = bts.ToArray();
            res.dialog = dlg;
            InitClientMessage(res);
            return res;
        }

        private string GetPropertyName(object propertyObject)
        {
            var pName = "";

            var properties =
                typeof(OlapAnalysis).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            foreach (var p in properties)
            {
                var pObject = p.GetValue(this, null);
                if (pObject == propertyObject)
                {
                    pName = p.Name;
                    break;
                }
            }

            return pName;
        }

        private string RenderPGButtonSettings(string name, string id, bool groupable, bool deletable)
        {
            var innerHtml = "";
            if (groupable)
                innerHtml += "<div class=\"rs-tbbutton-group\">";
            innerHtml += "<h3>" + name;
            if (deletable)
            {
                innerHtml +=
                    "<div class=\"pg-button pg-button-delete ui-state-default ui-corner-all\" style=\"float:right\" title=\"Delete button\">";
                innerHtml += "<span class=\"ui-icon ui-icon-close\"></span>";
                innerHtml += "</div>";
            }
            innerHtml += "</h3>";
            innerHtml += "<div>";
            innerHtml += "<div id=\"rc_property_grid_" + id + "\"></div>";
            innerHtml += "</div>";
            if (groupable)
                innerHtml += "</div>";
            return innerHtml;
        }

        protected override void Render(HtmlTextWriter writer)
        {
            if (IsCallback)
                return;
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-widget");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
#if EVAL
            writer.Write("RadarCube ASP.NET MVC OLAP Analysis evaluation beta version is: " + typeof(OlapAnalysis).Assembly.GetName().Version.ToString() +
                ".");
#endif

            //#if EVAL
            //            writer.Write("RadarCube evaluation version. MVC OLAP Analysis version is: " + typeof(OlapGrid).Assembly.GetName().Version.ToString() + 
            //                ". Click <a href=\"http://www.radar-soft.com/buy.aspx\" target=\"_blank\">here</a> to purchase the full version.");
            //#endif
            writer.RenderEndTag(); //Div
            base.Render(writer);
            //Cube.RenderContents(writer);
        }

        /// <exclude />
        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            dynamic settings = null;
            var jsonSettings = "";
            switch (AnalysisType)
            {
                case AnalysisType.Grid:
                    settings = new MvcJsonSettings(this);
                    settings.InitControlData(CellSet, this);
                    jsonSettings = JsonConvert.SerializeObject(settings);
                    break;
                case AnalysisType.Chart:
                    settings = new MvcChartJsonSettings(this);
                    settings.InitControlData(CellSet, this);
                    jsonSettings = JsonConvert.SerializeObject(settings);
                    break;
            }
            writer.Write("<script type=\"text/javascript\">" +
                         "RadarSoft.$(document).ready(function () {" +
                         " var grid = new RadarSoft.NetCoreOlapAnalysis();" +
                         " var jsonSettings = '" + jsonSettings + "';" +
                         " var formattedJsonSettings = jsonSettings.replace(/\\\\/g, \"'\");" +
                         " grid.set_settings(RadarSoft.$.parseJSON(formattedJsonSettings));" +
                         " grid.initialize();" +
                         "});</script>");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_olapgrid ui-widget");
            base.RenderBeginTag(writer);
            if (ShowToolbox)
            {
                Toolbox.RenderControl(writer);
                writer.AddStyleAttribute("display", "none");
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_DLG_loadlayout_" + ID);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_dialog rc_loadlayout_dialog");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olaptlw_loadwin");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);

                var formHtml = "<form id = \"loadLayoutForm_" + ID + "\"" +
                               "' enctype=\"multipart/form-data\">" +
                               "<input type=\"hidden\" name=\"olapcallbackarg\" value=\"upload|_\">" +
                               "<input type=\"hidden\" name=\"context\" value=\"" + ID + "\">" +
                               "<input type=\"file\" name=\"layout\">" +
                               "</form>";
                writer.Write(formHtml);

                writer.RenderEndTag(); // div
                writer.RenderEndTag(); //div
            }
            if (IsSettingsEditable)
            {
                writer.AddStyleAttribute("display", "none");
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_DLG_loadsettings_" + ID);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_dialog rc_loadsettings_dialog");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olaptlw_loadwin");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);

                var formHtml = "<form id = \"loadSettingsForm_" + ID + "\"" +
                               "' enctype=\"multipart/form-data\">" +
                               "<input type=\"hidden\" name=\"context\" value=\"" + ID + "\">" +
                               "<input type=\"file\" name=\"file\">" +
                               "</form>";
                writer.Write(formHtml);

                writer.RenderEndTag(); // div
                writer.RenderEndTag(); //div
            }
            if (ShowFilterGrid)
                fFilter.RenderControl(writer);

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_olapgrid_container");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            writer.AddAttribute(HtmlTextWriterAttribute.Height, "100%");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_olapgrid_table ui-widget-content");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
        }

        //public override void RenderEndTag(HtmlTextWriter writer)
        //{
        //    base.RenderEndTag(writer);
        //    //writer.RenderEndTag(); //div
        //}

        protected void DoUnload()
        {
            if (heditor != null)
                heditor.Unload();

            Cube.DoUnload();
        }

        internal override void WriteStreamedObjectByDerivedClass(BinaryWriter writer)
        {
            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_MaxColumnsInGrid);
            StreamUtils.WriteInt32(writer, MaxColumnsInGrid);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_MaxRowsInGrid);
            StreamUtils.WriteInt32(writer, MaxRowsInGrid);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_LinesInPage);
            StreamUtils.WriteInt32(writer, LinesInPage);

            //StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_ClientLibPath);
            //StreamUtils.WriteString(writer, ClientLibPath);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_EmptyCellString);
            StreamUtils.WriteString(writer, EmptyCellString);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_MaxTextLength);
            StreamUtils.WriteInt32(writer, MaxTextLength);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_AnalysisType);
            StreamUtils.WriteInt32(writer, (int) _analysisType);

            if (!FilterAreaVisible)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_FilterAreaVisible);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_ShowAreasMode);
            StreamUtils.WriteInt32(writer, (int) ShowAreasMode);

            if (!UseCheckboxesInTree)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_UseCheckboxesInTree);

            if (!ShowModificationAreas)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_ShowModificationAreas);

            if (!ShowLegends)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_ShowLegends);

            if (!ShowToolbox)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_ShowToolbox);

            if (!ShowFilterGrid)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_ShowFilterGrid);

            if (!AllowScrolling)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_AllowScrolling);

            if (!AllowPaging)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_AllowPaging);

            if (!AllowDrilling)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_AllowDrilling);

            if (AllowEditing)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_AllowEditing);

            if (!AllowFiltering)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_AllowFiltering);

            if (AllowResizing)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_AllowResizing);

            if (AllowSelectionFormatting)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_AllowSelectionFormatting);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_Height);
            StreamUtils.WriteString(writer, Height);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_Width);
            StreamUtils.WriteString(writer, Width);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_StructureTreeWidth);
            StreamUtils.WriteString(writer, StructureTreeWidth);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_SupportEMail);
            StreamUtils.WriteString(writer, _SupportEMail);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_ClientCallbackFunction);
            StreamUtils.WriteString(writer, ClientCallbackFunction);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_ErrorHandler);
            StreamUtils.WriteString(writer, ErrorHandler);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_MessageHandler);
            StreamUtils.WriteString(writer, MessageHandler);

            if (DelayPivoting)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_DelayPivoting);

            if (!UseFixedHeaders)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_UseFixedHeaders);

            if (ExpandDimensionsNode)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_ExpandDimensionsNode);

            if (ExpandMeasuresNode)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_ExpandMeasuresNode);

            if (!HideDimensionNameIfPossible)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_HideDimensionNameIfPossible);

            if (DrillthroughExportType != ExportType.XLSX)
            {
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_DrillthroughExportType);
                StreamUtils.WriteInt32(writer, (int) DrillthroughExportType);
            }

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_CurrencyFormatString);
            StreamUtils.WriteString(writer, CurrencyFormatString);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_CallbackController);
            StreamUtils.WriteString(writer, CallbackController);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_CallbackAction);
            StreamUtils.WriteString(writer, CallbackAction);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_ExportController);
            StreamUtils.WriteString(writer, ExportController);

            StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_ExportAction);
            StreamUtils.WriteString(writer, ExportAction);

            if (!IsSettingsEditable)
                StreamUtils.WriteTag(writer, Tags.tgOLAPGrid_IsSettingsEditable);

            StreamUtils.WriteStreamedObject(writer, (OlapToolbox) Toolbox, Tags.tgOLAPGrid_Toolbox);

            StreamUtils.WriteStreamedObject(writer, HierarchyEditorStyle, Tags.tgOLAPGrid_HierarchyEditorStyle);

            base.WriteStreamedObjectByDerivedClass(writer);
        }

        internal override void ReadByDerivedClass(Tags tag, BinaryReader reader)
        {
            switch (tag)
            {
                case Tags.tgOLAPGrid_UseCheckboxesInTree:
                    UseCheckboxesInTree = false;
                    break;
                case Tags.tgOLAPGrid_MaxColumnsInGrid:
                    MaxColumnsInGrid = StreamUtils.ReadInt32(reader);
                    break;
                case Tags.tgOLAPGrid_MaxRowsInGrid:
                    MaxRowsInGrid = StreamUtils.ReadInt32(reader);
                    break;
                case Tags.tgOLAPGrid_LinesInPage:
                    LinesInPage = StreamUtils.ReadInt32(reader);
                    break;
                //case Tags.tgOLAPGrid_ClientLibPath:
                //    ClientLibPath = StreamUtils.ReadString(reader);
                //    break;
                case Tags.tgOLAPGrid_EmptyCellString:
                    EmptyCellString = StreamUtils.ReadString(reader);
                    break;
                case Tags.tgOLAPGrid_MaxTextLength:
                    MaxTextLength = StreamUtils.ReadInt32(reader);
                    break;
                case Tags.tgOLAPGrid_AnalysisType:
                    _analysisType = (AnalysisType) StreamUtils.ReadInt32(reader);
                    break;
                case Tags.tgOLAPGrid_FilterAreaVisible:
                    FilterAreaVisible = false;
                    break;
                case Tags.tgOLAPGrid_ShowAreasMode:
                    ShowAreasMode = (rsShowAreasOlapGrid) StreamUtils.ReadInt32(reader);
                    break;
                case Tags.tgOLAPGrid_DrillthroughExportType:
                    DrillthroughExportType = (ExportType) StreamUtils.ReadInt32(reader);
                    break;
                case Tags.tgOLAPGrid_ShowModificationAreas:
                    ShowModificationAreas = false;
                    break;
                case Tags.tgOLAPGrid_ShowLegends:
                    ShowLegends = false;
                    break;
                case Tags.tgOLAPGrid_IsSettingsEditable:
                    IsSettingsEditable = false;
                    break;
                case Tags.tgOLAPGrid_ShowToolbox:
                    ShowToolbox = false;
                    break;
                case Tags.tgOLAPGrid_ShowFilterGrid:
                    ShowFilterGrid = false;
                    break;
                case Tags.tgOLAPGrid_AllowScrolling:
                    AllowScrolling = false;
                    break;
                case Tags.tgOLAPGrid_AllowPaging:
                    AllowPaging = false;
                    break;
                case Tags.tgOLAPGrid_AllowDrilling:
                    AllowDrilling = false;
                    break;
                case Tags.tgOLAPGrid_AllowEditing:
                    AllowEditing = true;
                    break;
                case Tags.tgOLAPGrid_AllowFiltering:
                    AllowFiltering = false;
                    break;
                case Tags.tgOLAPGrid_AllowResizing:
                    AllowResizing = true;
                    break;
                case Tags.tgOLAPGrid_AllowSelectionFormatting:
                    AllowSelectionFormatting = true;
                    break;
                case Tags.tgOLAPGrid_ExpandDimensionsNode:
                    ExpandDimensionsNode = true;
                    break;
                case Tags.tgOLAPGrid_ExpandMeasuresNode:
                    ExpandMeasuresNode = true;
                    break;
                case Tags.tgOLAPGrid_HideDimensionNameIfPossible:
                    HideDimensionNameIfPossible = false;
                    break;
                case Tags.tgOLAPGrid_CurrencyFormatString:
                    CurrencyFormatString = StreamUtils.ReadString(reader);
                    break;
                case Tags.tgOLAPGrid_Height:
                    Height = StreamUtils.ReadString(reader);
                    break;
                case Tags.tgOLAPGrid_Width:
                    Width = StreamUtils.ReadString(reader);
                    break;
                case Tags.tgOLAPGrid_StructureTreeWidth:
                    StructureTreeWidth = StreamUtils.ReadString(reader);
                    break;
                case Tags.tgOLAPGrid_ClientCallbackFunction:
                    ClientCallbackFunction = StreamUtils.ReadString(reader);
                    break;
                case Tags.tgOLAPGrid_ErrorHandler:
                    ErrorHandler = StreamUtils.ReadString(reader);
                    break;
                case Tags.tgOLAPGrid_MessageHandler:
                    MessageHandler = StreamUtils.ReadString(reader);
                    break;
                case Tags.tgOLAPGrid_SupportEMail:
                    _SupportEMail = StreamUtils.ReadString(reader);
                    break;
                case Tags.tgOLAPGrid_DelayPivoting:
                    DelayPivoting = true;
                    break;
                case Tags.tgOLAPGrid_UseFixedHeaders:
                    UseFixedHeaders = false;
                    break;
                case Tags.tgOLAPGrid_CallbackController:
                    CallbackController = StreamUtils.ReadString(reader);
                    break;
                case Tags.tgOLAPGrid_CallbackAction:
                    CallbackAction = StreamUtils.ReadString(reader);
                    break;
                case Tags.tgOLAPGrid_ExportController:
                    ExportController = StreamUtils.ReadString(reader);
                    break;
                case Tags.tgOLAPGrid_ExportAction:
                    ExportAction = StreamUtils.ReadString(reader);
                    break;
                case Tags.tgOLAPGrid_Toolbox:
                    StreamUtils.ReadStreamedObject(reader, (OlapToolbox) Toolbox);
                    break;
                case Tags.tgOLAPGrid_HierarchyEditorStyle:
                    StreamUtils.ReadStreamedObject(reader, HierarchyEditorStyle);
                    break;
            }

            base.ReadByDerivedClass(tag, reader);
        }

        protected void PreExecuteRequest()
        {
            images.ExtractClientRsolapLibrary();

            Cube.CheckForNewVersions();

            EnsureStateRestored();

            CreateChildControls();
            InitChildrenControls();
        }

        internal dynamic ExecuteRequest(string callbackParam = "", string data = "", string exportParam = "", IFormFile fileupload = null)
        {
            PreExecuteRequest();
            
            var sessionTimeoutException = RadarUtils.GetResStr("rsSessionTimeoutException");

            if (callbackParam.IsFill() || fileupload != null)
            {
                if (!IsStored)
                    callbackException = new Exception(sessionTimeoutException);

                IsCallback = true;

                if (callbackParam.IsFill())
                    RaiseCallback(callbackParam, data);
                else if (fileupload != null)
                    LoadSettingsInner(fileupload);

                var gridRespose = MakeCallbackResponse();
                gridRespose.DoSerialize(this);
                DoUnload();

                if (callbackException == null)
                    InitSessionData();

                return gridRespose;
            }

            if (exportParam.IsFill())
            {
                if (!IsStored)
                    callbackException = new Exception(sessionTimeoutException);

                return Toolbox.DoExport(exportParam);
            }



            if (InitOlap != null)
            {
                var delaypivot = DelayPivoting;
                DelayPivoting = false;
                HandleInitOlap();
                DelayPivoting = delaypivot;
            }


            if (callbackException == null)
                InitSessionData();

            if (fSeparatedTree == null)
                FillTree();

            var htmlWriter = new HtmlTextWriter();
            Render(htmlWriter);
            DoUnload();
            return new HtmlString(htmlWriter.ToString());
        }

        internal override JsonResponse MakeCallbackResponse()
        {
            var response = new JsonResponse();
            if (callbackException != null)
            {
                if (!IsStored)
                {
                    response.exception = SessionTimeoutDialog.RenderMassage(this, callbackException);
                }
                else
                {
                    response.exception = HtmlExceptionDialog.RenderException(Cube, this, callbackException);

                    if (string.IsNullOrEmpty(ErrorHandler) == false)
                    {
                        response.errorClientData = new ErrorData(callbackException, this);
                        response.errorHandler = ErrorHandler;
                    }
                }

                return response;
            }
            try
            {
                //if (Page.Session["CurrentRequestCode"] != ViewState["CurrentRequestCode"]) return "";

                if (callbackData == CallbackData.ClientError)
                {
                    response.target = "#ERROR#";
                    response.data = _callbackClientErrorString;
                    return response;
                }
                if (callbackData == CallbackData.Nothing)
                {
                    InitClientMessage(response);
                    return response;
                }

                HandleOnCallback();

                var writer = new HtmlTextWriter();
                if (callbackData == CallbackData.HierarchyEditor)
                {
                    response.dialog = heditor.Render();
                    SessionState.Write(heditor, SessionKey.olapgrid_heditor, UniqueID);
                    InitClientMessage(response);
                    return response;
                }

                if (callbackData == CallbackData.HierarchyEditorTree)
                {
                    heditor.DoRenderTree(writer);
                    SessionState.Write(heditor, SessionKey.olapgrid_heditor, UniqueID);
                    response.data = writer.ToString();
                    InitClientMessage(response);
                    return response;
                }

                if (callbackData == CallbackData.Data)
                {
                    RenderInternalGrid(writer);
                    InitSessionData();
                    response.datagrid = writer.ToString();
                    response.InitControlData(CellSet, this);
                    InitClientMessage(response);
                    return response;
                }

                if (callbackData == CallbackData.PivotAndData
                    || callbackData == CallbackData.CubeTree)
                {
                    RenderInternalGrid(writer);
                    response.datagrid = writer.ToString();
                    writer = new HtmlTextWriter();
                    RenderPivot(writer);
                    InitSessionData();

                    response.pivot = writer.ToString();

                    writer = new HtmlTextWriter();
                    RenderModifiers(writer);
                    response.modifiersarea = writer.ToString();

                    if (fFilter != null)
                    {
                        writer = new HtmlTextWriter();
                        fFilter.DoRenderContents(writer);
                        response.filtergrid = writer.ToString();
                    }

                    if (callbackData == CallbackData.CubeTree)
                    {
                        writer = new HtmlTextWriter();
                        FillTree();
                        _FTree.RenderControl(writer);
                        response.treearea = writer.ToString();
                    }

                    response.InitControlData(CellSet, this);
                    InitClientMessage(response);
                    return response;
                }
                if (callbackData == CallbackData.Popup)
                {
                    mnu_control.Items.Clear();
                    if (FMode == OlapGridMode.gmStandard)
                        if (!string.IsNullOrEmpty(uid))
                        {
                            IDescriptionable dim = Dimensions.FindHierarchy(uid);
                            if (dim == null)
                                dim = Measures.Find(uid);
                            if (dim == null) return response;
                            MakePivotMenu(dim);

                            HandleOnShowContextMenu(null, dim, mnu_control);
                        }
                        else if (!string.IsNullOrEmpty(legendId))
                        {
                            MakeLegendMenu(legendId);
                            HandleOnShowContextMenu(null, null, mnu_control);
                        }

                        else
                        {
                            var c = CellSet.Cells(icol, irow);
                            MakeMenu(c);

                            HandleOnShowContextMenu(c, null, mnu_control);
                        }
                    if (mnu_control.Items.Count == 0)
                        return response;

                    foreach (var MI in mnu_control.Items)
                        UpdateMenuRecursive(MI);
                    mnu_control.RenderControl(writer);
                    response.data = writer.ToString();

                    response.InitControlData(CellSet, this);
                    InitClientMessage(response);
                    return response;
                }
                if (callbackData == CallbackData.FilterSettings)
                {
                    response.dialog = _filterconditiondlg;
                    InitClientMessage(response);
                    return response;
                }
                if (callbackData == CallbackData.ResultString)
                {
                    response.resultString = _CallbackResult;
                    return response;
                }

                if (callbackData == CallbackData.Loading)
                    return MakeLoadingResponse();

                if (callbackData == CallbackData.Settings)
                    return MakeSettingsResponse();

                if (callbackData == CallbackData.Toolbox)
                {
                    if (_needInitialization)
                    {
                        JsonSettings settings = null;
                        switch (AnalysisType)
                        {
                            case AnalysisType.Grid:
                                settings = new MvcJsonSettings(this);
                                settings.InitControlData(CellSet, this);
                                break;
                            case AnalysisType.Chart:
                                settings = new MvcChartJsonSettings(this);
                                settings.InitControlData(CellSet, this);
                                break;
                        }
                        response.settings = settings;
                    }


                    if (ShowToolbox)
                        Toolbox.RenderButtons(writer);
                    response.toolbox = writer.ToString();

                    if (fFilter != null && postbackData.HasFlag(PostbackData.FilterGrid))
                    {
                        writer = new HtmlTextWriter();
                        if (ShowFilterGrid)
                            fFilter.DoRenderContents(writer);
                        response.filtergrid = writer.ToString();
                    }

                    if (postbackData.HasFlag(PostbackData.OlapGridContainer))
                    {
                        writer = new HtmlTextWriter();
                        FillTree();
                        RenderContents(writer);
                        response.olapgridcontainer = writer.ToString();
                        return response;
                    }

                    if (postbackData.HasFlag(PostbackData.Data))
                    {
                        writer = new HtmlTextWriter();
                        RenderInternalGrid(writer);
                        response.datagrid = writer.ToString();
                    }

                    if (postbackData.HasFlag(PostbackData.PivotArea))
                    {
                        writer = new HtmlTextWriter();
                        RenderPivot(writer);
                        response.pivot = writer.ToString();
                    }

                    if (postbackData.HasFlag(PostbackData.Modificators))
                    {
                        writer = new HtmlTextWriter();
                        RenderModifiers(writer);
                        response.modifiersarea = writer.ToString();
                    }

                    if (postbackData.HasFlag(PostbackData.CubeTree))
                    {
                        writer = new HtmlTextWriter();
                        FillTree();
                        _FTree.RenderControl(writer);
                        response.treearea = writer.ToString();
                    }

                    return response;
                }
            }
            catch (Exception E)
            {
                response.exception = HtmlExceptionDialog.RenderException(Cube, this, E);

                if (string.IsNullOrEmpty(ErrorHandler) == false)
                {
                    response.errorClientData = new ErrorData(E, this);
                    response.errorHandler = ErrorHandler;
                }
            }

            return response;
        }

        public object LoadSettings(IFormFile file)
        {
            return ExecuteRequest(fileupload: file);
        }

        internal void LoadSettingsInner(IFormFile file)
        {
            try
            {

                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    Load(stream);
                    _needInitialization = true;
                    callbackData = CallbackData.Toolbox;
                    postbackData = PostbackData.OlapGridContainer | PostbackData.FilterGrid;
                    CellSet.Rebuild();
                    ApplyChanges();
                }
            }
            catch (Exception E)
            {
                callbackException = E;
            }
        }


        public object DoCallback(string callbackParam, string data)
        {
            return ExecuteRequest(callbackParam: callbackParam, data: data);
        }

        public virtual HtmlString Render()
        {
            return ExecuteRequest();
        }

        public virtual JsonResponse AjaxLoading()
        {
            PreExecuteRequest();

            if (InitOlap != null)
            {
                var delaypivot = DelayPivoting;
                DelayPivoting = false;
                InitOlap(this, new EventArgs());
                DelayPivoting = delaypivot;
            }


            callbackData = CallbackData.Loading;

            if (fSeparatedTree == null)
                FillTree();

            var gridRespose = MakeCallbackResponse();
            gridRespose.DoSerialize(this);

            if (callbackException == null)
                InitSessionData();

            DoUnload();
            return gridRespose;
        }
    }
}