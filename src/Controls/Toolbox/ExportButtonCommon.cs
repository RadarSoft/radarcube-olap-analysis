namespace RadarSoft.RadarCube.Controls.Toolbox
{
    public abstract class ExportButtonCommon : CommonToolboxButton
    {
        /// <summary>
        ///     The name of the file to export (without extension)
        /// </summary>
        public abstract string FileName { get; set; }
    }
}