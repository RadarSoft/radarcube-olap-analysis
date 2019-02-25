using System.Collections.Generic;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Events
{
    /// <summary>An object passed as the parameter in the OlapGrid.OnShowMeasure event.</summary>
    /// <remarks>
    ///     The value calculated for the CurrentCell, whose measure is located in the
    ///     ShowMode display mode, should be assigned to the FormattedValue property in the event
    ///     handler of the specified event
    /// </remarks>
    public class ShowMeasureArgs : CalcMemberArgs
    {
        internal List<Member> fColumnSiblings = new List<Member>();

        internal List<Member> fRowSiblings = new List<Member>();

        internal MeasureShowMode fShowMode;

        public ShowMeasureArgs(object originalData, MeasureShowMode mode, IDataCell cell)
        {
            OriginalData = originalData;
            ReturnData = null;
            fValue = "";
            fShowMode = mode;

            if (cell != null)
            {
                var member = cell.RowMember;
                if (member != null) member = member.HierarchyMemberCell;
                if (member != null)
                    for (var i = 0; i < member.SiblingsCount; i++)
                    {
                        var m = member.Siblings(i).Member;
                        if (m != null) fRowSiblings.Add(m);
                    }

                member = cell.ColumnMember;
                if (member != null) member = member.HierarchyMemberCell;
                if (member != null)
                    for (var i = 0; i < member.SiblingsCount; i++)
                    {
                        var m = member.Siblings(i).Member;
                        if (m != null) fColumnSiblings.Add(m);
                    }
            }
        }

        /// <summary>The display mode the cell value is calculated for.</summary>
        public MeasureShowMode ShowMode => fShowMode;

        /// <summary>
        ///     Contains the original value (exposed by the "Value" measure mode) of the cell
        /// </summary>
        public object OriginalData { get; }

        /// <summary>The list of row neighbors of the current cell (including it)</summary>
        public List<Member> RowSiblings => fRowSiblings;

        /// <summary>The list of column neighbors of the current cell (including it)</summary>
        public List<Member> ColumnSiblings => fColumnSiblings;
    }
}