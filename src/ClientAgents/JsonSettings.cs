using System.Linq;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Controls.Grid;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.ClientAgents
{
    public class JsonSettings
    {
        public bool allowResizing;
        public bool allowSelectionFormatting = true;
        public string analysisType;
        public RCellset Cellset;
        public SeriesType[] chartsType;
        public string cid;
        public string clientMessage;
        public string errorHandler;
        public JsonDialog exception;
        public string filterClientId;
        public string[] filtered;
        public bool heditorResizable = true;
        public int heditorWidth;
        public string height;
        public string hint_CollapseCell;
        public string hint_DrillNextHierarchy;
        public string hint_DrillNextLevel;
        public string hint_DrillParentChild;
        public ClientLayout Layout;
        public string loading;
        public string loadlayoutTitle;
        public string loadsettingsTitle;
        public string messageHandler;
        public string pagePrompt;
        public string popupLoading;
        public string processing;
        public string rsAverage;
        public string rsBelowAverage;
        public string rsBelowThan;
        public string rsCancel;
        public string rsCount;
        public string rsFarAverage;
        public string rsMax;
        public string rsMin;
        public string rsMoreAverage;
        public string rsMoreThan;
        public string rsNearAverage;

        //public bool isMobile = false;
        public string rsOk;

        public string rsRemove;
        public string rsSumma;
        public string toolboxClientId;
        public string uid;
        public string url_collapsedh;
        public string url_collapsedl;
        public string url_collapsedp;
        public string url_del;
        public string url_DeleteGroup;
        public string url_delover;
        public string url_filter;
        public string url_filterover;
        public string url_loader;
        public string url_loaderbg;
        public string url_nextchildren;
        public string url_nexthier;
        public string url_nextlevel;
        public string width;

        public JsonSettings(OlapControl grid)
        {
            uid = grid.UniqueID;
            cid = grid.ClientID;
            loading = RadarUtils.GetResStr("rsLoading");
            processing = RadarUtils.GetResStr("rsPleaseWait");
            popupLoading = RadarUtils.GetResStr("rsPopupLoading");
            url_nextlevel = grid.ImageUrl("svernut_grey.png");
            hint_DrillNextLevel = RadarUtils.GetResStr("hint_DrillNextLevel");
            url_nexthier = grid.ImageUrl("plus.png");
            hint_DrillNextHierarchy = RadarUtils.GetResStr("hint_DrillNextHierarchy");
            url_collapsedh = grid.ImageUrl("minus.png");
            url_collapsedl = grid.ImageUrl("razvernut_grey.png");
            url_collapsedp = grid.ImageUrl("razvernut_blue.png");
            hint_CollapseCell = RadarUtils.GetResStr("hint_CollapseCell");
            url_nextchildren = grid.ImageUrl("svernut_blue.png");
            hint_DrillParentChild = RadarUtils.GetResStr("hint_DrillParentChild");
            url_del = grid.ImageUrl("del.png");
            url_delover = grid.ImageUrl("delover.png");
            url_filter = grid.ImageUrl("filter.png");
            url_filterover = grid.ImageUrl("filterover.png");
            url_loader = grid.ImageUrl("AjaxLoader.gif");
            url_loaderbg = grid.ImageUrl("loader_bg.gif");
            url_DeleteGroup = grid.ImageUrl("DeleteGroup.gif");
            pagePrompt = RadarUtils.GetResStr("rsEnterPageNumber");
            rsOk = RadarUtils.GetResStr("rsOk");
            rsCancel = RadarUtils.GetResStr("rsCancel");
            rsMax = RadarUtils.GetResStr("rsMax");
            rsMin = RadarUtils.GetResStr("rsMin");

            rsAverage = RadarUtils.GetResStr("rsAverage");
            rsCount = RadarUtils.GetResStr("rsCount");
            rsSumma = RadarUtils.GetResStr("rsSumma");
            rsRemove = RadarUtils.GetResStr("rsRemove");
            rsBelowThan = RadarUtils.GetResStr("MF_BelowThan");
            rsMoreThan = RadarUtils.GetResStr("MF_MoreThan");
            rsBelowAverage = RadarUtils.GetResStr("MF_BelowAverage");
            rsMoreAverage = RadarUtils.GetResStr("MF_MoreAverage");
            rsNearAverage = RadarUtils.GetResStr("MF_NearAverage");
            rsFarAverage = RadarUtils.GetResStr("MF_FarAverage");
            loadlayoutTitle = ((OlapGrid) grid).Toolbox.LoadLayoutButton.FileNamePrompt;
            loadsettingsTitle = RadarUtils.GetResStr("rsLoadSettingsDialog_Title");

            if (grid.FFilteredHierarchies != null)
                filtered = grid.FFilteredHierarchies.Select(item => item.UniqueName).Union(
                    grid.FFilteredLevels.Select(item => item.UniqueName)).Distinct().Union(
                    grid.Measures.Where(item => item.Filter != null).Select(item => item.UniqueName)).ToArray();

            clientMessage = grid._ClientMassage;
            messageHandler = grid.MessageHandler;

            filterClientId = ((OlapGrid) grid).Filter.ClientID;

            //toolboxClientId = grid.GetRelatedToolboxControlClientId();
            //string ua = "alcatel|amoi|android|avantgo|blackberry|benq|cell|cricket|docomo|elaine|htc|iemobile|iphone|ipad|ipaq|ipod|j2me|java|midp|mini|mmp|mobi|motorola|nec-|nokia|palm|panasonic|philips|phone|sagem|sharp|sie-|smartphone|sony|symbian|t-mobile|telus|vodafone|wap|webos|wireless|xda|xoom|zte";
            //if (grid.Page != null)
            //    isMobile = (ua.Split('|').Any(item => grid.Page.Request.UserAgent.ToLower().Contains(item)));

            heditorWidth = ((OlapGrid) grid).HierarchyEditorStyle.Width;
            heditorResizable = ((OlapGrid) grid).HierarchyEditorStyle.Resizable;

            width = grid.Width;
            height = grid.Height;

            allowSelectionFormatting = ((OlapGrid) grid).AllowSelectionFormatting;
            allowResizing = ((OlapGrid) grid).AllowResizing;
        }

        public virtual void InitControlData(CellSet.CellSet cs, OlapControl grid)
        {
            if (grid.callbackException != null)
            {
                exception = SessionTimeoutDialog.RenderMassage(grid, grid.callbackException);
                return;
            }

            Cellset = new RCellset(cs, grid.MaxTextLength);
            Layout = new ClientLayout(grid.AxesLayout);
            chartsType = grid.ChartsType;
            analysisType = "grid";
        }
    }
}