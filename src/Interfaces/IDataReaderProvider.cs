using System.Data;

namespace RadarSoft.RadarCube.Interfaces
{
    public interface IDataReaderProvider
    {
        IDataReader DataReader { get; }
    }
}