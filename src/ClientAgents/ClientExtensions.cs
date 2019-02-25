using System.Linq;
using RadarSoft.RadarCube.Controls.Chart;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.ClientAgents
{
//#if !SILVERLIGHT
//        public ClientChartAxis(ChartAxis a)
//        {
//        }
//#endif

    public static class ClientExtensions
    {
        internal static ClientChartAxis CreateChartAxis(this ChartAxis a)
        {
            var ca = new ClientChartAxis();
            ca.Format = a.Format;
            if (ca.Format == ChartAxisFormat.Continuous)
            {
                ca.Min = a.Min;
                ca.Max = a.Max;
            }
            return ca;
        }


        internal static ClientChartArea CreateChartArea(this ChartArea area)
        {
            var a = new ClientChartArea();
            area.SeriesList.Sort(CompareSeriesByFirstDetailsXValue);
            a.Series = area.SeriesList.Select(e => e.CreateClientSeries()).ToArray();
            return a;
        }


        internal static ClientChartSeries CreateClientSeries(this OlapChartSeries cs)
        {
            var ccs = new ClientChartSeries();
            ccs.Details = cs.Data.Select(e => e.CreateClientDetails()).ToArray();
            if (cs.fColorMember != null) ccs.ColorMember = cs.fColorMember.UniqueName;
            if (cs.fSizeMember != null) ccs.SizeMember = cs.fSizeMember.UniqueName;
            if (cs.fShapeMember != null) ccs.ShapeMember = cs.fShapeMember.UniqueName;
            if (cs.fMeasure != null) ccs.Measure = cs.fMeasure.UniqueName;
            return ccs;
        }

        private static int CompareSeriesByFirstDetailsXValue(OlapChartSeries x, OlapChartSeries y)
        {
            var comparer = new ChartCellDetails.XLabelComparer();
            return comparer.Compare(x.Data[0], y.Data[0]);
        }


        internal static ClientChartCellDetails CreateClientDetails(this ChartCellDetails cd)
        {
            var ccd = new ClientChartCellDetails();
            var g = cd.Series.Area.Cell.CellSet.Grid;
            ccd.DetailMembers = g.AxesLayout.DetailsAxis.Select(
                e => cd.Address.GetMemberByHierarchy(e)).Where(
                e => e != null).Select(
                e => e.UniqueName).ToArray();

            if (cd._XValue != null)
                if (cd._XValue is Member)
                {
                    ccd.XValue = ((Member) cd._XValue).UniqueName;
                }
                else
                {
                    ccd.XValue = cd._XValue;
                    ccd.XValueFormatted = cd._XValueFormatted;
                }

            if (cd._YValue != null)
                if (cd._YValue is Member)
                {
                    ccd.YValue = ((Member) cd._YValue).UniqueName;
                }
                else
                {
                    ccd.YValue = cd._YValue;
                    ccd.YValueFormatted = cd._YValueFormatted;
                }

            if (cd.ColorMember != null && cd.Series.fColorMember != cd.ColorMember)
                ccd.ColorMember = cd.ColorMember.UniqueName;

            if (cd.SizeMember != null && cd.Series.fSizeMember != cd.SizeMember)
                ccd.SizeMember = cd.SizeMember.UniqueName;

            if (cd.ShapeMember != null && cd.Series.fShapeMember != cd.ShapeMember)
                ccd.ShapeMember = cd.ShapeMember.UniqueName;

            if (!double.IsNaN(cd.ColorValue))
            {
                ccd.ColorValue = cd.ColorValue;
                ccd.ColorValueFormatted = cd.ColorValueFormatted;
            }

            if (!double.IsNaN(cd.SizeValue))
            {
                ccd.SizeValue = cd.SizeValue;
                ccd.SizeValueFormatted = cd.SizeValueFormatted;
            }

            if (!double.IsNaN(cd.ShapeValue))
            {
                ccd.ShapeValue = cd.ShapeValue;
                ccd.ShapeValueFormatted = cd.ShapeValueFormatted;
            }
            return ccd;
        }
    }
}