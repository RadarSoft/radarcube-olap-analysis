using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.ClientAgents
{
    internal class MakeCalculated
    {
        private static string MakeProperName(IDescriptionable source)
        {
            var un = source.UniqueName;
            if (un.Length == 36 && un[8] == '-' && un[8] == '-' && un[13] == '-' && un[23] == '-')
            {
                var m = source as Measure;
                if (m != null) return "[Measures].[" + m.DisplayName + "]";

                var cm = source as CubeMeasure;
                if (cm != null) return "[Measures].[" + cm.DisplayName + "]";

                var d = source as Dimension;
                if (d != null) return "[" + d.DisplayName + "]";

                var h = source as Hierarchy;
                if (h != null) return "[" + h.Dimension.DisplayName + "].[" + h.DisplayName + "]";

                var l = source as Level;
                if (l != null)
                    return "[" + l.Hierarchy.Dimension.DisplayName + "].[" + l.Hierarchy.DisplayName + "].[" +
                           l.DisplayName + "]";

                var mm = source as Member;
                if (mm != null)
                    return "[" + mm.Level.Hierarchy.Dimension.DisplayName + "].[" + mm.Level.Hierarchy.DisplayName +
                           "].[" + mm.Level.DisplayName + "].[" + mm.DisplayName + "]";
            }
            return un;
        }


        internal static JsonDialog MakeHTML(OlapControl grid, Measure measure)
        {
            var result = new JsonDialog();
            result.width = 500;
            string uniqueName;
            string displayName;
            string expression;
            string format;
            if (measure == null)
            {
                uniqueName = "NULL";
                displayName = RadarUtils.GetResStr("rsTypeNameMeasure");
                expression = "";
                format = "Standard";
            }
            else
            {
                uniqueName = measure.UniqueName;
                displayName = measure.DisplayName;
                expression = measure.Expression;
                format = measure.DefaultFormat;
            }

            var writer = new HtmlTextWriter();
            writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            if (measure == null)
                result.title = RadarUtils.GetResStr("d_edNewCalculatedMeas");
            else
                result.title = string.Format(RadarUtils.GetResStr("rsEditPropName"), measure.DisplayName);

            if (measure == null)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                writer.Write(RadarUtils.GetResStr("exprt_Caption") + ":");

                writer.RenderEndTag(); //td
                writer.RenderEndTag(); // tr

                writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingLeft, "20px");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "OCM_tbDisplayName");
                writer.AddAttribute(HtmlTextWriterAttribute.Type, "text");
                writer.AddAttribute("class", "ui-widget-content");
                writer.AddStyleAttribute("width", "95%");
                writer.AddAttribute(HtmlTextWriterAttribute.Value, displayName);
                writer.RenderBeginTag(HtmlTextWriterTag.Input);
                writer.RenderEndTag(); // input


                writer.RenderEndTag(); //td
                writer.RenderEndTag(); // tr
            }

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.Write(RadarUtils.GetResStr("rsFormat") + ":");

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); // tr

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingLeft, "20px");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            //Ravish Begin
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "OCM_tbFormat");
            writer.RenderBeginTag(HtmlTextWriterTag.Select);

            writer.AddAttribute(HtmlTextWriterAttribute.Value, "#");
            writer.RenderBeginTag(HtmlTextWriterTag.Option);
            writer.Write("#");
            writer.RenderEndTag(); //option

            writer.AddAttribute(HtmlTextWriterAttribute.Value, "#,#");
            writer.RenderBeginTag(HtmlTextWriterTag.Option);
            writer.Write("#,#");
            writer.RenderEndTag(); //option

            writer.AddAttribute(HtmlTextWriterAttribute.Value, "#,#.0");
            writer.RenderBeginTag(HtmlTextWriterTag.Option);
            writer.Write("#,#.0");
            writer.RenderEndTag(); //option

            writer.AddAttribute(HtmlTextWriterAttribute.Value, "#,#.00");
            writer.RenderBeginTag(HtmlTextWriterTag.Option);
            writer.Write("#,#.00");
            writer.RenderEndTag(); //option

            writer.AddAttribute(HtmlTextWriterAttribute.Selected, "selected");
            writer.AddAttribute(HtmlTextWriterAttribute.Value, "Standard");
            writer.RenderBeginTag(HtmlTextWriterTag.Option);
            writer.Write("Standard");
            writer.RenderEndTag(); //option

            writer.AddAttribute(HtmlTextWriterAttribute.Value, "Currency");
            writer.RenderBeginTag(HtmlTextWriterTag.Option);
            writer.Write("Currency");
            writer.RenderEndTag(); //option

            writer.AddAttribute(HtmlTextWriterAttribute.Value, "Short Date");
            writer.RenderBeginTag(HtmlTextWriterTag.Option);
            writer.Write("Short Date");
            writer.RenderEndTag(); //option

            writer.AddAttribute(HtmlTextWriterAttribute.Value, "Short Time");
            writer.RenderBeginTag(HtmlTextWriterTag.Option);
            writer.Write("Short Time");
            writer.RenderEndTag(); //option

            writer.AddAttribute(HtmlTextWriterAttribute.Value, "Percent");
            writer.RenderBeginTag(HtmlTextWriterTag.Option);
            writer.Write("Percent");
            writer.RenderEndTag(); //option

            writer.RenderEndTag(); //select

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); // tr


            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            //Ravish Begin
            writer.AddStyleAttribute(HtmlTextWriterStyle.VerticalAlign, "bottom");
            //Ravish End
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.Write(RadarUtils.GetResStr("rsExpression") + ":");

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); // tr

            //Ravish Begin
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingLeft, "20px");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);


            writer.AddAttribute(HtmlTextWriterAttribute.Id, "OCM_tbMeasures");
            writer.RenderBeginTag(HtmlTextWriterTag.Select);

            for (var mCount = 0; mCount < grid.Cube.Measures.Count; mCount++)
                if (grid.Cube.Measures[mCount].VisibleInTree)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Value, MakeProperName(grid.Cube.Measures[mCount]));
                    writer.RenderBeginTag(HtmlTextWriterTag.Option);
                    writer.Write(grid.Cube.Measures[mCount].DisplayName);
                    writer.RenderEndTag(); //option
                }

            writer.RenderEndTag(); //select


            writer.RenderEndTag(); //td
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "rs_uibtn");
            writer.AddAttribute("onclick", "RadarSoft.$('#" + grid.ClientID + "').data('grid').insertMeasure()");
            writer.AddStyleAttribute("margin-left", "5px");
            writer.RenderBeginTag(HtmlTextWriterTag.Button);
            writer.AddStyleAttribute(HtmlTextWriterStyle.WhiteSpace, "nowrap");
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(RadarUtils.GetResStr("rsInsert"));
            writer.RenderEndTag(); // span
            writer.RenderEndTag(); // button

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); // tr
            writer.RenderEndTag(); // table

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); // tr
            //Ravish End

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingLeft, "20px");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-widget-content");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "OCM_tbExpression");
            writer.AddAttribute(HtmlTextWriterAttribute.Value, expression);
            writer.AddStyleAttribute("width", "95%");
            writer.AddStyleAttribute("height", "150px");
            writer.RenderBeginTag(HtmlTextWriterTag.Textarea);
            writer.RenderEndTag(); // textarea

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); // tr

            writer.RenderEndTag(); // table

            result.buttons = new[]
                             {
                                 new JsonDialogButton
                                 {
                                     text = RadarUtils.GetResStr("rsOk"),
                                     code = "RadarSoft.$('#" + grid.ClientID +
                                            "').data('grid').applyCalculated('applycalcmeasure|" + uniqueName +
                                            "')"
                                 },
                                 new JsonDialogButton
                                 {
                                     text = RadarUtils.GetResStr("rsCancel"),
                                     code = "RadarSoft.$(this).dialog('close')"
                                 }
                             };

            result.data = writer.ToString();
            return result;
        }

        internal static JsonDialog MakeHTMLMember(OlapControl grid, Level level, CalculatedMember member)
        {
            var result = new JsonDialog();
            result.width = 500;
            string uniqueName;
            string displayName;
            string expression;
            if (member == null)
            {
                uniqueName = "NULL";
                displayName = RadarUtils.GetResStr("rsTypeNameMember");
                expression = "0";
            }
            else
            {
                uniqueName = member.UniqueName;
                displayName = member.DisplayName;
                expression = member.Expression;
            }

            var writer = new HtmlTextWriter();
            writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            if (member == null)
                result.title = RadarUtils.GetResStr("rsNewCalculatedMember");
            else
                result.title = string.Format(RadarUtils.GetResStr("rsEditPropName"), member.DisplayName);

            if (member == null)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                writer.Write(RadarUtils.GetResStr("exprt_Caption") + ":");

                writer.RenderEndTag(); //td
                writer.RenderEndTag(); // tr

                writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingLeft, "20px");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "OCM_tbDisplayName");
                writer.AddAttribute(HtmlTextWriterAttribute.Type, "text");
                writer.AddAttribute("class", "ui-widget-content");
                writer.AddStyleAttribute("width", "95%");
                writer.AddAttribute(HtmlTextWriterAttribute.Value, displayName);
                writer.RenderBeginTag(HtmlTextWriterTag.Input);
                writer.RenderEndTag(); // input

                writer.RenderEndTag(); //td
                writer.RenderEndTag(); // tr
            }

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.Write(RadarUtils.GetResStr("rsExpression") + ":");

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); // tr

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingLeft, "20px");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-widget-content");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "OCM_tbExpression");
            writer.AddAttribute(HtmlTextWriterAttribute.Value, expression);
            writer.AddStyleAttribute("width", "95%");
            writer.AddStyleAttribute("height", "150px");
            writer.RenderBeginTag(HtmlTextWriterTag.Textarea);
            writer.RenderEndTag(); // textarea

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); // tr
            writer.RenderEndTag(); // table

            result.buttons = new[]
                             {
                                 new JsonDialogButton
                                 {
                                     text = RadarUtils.GetResStr("rsOk"),
                                     code = "RadarSoft.$('#" + grid.ClientID +
                                            "').data('grid').applyCalculated('applycalcmember|" +
                                            level.UniqueName + "|" + uniqueName + "')"
                                 },
                                 new JsonDialogButton
                                 {
                                     text = RadarUtils.GetResStr("rsCancel"),
                                     code = "RadarSoft.$(this).dialog('close')"
                                 }
                             };

            result.data = writer.ToString();
            return result;
        }
    }
}