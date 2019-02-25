using System;
using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.ClientAgents;
using RadarSoft.RadarCube.Controls.Grid;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.HeirarchyEditor
{
    [Serializable]
    internal class HierarchyEditor : IStreamedObject
    {
        internal Dictionary<string, Node> allnodes = new Dictionary<string, Node>();
        private string fContextLevelName;
        internal string FFilterString = "";

        [NonSerialized] private OlapControl fGrid;

        [NonSerialized] internal Hierarchy fHierarchy;

        private string fHierarchyName;
        internal Node root;

        internal HierarchyEditor(OlapControl grid)
        {
            fGrid = grid;
        }

        internal bool Filtered => FFilterString != "";

        private string ImageUrl(string resName)
        {
            return fGrid.images.ImageUrl(resName, typeof(OlapGrid), fGrid.TempPath);
        }

        private void Init(string hierarchyName)
        {
            fHierarchyName = hierarchyName;
            Restore(fGrid);
            root = new Node(fGrid.fHierarchyEditorStyle.ItemsInPage);
            root.fChecked = fHierarchy.UnfetchedMembersVisible;
            root.fMemberName = "all_members";
            root.fParent = null;
            allnodes.Add(root.fMemberName, root);

            //if (!fHierarchy.IsFullyFetched)
            //{
            //    fHierarchy.FInitialized = false;
            //    fHierarchy.InitHierarchy(-1);
            //}
            if (fHierarchy.Levels == null)
                fHierarchy.DefaultInit();

            DoFillMembers(fHierarchy.Levels[0].Members, root.fChildren, root);
            //TODO (Stepanov): Fixed an error occurs when opening levels in the Hierarchy editor tree. 
            fGrid.ApplyChanges();
        }

        internal void Restore(OlapControl grid)
        {
            fGrid = grid;
            fHierarchy = fGrid.Dimensions.FindHierarchy(fHierarchyName);
        }

        private void DoFillMembers(Members ms, Nodes nodes, Node parent)
        {
            foreach (var m in ms)
                if (m.MemberType == MemberType.mtGroup || m.MemberType == MemberType.mtCalculated)
                {
                    object o;
                    parent.hasCustomMembers = true;
                    if (parent == root)
                        o = fHierarchy.FLevels[0];
                    else
                        o = fHierarchy.FindMemberByUniqueName(parent.fMemberName);
                    bool hasNewMembers;
                    fGrid.Cube.RetrieveMembersPartial(fGrid, o, 0, -1, null, out hasNewMembers);
                    if (hasNewMembers) fGrid.ApplyChanges();
                }
            foreach (var m in ms)
            {
                var n = new Node(fGrid.fHierarchyEditorStyle.ItemsInPage);
                n.IsCustomMember = m.MemberType != MemberType.mtCommon;
                n.fParent = parent;
                n.fChecked = m.Visible;
                nodes.Add(n);
                allnodes.Add(m.FUniqueName, n);
                n.fMemberName = m.FUniqueName;
                if (m.Children.Count > 0) DoFillMembers(m.Children, n.fChildren, n);
                if (m.NextLevelChildren.Count > 0)
                    DoFillMembers(m.NextLevelChildren, n.fChildren, n);
            }
        }

        private void DoSetChildrenCheckState(bool state, Node node)
        {
            node.fChecked = state;
            foreach (var n in node.fChildren) DoSetChildrenCheckState(state, n);
        }

        private void DoSetParentCheckState(bool state, Node node)
        {
            var parent = node.fParent;
            if (parent == root) return;
            if (parent == null) return;
            if (state)
            {
                parent.fChecked = true;
                DoSetParentCheckState(true, parent);
            }
            else
            {
                foreach (var n in parent.fChildren)
                    if (n.fChecked) return;
                parent.fChecked = false;
                DoSetParentCheckState(false, parent);
            }
        }

        internal void ChangeCheckState(string nodeName)
        {
            Node n;
            if (!allnodes.TryGetValue(nodeName, out n)) return;
            var state = !n.fChecked;
            DoSetChildrenCheckState(state, n);
            DoSetParentCheckState(state, n);
        }

        private void DoCollapse(Node node)
        {
            node.fExpanded = false;
            foreach (var n in node.fChildren)
                if (n.fExpanded) DoCollapse(n);
        }

        internal void Unload()
        {
            fGrid = null;
            fHierarchy = null;
        }

        internal void ChangeExpandState(string nodeName)
        {
            Node n;
            if (!allnodes.TryGetValue(nodeName, out n)) return;
            if (n.fExpanded)
            {
                DoCollapse(n);
            }
            else
            {
                if (n.IsCustomMember || n.hasCustomMembers)
                {
                    n.fChildren.FTotalCount = n.fChildren.Count;
                }
                else
                {
                    if (n.fChildren.FTotalCount == -1)
                    {
                        object o;
                        var b = true;
                        if (n == root)
                        {
                            o = fHierarchy.FLevels[0];
                            b = fHierarchy.Origin != HierarchyOrigin.hoNamedSet;
                        }
                        else
                        {
                            o = fHierarchy.FindMemberByUniqueName(n.fMemberName);
                        }
                        if (b)
                            n.fChildren.FTotalCount = fGrid.Cube.RetrieveMembersCount(fGrid, o);
                        else
                            n.fChildren.FTotalCount = ((Level) o).Members.Count;
                    }
                }
                if (n.fChildren.FTotalCount > 0) n.fExpanded = true;
            }
        }

        internal void DoRenderTree(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddStyleAttribute(HtmlTextWriterStyle.MarginLeft, "2px");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            DoRenderNode(root, writer);
            writer.RenderEndTag(); // tree table
        }

        internal JsonDialog Render()
        {
            var result = new JsonDialog();
            var writer = new HtmlTextWriter();
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
            //writer.AddStyleAttribute(HtmlTextWriterStyle.Width, (fGrid.fHierarchyEditorStyle.Width + 4).ToString() + "px");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            result.title = RadarUtils.GetResStr("repHierarhyEditor") + ": " + fHierarchy.DisplayName;

            // context filter
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingBottom, "5px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingTop, "2px");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.Write("<b>" + RadarUtils.GetResStr("rsContextFilterSettings") + "</b>");
            writer.RenderEndTag(); //div

            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingBottom, "2px");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.Write(RadarUtils.GetResStr("repHierarhyEditorLevel"));
            writer.RenderEndTag(); //div
            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingBottom, "5px");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "heditor_selectlevel");
            writer.RenderBeginTag(HtmlTextWriterTag.Select);
            if (fHierarchy.Origin == HierarchyOrigin.hoParentChild)
                for (var i = 0; i < fHierarchy.CubeHierarchy.FMDXLevelNames.Count; i++)
                {
                    var s = fHierarchy.CubeHierarchy.FMDXLevelNames[i];

                    if (fContextLevelName == null && i == 0)
                        writer.AddAttribute("selected", "selected");

                    if (s == fContextLevelName)
                        writer.AddAttribute("selected", "selected");

                    writer.AddAttribute(HtmlTextWriterAttribute.Value, s);
                    writer.RenderBeginTag(HtmlTextWriterTag.Option);
                    writer.Write(s);
                    writer.RenderEndTag(); //option
                }
            if (fHierarchy.CubeHierarchy.FMDXLevelNames.Count == 0)
                for (var i = 0; i < fHierarchy.Levels.Count; i++)
                {
                    var l = fHierarchy.Levels[i];

                    if (fContextLevelName == null && i == 0)
                        writer.AddAttribute("selected", "selected");

                    if (l.UniqueName == fContextLevelName)
                        writer.AddAttribute("selected", "selected");

                    writer.AddAttribute(HtmlTextWriterAttribute.Value, l.UniqueName);
                    writer.RenderBeginTag(HtmlTextWriterTag.Option);
                    writer.Write(l.DisplayName);
                    writer.RenderEndTag(); //option
                }

            writer.RenderEndTag(); //select

            writer.RenderEndTag(); //div

            var lv = fHierarchy.Levels.Find(fContextLevelName) ?? fHierarchy.Levels[0];
            var mnu = new GenericMenu();
            fGrid.FillFilterMenu(mnu, lv, fContextLevelName, null,
                "RadarSoft.$('#" + fGrid.ClientID + "').data('grid').heditor.cancel2('" +
                RadarUtils.GetResStr("rsLoading") + "')");
//            fGrid.FillFilterMenu(mnu, lv, fContextLevelName, null, "");
            fGrid.mnu_cf.Embedded = true;
            fGrid.ConvertGenericMenu(mnu, fGrid.mnu_cf);

            if (fGrid.mnu_cf.Items.Count > 0)
                fGrid.mnu_cf.RenderControl(writer);

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); // tr

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingBottom, "5px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingTop, "5px");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.Write("<b>" + RadarUtils.GetResStr("rsMemberFilterSettings") + "</b>");
            writer.RenderEndTag(); //div

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); // tr

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingBottom, "5px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-widget-content");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_HFilter");
            writer.AddAttribute(HtmlTextWriterAttribute.Rows, "2");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
            writer.RenderBeginTag(HtmlTextWriterTag.Textarea);
            if (FFilterString != "")
                writer.Write(FFilterString);
            writer.RenderEndTag(); // textarea
            writer.RenderEndTag(); //td
            writer.RenderEndTag(); // tr

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.AddAttribute(HtmlTextWriterAttribute.Align, "right");
            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingBottom, "5px");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "heditor_exactmatching");
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "checkbox");
            writer.AddAttribute("checked", "checked");
            writer.RenderBeginTag(HtmlTextWriterTag.Input);
            writer.RenderEndTag(); // input

            writer.Write(RadarUtils.GetResStr("rshExactMatching"));

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_HFilter_btn");
            writer.AddStyleAttribute(HtmlTextWriterStyle.MarginLeft, "20px");
            writer.RenderBeginTag(HtmlTextWriterTag.Button);
            writer.Write(RadarUtils.GetResStr("rsFind"));
            //HtmlImage img = new HtmlImage();
            //img.Src = OlapGrid.images.ImageUrl("FilterHierarchy.png", fGrid.Page);
            //img.Alt = RadarUtils.GetResStr("rsFilterTree");
            //img.RenderControl(writer);
            writer.RenderEndTag(); // filter button

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); // tr

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "auto");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Height, fGrid.fHierarchyEditorStyle.TreeHeight + "px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "100%");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-widget ui-widget-content");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "heditor_TREE");
            writer.AddStyleAttribute(HtmlTextWriterStyle.MarginBottom, "5px");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            DoRenderTree(writer);

            writer.RenderEndTag(); // div

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); // tr 2 filter box


            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            writer.AddAttribute(HtmlTextWriterAttribute.Align, "right");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "btnApplyFilter");
            writer.RenderBeginTag(HtmlTextWriterTag.Button);
            writer.Write(RadarUtils.GetResStr("rsApply"));
            writer.RenderEndTag(); // button
            writer.RenderEndTag(); //td

            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "btnResetFilter");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "none");
            writer.RenderBeginTag(HtmlTextWriterTag.Button);
            writer.Write("Confirm");
            writer.RenderEndTag(); // button
            writer.RenderEndTag(); //td


            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.AddStyleAttribute(HtmlTextWriterStyle.MarginLeft, "20px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.MarginRight, "20px");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "btnCancelFilter");
            writer.RenderBeginTag(HtmlTextWriterTag.Button);
            writer.Write(RadarUtils.GetResStr("rsCancel"));
            writer.RenderEndTag(); // filter button

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "btnCancelResetFilter");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "none");
            writer.RenderBeginTag(HtmlTextWriterTag.Button);
            writer.Write("Cancel");
            writer.RenderEndTag(); // button
            writer.RenderEndTag(); // div
            writer.RenderEndTag(); //td

            writer.RenderEndTag(); // tr
            writer.RenderEndTag(); // table

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); // tr 2 filter box

            writer.RenderEndTag(); // table

            result.buttons = new JsonDialogButton[0];
            result.data = writer.ToString();
            return result;
        }

        internal CallbackData ProcessCallback(string[] args)
        {
            if (args.Length > 2)
                args[2] = args[2].Replace("@@@", "&");

            var callbackData = CallbackData.HierarchyEditorTree;
            if (args[1] == "init")
            {
                if (root != null) // already showed
                    return CallbackData.Nothing;
                Init(args[2]);
                callbackData = CallbackData.HierarchyEditor;
            }
            if (args[1] == "chlevel")
            {
                fContextLevelName = args[2];
                callbackData = CallbackData.HierarchyEditor;
            }
            if (args[1] == "expand")
                ChangeExpandState(args[2]);
            if (args[1] == "check")
                ChangeCheckState(args[2]);
            if (args[1] == "filter")
                SetFilter(args[2], args[3] == "true");
            if (args[1] == "page")
            {
                Node n;
                allnodes.TryGetValue(args[2], out n);
                if (args[3] != "null")
                    if (args[3] == "next")
                        n.fChildren.fCurrentPage++;
                    else if (args[3] == "prev")
                        n.fChildren.fCurrentPage--;
                    else
                        n.fChildren.fCurrentPage = Convert.ToInt32(args[3]);
                n.fChildren.frame.Clear();
            }
            return callbackData;
        }

        private void DoRenderNode(Node node, HtmlTextWriter writer)
        {
            if (node.fParent == null && node.fChildren.FTotalCount == -1)
                ChangeExpandState(node.fMemberName);

            Member mm = null;
            if (node.fMemberName != "all_members")
                mm = fHierarchy.FindMemberByUniqueName(node.fMemberName);

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.AddAttribute("membername", node.fMemberName);
            writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, "NOWRAP");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            var depth = node.Depth;
            var _left = 0;
            if (depth > 0)
                _left += depth * 15;
            var renderExpand = (node.fChildren.FTotalCount != 0 && !Filtered ||
                                node.fChildren.frame2.Count > 0 && Filtered) &&
                               (mm == null || !mm.IsLeaf);

            if (renderExpand)
                writer.AddAttribute("expandable", "true");
            _left += 11;
            if (_left > 0)
                writer.AddStyleAttribute(HtmlTextWriterStyle.MarginLeft, _left + "px");
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            if (renderExpand)
            {
                writer.AddStyleAttribute("display", "inline-block");
                if (node.fExpanded)
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon ui-icon-triangle-1-se");
                else
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon ui-icon-triangle-1-e");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.RenderEndTag(); // span
            }
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "checkbox");
            if (node.fChecked)
                writer.AddAttribute(HtmlTextWriterAttribute.Checked, "CHECKED");
            writer.RenderBeginTag(HtmlTextWriterTag.Input);
            writer.RenderEndTag(); // input
            writer.Write("&nbsp;");
            if (node == root)
            {
                writer.Write(RadarUtils.GetResStr("rsAllMembersCaption"));
            }
            else
            {
                var m = fHierarchy.FindMemberByUniqueName(node.fMemberName);
                writer.Write(m.DisplayName);
            }
            writer.RenderEndTag(); //span
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); //tr
            if (!Filtered)
            {
                if (node.fExpanded)
                {
                    if (node.fChildren.frame.Count == 0)
                        RetrieveFrame(node);
                    foreach (var n in node.fChildren.frame)
                        DoRenderNode(n, writer);
                    RenderPagesLine(node, writer);
                }
            }
            else
            {
                if (node.fExpanded)
                {
                    if (node.fChildren.frame.Count == 0)
                        RetrieveFrame(node);
                    foreach (var n in node.fChildren.frame2)
                        DoRenderNode(n, writer);
                }
            }
        }

        private void RetrieveFrame(Node node)
        {
            if (node.hasCustomMembers || node.IsCustomMember)
            {
                RetrieveFrame2(node);
                return;
            }
            var nodes = node.fChildren;
            var _from = 0;
            var _count = nodes.FTotalCount;
            if (nodes.fItemsInPage + 5 < _count)
            {
                _count = nodes.fItemsInPage;
                _from = (nodes.fCurrentPage - 1) * nodes.fItemsInPage;
            }
            var list = new List<string>();
            object o;
            if (node == root)
                o = fHierarchy.FLevels[0];
            else
                o = fHierarchy.FindMemberByUniqueName(node.fMemberName);
            bool hasNewMembers;
            fGrid.Cube.RetrieveMembersPartial(fGrid, o, _from, _count, list, out hasNewMembers);

            //TODO (Stepanov): Fixed an error occurs when opening levels in the Hierarchy editor tree. 
            ////if (hasNewMembers)
                //fGrid.ApplyChanges();

            foreach (var s in list)
            {
                Node n;
                allnodes.TryGetValue(s, out n);
                if (n == null)
                {
                    n = new Node(fGrid.fHierarchyEditorStyle.ItemsInPage);
                    var m = fHierarchy.FindMemberByUniqueName(s);
                    n.IsCustomMember = m.MemberType != MemberType.mtCommon;
                    n.fParent = node;
                    n.fChecked = node.fChecked;
                    nodes.Add(n);
                    allnodes.Add(s, n);
                    n.fMemberName = s;
                }
                nodes.frame.Add(n);
            }
        }

        private void RetrieveFrame2(Node node)
        {
            var nodes = node.fChildren;
            var _from = 0;
            var _count = nodes.FTotalCount;
            if (nodes.fItemsInPage + 5 < _count)
            {
                _count = nodes.fItemsInPage;
                _from = (nodes.fCurrentPage - 1) * nodes.fItemsInPage;
            }
            for (var i = _from; i < Math.Min(_from + _count, node.fChildren.Count); i++)
            {
                var n = node.fChildren[i];
                nodes.frame.Add(n);
            }
        }

        private void RenderPagesLine(Node node, HtmlTextWriter writer)
        {
            // << | 1 | ... | 336 | >>
            if (node.fChildren.frame.Count == node.fChildren.FTotalCount) return;

            var pages = (node.fChildren.FTotalCount - 1) / node.fChildren.fItemsInPage + 1;

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, "NOWRAP");
            writer.AddAttribute("member", node.fMemberName);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            var depth = node.Depth;
            writer.AddStyleAttribute(HtmlTextWriterStyle.MarginLeft, (depth + 1) * 20 + "px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "inline-block");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Padding, "2px");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-widget ui-corner-all ui-widget-header");
            writer.RenderBeginTag(HtmlTextWriterTag.Span);

            if (node.fChildren.fCurrentPage > 1)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                writer.AddAttribute("page-value", "prev");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon ui-icon-seek-prev");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.RenderEndTag(); //span
                writer.RenderEndTag(); //span
            }

            if (pages < 10)
            {
                for (var ii = 1; ii <= pages; ii++)
                {
                    if (node.fChildren.fCurrentPage != ii)
                    {
                        writer.AddAttribute("page-value", ii.ToString());
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                        writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    }
                    writer.WriteInLine(ii.ToString());
                    if (node.fChildren.fCurrentPage != ii)
                        writer.RenderEndTag(); //span
                }
            }
            else
            {
                if (node.fChildren.fCurrentPage > 1)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                    writer.AddAttribute("page-value", "1");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                }
                writer.WriteInLine("1");
                if (node.fChildren.fCurrentPage > 1)
                    writer.RenderEndTag(); //span

                if (node.fChildren.fCurrentPage > 2)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                    writer.AddAttribute("page-value", "...");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.WriteInLine("...");
                    writer.RenderEndTag(); //span
                }

                if (node.fChildren.fCurrentPage > 1)
                    writer.WriteInLine(node.fChildren.fCurrentPage.ToString());

                if (node.fChildren.fCurrentPage + 1 < pages)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                    writer.AddAttribute("page-value", "...");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.WriteInLine("...");
                    writer.RenderEndTag(); //span
                }

                if (node.fChildren.fCurrentPage < pages)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                    writer.AddAttribute("page-value", pages.ToString());
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.WriteInLine(pages.ToString());
                    writer.RenderEndTag(); //Span
                }
            }

            if (node.fChildren.fCurrentPage < pages)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "pager ui-state-default ui-corner-all");
                writer.AddAttribute("page-value", "next");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon ui-icon-seek-next");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.RenderEndTag(); //Span
                writer.RenderEndTag(); //Span
            }

            writer.RenderEndTag(); //span
            writer.RenderEndTag(); // td
            writer.RenderEndTag(); //tr
        }

        internal void SetFilter(string filter, bool exactMatching)
        {
            FFilterString = filter;
            foreach (var n in allnodes.Values)
                n.fChildren.frame2.Clear();
            if (FFilterString == "") return;
            var list = new List<string>();

            bool hasNewMembers;
            fGrid.Cube.RetrieveMembersFiltered(fGrid, fHierarchy, filter, list, out hasNewMembers, exactMatching,
                false);
            if (hasNewMembers)
                fGrid.ApplyChanges();

            foreach (var s in list)
            {
                var ms = new List<Member>();
                var current = fHierarchy.FindMemberByUniqueName(s);
                while (current != null)
                {
                    ms.Add(current);
                    current = current.Parent;
                }
                var curnode = root;
                for (var i = ms.Count - 1; i >= 0; i--)
                {
                    Node n;
                    if (!allnodes.TryGetValue(ms[i].UniqueName, out n))
                    {
                        n = new Node(fGrid.fHierarchyEditorStyle.ItemsInPage);
                        n.IsCustomMember = ms[i].MemberType != MemberType.mtCommon;
                        n.fParent = curnode;
                        n.fChecked = curnode.fChecked;
                        curnode.fChildren.Add(n);
                        allnodes.Add(ms[i].UniqueName, n);
                        n.fMemberName = ms[i].UniqueName;
                    }
                    if (!curnode.fChildren.frame2.Contains(n))
                        curnode.fChildren.frame2.Add(n);
                    curnode = n;
                }
            }
        }

        internal void Apply()
        {
            fHierarchy.BeginUpdate();
            foreach (var n in allnodes.Values)
            {
                if (n == root) continue;
                var m = fHierarchy.FindMemberByUniqueName(n.fMemberName);
                m.FVisible = n.fChecked;
            }
            fHierarchy.UnfetchedMembersVisible = root.fChecked;
            fHierarchy.EndUpdate();
            fHierarchy.UpdateFilterState(true);
        }

        [Serializable]
        internal class Nodes : List<Node>, IStreamedObject
        {
            internal int fCurrentPage = 1; // related to WriteStream
            internal int fItemsInPage = 10;
            internal Frame frame = new Frame();
            internal Frame frame2 = new Frame();
            internal int FTotalCount = -1; // related to WriteStream

            #region IStreamedObject Members

            void IStreamedObject.WriteStream(BinaryWriter writer, object options)
            {
                StreamUtils.WriteTag(writer, Tags.tgASPHierNodes);

                StreamUtils.WriteTag(writer, Tags.tgASPHierNodes_Count);
                StreamUtils.WriteInt32(writer, Count);
                foreach (var n in this)
                    (n as IStreamedObject).WriteStream(writer, null);

                if (frame.Count > 0)
                {
                    StreamUtils.WriteTag(writer, Tags.tgASPHierNodes_Frame1);
                    StreamUtils.WriteInt32(writer, frame.Count);
                    foreach (var n in frame)
                        StreamUtils.WriteString(writer, n.fMemberName);
                }

                if (frame2.Count > 0)
                {
                    StreamUtils.WriteTag(writer, Tags.tgASPHierNodes_Frame2);
                    StreamUtils.WriteInt32(writer, frame2.Count);
                    foreach (var n in frame2)
                        StreamUtils.WriteString(writer, n.fMemberName);
                }

                if (fCurrentPage != 1)
                {
                    StreamUtils.WriteTag(writer, Tags.tgASPHierNodes_CurrentPage);
                    StreamUtils.WriteInt32(writer, fCurrentPage);
                }

                StreamUtils.WriteTag(writer, Tags.tgASPHierNodes_ItemsInPage);
                StreamUtils.WriteInt32(writer, fItemsInPage);

                if (FTotalCount != -1)
                {
                    StreamUtils.WriteTag(writer, Tags.tgASPHierNodes_TotalCount);
                    StreamUtils.WriteInt32(writer, FTotalCount);
                }

                StreamUtils.WriteTag(writer, Tags.tgASPHierNodes_EOT);
            }

            void IStreamedObject.ReadStream(BinaryReader reader, object options)
            {
                var editor = (HierarchyEditor) options;
                StreamUtils.CheckTag(reader, Tags.tgASPHierNodes);
                for (var exit = false; !exit;)
                {
                    var tag = StreamUtils.ReadTag(reader);
                    switch (tag)
                    {
                        case Tags.tgASPHierNodes_Count:
                            var c = StreamUtils.ReadInt32(reader);
                            for (var i = 0; i < c; i++)
                            {
                                var n = new Node(fItemsInPage);
                                Add(n);
                                (n as IStreamedObject).ReadStream(reader, editor);
                            }
                            break;
                        case Tags.tgASPHierNodes_Frame1:
                            c = StreamUtils.ReadInt32(reader);
                            for (var i = 0; i < c; i++)
                            {
                                Node n;
                                editor.allnodes.TryGetValue(StreamUtils.ReadString(reader), out n);
                                frame.Add(n);
                            }
                            break;
                        case Tags.tgASPHierNodes_Frame2:
                            c = StreamUtils.ReadInt32(reader);
                            for (var i = 0; i < c; i++)
                            {
                                Node n;
                                editor.allnodes.TryGetValue(StreamUtils.ReadString(reader), out n);
                                frame2.Add(n);
                            }
                            break;
                        case Tags.tgASPHierNodes_CurrentPage:
                            fCurrentPage = StreamUtils.ReadInt32(reader);
                            break;
                        case Tags.tgASPHierNodes_ItemsInPage:
                            fItemsInPage = StreamUtils.ReadInt32(reader);
                            break;
                        case Tags.tgASPHierNodes_TotalCount:
                            FTotalCount = StreamUtils.ReadInt32(reader);
                            break;
                        case Tags.tgASPHierNodes_EOT:
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

        [Serializable]
        internal class Node : IStreamedObject
        {
            internal bool fChecked;
            internal Nodes fChildren = new Nodes();
            internal bool fExpanded;
            internal string fMemberName;
            internal Node fParent;
            internal bool hasCustomMembers;
            internal bool IsCustomMember;

            internal Node(int itemsInPage)
            {
                fChildren.fItemsInPage = itemsInPage;
            }

            internal int Depth => fParent == null ? 0 : fParent.Depth + 1;

            #region IStreamedObject Members

            void IStreamedObject.WriteStream(BinaryWriter writer, object options)
            {
                StreamUtils.WriteTag(writer, Tags.tgASPHierNode);

                StreamUtils.WriteTag(writer, Tags.tgASPHierNode_MemberName);
                StreamUtils.WriteString(writer, fMemberName);

                if (hasCustomMembers)
                    StreamUtils.WriteTag(writer, Tags.tgASPHierNode_HasCustomMembers);

                if (IsCustomMember)
                    StreamUtils.WriteTag(writer, Tags.tgASPHierNode_IsCustomMember);

                StreamUtils.WriteTag(writer, Tags.tgASPHierNode_Checked);
                StreamUtils.WriteBoolean(writer, fChecked);

                if (fExpanded)
                    StreamUtils.WriteTag(writer, Tags.tgASPHierNode_Expanded);

                if (fParent != null)
                {
                    StreamUtils.WriteTag(writer, Tags.tgASPHierNode_ParentName);
                    StreamUtils.WriteString(writer, fParent.fMemberName);
                }

                if (fChildren.Count > 0)
                    StreamUtils.WriteStreamedObject(writer, fChildren, Tags.tgASPHierNode_Children);

                StreamUtils.WriteTag(writer, Tags.tgASPHierNode_EOT);
            }

            void IStreamedObject.ReadStream(BinaryReader reader, object options)
            {
                var editor = (HierarchyEditor) options;
                StreamUtils.CheckTag(reader, Tags.tgASPHierNode);
                for (var exit = false; !exit;)
                {
                    var tag = StreamUtils.ReadTag(reader);
                    switch (tag)
                    {
                        case Tags.tgASPHierNode_MemberName:
                            fMemberName = StreamUtils.ReadString(reader);
                            editor.allnodes.Add(fMemberName, this);
                            break;
                        case Tags.tgASPHierNode_HasCustomMembers:
                            hasCustomMembers = true;
                            break;
                        case Tags.tgASPHierNode_IsCustomMember:
                            IsCustomMember = true;
                            break;
                        case Tags.tgASPHierNode_Checked:
                            fChecked = StreamUtils.ReadBoolean(reader);
                            break;
                        case Tags.tgASPHierNode_Expanded:
                            fExpanded = true;
                            break;
                        case Tags.tgASPHierNode_ParentName:
                            editor.allnodes.TryGetValue(StreamUtils.ReadString(reader), out fParent);
                            break;
                        case Tags.tgASPHierNode_Children:
                            StreamUtils.ReadStreamedObject(reader, fChildren, editor);
                            break;
                        case Tags.tgASPHierNode_EOT:
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

        [Serializable]
        internal class Frame : List<Node>
        {
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            //internal Node root;
            StreamUtils.WriteTag(writer, Tags.tgASPHierEditor);

            if (!string.IsNullOrEmpty(FFilterString))
            {
                StreamUtils.WriteTag(writer, Tags.tgASPHierEditor_FilterString);
                StreamUtils.WriteString(writer, FFilterString);
            }

            if (!string.IsNullOrEmpty(fContextLevelName))
            {
                StreamUtils.WriteTag(writer, Tags.tgASPHierEditor_ContextLevelName);
                StreamUtils.WriteString(writer, fContextLevelName);
            }

            StreamUtils.WriteTag(writer, Tags.tgASPHierEditor_HierarchyName);
            StreamUtils.WriteString(writer, fHierarchyName);

            StreamUtils.WriteStreamedObject(writer, root, Tags.tgASPHierEditor_Root);

            StreamUtils.WriteTag(writer, Tags.tgASPHierEditor_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgASPHierEditor);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgASPHierEditor_FilterString:
                        FFilterString = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgASPHierEditor_HierarchyName:
                        fHierarchyName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgASPHierEditor_ContextLevelName:
                        fContextLevelName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgASPHierEditor_Root:
                        if (root == null) root = new Node(fGrid.fHierarchyEditorStyle.ItemsInPage);
                        StreamUtils.ReadStreamedObject(reader, root, this);
                        break;
                    case Tags.tgASPHierEditor_EOT:
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