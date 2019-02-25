using RadarSoft.RadarCube.Controls.Grid;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    public class LoadLayoutFileDialog // : HtmlInputFile
    {
        private OlapGrid Grid { get; }

        public LoadLayoutFileDialog(OlapGrid grid)
        {
            Grid = grid;
        }
        //public override void RenderControl(HtmlTextWriter writer)
        //{
        //    if (!DesignMode)
        //    {
        //        writer.AddAttribute(HtmlTextWriterAttribute.Id, "olapgrid_DLG_loadlayout_" + Grid.ClientID);
        //        writer.AddAttribute(HtmlTextWriterAttribute.Class, "rc_dialog");
        //        writer.RenderBeginTag(HtmlTextWriterTag.Div);
        //        writer.AddAttribute(HtmlTextWriterAttribute.Id, "olaptlw_loadwin");
        //        writer.RenderBeginTag(HtmlTextWriterTag.Div);
        //        writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "2");
        //        writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
        //        writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
        //        writer.RenderBeginTag(HtmlTextWriterTag.Table);
        //        writer.RenderBeginTag(HtmlTextWriterTag.Tr);
        //        writer.RenderBeginTag(HtmlTextWriterTag.Td);

        //        base.RenderControl(writer);

        //        writer.RenderEndTag(); // tr
        //        writer.RenderEndTag(); // td
        //        writer.RenderEndTag(); // table
        //        writer.RenderEndTag(); // div
        //        writer.RenderEndTag();//div
        //    }
        //}
    }
}