using System;
using System.Drawing;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.State;

namespace RadarSoft.RadarCube.Events
{
    public delegate void SelectionInfoResultFormatHandler(object sender, SelectionInfoResultFormatHandlerArgs e);

    public class SelectionInfoResultFormatHandlerArgs
    {
        internal SelectionInfoResultFormatHandlerArgs(string aFunctionName)
        {
            // TODO: Complete member initialization
            FunctionName = aFunctionName;
        }

        public string FunctionName { get; }

        public string Result { get; set; }

        public object Source { get; internal set; }
    }

    public delegate void AnalysisTypeChangingHandler(object sender, AnalysisTypeChangingHandlerArgs e);

    public class AnalysisTypeChangingHandlerArgs
    {
        internal AnalysisTypeChangingHandlerArgs()
        {
            AnalysisTypeMethod = AnalysisTypeMethod.Default;
        }

        public AnalysisTypeMethod AnalysisTypeMethod { get; set; }
    }

    public delegate void AnalysisTypeChangedHandler(object sender, AnalysisTypeChangedHandlerArgs e);

    public class AnalysisTypeChangedHandlerArgs
    {
        internal AnalysisTypeChangedHandlerArgs()
        {
        }
    }

    public delegate void DataConverterHandler(object sender, DataConverterHandlerArgs e);

    public class DataConverterHandlerArgs
    {
        private readonly OlapControl tOLAPGridGeneric;

        internal DataConverterHandlerArgs(object AData, OlapControl tOLAPGridGeneric)
        {
            InputData = AData;
            this.tOLAPGridGeneric = tOLAPGridGeneric;
        }

        public object OutputData { get; set; }

        public object InputData { get; }

        internal object ConvertString(object AData)
        {
            if (AData is string)
            {
                var src = AData as string;
                if (tOLAPGridGeneric.CutLength > 0 && src.Length > tOLAPGridGeneric.CutLength)
                {
                    //return src;
                    var lines = src.Split(new string[1] {"\n"}, StringSplitOptions.RemoveEmptyEntries);
                    for (var i = 0; i < lines.Length; i++)
                        lines[i] = tOLAPGridGeneric.CutLineOfText(lines[i]);
                    return string.Join("\n", lines);
                }
            }
            return AData;
        }
    }

    internal class SizeModificatorArgs
    {
        public SizeModificatorArgs(Size minSize)
        {
            Value = minSize;
        }

        public Size Value { get; set; }
    }

    internal delegate void SizeModificatorHandler(object sender, SizeModificatorArgs e);

    public class OnSerializeArgs
    {
        private readonly OlapAxisLayoutSerializer _Serializer;

        internal OnSerializeArgs(OlapAxisLayoutSerializer arg)
        {
            _Serializer = arg;
        }

        public string Data
        {
            get => _Serializer.Data;
            set => _Serializer.Data = value;
        }
    }

    public delegate void OnSerializeHandler(object sender, OnSerializeArgs e);

    public class ValueChangedEventArgs<T> : EventArgs
    {
        internal ValueChangedEventArgs(T old_value, T new_value)
        {
            OldValue = old_value;
            NewValue = new_value;
        }

        public T NewValue { get; }
        public T OldValue { get; }
    }

    public delegate void ValueChangedEvent<T>(object sender, ValueChangedEventArgs<T> e);
}