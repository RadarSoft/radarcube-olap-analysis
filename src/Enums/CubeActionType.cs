namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Enumerates the types of MSAS Actions
    /// </summary>
    public enum CubeActionType
    {
        caURL = 1,
        caReport = 128,
        caDataSet = 8,
        caProprietary = 64,
        caRowset = 16,
        caStatement = 4,
        csDrillthrough = 256
    }
}