using System.ComponentModel;

namespace RadarSoft.RadarCube.ClientAgents
{
    public enum ImagePosition
    {
        /// <summary>
        ///     Left on text
        /// </summary>
        [Description("Left on text")] ipLeftOnText,

        /// <summary>
        ///     Right on text
        /// </summary>
        [Description("Right on text")] ipRightOnText,

        /// <summary>
        ///     Top on text.
        /// </summary>
        [Description("Top on text")] ipTopOnText,

        /// <summary>
        ///     Bottom on text
        /// </summary>
        [Description("Bottom on text")] ipBottomOnText,

        /// <summary>
        ///     Behind on text
        /// </summary>
        [Description("Behind on text")] ipBehindOnText
    }
}