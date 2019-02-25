using System;
using System.Collections.Generic;
using System.Linq;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.CellSet
{
    /// <summary>
    ///     An auxiliary object containing a set of methods to be used in the events which
    ///     calculate values of the calculated hierarchy members, the calculated measures and
    ///     measure display modes
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In the OnApplyFilter, OnCalcMember, OnShowMeasure events, an instance of this
    ///         object is passed as one of the parameters.
    ///     </para>
    ///     <para>
    ///         Each instance of this type is connected to some Cube cell which value is
    ///         calculated in the event. Almost all methods of this object handle the Cube address
    ///         space with respect to the current Cube cell's coordinate
    ///     </para>
    /// </remarks>
    public class Evaluator
    {
        internal ICubeAddress fAddress;
        internal Members fCalculatedMembers;
        internal OlapControl fGrid;

        internal Evaluator(OlapControl Grid, ICubeAddress Address)
        {
            fGrid = Grid;
            fAddress = Address;
        }

        /// <summary>
        ///     The list of calculated members "determined" for the current multidimensional Cube
        ///     address for which the Evaluator object has been created
        /// </summary>
        public Members CalculatedMembers
        {
            get
            {
                if (fCalculatedMembers != null) return fCalculatedMembers;
                fCalculatedMembers = new Members();
                for (var i = 0; i < fAddress.LevelsCount; i++)
                    if (fAddress.Members(i).MemberType == MemberType.mtCalculated)
                        fCalculatedMembers.Add(fAddress.Members(i));
                return fCalculatedMembers;
            }
        }

        internal static double sqr(double X)
        {
            return X * X;
        }

        public static double Covariance(double[] x,
            double[] y)
        {
            var n = Math.Min(x.Length, y.Length);
            double result = 0;
            var i = 0;
            double xmean = 0;
            double ymean = 0;
            double v = 0;
            double x0 = 0;
            double y0 = 0;
            double s = 0;
            var samex = new bool();
            var samey = new bool();

            if (n <= 1)
            {
                result = 0;
                return result;
            }

            xmean = 0;
            ymean = 0;
            samex = true;
            samey = true;
            x0 = x[0];
            y0 = y[0];
            v = 1 / (double) n;
            for (i = 0; i <= n - 1; i++)
            {
                s = x[i];
                samex = samex & (s == x0);
                xmean = xmean + s * v;
                s = y[i];
                samey = samey & (s == y0);
                ymean = ymean + s * v;
            }
            if (samex | samey)
            {
                result = 0;
                return result;
            }

            v = 1 / (double) (n - 1);
            result = 0;
            for (i = 0; i <= n - 1; i++)
                result = result + v * (x[i] - xmean) * (y[i] - ymean);
            return result;
        }

        public static double Correlation(double[] x,
            double[] y)
        {
            var n = Math.Min(x.Length, y.Length);
            double result = 0;
            var i = 0;
            double xmean = 0;
            double ymean = 0;
            double v = 0;
            double x0 = 0;
            double y0 = 0;
            double s = 0;
            var samex = new bool();
            var samey = new bool();
            double xv = 0;
            double yv = 0;
            double t1 = 0;
            double t2 = 0;

            if (n <= 1)
            {
                result = 0;
                return result;
            }

            xmean = 0;
            ymean = 0;
            samex = true;
            samey = true;
            x0 = x[0];
            y0 = y[0];
            v = 1 / (double) n;
            for (i = 0; i <= n - 1; i++)
            {
                s = x[i];
                samex = samex & (s == x0);
                xmean = xmean + s * v;
                s = y[i];
                samey = samey & (s == y0);
                ymean = ymean + s * v;
            }
            if (samex | samey)
            {
                result = 0;
                return result;
            }

            //
            // numerator and denominator
            //
            s = 0;
            xv = 0;
            yv = 0;
            for (i = 0; i <= n - 1; i++)
            {
                t1 = x[i] - xmean;
                t2 = y[i] - ymean;
                xv = xv + sqr(t1);
                yv = yv + sqr(t2);
                s = s + t1 * t2;
            }
            if ((xv == 0) | (yv == 0))
                result = 0;
            else
                result = s / (Math.Sqrt(xv) * Math.Sqrt(yv));
            return result;
        }

        public static void LinearTrend(IList<double> arrayX, IList<double> arrayY, out double A, out double B)
        {
            if (arrayX.Count == 0)
                throw new ArgumentException("The passed arrays cannot be empty");
            if (arrayX.Count != arrayY.Count)
                throw new ArgumentException("The length of passed arrays must be equal");
            var Xmean = Mean(arrayX);
            var Ymean = Mean(arrayY);
            double a = 0;
            double b = 0;
            for (var i = 0; i < arrayX.Count; i++)
            {
                var c = arrayX[i] - Xmean;
                a += c * (arrayY[i] - Ymean);
                b += c * c;
            }
            B = a / b;
            A = Ymean - B * Xmean;
        }

        public static void LinearTrend(IList<double> array, out double A, out double B)
        {
            if (array.Count == 0)
                throw new ArgumentException("The passed array cannot be empty");

            var Xmean = (array.Count - 1.0) / 2.0;
            var Ymean = Mean(array);
            double a = 0;
            double b = 0;
            for (var i = 0; i < array.Count; i++)
            {
                var c = i - Xmean;
                a += c * (array[i] - Ymean);
                b += c * c;
            }
            B = a / b;
            A = Ymean - B * Xmean;
        }

        public static double Mean(IList<double> array)
        {
            if (array.Count == 0) throw new ArgumentException("The passed array cannot be empty");
            var Result = array[0];
            for (var i = 1; i < array.Count; i++) Result += array[i];
            return Result / array.Count;
        }

        public static double Variance(IList<double> arr, bool IsBiased)
        {
            double avg = 0;
            foreach (var d in arr) avg += d;
            avg /= arr.Count;
            double result = 0;
            foreach (var d in arr) result += (d - avg) * (d - avg);
            if (IsBiased)
                result /= arr.Count;
            else if (arr.Count == 1) result = 0;
            else result /= arr.Count - 1;
            return result;
        }

        public static double StdDev(IList<double> arr, bool IsBiased)
        {
            return Math.Sqrt(Variance(arr, IsBiased));
        }

        public static double Median(IList<double> arr)
        {
            if ((arr.Count & 1) > 0) // if odd
                return arr[(arr.Count + 1) >> 1];
            return (arr[arr.Count >> 1] + arr[arr.Count >> (1 + 1)]) / 2.0;
        }

        /// <summary>
        ///     Returns the hierarchy "neighboring" of the current cell, different from it by the
        ///     value of the members passed as the parameter.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For example, we have a Cube cell specified by the coordinates "Year-2003" and
        ///         "Product-Beverages".
        ///     </para>
        ///     <para>
        ///         This method along with the "Product-Seafood" parameter will return the cell
        ///         with the address of the coordinates "Year-2003" and "Product-Seafood". The method
        ///         along with the "Customer-Ben Carlos" parameter will return the cell with the
        ///         address of the coordinates "Year-2003", "Product-Beverages" and "Customer-Ben
        ///         Carlos". Overloads: public ICubeAddress Sibling( IList Members );
        ///     </para>
        /// </remarks>
        public ICubeAddress Sibling(IList<Member> Members)
        {
            var Result = fAddress.Clone();
            for (var i = 0; i < Members.Count; i++) Result.AddMember(Members[i]);
            if (Result.Measure != null)
                Result.MeasureMode = Result.Measure.ShowModes[0];
            return Result;
        }

        /// <summary>
        ///     Searches for the hierarchy member by its name in all initialized Grid
        ///     hierarchies.
        /// </summary>
        public Member Member(string MemberName)
        {
            return fGrid.FindMemberByName(MemberName);
        }

        internal Measure FindMeasureByName(string measureName)
        {
            return fGrid.Measures.FirstOrDefault(item =>
                                                     string.Compare(measureName, item.UniqueName, true) == 0 ||
                                                     string.Compare(measureName,
                                                         "[Measures].[" + item.DisplayName + "]", true) == 0);
        }

        internal Level FindLevelByName(string levelName)
        {
            foreach (var d in fGrid.Dimensions)
            foreach (var h in d.Hierarchies)
                if (h.Levels != null)
                    foreach (var l in h.Levels)
                        if (string.Compare(levelName, l.UniqueName, true) == 0 ||
                            string.Compare(levelName,
                                "[" + d.DisplayName + "].[" + h.DisplayName + "].[" + l.DisplayName + "]", true) == 0)
                            return l;
            return null;
        }

        internal Hierarchy FindHierarchyByName(string hName)
        {
            foreach (var d in fGrid.Dimensions)
            foreach (var h in d.Hierarchies)
                if (string.Compare(hName, h.UniqueName, true) == 0 ||
                    string.Compare(hName, "[" + d.DisplayName + "].[" + h.DisplayName + "]", true) == 0)
                    return h;
            return null;
        }

        /// <summary>
        ///     Returns True, if a hierarchy passed as the parameter is determined for the
        ///     current cube cell.
        /// </summary>
        public bool IsHierarchyDetermined(Hierarchy Hierarchy)
        {
            for (var i = 0; i < fAddress.LevelsCount; i++)
                if (Hierarchy == fAddress.Levels(i).Hierarchy) return true;
            return false;
        }

        /// <summary>
        ///     Forms a value of the Value variable using either a formatting string passed as the parameter,
        ///     or a formatting string accepted by default for the current measure.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public string Format(object Value)
        {
            return fAddress.Measure.FormatValue(Value, fAddress.Measure.DefaultFormat);
        }

        public string Format(object Value, string FormatString)
        {
            return fAddress.Measure.FormatValue(Value, FormatString);
        }

        /// <summary>
        ///     Returns a cube cell value passed by the Address parameter.
        /// </summary>
        /// <param name="Address"></param>
        /// <returns></returns>
        public object GetValue(ICubeAddress Address)
        {
            object Result;
            fGrid.Engine.GetCellValue(Address, out Result);
            return Result;
        }

        /// <summary>
        ///     Returns a "neighbor" value of the current cell, different from the current by a
        ///     value of the members passed as the parameter.
        /// </summary>
        public object SiblingValue(params Member[] Members)
        {
            object Result;
            fGrid.Engine.GetCellValue(Sibling(Members), out Result);
            return Result;
        }

        /// <summary>
        ///     Returns a "neighbor" value of the current cell, different from the current by a
        ///     value of the members passed as the parameter.
        /// </summary>
        public object SiblingValue(string MemberName)
        {
            var M = Member(MemberName);
            if (M == null) throw new Exception(string.Format("The member \"{0}\" not found", MemberName));
            return SiblingValue(M);
        }

        /// <summary>
        ///     Creates an instance of the ICubeAddress interface.
        /// </summary>
        public ICubeAddress CreateCubeAddress()
        {
            return fGrid.Engine.CreateCubeAddress();
        }
    }
}