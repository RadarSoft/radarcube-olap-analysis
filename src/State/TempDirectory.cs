using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RadarSoft.RadarCube.State
{
    internal static class TempDirectory
    {
        internal const string LATFile = "$lat$.$";
        internal const string CacheDependencyFile = "$cache$.$";

        /// <summary>
        ///     Deletes the given directory safely, i.e. supressing possible exceptions.
        /// </summary>
        internal static void SafeDirectoryDelete(string path)
        {
            var p = ExtractPath(path);
            if (!Directory.Exists(p)) return;
            try
            {
                var files = Directory.GetFiles(p, path.Substring(p.Length) + "*.*");
                ExcludeReportFiles(ref files);

                foreach (var f in files)
                    if (f.IndexOf(LATFile) < 0) File.Delete(f);

                if (files.Length > 0)
                {
                    files = Directory.GetFiles(p, path.Substring(p.Length) + "*.*");
                    if (files.Length == 1) File.Delete(files[0]);
                }
            }
            catch
            {
                ;
            }
        }

        private static void ExcludeReportFiles(ref string[] tempFiles)
        {
            var resFiles = new List<string>();

            foreach (var f in tempFiles)
            {
                var reportExt = Path.GetExtension(f).ToLower();
                if (reportExt == ".dll" || reportExt == ".rbi")
                    continue;
                resFiles.Add(f);
            }

            tempFiles = resFiles.ToArray();
        }

        internal static string ExtractPath(string path)
        {
            var s = path[path.Length - 1] == Path.DirectorySeparatorChar ? path.Remove(path.Length - 1) : path;
            return s.Substring(0, s.LastIndexOf(Path.DirectorySeparatorChar) + 1);
        }

        internal static void SetLastAccessTime(string path)
        {
            var path2 = ExtractPath(path);
            // Writes the file showing the last access time in the given directory
            if (Directory.Exists(path2))
            {
                //Directory.SetLastAccessTime(path, DateTime.Now);
                var fname = path + LATFile;
                var cacheFile = path + CacheDependencyFile;
                try
                {
                    using (var stream = new FileStream(fname, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        using (var writer = new BinaryWriter(stream))
                        {
                            writer.Write(DateTime.Now.ToBinary());
                        }
                    }

                    if (File.Exists(cacheFile) == false)
                        File.Create(cacheFile).Dispose();
                }
                catch
                {
                }
            }
        }

        internal static DateTime GetLastAccessTime(string path)
        {
            // Reads the time value when the directory was accessed last time
            if (Directory.Exists(path))
            {
                //return Directory.GetLastAccessTime(p);
                var fname = Path.Combine(path, LATFile);
                if (File.Exists(fname))
                    try
                    {
                        using (var stream = new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            using (var reader = new BinaryReader(stream))
                            {
                                var dt = DateTime.FromBinary(reader.ReadInt64());
                                return dt;
                            }
                        }
                    }
                    catch
                    {
                        return DateTime.Now;
                    }
                return Directory.GetLastAccessTime(path);
            }
            return DateTime.Now;
        }

        internal static void CreateDirIfNeeded(string path)
        {
            path = ExtractPath(path);
            if (!Directory.Exists(path))
                SessionState.DoCreateDirectory(path);
        }

        /// <summary>
        ///     Goes through the temp directory and deletes all sub-directories whose data was left from another session and the
        ///     session timeout has expired.
        /// </summary>
        /// <param name="path">The directory to delete</param>
        /// <param name="Timeout">
        ///     The time in minutes before the session state provider terminates the session
        ///     (HttpSessionState.Timeout).
        /// </param>
        internal static void ClearExpiredSessionsData(string path, int Timeout)
        {
            if (!Directory.Exists(path)) return;
            var items = Directory.GetDirectories(path);
            foreach (var dir in items)
            {
                var LAT = GetLastAccessTime(dir);
                if (TimeSpan.Compare(DateTime.Now - LAT, TimeSpan.FromMinutes(Timeout)) > 0)
                    //if (TimeSpan.Compare(DateTime.Now - LAT, TimeSpan.FromMinutes(1)) > 0)
                    try
                    {
                        //TempDirectory.SafeDirectoryDelete(dir);
                        Directory.Delete(dir, true);
                    }
                    catch
                    {
                    }
            }

            items = Directory.GetFiles(path, "*" + LATFile);
            foreach (var dir in items)
            {
                var LAT = GetLastAccessTime2(dir);
                if (TimeSpan.Compare(DateTime.Now - LAT, TimeSpan.FromMinutes(Timeout)) > 0)
                    //if (TimeSpan.Compare(DateTime.Now - LAT, TimeSpan.FromMinutes(1)) > 0)
                {
                    var pos = dir.LastIndexOf(Path.DirectorySeparatorChar);
                    var s1 = dir.Substring(pos + 1).Replace(LATFile, "*.dat");
                    var items2 = Directory.GetFiles(path, s1).ToList();
                    s1 = dir.Substring(pos + 1).Replace(LATFile, "*.db");
                    items2.AddRange(Directory.GetFiles(path, s1));
                    s1 = dir.Substring(pos + 1).Replace(LATFile, "*.ob");
                    items2.AddRange(Directory.GetFiles(path, s1));
                    s1 = dir.Substring(pos + 1).Replace(LATFile, "*.x*");
                    items2.AddRange(Directory.GetFiles(path, s1));
                    s1 = dir.Substring(pos + 1).Replace(LATFile, CacheDependencyFile);
                    items2.AddRange(Directory.GetFiles(path, s1));
                    foreach (var s in items2)
                        try
                        {
                            File.Delete(s);
                        }
                        catch
                        {
                        }
                    if (Directory.GetFiles(path, s1).Length == 0)
                        try
                        {
                            File.Delete(dir);
                        }
                        catch
                        {
                        }
                }
            }
        }

        private static DateTime GetLastAccessTime2(string path)
        {
            if (File.Exists(path))
                try
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var reader = new BinaryReader(stream))
                        {
                            var dt = DateTime.FromBinary(reader.ReadInt64());
                            return dt;
                        }
                    }
                }
                catch
                {
                    return DateTime.Now;
                }
            return DateTime.Now;
        }
    }
}