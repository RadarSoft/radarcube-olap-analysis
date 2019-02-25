using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RadarSoft.RadarCube.Controls;

namespace RadarSoft.RadarCube.Tools
{
    public class StoredImagesProvider
    {
        protected Dictionary<string, string> names = new Dictionary<string, string>();
        protected string wd;

        public StoredImagesProvider()
        {
        }


        public StoredImagesProvider(OlapControl analysis)
        {
            Analysis = analysis;
        }

        public OlapControl Analysis { get; }

        protected virtual string ExtractImage(string resName, Type controlType, string tempPath)
        {
            try
            {
                var ver = typeof(OlapControl).GetTypeInfo().Assembly.GetName().Version.ToString().Replace('.', '_');

                if (wd == null)
                    wd = Analysis.MapPath(Path.Combine(tempPath, ver));
                var fileName = Path.Combine(wd, "images", resName);
                if (File.Exists(fileName))
                    return string.Format("/{0}/images/", tempPath + "/" + ver) + resName;
                lock (typeof(OlapControl))
                {
                    var frn = controlType.GetTypeInfo().IsSubclassOf(typeof(OlapControl))
                        ? "RadarSoft.RadarCube.images." + resName
                        : resName;

                    var s = controlType.GetTypeInfo().Assembly.GetManifestResourceStream(frn);
                    var buf = new byte[s.Length];
                    using (s)
                    {
                        s.Read(buf, 0, buf.Length);
                    }
                    if (!Directory.Exists(Path.Combine(wd, "images")))
                        Directory.CreateDirectory(Path.Combine(wd, "images"));
                    File.WriteAllBytes(fileName, buf);
                }

                if (File.Exists(fileName))
                    return string.Format("/{0}/images/", tempPath + "/" + ver) + resName;
            }
            catch
            {
                ;
            }
            return null;
        }

        internal virtual string ImageUrl(string resName, Type controlType, string tempPath)
        {
            var s = ExtractImage(resName, controlType, tempPath);
            if (s.IsNullOrEmpty())
                return "";
            return s.TrimStart('~');
        }

        public virtual void CheckTempAccessibility()
        {
            DoCheckTempAccessibilitty(Analysis.MapPath(Analysis.TempPath));
        }

        protected void DoCheckTempAccessibilitty(string s)
        {
            var di = new DirectoryInfo(s);
            try
            {
                if (!di.Exists) di.Create();
            }
            catch (UnauthorizedAccessException E)
            {
                throw new UnauthorizedAccessException("There's no access to writing in the /Temp directory. " +
                                                      "To operate correctly RadarCube requires the user, under whose account the web application operates, to have the rights for writing in the /Temp directory (" +
                                                      s + ").", E);
            }
        }

        internal void ExtractClientRsolapLibrary()
        {
            var tempPath = Analysis.TempPath;
            CheckTempAccessibility();
            var asm = Analysis.GetType().GetTypeInfo().Assembly;
            var res = asm.GetManifestResourceNames();
            var jQueryRes = res.Where(item => item.StartsWith("RadarSoft.RadarCube.Content."))
                .Select(item => item.Replace("RadarSoft.RadarCube.Content.", ""));

            var ver = typeof(OlapControl).GetTypeInfo().Assembly.GetName().Version.ToString().Replace('.', '_');

            if (wd == null)
                wd = Analysis.MapPath(Path.Combine(tempPath, ver));

            string wd1;
            string s1;
            string fileName;
            foreach (var s in jQueryRes)
            {
                wd1 = wd;
                s1 = s;

                if (s1.StartsWith("css.images."))
                {
                    wd1 += @"css\images\";
                    s1 = s1.Replace("css.images.", "");
                }
#if DEBUG
                if (s1.StartsWith("css.debug."))
                {
                    wd1 += @"css\";
                    s1 = s1.Replace("css.debug.", "");
                }

                if (s1.StartsWith("debug.ts.d.ts."))
                {
                    wd1 += @"ts\d.ts\";
                    s1 = s1.Replace("debug.ts.d.ts.", "");
                }

                if (s1.StartsWith("debug.ts."))
                {
                    wd1 += @"ts\";
                    s1 = s1.Replace("debug.ts.", "");
                }

                if (s1.StartsWith("debug."))
                    s1 = s1.Replace("debug.", "");

                if (s1.StartsWith("css.release.") || s1.StartsWith("release."))
                    continue;
#else
                if (s1.StartsWith("css.release."))
                {
                    wd1 += @"css\";
                    s1 = s1.Replace("css.release.", "");
                }

                if (s1.StartsWith("release."))
                {
                    s1 = s1.Replace("release.", "");
                }

                if (s1.StartsWith("css.debug.") || s1.StartsWith("debug."))
                    continue;
#endif
                if (s1.StartsWith("css."))
                {
                    wd1 += @"css\";
                    s1 = s1.Replace("css.", "");
                }

                if (s1.StartsWith("images."))
                    continue;

                fileName = wd1 + s1;

                if (File.Exists(fileName))
                    continue;

                if (!Directory.Exists(wd1))
                    Directory.CreateDirectory(wd1);

                var str = asm.GetManifestResourceStream("RadarSoft.RadarCube.Content." + s);
                var buf = new byte[str.Length];

                using (str)
                {
                    str.Read(buf, 0, buf.Length);
                }
                File.WriteAllBytes(fileName, buf);
            }
        }
    }
}