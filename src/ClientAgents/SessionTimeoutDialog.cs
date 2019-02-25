using System;
using System.Collections.Generic;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.ClientAgents
{
    internal class SessionTimeoutDialog
    {
        internal static JsonDialog RenderMassage(OlapControl Grid, Exception E)
        {
            var result = new JsonDialog();
            result.title = RadarUtils.GetResStr("rsErrorMessage");

            var buttons = new List<JsonDialogButton>();
            if (!string.IsNullOrEmpty(Grid.SupportEMail))
                buttons.Add(new JsonDialogButton
                            {
                                text = "Refresh", //RadarUtils.GetResStr("rsSendToSupport"),
                                code = "window.location = window.location.pathname; " +
                                       "RadarSoft.$(this).dialog('close');"
                            });

            buttons.Add(new JsonDialogButton
                        {
                            text = RadarUtils.GetResStr("rsClose"),
                            code = "RadarSoft.$(this).dialog('close')"
                        });

            result.data = "<div width='500'>" +
                          "<div style='width:490px;margin:5px;overflow:auto;border:2px inset #C0C0C0;;color:#404040;;height:90px;'>" +
                          E.Message.Replace("\n", "<br />") +
                          "</div>" +
                          "</div>";
            result.buttons = buttons.ToArray();
            return result;
        }
    }
}