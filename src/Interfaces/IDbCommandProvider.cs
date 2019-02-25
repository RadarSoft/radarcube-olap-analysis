using System.Data;

namespace RadarSoft.RadarCube.Interfaces
{
    public interface IDbCommandProvider
    {
        IDbCommand DbCommand { get; }
    }
}