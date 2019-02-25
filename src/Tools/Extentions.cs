using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using RadarSoft.RadarCube.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

namespace RadarSoft.RadarCube.Tools
{
    internal static class Extentions
    {
        //internal static OSVersion GetVersion(this OperatingSystem OperatingSystem)
        //{
        //    //Windows NT 3.1 (1993)
        //    //Windows NT 3.5 (1994)
        //    //Windows NT 3.51 (1995)
        //    //Windows NT 4.0 (1996)
        //    //Windows 2000 (2000) — Windows NT 5.0
        //    //Windows XP (2001) — Windows NT 5.1
        //    //Windows XP 64-bit Edition (2006) — Windows NT 5.2
        //    //Windows Server 2003 (2003) — Windows NT 5.2
        //    //Windows Vista (2006) — Windows NT 6.0
        //    //Windows Home Server (2007) — Windows NT 5.2
        //    //Windows Server 2008 (2008) — Windows NT 6.0
        //    //Windows Small Business Server (2008) — Windows NT 6.0
        //    //Windows 7 — Windows NT 6.1 (2009)
        //    //Windows Server 2008 R2 — Windows NT 6.1 (2009)
        //    //Windows Home Server 2011 — Windows NT 6.1 (2011)
        //    //Windows 8 — Windows NT 6.2 (2012)
        //    //Windows Server 2012 — Windows NT 6.2 (2012)

        //    if (Environment.OSVersion.Version.Major == 5
        //        && Environment.OSVersion.Version.Minor == 1
        //        && Environment.OSVersion.Platform == PlatformID.Win32NT)
        //    {
        //        return OSVersion.Windows_XP_2001;
        //    }

        //    return OSVersion.Unknown;
        //}

        private const string __EMPTY_YIELD = "[-Empty-]";

        internal static bool IsNumericType(this Type typ)
        {
            bool f;
            TypeCode typeCode;
            if (typ.IsArray)
                return false;
            switch ((int) Type.GetTypeCode(typ))
            {
                case 3:
                case 6:
                case 7:
                case 9:
                case 11:
                case 13:
                case 14:
                case 15:
                    return true;
            }
            return false;
        }

        internal static string ColorToString(this Color c, bool withoutAlpha)
        {
            return RadarUtils.ColorToString(c, withoutAlpha);
        }


        internal static string ColorToString(this Color c)
        {
            return RadarUtils.ColorToString(c);
        }

        internal static byte IndentOfCell(this ICell cell)
        {
            if (cell is ILevelCell)
                return ((ILevelCell) cell).Indent;
            if (cell is IMemberCell)
                return ((IMemberCell) cell).Indent;
            return 0;
        }

        internal static string ConvertTo64BasedString(this Stream s, bool ACloseStream = false)
        {
            s.Position = 0;
            var source = new byte[s.Length];
            s.Read(source, 0, source.Length);

            if (ACloseStream)
                s.Flush();

            return Convert.ToBase64String(source);
        }

        internal static Stream ConvertFrom64BasedString(this string Arg)
        {
            var ms = new MemoryStream();

            var source = Convert.FromBase64String(Arg);
            ms.Write(source, 0, source.Length);
            return ms;
        }

        [DebuggerStepThrough]
        internal static bool IsNaN(this double arg)
        {
            return double.IsNaN(arg);
        }

        [DebuggerStepThrough]
        internal static bool IsPositiveInfinity(this double arg)
        {
            return double.IsPositiveInfinity(arg);
        }

        [DebuggerStepThrough]
        internal static bool IsNegativeInfinity(this double arg)
        {
            return double.IsNegativeInfinity(arg);
        }

        [DebuggerStepThrough]
        internal static bool IsInfinity(this double arg)
        {
            return double.IsInfinity(arg);
        }

        /// <summary>
        ///     return arg.IsInfinity() || arg.IsPositiveInfinity() || arg.IsNegativeInfinity();
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        internal static bool IsAnyInfinity(this double arg)
        {
            return arg.IsInfinity() || arg.IsPositiveInfinity() || arg.IsNegativeInfinity();
        }

        /// <summary>
        ///     return arg.IsAnyInfinity() || arg == double.MaxValue;
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        internal static bool IsInfinityLike(this double arg)
        {
            return arg.IsAnyInfinity() || arg == double.MaxValue;
        }

        /// <summary>
        ///     return arg.IsNaN() || arg.IsInfinityLike();
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        internal static bool IsInvalid(this double arg)
        {
            return arg.IsNaN() || arg.IsInfinityLike();
        }

        /// <summary>
        ///     return ! arg.IsNaN() || arg.IsInfinityLike();
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        internal static bool IsValid(this double arg)
        {
            return arg.IsInvalid() == false;
        }

        [DebuggerStepThrough]
        internal static bool IsNullOrEmpty(this string arg)
        {
            return string.IsNullOrEmpty(arg);
        }

        [DebuggerStepThrough]
        internal static bool IsFill(this string arg)
        {
            return string.IsNullOrEmpty(arg) == false;
        }

        internal static void ForEach<T>(this IEnumerable<T> en, Action<T> act)
        {
            foreach (var item in en)
                act(item);
        }

        internal static IEnumerable<Tuple<T, T>> GetCurrentAndNext<T>(this IEnumerable<T> en)
            where T : class
        {
            var index = 0;
            T last = null;
            foreach (var item in en)
            {
                if (last != null)
                    yield return new Tuple<T, T>(last, item);
                last = item;
                index++;
                if (index > en.Count() - 1)
                    break;
            }
        }

        internal static int GetPageCount(this IMemberCell imemcell)
        {
            return Convert.ToInt32(Math.Ceiling(imemcell.SiblingsCount * 1.0 /
                                                imemcell.Level.Level.PagerSettings.LinesInPage));
        }

        internal static string ConvertToString(long[] list)
        {
            if (list == null || list.Count() == 0)
                return __EMPTY_YIELD;

            return "[" + string.Join(", ", list.Select(x => x.ToString()).ToArray()) + "]";
        }

        internal static string ConvertToString(IList<int> list)
        {
            if (list == null || !list.Any())
                return __EMPTY_YIELD;

            return "[" + string.Join(", ", list.Select(x => x.ToString()).ToArray()) + "]";
        }

        internal class DPoint
        {
            private double x1;
            private double y1;

            public DPoint(double x1, double y1)
            {
                // TODO: Complete member initialization
                this.x1 = x1;
                this.y1 = y1;
            }
        }

        //public static void Set<T>(this IMemoryCache cache, string key, T value, FileCacheDependency dependency)
        //{
        //    var fileInfo = new FileInfo(dependency.FileName);
        //    var fileProvider = new PhysicalFileProvider(fileInfo.DirectoryName);
        //    cache.Set(key, value, new MemoryCacheEntryOptions()
        //                        .AddExpirationToken(fileProvider.Watch(fileInfo.Name)));

        //}
    }
}