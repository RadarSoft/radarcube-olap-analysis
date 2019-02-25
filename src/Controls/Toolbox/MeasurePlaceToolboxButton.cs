using System;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the "Measures place" toolbox button
    /// </summary>
    public class MeasurePlaceToolboxButton : CommonToolboxButton
    {
        private static Guid fID = new Guid("CA761232-ED42-11CE-BACD-00AA0057B223");

        public override string ButtonID
        {
            get => fID.ToString();
            set
            {
                ;
            }
        }

        public override string Text
        {
            get
            {
                if (base.Text.IsNullOrEmpty())
                    return RadarUtils.GetResStr("rsMeasuresPosition");
                return base.Text;
            }
            set => base.Text = value;
        }

        protected override string RealTooltip()
        {
            if (string.IsNullOrEmpty(Tooltip))
                return "Defines placement for measures";
            return Tooltip;
        }

        internal override void RenderContents(HtmlTextWriter writer)
        {
            var grid = fOwner.OlapControl;

            if (RealTooltip() != "")
                writer.AddAttribute(HtmlTextWriterAttribute.Title, RealTooltip());

            writer.AddAttribute(HtmlTextWriterAttribute.Style, "margin-right: 1px");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_measureplace_toolbox_button");

            writer.RenderBeginTag(HtmlTextWriterTag.Button);

            writer.AddStyleAttribute(HtmlTextWriterStyle.WhiteSpace, "nowrap");
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(Text);
            writer.RenderEndTag(); // span

            writer.RenderEndTag(); // button

            writer.AddStyleAttribute(HtmlTextWriterStyle.Display, "none");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Position, "absolute");
            writer.AddStyleAttribute(HtmlTextWriterStyle.ZIndex, "1000");
            writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, "center");
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rs_measureplace_menu ui-widget-content");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingTop, "3px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingLeft, "3px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, "left");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_measuresplace");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.AddAttribute(HtmlTextWriterAttribute.Name, "rc_measuresplace_order");
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "radio");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "rc_measuresplace_order_beforall" + grid.ClientID);
            writer.AddAttribute(HtmlTextWriterAttribute.Onclick,
                "RadarSoft.$('#" + grid.ClientID + "').data('grid').setMeasuresPosition(true)");
            if (grid.FLayout.MeasurePosition == MeasurePosition.mpFirst)
                writer.AddAttribute(HtmlTextWriterAttribute.Checked, "checked");
            writer.RenderBeginTag(HtmlTextWriterTag.Input);
            writer.RenderEndTag(); //input
            writer.AddAttribute(HtmlTextWriterAttribute.For, "rc_measuresplace_order_beforall" + grid.ClientID);
            writer.RenderBeginTag(HtmlTextWriterTag.Label);
            writer.Write(RadarUtils.GetResStr("rsBeforeAll"));
            writer.RenderEndTag(); //lable

            writer.AddAttribute(HtmlTextWriterAttribute.Name, "rc_measuresplace_order");
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "radio");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "rc_measuresplace_order_afterall" + grid.ClientID);
            writer.AddAttribute(HtmlTextWriterAttribute.Onclick,
                "RadarSoft.$('#" + grid.ClientID + "').data('grid').setMeasuresPosition(false)");
            if (grid.FLayout.MeasurePosition == MeasurePosition.mpLast)
                writer.AddAttribute(HtmlTextWriterAttribute.Checked, "checked");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "rc_measuresplace_order_afterall" + grid.ClientID);
            writer.RenderBeginTag(HtmlTextWriterTag.Input);
            writer.RenderEndTag(); //input
            writer.AddAttribute(HtmlTextWriterAttribute.For, "rc_measuresplace_order_afterall" + grid.ClientID);
            writer.RenderBeginTag(HtmlTextWriterTag.Label);
            writer.Write(RadarUtils.GetResStr("rsAfterAll"));
            writer.RenderEndTag(); //lable
            writer.RenderEndTag(); //div
            writer.RenderEndTag(); //td
            writer.RenderEndTag(); //tr

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingTop, "3px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingLeft, "3px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.PaddingBottom, "3px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, "left");
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_measuresplace");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            writer.AddAttribute(HtmlTextWriterAttribute.Name, "rc_measuresplace_position");
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "radio");
            writer.AddAttribute(HtmlTextWriterAttribute.Onclick,
                "RadarSoft.$('#" + grid.ClientID + "').data('grid').setMeasuresArea(false)");
            if (grid.FLayout.MeasureLayout == LayoutArea.laRow)
                writer.AddAttribute(HtmlTextWriterAttribute.Checked, "checked");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "rc_measuresplace_position_row" + grid.ClientID);
            writer.RenderBeginTag(HtmlTextWriterTag.Input);
            writer.RenderEndTag(); //input
            writer.AddAttribute(HtmlTextWriterAttribute.For, "rc_measuresplace_position_row" + grid.ClientID);
            writer.RenderBeginTag(HtmlTextWriterTag.Label);
            writer.Write(RadarUtils.GetResStr("rsRowArea"));
            writer.RenderEndTag(); //lable

            writer.AddAttribute(HtmlTextWriterAttribute.Name, "rc_measuresplace_position");
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "radio");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "rc_measuresplace_position_column" + grid.ClientID);
            writer.AddAttribute(HtmlTextWriterAttribute.Onclick,
                "RadarSoft.$('#" + grid.ClientID + "').data('grid').setMeasuresArea(true)");
            if (grid.FLayout.MeasureLayout == LayoutArea.laColumn)
                writer.AddAttribute(HtmlTextWriterAttribute.Checked, "checked");
            writer.RenderBeginTag(HtmlTextWriterTag.Input);
            writer.RenderEndTag(); //input
            writer.AddAttribute(HtmlTextWriterAttribute.For, "rc_measuresplace_position_column" + grid.ClientID);
            writer.RenderBeginTag(HtmlTextWriterTag.Label);
            writer.Write(RadarUtils.GetResStr("rsColumnArea"));
            writer.RenderEndTag(); //lable

            writer.RenderEndTag(); //div
            writer.RenderEndTag(); //td
            writer.RenderEndTag(); //tr

            writer.RenderEndTag(); //table

            writer.RenderEndTag(); // div
        }
    }
}