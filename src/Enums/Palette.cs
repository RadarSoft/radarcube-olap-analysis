using RadarSoft.RadarCube.Attributes;

namespace RadarSoft.RadarCube.Enums
{
    public enum Palette
    {
        [LocalizedDisplayName("rsPaletteColored")] Colored = 0,
        [LocalizedDisplayName("rsPaletteColoredLight")] ColoredLight = 1,
        [LocalizedDisplayName("rsPaletteGray")] Gray = 2,
        [LocalizedDisplayName("rsPaletteBlack")] Black = 3,
        [LocalizedDisplayName("rsPaletteWhite")] White = 4
    }
}