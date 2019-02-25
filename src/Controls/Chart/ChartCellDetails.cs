using System;
using System.Collections.Generic;
using System.Text;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Controls.Chart
{
    /// <summary>
    ///     Represents the Chart point.
    /// </summary>
    public class ChartCellDetails : ICloneable
    {
        internal double _ColorValue = double.NaN;
        internal string _ColorValueFormatted = string.Empty;
        private string _descr;
        internal double _ShapeValue = double.NaN;
        internal string _ShapeValueFormatted = string.Empty;
        internal double _SizeValue = double.NaN;
        internal string _SizeValueFormatted = string.Empty;
        internal object _XValue;
        internal string _XValueFormatted;
        internal object _YValue;
        internal string _YValueFormatted;

        private readonly OlapControl fGrid;
        internal OlapChartSeries fSeries;

        internal ChartCellDetails(OlapControl grid, List<Level> clue, int[] memberIDs)
        {
            fGrid = grid;
            Address = new ICubeAddress(fGrid);
            for (var i = 0; i < clue.Count; i++)
                Address.AddMember(clue[i].GetMemberByID(memberIDs[i]));
        }

        internal ChartCellDetails(ChartCellDetails source)
        {
            fGrid = source.fGrid;
            Address = source.Address.Clone();
            _XValue = source.XValue;
            _YValue = source.YValue;
            _XValueFormatted = source.XValueFormatted;
            _YValueFormatted = source.YValueFormatted;
            _SizeValue = source.SizeValue;
            _SizeValueFormatted = source.SizeValueFormatted;
            _ShapeValue = source.ShapeValue;
            _ShapeValueFormatted = source.ShapeValueFormatted;
            _ColorValue = source.ColorValue;
            _ColorValueFormatted = source.ColorValueFormatted;
            fSeries = source.fSeries;
            _descr = source._descr;
        }

        public object XValue => _XValue;

        /// <summary>
        ///     <para>
        ///         Point value on the Y axis. It can be double, if the axis is numeric, or
        ///         Member, if it is discrete.
        ///     </para>
        /// </summary>
        public object YValue => _YValue;

        /// <summary>
        ///     Formatted point value on the X axis.
        /// </summary>
        public string XValueFormatted => _XValueFormatted;

        /// <summary>
        ///     Formatted point value on the Y value.
        /// </summary>
        public string YValueFormatted => _YValueFormatted;

        /// <summary>
        ///     Point's size measure value, if there is a size modifier. Otherwise - NaN.
        /// </summary>
        public double SizeValue => _SizeValue;

        /// <summary>
        ///     Formatted point's size measure value.
        /// </summary>
        public string SizeValueFormatted => _SizeValueFormatted;

        /// <summary>
        ///     Point's color measure value, if there is a color modifier. Otherwise - NaN.
        /// </summary>
        public double ColorValue => _ColorValue;

        /// <summary>
        ///     Formatted point's color measure value.
        /// </summary>
        public string ColorValueFormatted => _ColorValueFormatted;

        /// <summary>
        ///     Point's shape measure value, if there is a shape modifier. Otherwise - NaN.
        /// </summary>
        public double ShapeValue => _ShapeValue;

        /// <summary>
        ///     Formatted point's shape measure value.
        /// </summary>
        public string ShapeValueFormatted => _ShapeValueFormatted;

        /// <summary>
        ///     A hierarchy member that is the point's color modifier, if the Grid's color modifier is a hierarchy. Otherwise -
        ///     null.
        /// </summary>
        public Member ColorMember
        {
            get
            {
                if (fSeries.fColorMember != null)
                {
                    if (XValue is Member)
                    {
                        var m = (Member) XValue;
                        if (m.Level == fSeries.fColorMember.Level)
                            return m;
                    }

                    if (YValue is Member)
                    {
                        var m = (Member) YValue;
                        if (m.Level == fSeries.fColorMember.Level)
                            return m;
                    }
                }
                return fSeries.fColorMember;
            }
        }

        /// <summary>
        ///     A hierarchy member that is the point's size modifier, if the Grid's size modifier is a hierarchy. Otherwise - null.
        /// </summary>
        public Member SizeMember
        {
            get
            {
                if (fSeries.fSizeMember != null)
                {
                    if (XValue is Member)
                    {
                        var m = (Member) XValue;
                        if (m.Level == fSeries.fSizeMember.Level)
                            return m;
                    }

                    if (YValue is Member)
                    {
                        var m = (Member) YValue;
                        if (m.Level == fSeries.fSizeMember.Level)
                            return m;
                    }
                }
                return fSeries.fSizeMember;
            }
        }

        /// <summary>
        ///     A hierarchy member that is the point's shape modifier, if the Grid's shape modifier is a hierarchy. Otherwise -
        ///     null.
        /// </summary>
        public Member ShapeMember
        {
            get
            {
                if (fSeries.fShapeMember != null)
                {
                    if (XValue is Member)
                    {
                        var m = (Member) XValue;
                        if (m.Level == fSeries.fShapeMember.Level)
                            return m;
                    }

                    if (YValue is Member)
                    {
                        var m = (Member) YValue;
                        if (m.Level == fSeries.fShapeMember.Level)
                            return m;
                    }
                }
                return fSeries.fShapeMember;
            }
        }

        /// <summary>
        ///     Identifies the position of the specified point within a multidimensional
        ///     cube.
        /// </summary>
        /// <remarks>
        ///     The Cube cell's address, returned by this property, does not contain any reference to measures.
        ///     You can learn the measures, whose values are represented by the specified point, by
        ///     examining the XMeasure and YMeasure properties.
        /// </remarks>
        public ICubeAddress Address { get; }

        /// <summary>
        ///     References to the series that contain the specified point.
        /// </summary>
        public OlapChartSeries Series => fSeries;

        /// <summary>
        ///     Rurnes the text description of the point that will show up as a tooltip when the mouse is over the point.
        /// </summary>
        public string Description => _descr ?? MakeDescription();

        /// <summary>
        ///     References to the measure (if any) on the X axis.
        /// </summary>
        public Measure XMeasure => Series.Area.Cell.CellSet.FGrid.FLayout.fXAxisMeasure;

        /// <summary>
        ///     References to the measure (if any) on the Y axis.
        /// </summary>
        public Measure YMeasure => Series.YMeasure;

        #region ICloneable Members

        object ICloneable.Clone()
        {
            return new ChartCellDetails(this);
        }

        #endregion

        internal ChartCellDetails ChangeXValue(Member XMember)
        {
            _XValue = XMember;
            _XValueFormatted = XMember.DisplayName;
            Address.AddMember(XMember);
            _descr = null;
            _YValue = 0;
            _YValueFormatted = YMeasure != null ? YMeasure.FormatValue(0, YMeasure.DefaultFormat) : "0";
            return this;
        }

        private string MakeDescription()
        {
            var ml = new List<Measure>();
            var sb = new StringBuilder();
            if (YMeasure != null)
            {
                sb.AppendLine(YMeasure.DisplayName + ": " + YValueFormatted);
                ml.Add(YMeasure);
            }
            if (XMeasure != null)
            {
                sb.AppendLine(XMeasure.DisplayName + ": " + XValueFormatted);
                ml.Add(XMeasure);
            }
            if (fGrid.FLayout.fColorAxisItem is Measure)
            {
                var m = (Measure) fGrid.FLayout.fColorAxisItem;
                if (!ml.Contains(m))
                {
                    sb.AppendLine(fGrid.FLayout.fColorAxisItem.DisplayName + ": " + ColorValueFormatted);
                    ml.Add(m);
                }
            }
            if (fGrid.FLayout.fSizeAxisItem is Measure)
            {
                var m = (Measure) fGrid.FLayout.fSizeAxisItem;
                if (!ml.Contains(m))
                {
                    sb.AppendLine(fGrid.FLayout.fSizeAxisItem.DisplayName + ": " + SizeValueFormatted);
                    ml.Add(m);
                }
            }
            if (fGrid.FLayout.fShapeAxisItem is Measure)
            {
                var m = (Measure) fGrid.FLayout.fShapeAxisItem;
                if (!ml.Contains(m))
                    sb.AppendLine(fGrid.FLayout.fShapeAxisItem.DisplayName + ": " + ShapeValueFormatted);
            }
            foreach (var m in Address.FLevelsAndMembers.Values)
            {
                var sm = new Stack<Member>();
                var m1 = m;
                while (m1 != null)
                {
                    sm.Push(m1);
                    m1 = m1.Parent;
                }
                while (sm.Count > 0)
                {
                    m1 = sm.Pop();
                    sb.AppendLine(m1.Level.DisplayName + ": " + m1.DisplayName);
                }
            }
            _descr = sb.ToString();
            return _descr;
        }

        internal void MakeUnderlyingData(Dictionary<string, string> measures,
            Dictionary<string, string> levels, out Dictionary<string, object> data)
        {
            data = new Dictionary<string, object>();

            if (YMeasure != null)
            {
                if (!measures.ContainsKey(YMeasure.UniqueName))
                    measures.Add(YMeasure.UniqueName, YMeasure.DisplayName);
                data.Add(YMeasure.UniqueName, YValue);
            }
            if (XMeasure != null)
            {
                if (!measures.ContainsKey(XMeasure.UniqueName))
                    measures.Add(XMeasure.UniqueName, XMeasure.DisplayName);
                data.Add(XMeasure.UniqueName, XValue);
            }
            if (fGrid.FLayout.fColorAxisItem is Measure)
            {
                var m = (Measure) fGrid.FLayout.fColorAxisItem;
                if (!measures.ContainsKey(m.UniqueName))
                    measures.Add(m.UniqueName, m.DisplayName);
                if (!data.ContainsKey(m.UniqueName))
                    data.Add(m.UniqueName, ColorValue);
            }
            if (fGrid.FLayout.fSizeAxisItem is Measure)
            {
                var m = (Measure) fGrid.FLayout.fSizeAxisItem;
                if (!measures.ContainsKey(m.UniqueName))
                    measures.Add(m.UniqueName, m.DisplayName);
                if (!data.ContainsKey(m.UniqueName))
                    data.Add(m.UniqueName, SizeValue);
            }
            if (fGrid.FLayout.fShapeAxisItem is Measure)
            {
                var m = (Measure) fGrid.FLayout.fShapeAxisItem;
                if (!measures.ContainsKey(m.UniqueName))
                    measures.Add(m.UniqueName, m.DisplayName);
                if (!data.ContainsKey(m.UniqueName))
                    data.Add(m.UniqueName, ShapeValue);
            }
            foreach (var m in Address.FLevelsAndMembers.Values)
            {
                var sm = new Stack<Member>();
                var m1 = m;
                while (m1 != null)
                {
                    sm.Push(m1);
                    m1 = m1.Parent;
                }
                while (sm.Count > 0)
                {
                    m1 = sm.Pop();
                    if (!levels.ContainsKey(m1.Level.UniqueName))
                        levels.Add(m1.Level.UniqueName, m1.Level.DisplayName);
                    data.Add(m1.Level.UniqueName, m1.DisplayName);
                }
            }
        }


        internal class XLabelComparer : IComparer<ChartCellDetails>
        {
            public int Compare(ChartCellDetails x, ChartCellDetails y)
            {
                if (x.XValue is Member && y.XValue is Member)
                    return ((Member) y.XValue).FSortPosition - ((Member) x.XValue).FSortPosition;
                if (x.XValue == null && y.XValue == null)
                    return 0;
                if (y.XValue == null)
                    return -1;
                if (x.XValue == null)
                    return 1;

                return ((double) x.XValue).CompareTo((double) y.XValue);
                return 0;
            }
        }

        internal class YLabelComparer : IComparer<ChartCellDetails>
        {
            public int Compare(ChartCellDetails x, ChartCellDetails y)
            {
                if (x.YValue is Member && x.YValue is Member)
                    return ((Member) y.YValue).FSortPosition - ((Member) x.YValue).FSortPosition;
                return 0;
            }
        }
    }
}