using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RadarSoft.RadarCube.ClientAgents;
using RadarSoft.RadarCube.Controls.Analysis;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Tools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    public abstract class OlapToolboxBase : WebControl
    {
        internal OlapToolboxBase(HttpContext context, IHostingEnvironment hosting):
            base(context, hosting)
        {

        }

        protected string callbackResult;

        protected AddCalculatedMeasureButton fAddCalcMeasureButton;

        protected AllAreasToolboxButton fAllAreasButton;

        protected ClearLayoutToolboxButton fClearLayoutButton;

        protected ConnectToolboxButton fConnectButton;


        protected DataAreaToolboxButton fDataAreaButton;

        protected DelayPivotingButton fDelayPivotingButton;

        protected LoadLayoutButton fLoadLayoutButton;

        protected MDXQueryButton fMDXQueryButton;

        protected MeasurePlaceToolboxButton fMeasurePlaceButton;

        protected ModeButton FModeButton;

        protected PivotAreaToolboxButton fPivotAreaButton;

        protected ScaleResetButton fResetZoomButton;

        protected ResizingButton fResizingButton;

        protected SaveLayoutToolboxButton fSaveLayoutButton;

        internal Dictionary<string, CommonToolboxButton> fToolItems = new Dictionary<string, CommonToolboxButton>();

        protected ScaleIncreaseButton fZoomInButton;

        protected ScaleDecreaseButton fZoomOutButton;

        /// <summary>
        ///     Contains all necessary properties for "Connect" toolbox button
        /// </summary>
        public virtual ConnectToolboxButton ConnectButton => fConnectButton;

        /// <summary>
        ///     Contains all necessary properties for "Save layout" toolbox button
        /// </summary>
        public virtual SaveLayoutToolboxButton SaveLayoutButton => fSaveLayoutButton;

        /// <summary>
        ///     Contains all necessary properties for "Load layout" toolbox button
        /// </summary>
        public virtual LoadLayoutButton LoadLayoutButton => fLoadLayoutButton;

        /// <summary>
        ///     Contains all necessary properties for "MDX Query" toolbox button
        /// </summary>
        public virtual MDXQueryButton MDXQueryButton => fMDXQueryButton;

        /// <summary>
        ///     Contains all necessary properties for "Add calculated measure" toolbox button
        /// </summary>
        public virtual AddCalculatedMeasureButton AddCalculatedMeasureButton => fAddCalcMeasureButton;

        /// <summary>
        ///     Contains all necessary properties for "Show all areas" toolbox button
        /// </summary>
        public virtual AllAreasToolboxButton AllAreasButton => fAllAreasButton;

        /// <summary>
        ///     Contains all necessary properties for "Clear layout" toolbox button
        /// </summary>
        public virtual ClearLayoutToolboxButton ClearLayoutButton => fClearLayoutButton;

        /// <summary>
        ///     Contains all necessary properties for "Show data area" toolbox button
        /// </summary>
        public virtual DataAreaToolboxButton DataAreaButton => fDataAreaButton;

        /// <summary>
        ///     Contains all necessary properties for "Show data and pivot areas" toolbox button
        /// </summary>
        public virtual PivotAreaToolboxButton PivotAreaButton => fPivotAreaButton;

        /// <summary>
        ///     Contains all necessary properties for "Zoom out" toolbox button
        /// </summary>
        public virtual ScaleDecreaseButton ZoomOutButton => fZoomOutButton;

        /// <summary>
        ///     Contains all necessary properties for "Zoom in" toolbox button (only for a OLAP Chart).
        /// </summary>
        public virtual ScaleIncreaseButton ZoomInButton => fZoomInButton;

        /// <summary>
        ///     Contains all necessary properties for "Reset zoom to 100%" toolbox button (only for a OLAP Chart).
        /// </summary>
        public virtual ScaleResetButton ResetZoomButton => fResetZoomButton;

        /// <summary>
        ///     Contains all necessary properties for "Switch to the Grid mode" toolbox button.
        /// </summary>
        public virtual ModeButton ModeButton => FModeButton;

        /// <summary>
        ///     Contains all necessary properties for "Switches the delay pivoting" toolbox button.
        /// </summary>
        public virtual DelayPivotingButton DelayPivotingButton => fDelayPivotingButton;

        /// <summary>
        ///     Contains all necessary properties for "Switches the possibility of resizing the cells" toolbox button.
        /// </summary>
        public virtual ResizingButton ResizingButton => fResizingButton;

        /// <summary>
        ///     Contains buttons to difine placement of measures.
        /// </summary>
        public virtual MeasurePlaceToolboxButton MeasurePlaceButton => fMeasurePlaceButton;

        internal virtual OlapControl OlapControl { get; set; }

        internal Cube.RadarCube Cube => OlapControl.Cube;

        internal IMOlapCube MDCube => Cube as IMOlapCube;

        /// <summary>
        ///     Represents the collection of custom toolbox buttons.
        /// </summary>
        public virtual CustomToolboxButtonCollection CustomButtons { get; } = new CustomToolboxButtonCollection();

        public virtual bool IsDesignMode => false;

        internal abstract string ImageUrl(string resName);
        internal abstract void InitButtons();

        internal virtual void SetDefaultSettings()
        {
            CustomButtons.Clear();

            foreach (var item in fToolItems.Values)
            {
                item.Visible = true;
                item.Image = null;
                item.PressedImage = null;
                item.PressedText = null;
                item.Text = null;
                item.Tooltip = null;
            }

            fToolItems.Clear();
            InitButtons();
        }

        internal virtual void RegisterToolItem(CommonToolboxButton item)
        {
            item.fOwner = this;
            fToolItems.Add(item.ButtonID, item);
        }

        internal void CorrectOrderOfToolItems()
        {
            var areaButtonIDs = fToolItems.Values.Where(x =>
                                                            x is AllAreasToolboxButton || x is PivotAreaToolboxButton ||
                                                            x is DataAreaToolboxButton).Select(x => x.ButtonID)
                .ToList();

            var newOrderKeys = new List<string>();

            if (areaButtonIDs.Count > 0)
                foreach (var item in fToolItems.Keys)
                {
                    if (item == areaButtonIDs[0])
                    {
                        newOrderKeys.AddRange(areaButtonIDs);
                        continue;
                    }

                    if (areaButtonIDs.Contains(item))
                        continue;

                    newOrderKeys.Add(item);
                }

            var newToolItems = new Dictionary<string, CommonToolboxButton>();

            foreach (var buttonIDs in newOrderKeys)
                newToolItems.Add(buttonIDs, fToolItems[buttonIDs]);
            fToolItems.Clear();
            fToolItems = newToolItems;
        }

        internal JsonDialog MakeConnectionDialog()
        {
            return ConnectButton.MakeDialog();
        }

        internal JsonDialog MakeMDXDialog()
        {
            return MDXQueryButton.MakeDialog();
        }

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
        public virtual event ToolboxItemActionHandler ToolboxItemAction;

        internal bool OnToolboxItemAction(ToolboxItemActionArgs e)
        {
            if (ToolboxItemAction != null)
            {
                ToolboxItemAction(this, e);
                return true;
            }
            return false;
        }

        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            writer.AddStyleAttribute("padding-right", "45px");
            writer.AddStyleAttribute("position", "relative");
            //writer.AddStyleAttribute("height", "30px");
            writer.AddStyleAttribute("box-sizing", "border-box!important");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_toolbox ui-widget-header");
            writer.RenderBeginTag("div");


        }

        /// <summary>
        ///     Renders the contents of the control to the specified writer. This method is used primarily by control developers
        /// </summary>
        /// <param name="writer">A HtmlTextWriter that represents the output stream to render HTML content on the client</param>
        protected override void RenderContents(HtmlTextWriter writer)
        {
            if (((OlapAnalysis)OlapControl).IsSettingsEditable)
            {
                writer.AddStyleAttribute(HtmlTextWriterStyle.Position, "absolute");
                writer.AddStyleAttribute("top", "50%");
                writer.AddStyleAttribute("margin-top", "-6px");
                writer.AddAttribute(HtmlTextWriterAttribute.Title, "Settings");
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc-grid-settings rs_icon_cover ui-state-default ui-corner-all");
                writer.AddStyleAttribute("cursor", "pointer");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon ui-icon-gear");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.RenderEndTag();//span
                writer.RenderEndTag();//span         
            }

            writer.AddStyleAttribute(HtmlTextWriterStyle.Position, "absolute");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "none");
            writer.AddStyleAttribute("top", "50%");
            writer.AddStyleAttribute("margin-top", "-6px");
            writer.AddStyleAttribute("right", "5px");
            writer.AddStyleAttribute("cursor", "pointer");
            writer.AddAttribute(HtmlTextWriterAttribute.Title, "Show more items");
            writer.AddAttribute(HtmlTextWriterAttribute.Class,
                "rc_toolox_menu_icon rs_icon_cover ui-state-default ui-corner-all");
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon ui-icon-caret-1-e");
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.RenderEndTag(); //span
            writer.RenderEndTag(); //span         

            writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "hidden");
            writer.AddStyleAttribute(HtmlTextWriterStyle.WhiteSpace, "nowrap");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_toolbox_buttons_container");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            RenderButtons(writer);

            writer.RenderEndTag(); // div
        }

        internal void RenderButtons(HtmlTextWriter writer)
        {
            var needSeparator = false;
            var beginRenderAreaButtons = false;
            ClearLayoutToolboxButton clearLayoutButton = null;

            foreach (var button in fToolItems.Values)
                if (button.Visible)
                {
                    if (button is AllAreasToolboxButton ||
                         button is PivotAreaToolboxButton ||
                         button is DataAreaToolboxButton ||
                         button is ClearLayoutToolboxButton)
                    {
                        if (button is ClearLayoutToolboxButton)
                            clearLayoutButton = button as ClearLayoutToolboxButton;

                        if (beginRenderAreaButtons == false)
                        {
                            writer.AddAttribute(HtmlTextWriterAttribute.Style, "margin-right: 1px");
                            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_layout_toolbox_button");
                            writer.AddAttribute(HtmlTextWriterAttribute.Title, RadarUtils.GetResStr("toolbox_Button_Layout"));
                            writer.RenderBeginTag(HtmlTextWriterTag.Button);

                            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon-font ui-icon-font-template");
                            writer.RenderBeginTag(HtmlTextWriterTag.Span);
                            writer.RenderEndTag(); // span
                            writer.RenderEndTag(); // button

                            writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "none");
                            writer.AddStyleAttribute(HtmlTextWriterStyle.Position, "absolute");
                            writer.AddStyleAttribute(HtmlTextWriterStyle.ZIndex, "1000");
                            writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, "center");
                            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_layout_menu ui-widget-content");
                            writer.RenderBeginTag(HtmlTextWriterTag.Div);

                            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_layout_toolbox_menu");
                            writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                            beginRenderAreaButtons = true;
                        }

                        if (button.NeedSeparator)
                            needSeparator = true;

                        if (button is ClearLayoutToolboxButton)
                            continue;

                        button.RenderContents(writer);

                        if (fToolItems.Values.Count - 1 != fToolItems.Keys.ToList().IndexOf(button.ButtonID))
                            continue;
                    }

                    if (beginRenderAreaButtons)
                    {
                        if (clearLayoutButton != null)
                        {
                            writer.RenderBeginTag("li");
                            writer.RenderEndTag();//li
                            clearLayoutButton.RenderContents(writer);
                        }

                        writer.RenderEndTag();//ul                  
                        writer.RenderEndTag();//div

                        if (needSeparator)
                            RenderSeparator(writer);

                        needSeparator = false;
                        beginRenderAreaButtons = false;

                        if (fToolItems.Values.Count - 1 == fToolItems.Keys.ToList().IndexOf(button.ButtonID))
                            break;

                    }

                    if ((button is ConnectToolboxButton || button is MDXQueryButton)
                        && MDCube == null)
                        continue;

                    if ((button == fZoomOutButton ||
                         button == fZoomInButton ||
                         button == fResetZoomButton) &&
                        OlapControl.AnalysisType != AnalysisType.Chart)
                        continue;

                    if (button == fResizingButton && OlapControl.AnalysisType == AnalysisType.Chart)
                        continue;

                    if (button == FModeButton && !(OlapControl is OlapAnalysis))
                        continue;

                    if (button is MeasurePlaceToolboxButton && OlapControl.AnalysisType == AnalysisType.Chart)
                        continue;

                    button.RenderContents(writer);
                    if (button.NeedSeparator)
                        RenderSeparator(writer);
                }

            if (CustomButtons.Count > 0)
                foreach (var cb in CustomButtons)
                {
                    cb.fOwner = this;
                    if (cb.Visible)
                        cb.RenderContents(writer);
                    if (cb.NeedSeparator)
                        RenderSeparator(writer);
                }
        }

        internal void RenderSeparator(HtmlTextWriter writer)
        {
            writer.AddStyleAttribute("position", "relative");
            writer.AddStyleAttribute("top", "1px");
            writer.AddStyleAttribute("display", "inline-block");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon ui-icon-grip-dotted-vertical");
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.RenderEndTag(); //span
        }

        public override void RenderEndTag(HtmlTextWriter writer)
        {
            writer.RenderEndTag(); //div
        }

        internal virtual bool RaiseCallback(string eventArgument)
        {
            CommonToolboxButton btn;

            if (eventArgument.StartsWith("connect|"))
            {
                var args = eventArgument.Split('|');
                var server = OlapControl.Session.GetString(OlapControl.ClientID + "server") ??
                             fConnectButton.LoginWindowSettings.ServerName;
                var db = OlapControl.Session.GetString(OlapControl.ClientID + "$db") ??
                         fConnectButton.LoginWindowSettings.DatabaseName;
                var e = new ToolboxItemActionArgs(fConnectButton);
                if (OnToolboxItemAction(e))
                {
                    callbackResult = e.ResultValue;
                    if (e.Handled) return true;
                }

                MDCube.Activate(server, db, args[1], "");

                OlapControl.callbackData = CallbackData.Toolbox;
                OlapControl.postbackData = PostbackData.All;

                return true;
            }
            if (eventArgument.StartsWith("execmdx|"))
            {
                var args = eventArgument.Split('|');
                fMDXQueryButton.MDX = args[1];

                var e = new ToolboxItemActionArgs(fMDXQueryButton);
                if (OnToolboxItemAction(e))
                {
                    callbackResult = e.ResultValue;
                    if (e.Handled) return true;
                }

                if (MDCube != null && OlapControl != null)
                    MDCube.ExecuteMDX(args[1], OlapControl);

                OlapControl.callbackData = CallbackData.Toolbox;
                OlapControl.postbackData = PostbackData.All;

                return true;
            }

            var argss = eventArgument.Split('|');
            if (fToolItems.TryGetValue(argss[0], out btn))
            {
                var e = new ToolboxItemActionArgs(btn);
                if (OnToolboxItemAction(e))
                {
                    callbackResult = e.ResultValue;
                    if (e.Handled) return true;
                }

                if (OlapControl != null)
                {
                    if (argss.Length > 1 && argss[1].IsFill() && OlapControl != null)
                    {
                        var chartsTypesObj = JsonConvert.DeserializeObject<ChartTypesJson>(argss[1]);

                        if (chartsTypesObj != null && chartsTypesObj.chartTypes != null)
                        {
                            OlapControl.ChartsType = new SeriesType[chartsTypesObj.chartTypes.Length];

                            for (var i = 0; i < chartsTypesObj.chartTypes.Length; i++)
                                OlapControl.ChartsType[i] = (SeriesType) chartsTypesObj.chartTypes[i];
                        }
                    }
                    if (btn == fConnectButton && OlapControl != null)
                        return true;
                    if (btn == fAllAreasButton && OlapControl != null)
                    {
                        OlapControl.ShowAreasMode = rsShowAreasOlapGrid.rsAll;
                        OlapControl.callbackData = CallbackData.Toolbox;
                        OlapControl.postbackData = PostbackData.OlapGridContainer;
                        return true;
                    }
                    if (btn == fClearLayoutButton && OlapControl != null)
                    {
                        OlapControl.AxesLayout.Clear();
                        OlapControl.callbackData = CallbackData.Toolbox;
                        OlapControl.postbackData = PostbackData.OlapGridContainer;
                        return true;
                    }
                    if (btn == fPivotAreaButton && OlapControl != null)
                    {
                        OlapControl.ShowAreasMode = rsShowAreasOlapGrid.rsPivot;
                        OlapControl.callbackData = CallbackData.Toolbox;
                        OlapControl.postbackData = PostbackData.OlapGridContainer;
                        return true;
                    }
                    if (btn == fDataAreaButton && OlapControl != null)
                    {
                        OlapControl.ShowAreasMode = rsShowAreasOlapGrid.rsDataOnly;
                        OlapControl.callbackData = CallbackData.Toolbox;
                        OlapControl.postbackData = PostbackData.OlapGridContainer;
                        return true;
                    }
                    if (btn == FModeButton && OlapControl != null)
                    {
                        OlapControl.BeginUpdate();

                        //OlapControl.AxesLayout.Clear();
                        //OlapControl.CellSet.ClearMembers();
                        //OlapControl.CellSet.FDrillActions.Clear();

                        OlapControl.ClearAxesLayout();
                        OlapControl.Active = false;
                        OlapControl.Active = true;

                        ((OlapAnalysis)OlapControl).HandleInitOlap();

                        OlapControl.EndUpdate();

                        OlapControl.AnalysisType = OlapControl.AnalysisType == AnalysisType.Chart
                            ? AnalysisType.Grid
                            : AnalysisType.Chart;
                        OlapControl.callbackData = CallbackData.Toolbox;
                        OlapControl.postbackData = PostbackData.OlapGridContainer;
                        ((OlapAnalysis)OlapControl)._needInitialization = true;
                        return true;
                    }
                    //if ((btn == fChartModeButton) && (Grid != null))
                    //{
                    //    OlapControl.AnalysisType = AnalysisType.Chart;
                    //    //OlapControl.UpdateCellSet();
                    //    return;
                    //}

                    if (btn == fResizingButton && OlapControl != null)
                    {
                        OlapControl.AllowResizing = !OlapControl.AllowResizing;

                        if (OlapControl.AllowResizing)
                            OlapControl.UseFixedHeaders = false;

                        OlapControl.UpdateCellSet();
                        OlapControl.callbackData = CallbackData.Toolbox;
                        ((OlapAnalysis)OlapControl)._needInitialization = true;
                        OlapControl.postbackData = PostbackData.ToolboxData;
                        return true;
                    }

                    if (btn == fDelayPivotingButton && OlapControl != null)
                    {
                        OlapControl.DelayPivoting = !OlapControl.DelayPivoting;
                        OlapControl.callbackData = CallbackData.Toolbox;
                        OlapControl.postbackData = PostbackData.ToolboxData;
                        return true;
                    }
                }
            }

            return false;
        }

        internal FileStreamResult DoExport(string exportParam)
        {
            var argss = exportParam.Split('|');

            switch (argss[0])
            {
                case "savesettings":
                    return SaveSettings(argss[1]);
            }

            return null;
        }

        private FileStreamResult SaveSettings(string fileName)
        {
            string tempFile = Cube.SessionState.WorkingDirectoryName + fileName + ".dat";
            using (FileStream fileStream = File.Create(tempFile))
            {
                OlapControl.SaveCompressed(fileStream, StreamContent.All);
            }

            FileStream fS = File.Open(tempFile, FileMode.Open);
            var res = new FileStreamResult(fS, "application/octet-stream")
                {
                    FileDownloadName = fileName + ".dat"
                };
            return res;
        }
    }
}