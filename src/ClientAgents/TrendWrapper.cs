using System.Drawing;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.ClientAgents
{
    public class TrendWrapper : Wrapper<Color, TrendType>
    {
        internal TrendWrapper(Color c, TrendType tt)
            : base(c, tt)
        {
        }

        internal string WrapperToString()
        {
            var str = Value1 + " " + Value2;

            return str;
        }
    }
}