using RadarSoft.RadarCube.Attributes;

namespace RadarSoft.RadarCube.Enums
{
    public enum TrendType
    {
        [LocalizedDescription("rsTrendLine")] Line,
        [LocalizedDescription("rsTrendQuadratic")] Quadre,
        [LocalizedDescription("rsTrendCubic")] Cubic
    }
}