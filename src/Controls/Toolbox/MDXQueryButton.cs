using System;
using System.Collections.Generic;
using RadarSoft.RadarCube.ClientAgents;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the "MDX Query" toolbox button
    /// </summary>
    public class MDXQueryButton : CommonToolboxButton
    {
        private const string _MDXQueryPrompt = "MDX Query:";
        private static Guid fID = new Guid("A567C26D-B9ED-4ac9-9FB3-CC73237999D3");

        public override string ButtonID
        {
            get => fID.ToString();
            set
            {
                ;
            }
        }

        /// <summary>
        ///     The MDX query is to be executed.
        /// </summary>
        public string MDX { get; set; }

        /// <summary>
        ///     The \"MDX query\" prompt.
        /// </summary>
        public string MDXQueryPrompt { get; set; } = "MDX Query";

        protected override string RealImage()
        {
            //if (string.IsNullOrEmpty(Image))
            //    return fOwner.ImageUrl("ToolMDXQuery.gif");

            return "";
        }

        protected override string RealTooltip()
        {
            if (string.IsNullOrEmpty(Tooltip))
                return "Execute MDX query";
            return Tooltip;
        }

        protected override string GetDefaultClientScript()
        {
            return "RadarSoft.$('#" + GetGridID() + "').data('grid').showDialog('mdxdialog'); return false;";
        }

        internal JsonDialog MakeDialog()
        {
            var result = new JsonDialog();
            result.title = MDXQueryPrompt;

            var writer = new HtmlTextWriter();

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "olaptlw_mdxwin");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "2");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            writer.RenderBeginTag(HtmlTextWriterTag.Tr);
            writer.RenderBeginTag(HtmlTextWriterTag.Td);

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-widget-content");
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "olaptlw_mdxtext");
            writer.AddAttribute(HtmlTextWriterAttribute.Value, MDX);
            writer.AddAttribute(HtmlTextWriterAttribute.Cols, "70");
            writer.AddAttribute(HtmlTextWriterAttribute.Rows, "10");
            writer.RenderBeginTag(HtmlTextWriterTag.Textarea);
            writer.RenderEndTag(); // textarea

            writer.RenderEndTag(); // td
            writer.RenderEndTag(); // tr

            writer.RenderEndTag(); // table

            writer.RenderEndTag(); // div

            var buttons = new List<JsonDialogButton>();

            var postback = "var grid = RadarSoft.$('#" + GetGridID() + "').data('grid');";
            postback += " var args = 'execmdx|' + RadarSoft.$('#olaptlw_mdxtext').val();";
            postback += " grid.postback(args); RadarSoft.$(this).dialog('close');";

            buttons.Add(new JsonDialogButton
                        {
                            text = RadarUtils.GetResStr("rsOk"),
                            code = postback
                        });

            buttons.Add(new JsonDialogButton
                        {
                            text = RadarUtils.GetResStr("rsCancel"),
                            code = "RadarSoft.$(this).dialog('close')"
                        });

            result.data = writer.ToString();
            result.buttons = buttons.ToArray();
            return result;
        }
    }
}