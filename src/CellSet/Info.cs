using System;
using System.IO;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.State;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.CellSet
{
    internal static class Info
    {
        public static readonly char rsFieldSeparator = ';';
        public static readonly string rsPrimaryIndexName = "$Primary";
        public static readonly char rsLeftBracket = '[';
        public static readonly char rsRightBracket = ']';
        public static readonly char rsMemberDelimiter = '.';

        public static bool IsString(Type type)
        {
            return type == typeof(string) || type == typeof(char);
        }

        public static bool IsFloat(Type type)
        {
            return type == typeof(float) || type == typeof(double);
        }

        public static bool IsInteger(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);
        }

        public static bool IsDecimal(Type type)
        {
            return type == typeof(decimal);
        }

        public static bool IsDateTime(Type type)
        {
            return type == typeof(DateTime) || type == typeof(TimeSpan);
        }

        public static bool IsNumeric(Type type)
        {
            return IsInteger(type) || IsFloat(type) || IsDecimal(type);
        }

        public static object NormilizeObjectType(object obj)
        {
            // normalizes the line data type
            if (obj == null) return null;
            if (IsFloat(obj.GetType())) return Convert.ToDouble(obj);
            if (IsDecimal(obj.GetType())) return Convert.ToDecimal(obj);
            if (IsInteger(obj.GetType())) return Convert.ToInt64(obj);
            return Convert.ToString(obj);
        }

        public static Type NormilizeType(Type type)
        {
            if (IsFloat(type)) return typeof(double);
            if (IsDecimal(type)) return typeof(decimal);
            if (IsInteger(type)) return typeof(long);
            return typeof(string);
        }

        internal static string ExtractWord(string s, ref int index)
        {
            // the string indexing starts with 0
            var StartPos = index;
            var n = s.Length;
            while (index < n)
                if (s[index] == rsFieldSeparator) break;
                else index++;
            if (index < n) return s.Substring(StartPos, index++ - StartPos);
            return s.Substring(StartPos, index - StartPos);
        }

        internal static bool SplitFieldName(string FullFieldName, out string TableName, out string ColumnName)
        {
            var index = FullFieldName.IndexOf('.');
            if (index < 0)
            {
                TableName = "";
                ColumnName = FullFieldName;
                return false;
            }
            TableName = FullFieldName.Substring(0, index);
            ColumnName = FullFieldName.Substring(index + 1);
            return true;
        }

        internal static string GenerateMeasureUniqueName(string DisplayName)
        {
            return RadarUtils.MakeUniqueName(DisplayName, "[Measures]");
        }


        internal static void CreateOrClear(string dir)
        {
            var p = TempDirectory.ExtractPath(dir);
            if (Directory.Exists(p))
                TempDirectory.SafeDirectoryDelete(dir);
            else
                SessionState.DoCreateDirectory(p);
        }

        internal static bool MeetsMeasureFilter(object Value, Type dataType, MeasureFilter F)
        {
            if (Value == null || F == null) return true;
            if (IsFloat(dataType))
            {
                var value = Convert.ToDouble(Value);
                double FilterFirstValue;
                double FilterSecondValue;
                try
                {
                    FilterFirstValue = Convert.ToDouble(F.FirstValue);
                }
                catch
                {
                    return true;
                }
                switch (F.FilterCondition)
                {
                    case OlapFilterCondition.fcEqual:
                        return value.CompareTo(FilterFirstValue) == 0;
                    case OlapFilterCondition.fcNotEqual:
                        return value.CompareTo(FilterFirstValue) != 0;
                    case OlapFilterCondition.fcLess:
                        return value.CompareTo(FilterFirstValue) < 0;
                    case OlapFilterCondition.fcNotLess:
                        return value.CompareTo(FilterFirstValue) >= 0;
                    case OlapFilterCondition.fcGreater:
                        return value.CompareTo(FilterFirstValue) > 0;
                    case OlapFilterCondition.fcNotGreater:
                        return value.CompareTo(FilterFirstValue) <= 0;
                    case OlapFilterCondition.fcBetween:
                        try
                        {
                            FilterSecondValue = Convert.ToDouble(F.SecondValue);
                        }
                        catch
                        {
                            return true;
                        }
                        return value.CompareTo(FilterFirstValue) >= 0 && value.CompareTo(FilterSecondValue) <= 0;
                    case OlapFilterCondition.fcNotBetween:
                        try
                        {
                            FilterSecondValue = Convert.ToDouble(F.SecondValue);
                        }
                        catch
                        {
                            return true;
                        }
                        return !(value.CompareTo(FilterFirstValue) >= 0 && value.CompareTo(FilterSecondValue) <= 0);
                }
            }
            else if (IsInteger(dataType))
            {
                var value = Convert.ToInt64(Value);
                long FilterFirstValue;
                long FilterSecondValue;
                try
                {
                    FilterFirstValue = Convert.ToInt64(F.FirstValue);
                }
                catch
                {
                    return true;
                }
                switch (F.FilterCondition)
                {
                    case OlapFilterCondition.fcEqual:
                        return value.CompareTo(FilterFirstValue) == 0;
                    case OlapFilterCondition.fcNotEqual:
                        return value.CompareTo(FilterFirstValue) != 0;
                    case OlapFilterCondition.fcLess:
                        return value.CompareTo(FilterFirstValue) < 0;
                    case OlapFilterCondition.fcNotLess:
                        return value.CompareTo(FilterFirstValue) >= 0;
                    case OlapFilterCondition.fcGreater:
                        return value.CompareTo(FilterFirstValue) > 0;
                    case OlapFilterCondition.fcNotGreater:
                        return value.CompareTo(FilterFirstValue) <= 0;
                    case OlapFilterCondition.fcBetween:
                        try
                        {
                            FilterSecondValue = Convert.ToInt64(F.SecondValue);
                        }
                        catch
                        {
                            return true;
                        }
                        return value.CompareTo(FilterFirstValue) >= 0 && value.CompareTo(FilterSecondValue) <= 0;
                    case OlapFilterCondition.fcNotBetween:
                        try
                        {
                            FilterSecondValue = Convert.ToInt64(F.SecondValue);
                        }
                        catch
                        {
                            return true;
                        }
                        return !(value.CompareTo(FilterFirstValue) >= 0 && value.CompareTo(FilterSecondValue) <= 0);
                }
            }
            else if (IsDecimal(dataType))
            {
                var value = Convert.ToDecimal(Value);
                decimal FilterFirstValue;
                decimal FilterSecondValue;
                try
                {
                    FilterFirstValue = Convert.ToDecimal(F.FirstValue);
                }
                catch
                {
                    return true;
                }
                switch (F.FilterCondition)
                {
                    case OlapFilterCondition.fcEqual:
                        return value.CompareTo(FilterFirstValue) == 0;
                    case OlapFilterCondition.fcNotEqual:
                        return value.CompareTo(FilterFirstValue) != 0;
                    case OlapFilterCondition.fcLess:
                        return value.CompareTo(FilterFirstValue) < 0;
                    case OlapFilterCondition.fcNotLess:
                        return value.CompareTo(FilterFirstValue) >= 0;
                    case OlapFilterCondition.fcGreater:
                        return value.CompareTo(FilterFirstValue) > 0;
                    case OlapFilterCondition.fcNotGreater:
                        return value.CompareTo(FilterFirstValue) <= 0;
                    case OlapFilterCondition.fcBetween:
                        try
                        {
                            FilterSecondValue = Convert.ToDecimal(F.SecondValue);
                        }
                        catch
                        {
                            return true;
                        }
                        return value.CompareTo(FilterFirstValue) >= 0 && value.CompareTo(FilterSecondValue) <= 0;
                    case OlapFilterCondition.fcNotBetween:
                        try
                        {
                            FilterSecondValue = Convert.ToDecimal(F.SecondValue);
                        }
                        catch
                        {
                            return true;
                        }
                        return !(value.CompareTo(FilterFirstValue) >= 0 && value.CompareTo(FilterSecondValue) <= 0);
                }
            }
            else if (IsString(dataType))
            {
                var value = Convert.ToString(Value);
                string FilterFirstValue;
                string FilterSecondValue;
                try
                {
                    FilterFirstValue = Convert.ToString(F.FirstValue);
                }
                catch
                {
                    return true;
                }
                switch (F.FilterCondition)
                {
                    case OlapFilterCondition.fcEqual:
                        return value.CompareTo(FilterFirstValue) == 0;
                    case OlapFilterCondition.fcNotEqual:
                        return value.CompareTo(FilterFirstValue) != 0;
                    case OlapFilterCondition.fcStartsWith:
                        return value.StartsWith(FilterFirstValue, StringComparison.CurrentCulture);
                    case OlapFilterCondition.fcNotStartsWith:
                        return value.StartsWith(FilterFirstValue, StringComparison.CurrentCulture) == false;
                    case OlapFilterCondition.fcEndsWith:
                        return value.EndsWith(FilterFirstValue, StringComparison.CurrentCulture);
                    case OlapFilterCondition.fcNotEndsWith:
                        return value.EndsWith(FilterFirstValue, StringComparison.CurrentCulture) == false;
                    case OlapFilterCondition.fcContains:
                        return value.Contains(FilterFirstValue);
                    case OlapFilterCondition.fcNotContains:
                        return value.Contains(FilterFirstValue) == false;
                    case OlapFilterCondition.fcLess:
                        return value.CompareTo(FilterFirstValue) < 0;
                    case OlapFilterCondition.fcNotLess:
                        return value.CompareTo(FilterFirstValue) >= 0;
                    case OlapFilterCondition.fcGreater:
                        return value.CompareTo(FilterFirstValue) > 0;
                    case OlapFilterCondition.fcNotGreater:
                        return value.CompareTo(FilterFirstValue) <= 0;
                    case OlapFilterCondition.fcBetween:
                        try
                        {
                            FilterSecondValue = Convert.ToString(F.SecondValue);
                        }
                        catch
                        {
                            return true;
                        }
                        return value.CompareTo(FilterFirstValue) >= 0 && value.CompareTo(FilterSecondValue) <= 0;
                    case OlapFilterCondition.fcNotBetween:
                        try
                        {
                            FilterSecondValue = Convert.ToString(F.SecondValue);
                        }
                        catch
                        {
                            return true;
                        }
                        return !(value.CompareTo(FilterFirstValue) >= 0 && value.CompareTo(FilterSecondValue) <= 0);
                }
            }
            return true;
        }

        internal static bool IsBytes(Type type)
        {
            return type == typeof(byte[]);
        }
    }


    //internal class OpenNodesCollection : SortedList<string, PossibleDrillActions>
    //{
    //    public new void Add(string key, PossibleDrillActions value)
    //    {
    //        if (!ContainsKey(key)) base.Add(key, value);
    //    }

    //    public OpenNodesCollection()
    //        : base()
    //    {
    //    }

    //    public OpenNodesCollection(int capacity)
    //        : base(capacity)
    //    {
    //    }

    //}


    //[Serializable]

    //[Serializable]

    //[Serializable]

    //[Serializable]

    //[Serializable]

    //[Serializable]

    //[Serializable]

    //[Serializable]

    //[Serializable]


    //[Serializable]

    //[Serializable]

    //[Serializable]
}