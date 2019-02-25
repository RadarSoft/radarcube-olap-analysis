using System;
using System.IO;
using System.IO.Compression;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.State
{
    [Serializable]
    internal class SessionStateData : IStreamedObject
    {
        protected string CubeID;
        protected int length;
        protected MemoryStream ms;

        internal void Init(OlapControl grid)
        {
            var cube = grid.Cube;
            if (cube != null) cube.InitSessionData(grid);
            CubeID = grid.Cube != null ? grid.Cube.ID : "";

            var tmp = new MemoryStream();
            var bw = new BinaryWriter(tmp);
            (grid as IStreamedObject).WriteStream(bw, null);

            length = Convert.ToInt32(tmp.Length);
            var b = tmp.ToArray();
            tmp = null;
            ms = new MemoryStream();
            using (var ds = new DeflateStream(ms, CompressionMode.Compress, true))
            {
                ds.Write(b, 0, b.Length);
            }
        }

        protected int ReadAllBytesFromStream(Stream stream, byte[] buffer)
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

        internal virtual void Restore(OlapControl grid)
        {
            if (grid.IsRestored)
                return;

            var rc = grid.Cube;

            if (rc == null)
                return;

            rc.RestoreCube();
            grid.Cube = rc;

            grid.BeginUpdate();
            try
            {
                ms.Position = 0;
                var ds = new DeflateStream(ms, CompressionMode.Decompress);
                var b = new byte[length + 4096];
                var br = ReadAllBytesFromStream(ds, b);
                var tmp = new MemoryStream(b);
                tmp.Position = 0;
                var reader = new BinaryReader(tmp);

                (grid as IStreamedObject).ReadStream(reader, null);

                grid.FLayout.fGrid = grid;
                if (grid.FCellSet != null)
                {
                    grid.FCellSet.FGrid = grid;
                    grid.FCellSet.RestoreAfterSerialization(grid);
                }

                if (grid.FCellSet != null)
                {
                    var i = grid.CellSet.FValueSortedColumn;
                    if (i >= 0)
                    {
                        grid.CellSet.Rebuild();
                        grid.CellSet.ValueSortedColumn = i;
                    }
                }
                grid.IsRestored = true;
            }
#if DEBUG
            catch (Exception e)
            {

            }
#endif
            finally
            {
                grid.EndUpdate();
            }
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgASPGridSessionState);

            StreamUtils.WriteStream(writer, ms, Tags.tgASPGridSessionState_Stream);

            StreamUtils.WriteTag(writer, Tags.tgASPGridSessionState_CubeId);
            StreamUtils.WriteString(writer, CubeID);

            StreamUtils.WriteTag(writer, Tags.tgASPGridSessionState_Length);
            StreamUtils.WriteInt32(writer, length);

            StreamUtils.WriteTag(writer, Tags.tgASPGridSessionState_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgASPGridSessionState);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgASPGridSessionState_Stream:
                        ms = new MemoryStream();
                        StreamUtils.ReadStream(reader, ms);
                        break;
                    case Tags.tgASPGridSessionState_CubeId:
                        CubeID = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgASPGridSessionState_Length:
                        length = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgASPGridSessionState_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        #endregion
    }
}