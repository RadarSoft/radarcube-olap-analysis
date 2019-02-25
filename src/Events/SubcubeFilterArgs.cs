using System;

namespace RadarSoft.RadarCube.Events
{
    /// <summary>
    ///     Provides data for the SubcubeFilter event.
    /// </summary>
    public class SubcubeFilterArgs : EventArgs
    {
        /// <summary>
        ///     A valid MDX subcube expression
        /// </summary>
        /// <example>
        ///     <code lang="CS" title="[New Example]">
        /// e.SubcubeExpression = "select {[Date].[Calendar].[Calendar Year].&amp;[2003]} on 0 from [Adventure Works]";
        /// </code>
        /// </example>
        public string SubcubeExpression { get; set; } = "";
    }
}