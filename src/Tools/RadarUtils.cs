using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.DependencyModel;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;
using DrawingColor = System.Drawing.Color;

namespace RadarSoft.RadarCube.Tools
{
    public static class RadarUtils
    {
        /// <summary>
        ///     rsdata
        /// </summary>
        internal const string __DEFAULT_EXTENSION = "rsdata";

        /// <summary>
        ///     XML files
        /// </summary>
        private const string __FILTER_XML = "XML files(*.xml)|*.xml";

        /// <summary>
        ///     Appearance
        /// </summary>
        public const string __APPEARANCE = "Appearance";

        /// <summary>
        ///     Layout
        /// </summary>
        public const string __LAYOUT = "Layout";

        internal static readonly string rsWinFormsMSAS = "RC-WF-MSAS";
        internal static readonly string rsWinFormsDesktop = "RC-WF-DESK";
        internal static readonly string rsAspNetMSAS = "RC-ASP-MSAS";
        internal static readonly string rsAspNetDesktop = "RC-ASP-DESK";
        internal static readonly string rsWpfMSAS = "RC-WPF-MSAS";
        internal static readonly string rsWpfDesktop = "RC-WPF-DESK";
        internal static readonly string rsAspNetCoreXmla = "RC-ASP-NETCORE-XMLA";

        private static readonly ResourceManager _Rm;


        //private static readonly Dictionary<string, Dictionary<string, string>> _Dresx = new Dictionary<string, Dictionary<string, string>>();

        private static readonly Dictionary<string, Dictionary<string, string>> _Nativeresx =
            new Dictionary<string, Dictionary<string, string>>();

        internal static readonly string __FILTER_RSDATA =
            string.Format("Radar-Soft Cube files(*.{0})|*.{0}", __DEFAULT_EXTENSION);

        internal static readonly string __FILTER_RSDATA_XML = __FILTER_RSDATA + "|" + __FILTER_XML;

        private static readonly Random random = new Random(10);

        static RadarUtils()
        {
            var asm = typeof(RadarUtils).GetTypeInfo().Assembly;
            _Rm = new ResourceManager("RadarSoft.RadarCube.Localization.Resources", asm);
        }

        internal static void AddResourceStrings(string fileName, CultureInfo culture)
        {
            Dictionary<string, string> d;

            var lang = culture.Name;

            if (_Nativeresx.TryGetValue(lang, out d) == false)
            {
                d = new Dictionary<string, string>();
                _Nativeresx.Add(lang, d);
            }

            var xd = new XmlDocument();
            using (var fstream = File.Open(fileName, FileMode.Open))
            {
                xd.Load(fstream);
                var values = new Dictionary<string, string>();
                Parse(xd.ChildNodes, values);

                foreach (var pair in values)
                    if (d.ContainsKey(pair.Key))
                        d[pair.Key] = pair.Value;
                    else
                        d.Add(pair.Key, pair.Value);
            }
        }

        internal static string GetCurrentDesktopProductID()
        {
            return rsAspNetDesktop;
        }

        internal static string GetCurrentMSASProductID()
        {
            return rsAspNetMSAS;
        }

        private static byte ScRgbTosRgb(float val)
        {
            if (val <= 0.0)
                return 0;
            if (val <= 0.0031308)
                return (byte) (255f * val * 12.92f + 0.5f);
            if (val < 1.0)
                return (byte) (255f * (1.055f * (float) Math.Pow(val, 5.0 / 12.0) - 0.055f) + 0.5f);
            return 0xff;
        }

        internal static string GetHexStringFromColor(DrawingColor c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        internal static DrawingColor GetColorFromHexString(string hex)
        {
            return ColorTranslator.FromHtml(hex);
        }

        internal static bool IsEqualsArrayFloat(float[] a1, float[] a2)
        {
            if (a1 == null && a2 == null)
                return true;
            if (a1 == null)
                return false;
            if (a2 == null)
                return false;
            if (a1.Length != a2.Length)
                return false;

            for (var i = 0; i < a1.Length; i++)
                if (Math.Abs(a1[i] - a2[i]) > float.Epsilon * 10)
                    return false;
            return true;
        }

        internal static LayoutArea? ParseLayoutArea(string s, bool isTimeDemention = false)
        {
            LayoutArea? area = null;
            if (s.StartsWith("la"))
            {
                if (s != "laNone")
                    area = (LayoutArea) Enum.Parse(typeof(LayoutArea), s);
            }
            else
            {
                if (s == "row") area = LayoutArea.laRow;
                if (s == "col") area = LayoutArea.laColumn;
                if (s == "page") area = LayoutArea.laPage;
                if (s == "colors") area = LayoutArea.laColor;
                if (s == "colorfore") area = LayoutArea.laColorFore;
                if (s == "shape") area = LayoutArea.laShape;
                if (s == "size") area = LayoutArea.laSize;
                if (s == "detail") area = LayoutArea.laDetails;
                if (s == "grid") area = isTimeDemention ? LayoutArea.laColumn : LayoutArea.laRow;
                if (s == "none" || s == "tree")
                    area = LayoutArea.laTree;
            }

            return area;
        }

        internal static LayoutArea ParseLayoutArea2(string s)
        {
            var area = ParseLayoutArea(s);

            return area.GetValueOrDefault();
        }

        public static string JScriptEscape(string str)
        {
            return str.Replace("'", "\\'");
        }

        internal static int ComputeCRC(byte[] val)
        {
            var crc = new Crc32();
            crc.Update(val);
            return (int) crc.Value;
        }

        internal static long ComputeCRC(string str)
        {
            var ch = str.ToCharArray();
            var b = new byte[ch.Length];
            for (var i = 0; i < ch.Length; i++)
                b[i] = (byte) ch[i];
            return ComputeCRC(b);
        }

        internal static string ColorToString(DrawingColor c, bool withoutAlpha)
        {
            if (c == DrawingColor.Empty)
                return "";
            return c.Format(withoutAlpha);
        }

        internal static string ColorToString(DrawingColor c)
        {
            if (c == DrawingColor.Empty)
                return "";

            return c.Format();

            //#if OLAPWINFORMS || OLAPWEB
            //            if (c == Color.Empty) 
            //                return "";
            //            return ("#" + c.R.ToString("X2", null) + c.G.ToString("X2", null) + c.B.ToString("X2", null));
            //#endif // OLAPWINFORMS || OLAPWEB
            //#if OLAPWPF
            //            throw new NotSupportedException();
            //            return c.ToString();
            //            //if (c == Color.Empty) return "";
            //            //return ("#" + c.R.ToString("X2", null) + c.G.ToString("X2", null) + c.B.ToString("X2", null));
            //#endif // OLAPWPF
        }

        internal static void DateToMemberName(DateTime date, BIMembersType biType, out double rank, out string memberName)
        {
            switch (biType)
            {
                case BIMembersType.ltTimeDayOfMonth:
                    {
                        rank = (new DateTime(date.Year, date.Month, date.Day)).ToOADate();
                        memberName = date.Day.ToString();
                        break;
                    }
                case BIMembersType.ltTimeDayOfWeekLong:
                    {
                        memberName = date.ToString("dddd", CultureInfo.CurrentUICulture);
                        rank = (new DateTime(date.Year, date.Month, date.Day)).ToOADate();
                        break;
                    }
                case BIMembersType.ltTimeDayOfWeekShort:
                    {
                        memberName = date.ToString("ddd", CultureInfo.CurrentUICulture);
                        rank = (new DateTime(date.Year, date.Month, date.Day)).ToOADate();
                        break;
                    }
                case BIMembersType.ltTimeDayOfYear:
                    {
                        memberName = date.DayOfYear.ToString();
                        rank = (new DateTime(date.Year, date.Month, date.Day)).ToOADate();
                        break;
                    }
                case BIMembersType.ltTimeHalfYear:
                    {
                        int i = (date.Month < 7) ? 1 : 2;
                        rank = date.Year + (i - 1.0) / 2;
                        memberName = String.Format(GetResStr("rsHYString"), i);
                        break;
                    }
                case BIMembersType.ltTimeHour:
                    {
                        rank = (new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0)).ToOADate();
                        memberName = date.Hour.ToString();
                        break;
                    }
                case BIMembersType.ltTimeMinute:
                    {
                        rank = (new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0)).ToOADate();
                        memberName = date.Minute.ToString();
                        break;
                    }
                case BIMembersType.ltTimeMonthLong:
                    {
                        rank = (new DateTime(date.Year, date.Month, 1)).ToOADate();
                        memberName = date.ToString("MMMM", CultureInfo.CurrentUICulture);
                        break;
                    }
                case BIMembersType.ltTimeMonthNumber:
                    {
                        rank = (new DateTime(date.Year, date.Month, 1)).ToOADate();
                        memberName = date.Month.ToString();
                        break;
                    }
                case BIMembersType.ltTimeMonthShort:
                    {
                        rank = (new DateTime(date.Year, date.Month, 1)).ToOADate();
                        memberName = date.ToString("MMM", CultureInfo.CurrentUICulture);
                        break;
                    }
                case BIMembersType.ltTimeQuarter:
                    {
                        int i = ((date.Month - 1) / 3) + 1;
                        rank = date.Year + (i - 1.0) / 4;
                        memberName = String.Format(GetResStr("rsQuarterString"), i);
                        break;
                    }
                case BIMembersType.ltTimeSecond:
                    {
                        rank = (new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second)).ToOADate();
                        memberName = date.Second.ToString();
                        break;
                    }
                case BIMembersType.ltTimeWeekOfYear:
                    {
                        int i = ((date.DayOfYear - 1) / 7) + 1;
                        rank = (new DateTime(date.Year, date.Month, date.Day)).ToOADate();
                        memberName = i.ToString();
                        break;
                    }
                case BIMembersType.ltTimeYear:
                    {
                        rank = date.Year;
                        memberName = date.Year.ToString();
                        break;
                    }
                default:
                    {
                        throw new Exception("Cannot format this BIType: " + biType);
                    }
            }
        }

        private static double TicksToOADate(long value)
        {
            if (value == 0)
            {
                return 0.0;
            }
            if (value < 0xc92a69c000L)
            {
                value += 0x85103c0cb83c000L;
            }
            if (value < 0x6efdddaec64000L)
            {
                throw new OverflowException("OleAutDateInvalid");
            }
            long num = (value - 0x85103c0cb83c000L) / 0x2710L;
            if (num < 0L)
            {
                long num2 = num % 0x5265c00L;
                if (num2 != 0)
                {
                    num -= (0x5265c00L + num2) * 2L;
                }
            }
            return (((double)num) / 86400000.0);
        }
        
        internal static double ToOADate(this DateTime arg)
        {
            return TicksToOADate(arg.Ticks);
        }

        internal static string GetBIFormatMask(BIMembersType biType)
        {
            switch (biType)
            {
                case BIMembersType.ltTimeDayOfMonth:
                {
                    return "d";
                }
                case BIMembersType.ltTimeDayOfWeekLong:
                {
                    return "dddd";
                }
                case BIMembersType.ltTimeDayOfWeekShort:
                {
                    return "ddd";
                }
                case BIMembersType.ltTimeHour:
                {
                    return "H";
                }
                case BIMembersType.ltTimeMinute:
                {
                    return "m";
                }
                case BIMembersType.ltTimeMonthLong:
                {
                    return "MMMM";
                }
                case BIMembersType.ltTimeMonthShort:
                {
                    return "MMM";
                }
                case BIMembersType.ltTimeSecond:
                {
                    return "s";
                }
                case BIMembersType.ltTimeYear:
                {
                    return "YYYY";
                }
                default:
                {
                    return "";
                }
            }
        }

        internal static double StringToDateTime(string DisplayName, string FormatString)
        {
            return 0;
        }

        internal static void Parse(XmlNodeList nodes, Dictionary<string, string> values)
        {
            foreach (XmlNode n in nodes)
                if (n.Name == "data")
                {
                    var name = n.Attributes["name"].Value;
                    foreach (XmlNode n1 in n.ChildNodes)
                        if (n1.Name == "value")
                        {
                            values.Add(name, n1.InnerText);
                            break;
                        }
                }
                else if (n.ChildNodes.Count > 0)
                {
                    Parse(n.ChildNodes, values);
                }
        }


        internal static bool IsOrdinal(object Value)
        {
            if (Value == null) return false;
            var t = Value.GetType();
            if (t == Type.GetType("System.Byte")) return true;
            if (t == Type.GetType("System.SByte")) return true;
            if (t == Type.GetType("System.Int16")) return true;
            if (t == Type.GetType("System.UInt16")) return true;
            if (t == Type.GetType("System.Int32")) return true;
            if (t == Type.GetType("System.UInt32")) return true;
            if (t == Type.GetType("System.Int64")) return true;
            if (t == Type.GetType("System.UInt64")) return true;
            return false;
        }

        internal static bool IsFloat(object Value)
        {
            if (Value == null) return false;
            var t = Value.GetType();
            if (t == Type.GetType("System.Single")) return true;
            if (t == Type.GetType("System.Double")) return true;
            return false;
        }

        internal static bool IsCurrency(object Value)
        {
            if (Value == null) return false;
            var t = Value.GetType();
            if (t == Type.GetType("System.Decimal")) return true;
            return false;
        }

        internal static bool IsDateTime(object Value)
        {
            if (Value == null) return false;
            var t = Value.GetType();
            if (t == Type.GetType("System.DateTime")) return true;
            if (t == Type.GetType("System.TimeSpan")) return true;
            return false;
        }

        internal static bool IsNumeric(object Value)
        {
            try
            {
                Convert.ToDouble(Value);
                return true;
            }
            catch

            {
                return false;
            }
        }

        internal static string InternalFormatValue(object Value, string FormatStr,
            string DefaultFormatStr, string DefaultCurrencyStr, string EmptyDataString)
        {
            var Result = "";

            if (Value == null) return EmptyDataString;

            if (FormatStr == "Standard") FormatStr = "";
            if (FormatStr == "" && IsOrdinal(Value))
                return Convert.ToInt64(Value).ToString("#,#");

            if (FormatStr == "" && IsFloat(Value))

                return Convert.ToDouble(Value).ToString("#,0.00");

            if (FormatStr == "" && IsCurrency(Value)) FormatStr = "Currency";
            if (FormatStr == "" && IsDateTime(Value)) FormatStr = "Short Date";

            if (FormatStr == "Currency" && IsNumeric(Value))
            {
                if (DefaultCurrencyStr == "")

                    return Convert.ToDouble(Value).ToString("c");
                return Convert.ToDouble(Value).ToString(DefaultCurrencyStr);
            }
            if (FormatStr == "Short Date" && IsDateTime(Value))
                return Convert.ToDateTime(Value).ToString();
            if (FormatStr == "Short Time" && IsDateTime(Value))
                return Convert.ToDateTime(Value).ToString();
            if (FormatStr == "Percent" && IsNumeric(Value))
                return Convert.ToDouble(Value).ToString("#,0.00%");

            if (Result == "" && IsDateTime(Value))
                return Convert.ToDateTime(Value).ToString(DefaultFormatStr);

            if (Result == "" && IsNumeric(Value))
                return Convert.ToDouble(Value).ToString(DefaultFormatStr);

            if (Result == "") Result = Value.ToString();

            return Result;
        }

        internal static string MakeUniqueName(string displayName, string parentUniqueName)
        {
            var regex = new Regex(@"[^\w\s\.:\$@#%&\*-/]", RegexOptions.ECMAScript);
            var s = string.IsNullOrEmpty(parentUniqueName) ? "" : parentUniqueName + ".";
            return s + "[" + (regex.IsMatch(displayName) ? ToMD5(displayName) : displayName) + "]";
        }

        private static string ToMD5(string source)
        {
            var b = Encoding.UTF8.GetBytes(source);
            var provider = MD5.Create();
            var bbb = provider.ComputeHash(b);
            var sb = new StringBuilder();
            foreach (var bb in bbb)
                sb.Append(bb.ToString("X2"));
            return sb.ToString();
        }

        internal static string GetMSASPublicName()
        {
            return "RadarCube ASP.NET for MSAS";
        }

        internal static string GetResStr(string resourceName, params object[] args)
        {
            return args.Length == 0 ? GetResStr(resourceName) : string.Format(GetResStr(resourceName), args);
        }

        internal static string GetResStr(string resourceName)
        {
            return GetResStr(resourceName, CultureInfo.CurrentCulture);
        }

        internal static string GetResStr(string resourceName, CultureInfo culture)
        {
            string s = null;
            var lang = culture.Name;

            Dictionary<string, string> cres;

            if (_Nativeresx.TryGetValue(lang, out cres) == false)
            {
                cres = new Dictionary<string, string>();
                _Nativeresx.Add(lang, cres);
            }

            if (cres.TryGetValue(resourceName, out s) == false)
                if (TryGetStringValue(resourceName, lang, out s) == false)
                    if (string.IsNullOrEmpty(s) && lang.Length > 2)
                    {
                        var shortLang = lang.Substring(0, 2);
                        s = GetResStr(resourceName, new CultureInfo(shortLang));
                        if (s != null)
                            cres.Add(resourceName, s);
                    }
            
            var enCI = new CultureInfo("en");
            if (s == null && culture.Name != enCI.Name)
            {
                s = GetResStr(resourceName, enCI);
                if (s != null)
                    cres.Add(resourceName, s);
            }

            return s ?? resourceName;
        }

        private static bool TryGetStringValue(string resourceName, string lang, out string s)
        {
            try
            {
                s = null;
                Dictionary<string, string> native;
                if (_Nativeresx.TryGetValue(lang, out native) == false)
                {
                    native = new Dictionary<string, string>();
                    _Nativeresx.Add(lang, native);
                }

                if (native.TryGetValue(resourceName, out s) == false)
                {
                    s = _Rm.GetString(resourceName, CultureInfo.CurrentCulture);
                    if (s != null)
                    {
                        native.Add(resourceName, s);
                        return true;
                    }
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                s = null;
                return false;
            }
        }

        internal static Member GetMember(IMemberCell AMemberCell, MemberType AMemberType)
        {
            var m = GetMemberCell(AMemberCell, AMemberType);
            return m != null ? m.Member : null;
        }

        internal static Member GetMember(IMemberCell AMemberCell)
        {
            return GetMember(AMemberCell, MemberType.mtCommon);
        }

        internal static IMemberCell GetMemberCell(IMemberCell AMemberCell, MemberType AMemberType)
        {
            return GetMemberCell2(AMemberCell, AMemberType);
        }

        internal static IMemberCell GetMemberCell(IMemberCell AMemberCell)
        {
            return GetMemberCell2(AMemberCell, MemberType.mtCommon);
        }

        internal static IMemberCell GetMemberCell1(IMemberCell AMemberCell, MemberType AMemberType)
        {
            if (AMemberCell == null)
                return null;
            var m = AMemberCell;
            while (m.Member != null && m.Member.MemberType != AMemberType && m.Parent != null)
                m = m.Parent;
            return m;
        }

        internal static IMemberCell GetMemberCell1(IMemberCell AMemberCell)
        {
            return GetMemberCell1(AMemberCell, MemberType.mtCommon);
        }

        internal static IMemberCell GetMemberCell2(IMemberCell AMemberCell, MemberType AMemberType)
        {
            var t_membercell = AMemberCell as MemberCell;
            if (t_membercell == null)
                return GetMemberCell1(AMemberCell);

            if (t_membercell.ParentCell == null)
                t_membercell.ParentCell = GetMemberCell1(AMemberCell, AMemberType);
            return t_membercell.ParentCell;
        }

        internal static IMemberCell GetMemberCell2(IMemberCell AMemberCell)
        {
            return GetMemberCell2(AMemberCell, MemberType.mtCommon);
        }

        internal static IEnumerable<Member> GetParents(IMemberCell Member)
        {
            if (Member == null)
                return new Member[0] { };

            return GetParents2(Member);
        }

        internal static IEnumerable<Member> GetParents1(IMemberCell RowMember)
        {
            var membercell = GetMemberCell(RowMember);
            var en_members = GetParentMembers(membercell);
            return en_members.ToList();
        }

        internal static IEnumerable<Member> GetParents2(IMemberCell AMemberCell)
        {
#if DEBUG
            //if (AMemberCell.Member != null && AMemberCell.Member.DisplayName == "Seattle")
            //{ 
            //    // 26146

            //}
#endif
            var t_membercell = AMemberCell as MemberCell;
            if (t_membercell == null)
                return GetParents1(AMemberCell);

            if (t_membercell.ParentCells == null)
            {
                var membercell = GetMemberCell(AMemberCell);
                var en = GetParentMembers(membercell).Distinct();
                t_membercell.ParentCells = en.ToList();
            }
#if DEBUG
            //else
            //{

            //}
#endif

            return t_membercell.ParentCells;
        }

        internal static IEnumerable<Member> GetParentMembers(IMemberCell mc)
        {
            var current = mc;
            yield return mc.Member;

            while (current != null && current.Member != null && current.Parent != null)
            {
                current = current.Parent;
                yield return current.Member;
            }
        }

        internal static string GetExceptionText(Exception ex, bool WithMessage)
        {
            var sb = new StringBuilder();
            if (WithMessage)
                sb.AppendFormat("Message: {0}", ex.Message);
            sb.AppendFormat("Stack trace: {0}", ex.Message);
            sb.AppendLine();
            sb.Append(ex.StackTrace);
            ex = ex.InnerException;
            while (ex != null)
            {
                sb.AppendLine();
                sb.AppendLine("-------------");
                sb.AppendLine();
                sb.Append("Inner exception: " + ex.GetType().FullName);
                sb.AppendLine();
                sb.Append("Message:");
                sb.AppendLine();
                sb.Append(ex.Message);
                sb.AppendLine();
                sb.AppendLine();
                sb.Append("Stack trace:");
                sb.AppendLine();
                sb.Append(ex.StackTrace);
                ex = ex.InnerException;
            }
            return sb.ToString();
        }

        internal static string GetExceptionText(Exception ex)
        {
            return GetExceptionText(ex, false);
        }

        public static bool CutText(string source, int MaxLength, out string result)
        {
            if (source.Length <= MaxLength)
            {
                result = source;
                return false;
            }

            var p = MaxLength - 1;
            for (var i = p; i > Math.Max(0, p - 5); i--)
                if (source[i] == ' ')
                {
                    p = i - 1;
                    break;
                }
            result = source.Substring(0, p) + "...";
            return true;
        }

        internal static string Join<T>(char aChar, IList<T> aList)
        {
            var sb = new StringBuilder(aList.Count * 2);

            for (var i = 0; i < aList.Count; i++)
            {
                if (i != 0)
                    sb.Append(aChar);

                sb.Append(aList[i]);
            }
            return sb.ToString();
        }

        internal static string Join<T>(string aChar, IEnumerable<T> aList)
        {
            if (!aList.Any())
                return string.Empty;

            var sb = new StringBuilder(aList.Count() + 2);
            sb.Append(aList.First());

            foreach (var item in aList.Skip(1))
            {
                sb.Append(aChar);
                sb.Append(item);
            }

            return sb.ToString();
        }

        internal static Bitmap GetBitmapFromCanvasData(string htmlCanvasData)
        {
            Bitmap image = null;
            var base64Data = Regex.Match(htmlCanvasData, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
            var binData = Convert.FromBase64String(base64Data);

            using (var stream = new MemoryStream(binData))
            {
                image = new Bitmap(stream);
            }

            return image;
        }

        internal static object GetPropertyValue(object source, string propertyName)
        {
            var pis = propertyName.Split('.');
            object res = null;
            foreach (var pi in pis)
            {
                var pimodel = source.GetType().GetProperty(pi,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pimodel == null)
                    return null;
                source = pimodel.GetValue(source, new object[] { });
            }
            //var pimodel = source.GetType().GetProperty(propertyName);
            //if (pimodel == null)
            //    return null;
            return source;
        }

        internal static object GetPropertyValueInner(object source, string propertyName)
        {
            var pimodel = source.GetType().GetProperty(propertyName);
            if (pimodel == null)
                return null;
            return pimodel.GetValue(source, new object[] { });
        }

        internal static IEnumerable<object> GetPropertyCollection(object source, string propertyName)
        {
            var pimodel = source.GetType().GetProperty(propertyName);
            if (pimodel == null)
                return null;
            var res = pimodel.GetValue(source, new object[] { }) as IEnumerable;
            if (res == null)
                return new object[] { };

            return res.Cast<object>();
        }

        internal static void SetPropertyValue(object source, string propertyName, object value)
        {
            var pimodel = source.GetType().GetProperty(propertyName);
            if (pimodel == null)
                return;

            pimodel.SetValue(source, value, new object[] { });
        }

        internal static string GetKeyName(object p)
        {
            if (Info.IsBytes(p.GetType()) == false)
                return p.ToString().Trim();

            var b = p as byte[];
            if (b == null)
                return null;
            return ComputeCRC(b).ToString();
        }

        internal static string ToKeyName(object p)
        {
            if (p == null)
                return string.Empty;

            if (Info.IsBytes(p.GetType()))
                return GetKeyName(p);

            if (Info.IsDateTime(p.GetType()))
                return ((DateTime) p).ToBinary().ToString();
            return p.ToString().Trim();
        }

        internal static string ToDisplayName(object p)
        {
            if (p == null)
                return null;
            if (Info.IsBytes(p.GetType()))
                return null;
            return p.ToString().Trim();
        }

        internal static Hierarchy ConvertLevelToHierarchy(Level aLevel)
        {
            var ch = aLevel.Hierarchy.Dimension.CubeDimension.Hierarchies.Find(aLevel.Hierarchy.UniqueName);
            if (ch == null || ch.ChildrenList.Count == 0)
                return null;

            var cl = ch.FCubeLevels.Find(aLevel.UniqueName);
            if (cl == null)
                return null;

            var index = ch.FCubeLevels.IndexOf(cl);
            if (index == -1)
                return null;

            var ch1 = ch.ChildrenList[index];
            if (ch1 == null)
                return null;

            return aLevel.Hierarchy.Dimension.Hierarchies.Find(ch1.UniqueName);
        }

        internal static string GetRandomName()
        {
            var res = Path.GetRandomFileName();
            res = res.Replace(".", random.Next().ToString());
            return res;
        }

        internal static bool IsStreamEquals(MemoryStream current, MemoryStream m)
        {
            if (current.Length != m.Length)
                return false;

            var equal = true;
            ArraySegment<byte> b1;
            current.TryGetBuffer(out b1);
            ArraySegment<byte> b;
            m.TryGetBuffer(out b);
            var b1_arr = b1.Array;
            var b_arr = b.Array;
            for (var i = 0; i < b_arr.Length; i++)
                if (b_arr[i] != b1_arr[i])
                    return false;

            return true;
        }

        internal static IEnumerable<Assembly> GetReferencingAssemblies()
        {
            var assemblyName = "RadarSoft.RadarCube.OlapAnalysis";
            
            var assemblies = new List<Assembly>();
            var dependencies = DependencyContext.Default.RuntimeLibraries;
            foreach (var library in dependencies)
                if (IsCandidateLibrary(library, assemblyName))
                {
                    var assembly = Assembly.Load(new AssemblyName(library.Name));
                    assemblies.Add(assembly);
                }
            return assemblies;
        }

        private static bool IsCandidateLibrary(RuntimeLibrary library, string assemblyName)
        {
            return library.Name == assemblyName
                   || library.Dependencies.Any(d => d.Name.StartsWith(assemblyName));
        }

        public sealed class Crc32
        {
            private const uint CrcSeed = 0xFFFFFFFF;

            private static readonly uint[] CrcTable =
            {
                0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA, 0x076DC419,
                0x706AF48F, 0xE963A535, 0x9E6495A3, 0x0EDB8832, 0x79DCB8A4,
                0xE0D5E91E, 0x97D2D988, 0x09B64C2B, 0x7EB17CBD, 0xE7B82D07,
                0x90BF1D91, 0x1DB71064, 0x6AB020F2, 0xF3B97148, 0x84BE41DE,
                0x1ADAD47D, 0x6DDDE4EB, 0xF4D4B551, 0x83D385C7, 0x136C9856,
                0x646BA8C0, 0xFD62F97A, 0x8A65C9EC, 0x14015C4F, 0x63066CD9,
                0xFA0F3D63, 0x8D080DF5, 0x3B6E20C8, 0x4C69105E, 0xD56041E4,
                0xA2677172, 0x3C03E4D1, 0x4B04D447, 0xD20D85FD, 0xA50AB56B,
                0x35B5A8FA, 0x42B2986C, 0xDBBBC9D6, 0xACBCF940, 0x32D86CE3,
                0x45DF5C75, 0xDCD60DCF, 0xABD13D59, 0x26D930AC, 0x51DE003A,
                0xC8D75180, 0xBFD06116, 0x21B4F4B5, 0x56B3C423, 0xCFBA9599,
                0xB8BDA50F, 0x2802B89E, 0x5F058808, 0xC60CD9B2, 0xB10BE924,
                0x2F6F7C87, 0x58684C11, 0xC1611DAB, 0xB6662D3D, 0x76DC4190,
                0x01DB7106, 0x98D220BC, 0xEFD5102A, 0x71B18589, 0x06B6B51F,
                0x9FBFE4A5, 0xE8B8D433, 0x7807C9A2, 0x0F00F934, 0x9609A88E,
                0xE10E9818, 0x7F6A0DBB, 0x086D3D2D, 0x91646C97, 0xE6635C01,
                0x6B6B51F4, 0x1C6C6162, 0x856530D8, 0xF262004E, 0x6C0695ED,
                0x1B01A57B, 0x8208F4C1, 0xF50FC457, 0x65B0D9C6, 0x12B7E950,
                0x8BBEB8EA, 0xFCB9887C, 0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3,
                0xFBD44C65, 0x4DB26158, 0x3AB551CE, 0xA3BC0074, 0xD4BB30E2,
                0x4ADFA541, 0x3DD895D7, 0xA4D1C46D, 0xD3D6F4FB, 0x4369E96A,
                0x346ED9FC, 0xAD678846, 0xDA60B8D0, 0x44042D73, 0x33031DE5,
                0xAA0A4C5F, 0xDD0D7CC9, 0x5005713C, 0x270241AA, 0xBE0B1010,
                0xC90C2086, 0x5768B525, 0x206F85B3, 0xB966D409, 0xCE61E49F,
                0x5EDEF90E, 0x29D9C998, 0xB0D09822, 0xC7D7A8B4, 0x59B33D17,
                0x2EB40D81, 0xB7BD5C3B, 0xC0BA6CAD, 0xEDB88320, 0x9ABFB3B6,
                0x03B6E20C, 0x74B1D29A, 0xEAD54739, 0x9DD277AF, 0x04DB2615,
                0x73DC1683, 0xE3630B12, 0x94643B84, 0x0D6D6A3E, 0x7A6A5AA8,
                0xE40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1, 0xF00F9344,
                0x8708A3D2, 0x1E01F268, 0x6906C2FE, 0xF762575D, 0x806567CB,
                0x196C3671, 0x6E6B06E7, 0xFED41B76, 0x89D32BE0, 0x10DA7A5A,
                0x67DD4ACC, 0xF9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5,
                0xD6D6A3E8, 0xA1D1937E, 0x38D8C2C4, 0x4FDFF252, 0xD1BB67F1,
                0xA6BC5767, 0x3FB506DD, 0x48B2364B, 0xD80D2BDA, 0xAF0A1B4C,
                0x36034AF6, 0x41047A60, 0xDF60EFC3, 0xA867DF55, 0x316E8EEF,
                0x4669BE79, 0xCB61B38C, 0xBC66831A, 0x256FD2A0, 0x5268E236,
                0xCC0C7795, 0xBB0B4703, 0x220216B9, 0x5505262F, 0xC5BA3BBE,
                0xB2BD0B28, 0x2BB45A92, 0x5CB36A04, 0xC2D7FFA7, 0xB5D0CF31,
                0x2CD99E8B, 0x5BDEAE1D, 0x9B64C2B0, 0xEC63F226, 0x756AA39C,
                0x026D930A, 0x9C0906A9, 0xEB0E363F, 0x72076785, 0x05005713,
                0x95BF4A82, 0xE2B87A14, 0x7BB12BAE, 0x0CB61B38, 0x92D28E9B,
                0xE5D5BE0D, 0x7CDCEFB7, 0x0BDBDF21, 0x86D3D2D4, 0xF1D4E242,
                0x68DDB3F8, 0x1FDA836E, 0x81BE16CD, 0xF6B9265B, 0x6FB077E1,
                0x18B74777, 0x88085AE6, 0xFF0F6A70, 0x66063BCA, 0x11010B5C,
                0x8F659EFF, 0xF862AE69, 0x616BFFD3, 0x166CCF45, 0xA00AE278,
                0xD70DD2EE, 0x4E048354, 0x3903B3C2, 0xA7672661, 0xD06016F7,
                0x4969474D, 0x3E6E77DB, 0xAED16A4A, 0xD9D65ADC, 0x40DF0B66,
                0x37D83BF0, 0xA9BCAE53, 0xDEBB9EC5, 0x47B2CF7F, 0x30B5FFE9,
                0xBDBDF21C, 0xCABAC28A, 0x53B39330, 0x24B4A3A6, 0xBAD03605,
                0xCDD70693, 0x54DE5729, 0x23D967BF, 0xB3667A2E, 0xC4614AB8,
                0x5D681B02, 0x2A6F2B94, 0xB40BBE37, 0xC30C8EA1, 0x5A05DF1B,
                0x2D02EF8D
            };

            /// <summary>
            ///     The crc data checksum so far.
            /// </summary>
            private uint crc;

            /// <summary>
            ///     Returns the CRC32 data checksum computed so far.
            /// </summary>
            public long Value
            {
                get => crc;
                set => crc = (uint) value;
            }

            internal static uint ComputeCrc32(uint oldCrc, byte value)
            {
                return CrcTable[(oldCrc ^ value) & 0xFF] ^ (oldCrc >> 8);
            }

            /// <summary>
            ///     Resets the CRC32 data checksum as if no update was ever called.
            /// </summary>
            public void Reset()
            {
                crc = 0;
            }

            /// <summary>
            ///     Updates the checksum with the int bval.
            /// </summary>
            /// <param name="value">
            ///     the byte is taken as the lower 8 bits of value
            /// </param>
            public void Update(int value)
            {
                crc ^= CrcSeed;
                crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
                crc ^= CrcSeed;
            }

            /// <summary>
            ///     Updates the checksum with the bytes taken from the array.
            /// </summary>
            /// <param name="buffer">
            ///     buffer an array of bytes
            /// </param>
            public void Update(byte[] buffer)
            {
                if (buffer == null)
                    throw new ArgumentNullException("buffer");

                Update(buffer, 0, buffer.Length);
            }

            /// <summary>
            ///     Adds the byte array to the data checksum.
            /// </summary>
            /// <param name="buffer">
            ///     The buffer which contains the data
            /// </param>
            /// <param name="offset">
            ///     The offset in the buffer where the data starts
            /// </param>
            /// <param name="count">
            ///     The number of data bytes to update the CRC with.
            /// </param>
            public void Update(byte[] buffer, int offset, int count)
            {
                if (buffer == null)
                    throw new ArgumentNullException("buffer");

                if (count < 0)
                {
#if NETCF_1_0
                    throw new ArgumentOutOfRangeException("count");
#else
                    throw new ArgumentOutOfRangeException("count", "Count cannot be less than zero");
#endif
                }

                if (offset < 0 || offset + count > buffer.Length)
                    throw new ArgumentOutOfRangeException("offset");

                crc ^= CrcSeed;

                while (--count >= 0)
                    crc = CrcTable[(crc ^ buffer[offset++]) & 0xFF] ^ (crc >> 8);

                crc ^= CrcSeed;
            }
        }
        internal static long DoubleDateToTicks(double value)
        {
            if ((value >= 2958466.0) || (value <= -657435.0))
            {
                throw new ArgumentException("Arg_OleAutDateInvalid");
            }
            long num = (long)((value * 86400000.0) + ((value >= 0.0) ? 0.5 : -0.5));
            if (num < 0L)
            {
                num -= (num % 0x5265c00L) * 2L;
            }
            num += 0x3680b5e1fc00L;
            if ((num < 0L) || (num >= 0x11efae44cb400L))
            {
                throw new ArgumentException("Arg_OleAutDateScale");
            }
            return (num * 0x2710L);
        }
        
        public static DateTime FromOADate(double arg)
        {
            return new DateTime(DoubleDateToTicks(arg), DateTimeKind.Unspecified);
            
        }
    }
}