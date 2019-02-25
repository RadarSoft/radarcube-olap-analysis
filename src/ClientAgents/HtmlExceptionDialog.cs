using System;
using System.Collections.Generic;
using System.Net;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.ClientAgents
{
    internal class HtmlExceptionDialog
    {
        internal static JsonDialog RenderException(Controls.Cube.RadarCube Cube, OlapControl Grid, Exception E)
        {
            var result = new JsonDialog();
            result.title = RadarUtils.GetResStr("rsException");

            var writer = new HtmlTextWriter();
            writer.AddAttribute(HtmlTextWriterAttribute.Width, "500");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderBeginTag(HtmlTextWriterTag.P);
            writer.Write(string.Format(RadarUtils.GetResStr("rsExceptionRaised"), E.GetType().FullName));
            writer.RenderEndTag(); //p
            writer.RenderBeginTag(HtmlTextWriterTag.P);
            writer.RenderBeginTag(HtmlTextWriterTag.B);
            writer.Write(string.Format(RadarUtils.GetResStr("rsErrorMessage"), E.GetType().FullName));
            writer.RenderEndTag(); //b
            writer.RenderEndTag(); //p

            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "490px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Margin, "5px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "auto");
            writer.AddStyleAttribute("border", "2px inset #C0C0C0;");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "#404040;");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Height, "90px");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.Write(E.Message.Replace("\n", "<br />"));
            writer.RenderEndTag(); //div

            writer.RenderBeginTag(HtmlTextWriterTag.P);
            writer.Write(string.Format(RadarUtils.GetResStr("rsAdditionalInfo"), E.GetType().FullName));
            writer.RenderEndTag(); //p


            writer.AddStyleAttribute(HtmlTextWriterStyle.Width, "490px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Margin, "5px");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Overflow, "auto");
            writer.AddStyleAttribute("border", "2px inset #C0C0C0;");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "#404040;");
            writer.AddStyleAttribute(HtmlTextWriterStyle.Height, "350px");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            if (E.StackTrace != null)
            {
                writer.Write("----- The stack trace -----<br />");
                writer.Write(WebUtility.HtmlEncode(E.StackTrace).Replace("\r\n", "<br />"));
            }

            if (Grid.callbackExceptionData != null)
            {
                writer.Write("<br />----- The additional information -----<br />");
                foreach (var item in Grid.callbackExceptionData)
                {
                    writer.Write(item.Key + ": ");
                    if (item.Value != null)
                        writer.Write(WebUtility.HtmlEncode(item.Value).Replace("|", "&#x7C;"));
                    else
                        writer.Write("NULL");
                }
            }
            writer.RenderEndTag(); //div
            writer.RenderEndTag(); //div

            var buttons = new List<JsonDialogButton>();
            if (!string.IsNullOrEmpty(Grid.SupportEMail))
            {
                var subj = "RadarCube ASP.NET OLAP Grid error";
                if (Cube != null)
                {
                    if (Cube.GetProductID() == "RC-ASP-MSAS")
                        subj = "RadarCube ASP.NET for MSAS error";
                    if (Cube.GetProductID() == "RC-ASP-DESK")
                        subj = "RadarCube ASP.NET Desktop error";
                }

                buttons.Add(new JsonDialogButton
                            {
                                text = RadarUtils.GetResStr("rsSendToSupport"),
                                code = "window.location = window.location.pathname; " +
                                       //"window.open('mailto:" + Grid.SupportEMail + "?subject=" + DoEncode(subj) +
                                       //"&body=" + DoEncode(E.Message + "\n" + E.StackTrace) + "'); " +
                                       "RadarSoft.$(this).dialog('close');"
                            });
            }

            buttons.Add(new JsonDialogButton
                        {
                            text = RadarUtils.GetResStr("rsClose"),
                            code = "RadarSoft.$(this).dialog('close')"
                        });

            result.data = writer.ToString();
            result.buttons = buttons.ToArray();
            return result;
        }
    }
}