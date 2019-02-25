using System;

namespace RadarSoft.RadarCube.Serialization
{
    /// <summary>
    ///     The type defines what content should be saved to the stream when the Grid saves
    ///     its data.
    /// </summary>
    [Flags]
    public enum StreamContent
    {
        /// <summary>
        ///     Nothing should be saved at all
        /// </summary>
        None = 0x00,

        /// <summary>
        ///     Only cube-specific data is saved. Thus on restoring such a stream back the grid will load the cube data and
        ///     will try to keep the grid state unchanged as long as possible.
        /// </summary>
        CubeData = 0x01,

        /// <summary>
        ///     Only rid-specific data is saved. Thus on restoring such a stream back the cube will remain unchanged,
        ///     however the grid will give it a try to restore the state having been saved in the stream.
        /// </summary>
        GridState = 0x02,
        GridAppearance = 0x04,

        /// <summary>
        ///     The combination of CubeData and GridState. Both cube-specific and grid-specific data is saved.
        ///     On restoring such a stream back the grid will first load the cube data and reactivate it.
        ///     Then the grid will load and restore the grid state having been saved in the stream.
        /// </summary>
        All = CubeData | GridState | GridAppearance
    }
}