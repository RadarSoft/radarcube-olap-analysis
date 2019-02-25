using System;
using System.Drawing;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Interfaces;

namespace RadarSoft.RadarCube.Events
{
    /// <summary>
    ///     The class is passed as an argument for calculation of calculated measures' and
    ///     calculated hierarchy members' values.
    /// </summary>
    public class CalcMemberArgs : EventArgs
    {
        internal Color fBackColor = Color.Empty;
        internal Evaluator fEvaluator;
        internal string fFontName;
        internal double? fFontSize;
        internal FontStyle? fFontStyle;

        internal Color fForeColor = Color.Empty;
        internal string fValue;

        /// <summary>The address of the current Cube cell whose value is calculated.</summary>
        public ICubeAddress CurrentAddress => fEvaluator.fAddress.Clone();

        /// <summary>
        ///     This is an auxiliary object, it contains methods that may be applicable for
        ///     calculating the values of the specified cell.
        /// </summary>
        public Evaluator Evaluator => fEvaluator;

        /// <summary>
        ///     In the event handler, should be assigned with the value of the calculated
        ///     Cell.
        /// </summary>
        public object ReturnData { get; set; }

        /// <summary>
        ///     In the event handler, should be assigned with the formatted value of the
        ///     calculated Cell.
        /// </summary>
        public string ReturnValue
        {
            get
            {
                if (fValue == null)
                    return ReturnData == null ? null : ReturnData.ToString();
                return fValue;
            }
            set => fValue = value;
        }

        /// <summary>
        ///     A backgound color of the returning cell
        /// </summary>
        public Color ReturnBackColor
        {
            get => fBackColor;
            set => fBackColor = value;
        }

        /// <summary>
        ///     A font color of the returning cell
        /// </summary>
        public Color ReturnForeColor
        {
            get => fForeColor;
            set => fForeColor = value;
        }

        /// <summary>If defined, sets the font style for the cell</summary>
        public FontStyle? FontStyle
        {
            get => fFontStyle;
            set => fFontStyle = null;
        }

        /// <summary>If defined, sets the font name for the cell</summary>
        public string FontName
        {
            get => fFontName;
            set => fFontName = null;
        }

        /// <summary>If defined, sets the font size for the cell</summary>
        public double? FontSize
        {
            get => fFontSize;
            set => fFontSize = null;
        }
    }
}