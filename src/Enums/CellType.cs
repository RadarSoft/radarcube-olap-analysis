namespace RadarSoft.RadarCube.Enums
{
    /// <summary>Enumerates the real type of the Grid cell (level, member or data).</summary>
    public enum CellType
    {
        /// <summary>
        ///     A real interface for this object is IDataCell.
        /// </summary>
        ctData,

        /// <summary>
        ///     A real interface for this object is IMemberCell.
        /// </summary>
        ctMember,

        /// <summary>
        ///     A real interface for this object is ILevelCell.
        /// </summary>
        ctLevel,

        /// <summary>
        ///     A real interface for this object is ICell.
        /// </summary>
        ctNone,
        ctChart
    }
}