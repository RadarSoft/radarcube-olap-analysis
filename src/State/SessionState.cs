using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.State
{
    public class SessionState
    {
        internal const string SessionDirName = "$Session$";

        private string _fWorkingDirectoryName;
        protected Controls.Cube.RadarCube fCube;

        internal SessionState(Controls.Cube.RadarCube Cube)
        {
            fCube = Cube;
        }

        private string fWorkingDirectoryName
        {
            get
            {
                if (_fWorkingDirectoryName.IsFill())
                    return _fWorkingDirectoryName;

                if (fCube.WorkingDirectoryForTest.IsFill())
                    _fWorkingDirectoryName = fCube.WorkingDirectoryForTest;

                return _fWorkingDirectoryName;
            }
            set => _fWorkingDirectoryName = value;
        }

        internal string WorkingDirectoryName
        {
            get
            {
                if (string.IsNullOrEmpty(fWorkingDirectoryName))
                {
                    fWorkingDirectoryName = fCube.Session.GetString(fCube.ID + "$WorkingDirectoryName");
                    if (fWorkingDirectoryName.IsNullOrEmpty())
                    {
                        fWorkingDirectoryName = Path.Combine(fCube.WorkingDirectory, SessionDirName, Path.GetRandomFileName());
                        fCube.Session.SetString(fCube.ID + "$WorkingDirectoryName", fWorkingDirectoryName);
                    }
                }
                return fWorkingDirectoryName;
            }
        }

        private string KeyToFileName(SessionKey key, string objectId)
        {
            return WorkingDirectoryName + (int) key + objectId + ".dat";
        }

        protected virtual void PrepareSessionDir()
        {
            var path = TempDirectory.ExtractPath(WorkingDirectoryName);
            if (!Directory.Exists(path))
                DoCreateDirectory(path);
        }

        internal static DirectoryInfo DoCreateDirectory(string path)
        {
            try
            {
                return Directory.CreateDirectory(path);
            }
            catch (UnauthorizedAccessException e)
            {
                var helpUrl =
                    "http://support.radar-soft.net/index.php?/Knowledgebase/Article/View/20/2/access-to-the-path-temp-is-denied-occurs-upon-opening-the-cube"; //string.Format(RadarCube.__RADARSOFT_SUPPORT_REDIRECT, 8);

                var ex = new UnauthorizedAccessException(e.Message + " Please visit the " + helpUrl +
                                                         " to resolve this issue.");
                ex.HelpLink = helpUrl;
                throw ex;
            }
        }

        private object ReadObject(BinaryReader reader)
        {
            return StreamUtils.ReadObject(reader);
        }

        private void ReadStreamedObject(BinaryReader reader, IStreamedObject StreamedObject)
        {
            StreamUtils.ReadStreamedObject(reader, StreamedObject);
        }

        private BinaryReader GetReader(SessionKey key, string objectId)
        {
            var fName = KeyToFileName(key, objectId);
            if (!File.Exists(fName)) return null;
            BinaryReader reader;
            try
            {
                var stream = new FileStream(fName, FileMode.Open, FileAccess.Read, FileShare.Read);
                reader = new BinaryReader(stream);
            }
            catch
            {
                reader = null;
            }
            return reader;
        }

        private BinaryWriter GetWriter(SessionKey key, string objectId)
        {
            PrepareSessionDir();
            var fName = KeyToFileName(key, objectId);
            var stream = new FileStream(fName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var writer = new BinaryWriter(stream);
            return writer;
        }

        internal void Write(object obj, SessionKey key, string objectId)
        {
            if (obj == null)
                return;
            using (var writer = GetWriter(key, objectId))
            {
                // DateTime just in case
                writer.Write(DateTime.Now.ToBinary());
                if (obj is IStreamedObject)
                    StreamUtils.WriteStreamedObject(writer, obj as IStreamedObject);
                else
                    StreamUtils.WriteObject(writer, obj);
            }
            TempDirectory.SetLastAccessTime(WorkingDirectoryName);
        }

        internal object ReadObject(SessionKey key, string objectId)
        {
            object result;
            using (var reader = GetReader(key, objectId))
            {
                if (reader == null) return null;
                try
                {
                    reader.ReadInt64();
                    result = ReadObject(reader);
                }
                catch
                {
                    result = null;
                }
            }
            TempDirectory.SetLastAccessTime(WorkingDirectoryName);
            return result;
        }

        internal void ReadStreamedObject(SessionKey key, IStreamedObject StreamedObject, string objectId)
        {
            using (var reader = GetReader(key, objectId))
            {
                if (reader == null) return;
                reader.ReadInt64();
                ReadStreamedObject(reader, StreamedObject);
            }
            TempDirectory.SetLastAccessTime(WorkingDirectoryName);
        }

        internal bool KeyExists(SessionKey key, string objectId)
        {
            var fName = KeyToFileName(key, objectId);
            return File.Exists(fName);
        }

        internal void Delete(SessionKey key, string objectId)
        {
            var fName = KeyToFileName(key, objectId);
            if (File.Exists(fName)) File.Delete(fName);
        }
    }
}