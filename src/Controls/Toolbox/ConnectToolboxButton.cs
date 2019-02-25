using System;
using System.Collections.Generic;
using RadarSoft.RadarCube.ClientAgents;
using RadarSoft.RadarCube.Html;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the "Connect" toolbox button
    /// </summary>
    public class ConnectToolboxButton : CommonToolboxButton
    {
        private static Guid fID = new Guid("9D25FF65-735D-479b-9416-355DA0C195B7");

        public override string ButtonID
        {
            get => fID.ToString();
            set
            {
                ;
            }
        }

        /// <summary>
        ///     Login Window settings
        /// </summary>
        public virtual LoginWindowSettings LoginWindowSettings { get; } = new LoginWindowSettings();

        protected override string RealImage()
        {
            if (string.IsNullOrEmpty(Image))
                return fOwner.ImageUrl("Connect.gif");

            return "";
        }

        protected override string GetDefaultClientScript()
        {
            return "RadarSoft.$('#" + GetGridID() + "').data('grid').showDialog('connectiondialog'); return false;";
        }

        protected override string RealTooltip()
        {
            if (string.IsNullOrEmpty(Tooltip))
                return RadarUtils.GetResStr("hint_ConnectTo");
            return Tooltip;
        }

        //internal virtual JsonDialog MakeDialog()
        //{
        //    throw new NotImplementedException();
        //}

        internal virtual JsonDialog MakeDialog()
        {
            var Cube = fOwner.Cube;
            var Grid = fOwner.OlapControl;
            var result = new JsonDialog();
            result.title = RadarUtils.GetResStr("rsConnectionDialogTitle");

            var writer = new HtmlTextWriter();
            var dbs = new List<string>();
            var cubes = new List<string>();
            var errorstr = "";
            if (Cube != null)
                if (!string.IsNullOrEmpty(LoginWindowSettings.ServerName))
                {
                    string s;
                    if (fOwner.MDCube.GetDatabasesList(LoginWindowSettings.ServerName,
                        null, out s))
                    {
                        dbs.Add("");
                        dbs.AddRange(s.Split('|'));
                        if (!string.IsNullOrEmpty(LoginWindowSettings.DatabaseName))
                            if (fOwner.MDCube.GetCubesList(LoginWindowSettings.ServerName,
                                LoginWindowSettings.DatabaseName, null, out s))
                                cubes.AddRange(s.Split('|'));
                            else
                                errorstr = s;
                    }
                    else
                    {
                        errorstr = s;
                    }
                }

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "olaptlw_connect");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "2");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            if (LoginWindowSettings.IsServerNameVisible)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                if (errorstr == "")
                    writer.AddStyleAttribute("display", "none");
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olaptlw_err");
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-state-error ui-corner-all");
                writer.AddStyleAttribute("padding", "0.7em");
                writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.RenderBeginTag(HtmlTextWriterTag.P);
                writer.AddStyleAttribute("float", "left");
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-icon ui-icon-alert");
                writer.AddStyleAttribute("margin-right", ".3em");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.RenderEndTag(); // span
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olaptlw_err_text");
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.Write(string.Format(LoginWindowSettings.ErrorString, errorstr));
                writer.RenderEndTag(); // span
                writer.RenderEndTag(); // p
                writer.RenderEndTag(); // div

                writer.RenderEndTag(); // error label td
                writer.RenderEndTag(); // error label tr

                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.Write(LoginWindowSettings.ServerLabel);
                writer.RenderEndTag(); // server label td
                writer.RenderEndTag(); // server label tr

                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olaptlw_servername");
                writer.AddAttribute(HtmlTextWriterAttribute.Type, "text");
                writer.AddStyleAttribute("width", "100%");
                if (!string.IsNullOrEmpty(LoginWindowSettings.ServerName))
                    writer.AddAttribute(HtmlTextWriterAttribute.Value, LoginWindowSettings.ServerName);
                writer.RenderBeginTag(HtmlTextWriterTag.Input);
                writer.RenderEndTag(); // input
                writer.RenderEndTag(); // server label td
                writer.RenderEndTag(); // server label tr
            }

            if (LoginWindowSettings.IsDatabaseNameVisible)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.Write(LoginWindowSettings.DatabaseLabel);
                writer.RenderEndTag(); // database label td
                writer.RenderEndTag(); // database label tr

                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olaptlw_db");
                writer.AddAttribute("contenteditable", "false");
                writer.AddStyleAttribute("width", "100%");
                foreach (var s_ in dbs)
                {
                    if (s_.ToLower() == LoginWindowSettings.DatabaseName)
                        writer.AddAttribute(HtmlTextWriterAttribute.Selected, "selected");

                    writer.AddAttribute(HtmlTextWriterAttribute.Value, s_);
                    writer.RenderBeginTag(HtmlTextWriterTag.Option);
                    writer.Write(s_);
                    writer.RenderEndTag(); //option
                }
                writer.RenderEndTag(); //select

                writer.RenderEndTag(); // buttons td
                writer.RenderEndTag(); // buttons tr
            }


            if (LoginWindowSettings.IsCubeNameVisible)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                writer.Write(LoginWindowSettings.CubeLabel);
                writer.RenderEndTag(); // cube label td
                writer.RenderEndTag(); // cube label tr

                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                writer.AddAttribute(HtmlTextWriterAttribute.Id, "olaptlw_cb");
                writer.AddAttribute("contenteditable", "false");
                writer.AddStyleAttribute("width", "100%");
                foreach (var s_ in cubes)
                {
                    if (s_.ToLower() == LoginWindowSettings.CubeName)
                        writer.AddAttribute(HtmlTextWriterAttribute.Selected, "selected");

                    writer.AddAttribute(HtmlTextWriterAttribute.Value, s_);
                    writer.RenderBeginTag(HtmlTextWriterTag.Option);
                    writer.Write(s_);
                    writer.RenderEndTag(); //option
                }
                writer.RenderEndTag(); //select

                writer.RenderEndTag(); // server label td
                writer.RenderEndTag(); // server label tr
            }

            writer.RenderEndTag(); // table

            writer.RenderEndTag(); // div

            var buttons = new List<JsonDialogButton>();

            var postback = "var grid = RadarSoft.$('#" + GetGridID() + "').data('grid');";
            postback += " var args = 'connect|' + RadarSoft.$('#olaptlw_cb').val();";
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