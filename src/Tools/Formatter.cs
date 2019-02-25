using System;
using System.Drawing;
using System.Globalization;

namespace RadarSoft.RadarCube.Tools
{
    /// <summary>
    ///     Provides XAML formatting functions.
    /// </summary>
    public static class Formatter
    {
        /// <summary>
        ///     {x:null}
        /// </summary>
        internal const string __xNULL = "{x:null}";

        /// <summary>
        ///     null
        /// </summary>
        internal const string __NULL = "null";

        /// <summary>
        ///     Formats the specified value.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <returns></returns>
        public static string Format(this bool value)
        {
            return value ? "true" : "false";
        }

        /// <summary>
        ///     For Nullable ONLY !!!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Format<T>(this T? value)
            where T : struct
        {
            if (value.HasValue)
                return value.HasValue.Format();
            return __xNULL;
        }

        /// <summary>
        ///     Formats the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string Format(this short value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Formats the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string Format(this int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Formats the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string Format(this double value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.###}", value);
        }

        /// <summary>
        ///     Formats the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string Format(this Enum value)
        {
            return value.ToString();
        }


        /// <summary>
        ///     Formats the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string Format(this Color value)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", value.A, value.R, value.G, value.B);
        }

        public static string Format(this Color value, bool withoutAlpha)
        {
            if (withoutAlpha)
                return string.Format("#{0:X2}{1:X2}{2:X2}", value.R, value.G, value.B);
            return value.Format();
        }

        /// <summary>
        ///     Formats the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string Format(this Uri value)
        {
            if (value == null) throw new ArgumentNullException("value");
            return value.ToString();
        }

        /// <summary>
        ///     Formats the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string Format(this FontFamily value)
        {
            if (value == null) throw new ArgumentNullException("value");
            return value.Name;
        }
    }
}