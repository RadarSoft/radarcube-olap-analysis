using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    internal static class StreamUtils
    {
        internal static void InvalidPropError(long Pos)
        {
            throw new StreamException(string.Format(RadarUtils.GetResStr("rsInvalidPropertyError"), Pos));
        }

        public static void WriteTag(BinaryWriter writer, Tags tag)
        {
            writer.Write((ushort) tag);
        }

        /// <exclude />
        public static Tags ReadTag(BinaryReader reader)
        {
            return (Tags) reader.ReadUInt16();
        }

        /// <exclude />
        public static void CheckTag(BinaryReader reader, Tags tag)
        {
            var t = (Tags) reader.ReadUInt16();
            if (t != tag)
            {
                reader.BaseStream.Seek(-sizeof(ushort), SeekOrigin.Current);
                throw new StreamException(string.Format(RadarUtils.GetResStr("rsInvalidTagError"),
                    reader.BaseStream.Position, string.Format("{0}({1:D})", tag, tag)));
            }
        }

        public static void WriteValueType(BinaryWriter writer, ValueType valueType)
        {
            writer.Write((byte) valueType);
        }

        public static ValueType ReadValueType(BinaryReader reader)
        {
            return (ValueType) reader.ReadByte();
        }

        public static ValueType NextValueType(BinaryReader reader)
        {
            var res = (ValueType) reader.ReadByte();
            reader.BaseStream.Seek(-1, SeekOrigin.Current);
            return res;
        }

        public static void SkipValue(BinaryReader reader)
        {
            switch (ReadValueType(reader))
            {
                case ValueType.Null:
                case ValueType.False:
                case ValueType.True:
                    break;
                case ValueType.Byte:
                    reader.ReadByte();
                    break;
                case ValueType.SByte:
                    reader.ReadSByte();
                    break;
                case ValueType.Int16:
                    reader.ReadInt16();
                    break;
                case ValueType.UInt16:
                    reader.ReadUInt16();
                    break;
                case ValueType.Int32:
                    reader.ReadInt32();
                    break;
                case ValueType.UInt32:
                    reader.ReadUInt32();
                    break;
                case ValueType.Int64:
                    reader.ReadInt64();
                    break;
                case ValueType.UInt64:
                    reader.ReadUInt64();
                    break;
                case ValueType.Char:
                    reader.ReadChar();
                    break;
                case ValueType.DateTime:
                    reader.ReadInt64();
                    break;
                case ValueType.Type:
                case ValueType.String:
                    reader.ReadString();
                    break;
                case ValueType.Guid:
                    reader.ReadBytes(0x10);
                    break;
                case ValueType.Object:
                    reader.BaseStream.Seek(-sizeof(ushort), SeekOrigin.Current);
                    ReadObject(reader);
                    break;
                case ValueType.Decimal:
                    reader.ReadDecimal();
                    break;
                case ValueType.Double:
                    reader.ReadDouble();
                    break;
                case ValueType.Single:
                    reader.ReadSingle();
                    break;
                case ValueType.Binary:
                    var int32 = reader.ReadInt32();
                    reader.BaseStream.Seek(int32, SeekOrigin.Current);
                    break;
                case ValueType.StreamedObject:
                case ValueType.TypedStreamedObject:
                case ValueType.Stream:
                    var int64 = reader.ReadInt64();
                    reader.BaseStream.Seek(int64, SeekOrigin.Current);
                    break;
                default:
                    InvalidPropError(reader.BaseStream.Position - sizeof(byte));
                    break;
            }
        }

        public static void WriteType(BinaryWriter writer, Type type)
        {
            WriteValueType(writer, ValueType.Type);
            writer.Write(type.ToString());
        }

        private static Type LookingForType(string name)
        {
            var t = Type.GetType(name);
            if (t != null) return t;
            var asm = RadarUtils.GetReferencingAssemblies();
            foreach (var a in asm)
            {
                t = a.GetType(name);
                if (t != null) return t;
            }
            return null;
        }

        public static Type ReadType(BinaryReader reader)
        {
            if (ReadValueType(reader) == ValueType.Type)
            {
                var s = reader.ReadString();
                var t = LookingForType(s);
                if (t != null) return t;
                return Type.GetType(s, true);
            }
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return null;
        }

        public static void WriteString(BinaryWriter writer, string s)
        {
            WriteValueType(writer, ValueType.String);
            if (s == null) writer.Write("");
            else writer.Write(s);
        }

        public static string ReadString(BinaryReader reader)
        {
            var vt = ReadValueType(reader);
            if (vt == ValueType.String || vt == ValueType.Type)
                return reader.ReadString();
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return null;
        }

        public static Guid ReadGuid(BinaryReader reader)
        {
            var vt = ReadValueType(reader);
            if (vt == ValueType.Guid)
            {
                var b = reader.ReadBytes(0x10);
                return new Guid(b);
            }
            if (vt == ValueType.String)
            {
                var s = reader.ReadString();
                return new Guid(s);
            }
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return Guid.Empty;
        }

        public static void WriteGuid(BinaryWriter writer, Guid guid)
        {
            WriteValueType(writer, ValueType.Guid);
            writer.Write(guid.ToByteArray());
        }

        public static void WriteBoolean(BinaryWriter writer, bool b)
        {
            if (b)
                WriteValueType(writer, ValueType.True);
            else
                WriteValueType(writer, ValueType.False);
        }

        public static bool ReadBoolean(BinaryReader reader)
        {
            return ReadValueType(reader) == ValueType.True;
        }

        public static void WriteByte(BinaryWriter writer, byte b)
        {
            WriteValueType(writer, ValueType.Byte);
            writer.Write(b);
        }

        public static byte ReadByte(BinaryReader reader)
        {
            if (ReadValueType(reader) == ValueType.Byte)
                return reader.ReadByte();
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return 0;
        }

        public static void WriteInt32(BinaryWriter writer, int value)
        {
            WriteValueType(writer, ValueType.Int32);
            writer.Write(value);
        }

        public static void WriteInt16(BinaryWriter writer, short value)
        {
            WriteValueType(writer, ValueType.Int16);
            writer.Write(value);
        }

        public static short ReadInt16(BinaryReader reader)
        {
            var vt = ReadValueType(reader);
            if (vt == ValueType.Int16)
                return reader.ReadInt16();
            if (vt == ValueType.Int32)
                return Convert.ToInt16(reader.ReadInt32());
            if (vt == ValueType.Int64)
                return Convert.ToInt16(reader.ReadInt64());
            if (vt == ValueType.Byte)
                return Convert.ToInt16(reader.ReadByte());
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return 0;
        }

        public static int ReadInt32(BinaryReader reader)
        {
            var vt = ReadValueType(reader);
            if (vt == ValueType.Int32)
                return reader.ReadInt32();
            if (vt == ValueType.Int16)
                return Convert.ToInt32(reader.ReadInt16());
            if (vt == ValueType.Int64)
                return Convert.ToInt32(reader.ReadInt64());
            if (vt == ValueType.Byte)
                return Convert.ToInt32(reader.ReadByte());
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return 0;
        }

        public static long ReadInt64(BinaryReader reader)
        {
            var vt = ReadValueType(reader);
            if (vt == ValueType.Int64)
                return reader.ReadInt64();
            if (vt == ValueType.Int16)
                return Convert.ToInt64(reader.ReadInt16());
            if (vt == ValueType.Int32)
                return Convert.ToInt64(reader.ReadInt32());
            if (vt == ValueType.Byte)
                return Convert.ToInt64(reader.ReadByte());
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return 0;
        }

        public static double ReadDouble(BinaryReader reader)
        {
            var vt = ReadValueType(reader);
            if (vt == ValueType.Double)
                return reader.ReadDouble();
            if (vt == ValueType.Single)
                return Convert.ToDouble(reader.ReadSingle());
            if (vt == ValueType.Int16)
                return Convert.ToDouble(reader.ReadInt16());
            if (vt == ValueType.Int32)
                return Convert.ToDouble(reader.ReadInt32());
            if (vt == ValueType.Byte)
                return Convert.ToDouble(reader.ReadByte());
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return 0;
        }

        public static float ReadSingle(BinaryReader reader)
        {
            var vt = ReadValueType(reader);
            if (vt == ValueType.Single)
                return reader.ReadSingle();
            if (vt == ValueType.Double)
                return Convert.ToSingle(reader.ReadDouble());
            if (vt == ValueType.Int16)
                return Convert.ToSingle(reader.ReadInt16());
            if (vt == ValueType.Int32)
                return Convert.ToSingle(reader.ReadInt32());
            if (vt == ValueType.Byte)
                return Convert.ToSingle(reader.ReadByte());
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return 0;
        }

        public static void WriteInt64(BinaryWriter writer, long value)
        {
            WriteValueType(writer, ValueType.Int64);
            writer.Write(value);
        }

        public static void WriteDouble(BinaryWriter writer, double value)
        {
            WriteValueType(writer, ValueType.Double);
            writer.Write(value);
        }

        public static void WriteSingle(BinaryWriter writer, float value)
        {
            WriteValueType(writer, ValueType.Single);
            writer.Write(value);
        }

        public static void WriteDateTime(BinaryWriter writer, DateTime value)
        {
            WriteValueType(writer, ValueType.DateTime);
            writer.Write(value.ToBinary());
        }

        public static DateTime ReadDateTime(BinaryReader reader)
        {
            var vt = ReadValueType(reader);
            if (vt == ValueType.DateTime)
                return DateTime.FromBinary(reader.ReadInt64());
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return DateTime.MinValue;
        }

        public static void WriteStreamedObject(BinaryWriter writer, IStreamedObject StreamedObject, Tags tag)
        {
            WriteStreamedObject(writer, StreamedObject, tag, null);
        }

        public static void WriteStreamedObject(BinaryWriter writer, IStreamedObject StreamedObject, Tags tag,
            object options)
        {
            if (StreamedObject == null) return;
            WriteTag(writer, tag);
            WriteValueType(writer, ValueType.StreamedObject);
            var pos = writer.BaseStream.Position;
            long L = 0;
            writer.Write(L); // Reserve the place for the size of the block
            StreamedObject.WriteStream(writer, options);
            var pos2 = writer.BaseStream.Position;
            writer.BaseStream.Seek(pos, SeekOrigin.Begin);
            L = pos2 - pos - sizeof(long);
            writer.Write(L);
            writer.BaseStream.Seek(pos2, SeekOrigin.Begin);
        }

        public static void WriteStreamedObject(BinaryWriter writer, IStreamedObject StreamedObject)
        {
            // This version doesn't write a tag before the StreamedObject
            if (StreamedObject == null) return;
            WriteValueType(writer, ValueType.StreamedObject);
            var pos = writer.BaseStream.Position;
            long L = 0;
            writer.Write(L); // Reserve the place for the size of the block
            StreamedObject.WriteStream(writer, null);
            var pos2 = writer.BaseStream.Position;
            writer.BaseStream.Seek(pos, SeekOrigin.Begin);
            L = pos2 - pos - sizeof(long);
            writer.Write(L);
            writer.BaseStream.Seek(pos2, SeekOrigin.Begin);
        }

        public static void WriteBinary(BinaryWriter writer, byte[] BinaryArray, Tags tag)
        {
            if (BinaryArray == null) return;
            WriteTag(writer, tag);
            WriteValueType(writer, ValueType.Binary);
            var L = BinaryArray.Length;
            writer.Write(L); // Size of the block
            writer.Write(BinaryArray);
        }

        public static byte[] ReadBinary(BinaryReader reader)
        {
            if (ReadValueType(reader) == ValueType.Binary)
            {
                var L = reader.ReadInt32();
                return reader.ReadBytes(L);
            }
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return null;
        }

        private static void DoWriteStream(BinaryWriter writer, Stream stream)
        {
            if (stream == null) return;
            stream.Seek(0, SeekOrigin.Begin);
            const int buf_size = 0x1000;
            var buffer = new byte[buf_size];
            for (var L = stream.Read(buffer, 0, buf_size); L > 0; L = stream.Read(buffer, 0, buf_size))
                writer.Write(buffer, 0, L);
        }

        public static void WriteStream(BinaryWriter writer, Stream stream, Tags tag)
        {
            if (stream == null) return;
            WriteTag(writer, tag);
            WriteValueType(writer, ValueType.Stream);
            writer.Write(stream.Length);
            DoWriteStream(writer, stream);
        }

        public static void WriteTypedStreamedObject(BinaryWriter writer, IStreamedObject StreamedObject, Tags tag)
        {
            WriteTypedStreamedObject(writer, StreamedObject, tag, null);
        }

        public static void WriteTypedStreamedObject(BinaryWriter writer, IStreamedObject StreamedObject, Tags tag,
            object options)
        {
            if (StreamedObject == null) return;
            WriteTag(writer, tag);
            WriteValueType(writer, ValueType.TypedStreamedObject);
            var pos = writer.BaseStream.Position;
            long L = 0;
            writer.Write(L); // Reserve the place for the size of the block
            // First write the type of the object - later on it'll be used to recreate the object
            WriteType(writer, StreamedObject.GetType());
            StreamedObject.WriteStream(writer, options);
            var pos2 = writer.BaseStream.Position;
            writer.BaseStream.Seek(pos, SeekOrigin.Begin);
            L = pos2 - pos - sizeof(long);
            writer.Write(L);
            writer.BaseStream.Seek(pos2, SeekOrigin.Begin);
        }

        public static void ReadStreamedObject(BinaryReader reader, IStreamedObject StreamedObject)
        {
            ReadStreamedObject(reader, StreamedObject, null);
        }

        public static void ReadStreamedObject(BinaryReader reader, IStreamedObject StreamedObject, object options)
        {
            if (ReadValueType(reader) == ValueType.StreamedObject)
            {
                reader.ReadInt64();
                StreamedObject.ReadStream(reader, options);
            }
            else
            {
                InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            }
        }

        public static MemoryStream ReadStreamedObjectToStream(BinaryReader reader)
        {
            if (ReadValueType(reader) == ValueType.StreamedObject)
            {
                // Instead of passing the stream to the reading StreamedObject.ReadStream method return this stream as a MemoryStream
                // The resulting MemoryStream can later on be used in the StreamedObject.ReadStream method
                var lo = reader.ReadInt64();
                var L = Convert.ToInt32(lo);
                var buf = reader.ReadBytes(L);
                var stream = new MemoryStream(buf);
                stream.Position = 0;
                return stream;
            }
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return null;
        }

        public static void ReadStream(BinaryReader reader, Stream stream)
        {
            if (ReadValueType(reader) == ValueType.Stream)
            {
                var Len = reader.ReadInt64();
                const int buf_size = 0x1000;
                var buffer = new byte[buf_size];
                long L = 0;
                while (L < Len)
                {
                    var N = L + buf_size <= Len ? buf_size : (int) (Len - L);
                    reader.Read(buffer, 0, N);
                    stream.Write(buffer, 0, N);
                    L += N;
                }
            }
            else
            {
                InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            }
        }

        public static MemoryStream ReadStreamToStream(BinaryReader reader)
        {
            if (ReadValueType(reader) == ValueType.Stream)
            {
                // returns the stream as a MemoryStream
                var lo = reader.ReadInt64();
                var L = Convert.ToInt32(lo);
                var buf = reader.ReadBytes(L);
                var stream = new MemoryStream(buf);
                stream.Position = 0;
                return stream;
            }
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return null;
        }

        public static IStreamedObject ReadTypedStreamedObject(BinaryReader reader)
        {
            return ReadTypedStreamedObject(reader, null);
        }

        public static IStreamedObject ReadTypedStreamedObject(BinaryReader reader, object options)
        {
            if (ReadValueType(reader) == ValueType.TypedStreamedObject)
            {
                reader.ReadInt64();
                var type = ReadType(reader);
                var o = Activator.CreateInstance(type);
                if (o is IStreamedObject)
                {
                    (o as IStreamedObject).ReadStream(reader, options);
                    return o as IStreamedObject;
                }
                return null;
            }
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return null;
        }

        public static void WriteList(BinaryWriter writer, IList list)
        {
            WriteList(writer, list, null);
        }

        public static void WriteList(BinaryWriter writer, IList list, object options)
        {
            WriteTag(writer, Tags.tgList);

            if (list.Count > 0)
            {
                WriteTag(writer, Tags.tgList_Count);
                WriteInt32(writer, list.Count);

                for (var i = 0; i < list.Count; i++)
                    WriteStreamedObject(writer, list[i] as IStreamedObject, Tags.tgList_Item, options);
            }

            WriteTag(writer, Tags.tgList_EOT);
        }

        public static void WriteFont(BinaryWriter writer, Font font)
        {
            var converter =
                TypeDescriptor.GetConverter(typeof(Font));
            WriteString(writer, converter.ConvertToString(font));
        }

        public static Font ReadFont(BinaryReader reader)
        {
            var converter =
                TypeDescriptor.GetConverter(typeof(Font));

            return (Font) converter.ConvertFromString(reader.ReadString());
        }

        public static void WriteSize(BinaryWriter writer, Size size)
        {
            WriteInt32(writer, size.Height);
            WriteInt32(writer, size.Width);
        }

        public static Size ReadSize(BinaryReader reader)
        {
            var height = reader.ReadInt32();
            var width = reader.ReadInt32();
            return new Size(width, height);
        }

        public static void WriteObject(BinaryWriter writer, object o)
        {
            WriteValueType(writer, ValueType.Object);
            if (o == null)
            {
                writer.Write(true);
            }
            else
            {
                writer.Write(false);
                var tc = Type.GetTypeCode(o.GetType());
                writer.Write((short) tc);
                switch (tc)
                {
                    case TypeCode.Boolean:
                        writer.Write((bool) o);
                        break;
                    case TypeCode.Char:
                        writer.Write((char) o);
                        break;
                    case TypeCode.SByte:
                        writer.Write((sbyte) o);
                        break;
                    case TypeCode.Byte:
                        writer.Write((byte) o);
                        break;
                    case TypeCode.Int16:
                        writer.Write((short) o);
                        break;
                    case TypeCode.UInt16:
                        writer.Write((ushort) o);
                        break;
                    case TypeCode.Int32:
                        writer.Write((int) o);
                        break;
                    case TypeCode.UInt32:
                        writer.Write((uint) o);
                        break;
                    case TypeCode.Int64:
                        writer.Write((long) o);
                        break;
                    case TypeCode.UInt64:
                        writer.Write((ulong) o);
                        break;
                    case TypeCode.Single:
                        writer.Write((float) o);
                        break;
                    case TypeCode.Double:
                        writer.Write((double) o);
                        break;
                    case TypeCode.Decimal:
                        writer.Write((decimal) o);
                        break;
                    case TypeCode.DateTime:
                        writer.Write(((DateTime) o).ToBinary());
                        break;
                    case TypeCode.String:
                        writer.Write((string) o);
                        break;
                }
            }
        }

        public static object ReadObject(BinaryReader reader)
        {
            var vt = ReadValueType(reader);
            if (vt == ValueType.Object)
            {
                if (reader.ReadBoolean()) return null;
                var tc = (TypeCode) reader.ReadInt16();
                switch (tc)
                {
                    case TypeCode.Boolean:
                        return reader.ReadBoolean();
                    case TypeCode.Char:
                        return reader.ReadChar();
                    case TypeCode.SByte:
                        return reader.ReadSByte();
                    case TypeCode.Byte:
                        return reader.ReadByte();
                    case TypeCode.Int16:
                        return reader.ReadInt16();
                    case TypeCode.UInt16:
                        return reader.ReadUInt16();
                    case TypeCode.Int32:
                        return reader.ReadInt32();
                    case TypeCode.UInt32:
                        return reader.ReadUInt32();
                    case TypeCode.Int64:
                        return reader.ReadInt64();
                    case TypeCode.UInt64:
                        return reader.ReadUInt64();
                    case TypeCode.Single:
                        return reader.ReadSingle();
                    case TypeCode.Double:
                        return reader.ReadDouble();
                    case TypeCode.Decimal:
                        return reader.ReadDecimal();
                    case TypeCode.DateTime:
                        var l = reader.ReadInt64();
                        return DateTime.FromBinary(l);
                    case TypeCode.String:
                        return reader.ReadString();
                    default:
                        return null;
                }
            }
            InvalidPropError(reader.BaseStream.Position - sizeof(byte));
            return null;
        }

        public static int ReadAllBytesFromStream(Stream stream, byte[] buffer)
        {
            var offset = 0;
            var totalCount = 0;
            while (true)
            {
                var bytesRead = stream.Read(buffer, offset, 4096);
                if (bytesRead == 0)
                    break;
                offset += bytesRead;
                totalCount += bytesRead;
            }
            return totalCount;
        }

        public static CommonStreamProperties GetCommonStreamProperties(BinaryReader reader)
        {
            var result = new CommonStreamProperties();
            result.Correct = false;
            if (reader == null) return result;
            try
            {
                var pos = reader.BaseStream.Position;
                if (reader.BaseStream.Length - pos < 32) return result;
                if (reader.ReadUInt32() != 0x46435255) return result;
                result.ProductID = reader.ReadString();
                result.Version = reader.ReadInt32();
                result.CompressionMethod = reader.ReadByte();
                // Skip the rest of the header
                reader.ReadBytes(32 - (int) (reader.BaseStream.Position - pos));
                result.Size = reader.ReadInt64();
                result.Correct = true;
            }
            catch
            {
                result.Correct = false;
            }
            return result;
        }

        public static void WriteURCFHeader(BinaryWriter writer, string ProductID, int VersionNumber,
            byte CompressionMethod)
        {
            // 00: URCF - Universal RadarCube Format descriptor
            var pos = writer.BaseStream.Position;
            writer.Write((uint) 0x46435255);
            writer.Write(ProductID);
            writer.Write(VersionNumber);
            // CompressionMethod: 0 - None, 1 - standard DeflateStream
            writer.Write(CompressionMethod);
            // Reserved
            writer.Write(new byte[32 - (writer.BaseStream.Position - pos)]);
        }

        internal static byte[] ReadBytes(BinaryReader aBinaryReader)
        {
            using (var ms = new MemoryStream())
            {
                ReadStream(aBinaryReader, ms);
                var res = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(res, 0, (int) ms.Length);
                return res;
            }
        }
    }
}