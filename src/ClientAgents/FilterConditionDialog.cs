using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.ClientAgents
{
    internal class FilterConditionDialog
    {
        internal static JsonDialog MakeHTML(Filter filter)
        {
            var result = new JsonDialog();
            var fGrid = filter.Level.Grid;

            var writer = new HtmlTextWriter();
            writer.AddAttribute(HtmlTextWriterAttribute.Width, "700");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            var formTitle = filter.FilterType != OlapFilterType.ftOnValue ? "rsfcTitleCaption" : "rsfcTitleValue";

            result.title = string.Format(RadarUtils.GetResStr(formTitle), filter.Level.DisplayName);

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.RenderBeginTag(HtmlTextWriterTag.Fieldset);
            writer.RenderBeginTag(HtmlTextWriterTag.Legend);
            writer.Write(RadarUtils.GetResStr("rsfcPrompt"));
            writer.RenderEndTag(); //legend

            writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            if (filter.FilterCondition == OlapFilterCondition.fcFirstTen)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "ODLG_cbTopBottom");
                writer.RenderBeginTag(HtmlTextWriterTag.Select);

                writer.AddAttribute(HtmlTextWriterAttribute.Value, "[0]");
                writer.RenderBeginTag(HtmlTextWriterTag.Option);
                writer.Write(RadarUtils.GetResStr("rsTop"));
                writer.RenderEndTag(); //option

                writer.AddAttribute(HtmlTextWriterAttribute.Value, "[1]");
                writer.RenderBeginTag(HtmlTextWriterTag.Option);
                writer.Write(RadarUtils.GetResStr("rsBottom"));
                writer.RenderEndTag(); //option

                writer.RenderEndTag(); //select

                writer.RenderEndTag(); //td
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "ODLG_tbFirst");
                writer.AddAttribute(HtmlTextWriterAttribute.Type, "text");
                writer.AddAttribute("maxlength", "5");
                writer.AddStyleAttribute("width", "100%");
                writer.AddAttribute(HtmlTextWriterAttribute.Value,
                    string.IsNullOrEmpty(filter.FirstValue) ? "10" : filter.FirstValue);
                writer.RenderBeginTag(HtmlTextWriterTag.Input);
                writer.RenderEndTag(); // input

                writer.RenderEndTag(); //td
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "ODLG_cbCondition2");
                writer.RenderBeginTag(HtmlTextWriterTag.Select);

                if (string.IsNullOrEmpty(filter.SecondValue))
                    writer.AddAttribute(HtmlTextWriterAttribute.Selected, "selected");
                writer.AddAttribute(HtmlTextWriterAttribute.Value, "[0]");
                writer.RenderBeginTag(HtmlTextWriterTag.Option);
                writer.Write(RadarUtils.GetResStr("rsItems"));
                writer.RenderEndTag(); //option

                if (!string.IsNullOrEmpty(filter.SecondValue) && filter.SecondValue.EndsWith(".[1]"))
                    writer.AddAttribute(HtmlTextWriterAttribute.Selected, "selected");
                writer.AddAttribute(HtmlTextWriterAttribute.Value, "[1]");
                writer.RenderBeginTag(HtmlTextWriterTag.Option);
                writer.Write(RadarUtils.GetResStr("rsPercentFromItemsCount"));
                writer.RenderEndTag(); //option

                if (!string.IsNullOrEmpty(filter.SecondValue) && filter.SecondValue.EndsWith(".[2]"))
                    writer.AddAttribute(HtmlTextWriterAttribute.Selected, "selected");
                writer.AddAttribute(HtmlTextWriterAttribute.Value, "[2]");
                writer.RenderBeginTag(HtmlTextWriterTag.Option);
                writer.Write(RadarUtils.GetResStr("rsSumma"));
                writer.RenderEndTag(); //option

                writer.RenderEndTag(); //select

                writer.RenderEndTag(); //td
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.Write(RadarUtils.GetResStr("rsIn"));

                writer.RenderEndTag(); //td
            }

            if (filter.FilterType == OlapFilterType.ftOnValue)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Width, "50%");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "ODLG_cbMeasures");
                writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                writer.RenderBeginTag(HtmlTextWriterTag.Select);

                for (var mCount = 0; mCount < fGrid.Measures.Count; mCount++)
                {
                    var M = fGrid.Measures[mCount];
                    if (filter.AppliesTo == M)
                        writer.AddAttribute(HtmlTextWriterAttribute.Selected, "selected");
                    writer.AddAttribute(HtmlTextWriterAttribute.Value, M.UniqueName);
                    writer.RenderBeginTag(HtmlTextWriterTag.Option);
                    writer.Write(M.DisplayName);
                    writer.RenderEndTag(); //option
                }

                writer.RenderEndTag(); //select

                writer.RenderEndTag(); //td
            }

            if (filter.FilterCondition != OlapFilterCondition.fcFirstTen)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, "1");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.Write(RadarUtils.GetResStr("rs" + filter.FilterCondition));
                writer.RenderEndTag(); //td

                var bw = filter.FilterCondition == OlapFilterCondition.fcBetween ||
                         filter.FilterCondition == OlapFilterCondition.fcNotBetween;
                writer.AddAttribute(HtmlTextWriterAttribute.Width, bw ? "25%" : "50%");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "ODLG_tbFirst");
                writer.AddAttribute(HtmlTextWriterAttribute.Type, "text");
                writer.AddStyleAttribute("width", "95%");
                if (!string.IsNullOrEmpty(filter.FirstValue))
                    writer.AddAttribute(HtmlTextWriterAttribute.Value, filter.FirstValue);
                writer.RenderBeginTag(HtmlTextWriterTag.Input);
                writer.RenderEndTag(); // input
                writer.RenderEndTag(); //td
            }

            if (filter.FilterCondition == OlapFilterCondition.fcNotBetween ||
                filter.FilterCondition == OlapFilterCondition.fcBetween)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Align, "center");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.Write(RadarUtils.GetResStr("rsAnd"));
                writer.RenderEndTag(); //td

                writer.AddAttribute(HtmlTextWriterAttribute.Width, "25%");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "ODLG_tbSecond");
                writer.AddAttribute(HtmlTextWriterAttribute.Type, "text");
                writer.AddStyleAttribute("width", "95%");
                if (!string.IsNullOrEmpty(filter.SecondValue))
                    writer.AddAttribute(HtmlTextWriterAttribute.Value, filter.SecondValue);
                writer.RenderBeginTag(HtmlTextWriterTag.Input);
                writer.RenderEndTag(); // input

                writer.RenderEndTag(); //td
            }

            writer.RenderEndTag(); //tr
            writer.RenderEndTag(); //table
            writer.RenderEndTag(); //fieldset

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); //tr
            writer.RenderEndTag(); //table

            result.buttons = new[]
                             {
                                 new JsonDialogButton
                                 {
                                     text = RadarUtils.GetResStr("rsOk"),
                                     code = "RadarSoft.$('#" + fGrid.ClientID +
                                            "').data('grid').applyContextFilter('cfilter|" + filter.MDXLevelName +
                                            "|" + filter.FilterType + "|" + filter.FilterCondition + "')"
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

        internal static JsonDialog MakeHTML(MeasureFilter filter)
        {
            var result = new JsonDialog();
            var fGrid = filter.Measure.Grid;

            var writer = new HtmlTextWriter();
            writer.AddAttribute(HtmlTextWriterAttribute.Width, "700");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            result.title = string.Format(RadarUtils.GetResStr("rsfcTitleValue"), filter.Measure.DisplayName);

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-widget-content");
            writer.RenderBeginTag(HtmlTextWriterTag.Fieldset);
            writer.RenderBeginTag(HtmlTextWriterTag.Legend);
            writer.Write(RadarUtils.GetResStr("rsfcPrompt"));
            writer.RenderEndTag(); //legend

            writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, "1");
            writer.AddAttribute(HtmlTextWriterAttribute.Width, "30%");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "ODLG_cond");
            writer.RenderBeginTag(HtmlTextWriterTag.Select);

            OlapFilterCondition[] value_filters =
            {
                OlapFilterCondition.fcEqual,
                OlapFilterCondition.fcNotEqual, OlapFilterCondition.fcLess,
                OlapFilterCondition.fcNotLess, OlapFilterCondition.fcGreater,
                OlapFilterCondition.fcNotGreater, OlapFilterCondition.fcBetween,
                OlapFilterCondition.fcNotBetween
            };

            for (var fCount = 0; fCount < value_filters.Length; fCount++)
            {
                var fc = value_filters[fCount];
                if (fc == filter.FilterCondition)
                    writer.AddAttribute(HtmlTextWriterAttribute.Selected, "selected");
                writer.AddAttribute(HtmlTextWriterAttribute.Value, fc.ToString());
                writer.RenderBeginTag(HtmlTextWriterTag.Option);
                writer.Write(RadarUtils.GetResStr("rs" + fc));
                writer.RenderEndTag(); //option
            }

            writer.RenderEndTag(); //select

            writer.RenderEndTag(); //td

            writer.AddAttribute(HtmlTextWriterAttribute.Width, "30%");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "ODLG_tbFirst");
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "text");
            writer.AddAttribute("class", "ui-widget-content");
            writer.AddStyleAttribute("width", "95%");
            if (!string.IsNullOrEmpty(filter.FirstValue))
                writer.AddAttribute(HtmlTextWriterAttribute.Value, filter.FirstValue);
            writer.RenderBeginTag(HtmlTextWriterTag.Input);
            writer.RenderEndTag(); // input

            writer.RenderEndTag(); //td

            writer.AddAttribute(HtmlTextWriterAttribute.Align, "center");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            if (filter.FilterCondition != OlapFilterCondition.fcNotBetween &&
                filter.FilterCondition != OlapFilterCondition.fcBetween)
                writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "none");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "ODLG_lbAnd");
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(RadarUtils.GetResStr("rsAnd"));
            writer.RenderEndTag(); //span
            writer.RenderEndTag(); //td

            writer.AddAttribute(HtmlTextWriterAttribute.Width, "30%");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "ODLG_tbSecond");
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "text");
            writer.AddAttribute("class", "ui-widget-content");
            writer.AddStyleAttribute("width", "95%");
            if (filter.FilterCondition != OlapFilterCondition.fcNotBetween &&
                filter.FilterCondition != OlapFilterCondition.fcBetween)
                writer.AddStyleAttribute("display", "none");

            if (!string.IsNullOrEmpty(filter.SecondValue))
                writer.AddAttribute(HtmlTextWriterAttribute.Value, filter.SecondValue);
            writer.RenderBeginTag(HtmlTextWriterTag.Input);
            writer.RenderEndTag(); // input

            writer.RenderEndTag(); //td

            writer.RenderEndTag(); //tr

            if (fGrid.Cube.GetProductID() == RadarUtils.rsAspNetDesktop)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.AddAttribute(HtmlTextWriterAttribute.Colspan, "4");
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                writer.Write(RadarUtils.GetResStr("rsRestricts"));

                writer.AddStyleAttribute(HtmlTextWriterStyle.MarginLeft, "10px");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);


                writer.AddAttribute(HtmlTextWriterAttribute.Id, "ODLG_RestrictsAggregates");
                writer.AddAttribute(HtmlTextWriterAttribute.Type, "radio");
                if (filter.RestrictsTo == MeasureFilterRestriction.mfrAggregatedValues)
                    writer.AddAttribute("checked");
                writer.RenderBeginTag(HtmlTextWriterTag.Input);
                writer.RenderEndTag(); // input

                writer.Write(RadarUtils.GetResStr("rsAggregatedValues"));

                writer.WriteBreak();

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "ODLG_RestrictsFacts");
                writer.AddAttribute(HtmlTextWriterAttribute.Type, "radio");
                if (filter.RestrictsTo == MeasureFilterRestriction.mfrFactTable)
                    writer.AddAttribute("checked");
                writer.RenderBeginTag(HtmlTextWriterTag.Input);
                writer.RenderEndTag(); // input

                writer.Write(RadarUtils.GetResStr("rsFactTableValues"));
                writer.RenderEndTag(); //div

                writer.RenderEndTag(); //td
                writer.RenderEndTag(); //tr
            }

            writer.RenderEndTag(); //table
            writer.RenderEndTag(); //fieldset

            writer.RenderEndTag(); //td
            writer.RenderEndTag(); //tr

            writer.RenderEndTag(); //table

            result.buttons = new[]
                             {
                                 new JsonDialogButton
                                 {
                                     text = RadarUtils.GetResStr("rsApply"),
                                     code = "RadarSoft.$('#" + fGrid.ClientID +
                                            "').data('grid').applyMeasureFilter('cmfilter|" +
                                            filter.Measure.UniqueName + "')"
                                 },
                                 new JsonDialogButton
                                 {
                                     text = RadarUtils.GetResStr("repResetFilter"),
                                     code = "RadarSoft.$('#" + fGrid.ClientID +
                                            "').data('grid').clearMeasureFilter('" + filter.Measure.UniqueName +
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
    }
}