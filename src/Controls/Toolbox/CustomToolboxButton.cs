namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     Represents the user button which can be added to the OLAP toolbox optionally.
    /// </summary>
    public class CustomToolboxButton : CommonToolboxButton
    {
        protected override string GetDefaultClientScript()
        {
            if (ClientScript != "")
                return "{ " + ClientScript + " }";

            return "";
        }
    }
}