using System.ComponentModel;

namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Enumerates the types of RadarCube's behavior upon pivoting the hierarchies in the row or column area.
    /// </summary>
    public enum PivotingBehavior
    {
        /// <summary>
        ///     Drilling down to the nodes of a new hierarchy pivoted to the active area is performed automatically.
        /// </summary>
        [Description("Excel 2010")] Excel2010,

        /// <summary>
        ///     Drilling down to the nodes of a new hierarchy pivoted to the active area must be performed manually.
        /// </summary>
        [Description("Radar Cube")] RadarCube
    }
}