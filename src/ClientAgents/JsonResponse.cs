using System.Linq;
using Newtonsoft.Json;
using RadarSoft.RadarCube.Controls;

namespace RadarSoft.RadarCube.ClientAgents
{
    public class JsonResponse
    {
        public string callbackScript;
        public RCellset Cellset;
        public string clientMessage;
        public string columnarea;
        public string data;
        public string datagrid;
        public JsonDialog dialog;
        public ErrorData errorClientData;
        public string errorHandler;
        public JsonDialog exception;
        public string[] filtered;
        public string filtergrid;
        public ClientLayout Layout;
        public string legendarea;
        public string messageHandler;
        public string modifiersarea;
        public string olapgridcontainer;
        public string pagearea;
        public string pivot;
        public string resultString;
        public string rowarea;
        public JsonSettings settings;
        public string target;
        public string toolbox;
        public string treearea;
        public string[] treechecked;
        public string valuearea;

        internal string Serialize(OlapControl grid)
        {
            DoSerialize(grid);
            return JsonConvert.SerializeObject(this);
        }

        internal void DoSerialize(OlapControl grid)
        {
            callbackScript = grid.ClientCallbackFunction;
            filtered = grid.FFilteredHierarchies.Select(item => item.UniqueName).Union(
                grid.FFilteredLevels.Select(item => item.UniqueName)).Distinct().Union(
                grid.Measures.Where(item => item.Filter != null).Select(item => item.UniqueName)).ToArray();

            var chekedList = grid.AxesLayout.PageAxis.Select(item => item.UniqueName).Union(
                grid.AxesLayout.RowAxis.Select(item => item.UniqueName)).Union(
                grid.AxesLayout.ColumnAxis.Select(item => item.UniqueName)).Union(
                grid.AxesLayout.DetailsAxis.Select(item => item.UniqueName)).Union(
                grid.Measures.Where(item => item.Visible).Select(item => item.UniqueName)).ToList();

            if (grid.AxesLayout.fColorAxisItem != null)
                chekedList.Add(grid.AxesLayout.fColorAxisItem.UniqueName);
            if (grid.AxesLayout.fColorForeAxisItem != null)
                chekedList.Add(grid.AxesLayout.fColorForeAxisItem.UniqueName);
            if (grid.AxesLayout.fShapeAxisItem != null)
                chekedList.Add(grid.AxesLayout.fShapeAxisItem.UniqueName);
            if (grid.AxesLayout.fSizeAxisItem != null)
                chekedList.Add(grid.AxesLayout.fSizeAxisItem.UniqueName);

            if (grid.AxesLayout.fXAxisMeasure != null)
                chekedList.Add(grid.AxesLayout.fXAxisMeasure.UniqueName);

            treechecked = chekedList.ToArray();
        }

        internal void InitControlData(CellSet.CellSet cs, OlapControl grid)
        {
            Cellset = new RCellset(cs, grid.MaxTextLength);
            Layout = new ClientLayout(grid.AxesLayout);
        }
    }
}