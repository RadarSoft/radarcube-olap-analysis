using System.IO;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    public interface IStreamedObject
    {
        void WriteStream(BinaryWriter writer, object options);
        void ReadStream(BinaryReader reader, object options);
    }
}