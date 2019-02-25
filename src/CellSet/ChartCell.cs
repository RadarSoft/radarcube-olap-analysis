using System;
using System.Collections.Generic;
using RadarSoft.RadarCube.ClientAgents;
using RadarSoft.RadarCube.Controls.Chart;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.CellSet
{
    internal class ChartCell : IChartCell
    {
        private readonly ICubeAddress A;
        internal ChartCellSet CS;
        private readonly ChartArea[] data;

        private readonly int FCol;
        private readonly int FRow;

        internal ChartCell(ChartCellSet ACS, int ARow, int ACol,
            Dictionary<long, Dictionary<ChartAxis, ChartArea>> cell_details)
        {
            CS = ACS;
            FRow = ARow;
            FCol = ACol;

            var i = ACS.FFixedColumns - 1 + ARow * ACS.FFixedColumns;
            var RM = i >= 0 ? ACS.FRowMembersArray[i] as CellsetMember : null;
            if (RM != null && RM.FIsPager) return;
            i = ACol + (ACS.FFixedRows - 1) * ACS.ColumnCount;
            var CM = i >= 0 ? ACS.FColMembersArray[i] as CellsetMember : null;
            if (CM != null && CM.FIsPager) return;
            var AM = new List<Member>();

            Hierarchy H = null;
            var C = RM;
            var MID = -1;
            while (C != null)
            {
                if (C.FMember != null)
                    if (C.FMember.MemberType == MemberType.mtMeasure)
                    {
                        MID = C.FMember.ID;
                    }
                    else
                    {
                        if (C.FMember.MemberType == MemberType.mtMeasureMode)
                        {
                            AM.Add(C.FMember);
                        }
                        else if (C.FMember.FLevel.FHierarchy != H)
                        {
                            AM.Add(C.FMember);
                            H = C.FMember.FLevel.FHierarchy;
                        }
                        ;
                    }
                C = C.FParent;
            }
            C = CM;
            while (C != null)
            {
                if (C.FMember != null)
                    if (C.FMember.MemberType == MemberType.mtMeasure)
                    {
                        MID = C.FMember.ID;
                    }
                    else
                    {
                        if (C.FMember.MemberType == MemberType.mtMeasureMode)
                        {
                            AM.Add(C.FMember);
                        }
                        else if (C.FMember.FLevel.FHierarchy != H)
                        {
                            AM.Add(C.FMember);
                            H = C.FMember.FLevel.FHierarchy;
                        }
                        ;
                    }
                C = C.FParent;
            }

            A = ACS.FGrid.FEngine.CreateCubeAddress(AM);

            Dictionary<ChartAxis, ChartArea> _d;
            cell_details.TryGetValue(A.FLineIdx, out _d);
            data = new ChartArea[CS.YAxesDescriptor.ChartAreas.Count];
            if (_d != null)
                foreach (var caa in _d.Values)
                    for (var ii = 0; ii < CS.YAxesDescriptor.ChartAreas.Count; ii++)
                        if (caa.AxisY == CS.YAxesDescriptor.ChartAreas[ii].AxisData)
                        {
                            data[ii] = caa;
                            caa.ConvertSeries(this);
                            break;
                        }
            for (var ii = 0; ii < CS.YAxesDescriptor.ChartAreas.Count; ii++)
                if (data[ii] == null)
                    data[ii] = new ChartArea(CS.YAxesDescriptor.ChartAreas[ii].AxisData);

            foreach (var ca2 in ((IChartCell) this).Charts)
            {
                var ay = ca2.AxisY;
                if (ay != null && ay.Descriptor != null && ay.Descriptor.DescriptorObject is Level)
                {
                    var l = (Level) ay.Descriptor.DescriptorObject;
                    foreach (var ca in data)
                    foreach (var cs in ca.SeriesList)
                        if (cs.Data.Count > 0)
                            cs.Data.Sort(new ChartCellDetails.YLabelComparer());
                }
            }

            var ax = ((IChartCell) this).AxisX;
            if (ax != null && ax.Descriptor != null && ax.Descriptor.DescriptorObject is Level)
            {
                var l = (Level) ax.Descriptor.DescriptorObject;
                foreach (var ca in data)
                foreach (var cs in ca.SeriesList)
                    if (cs.Data.Count > 0)
                        cs.Data.Sort(new ChartCellDetails.XLabelComparer());
            }
        }

        #region IChartCell Members

        int ICell.StartRow => FRow;

        int ICell.StartColumn => FCol;

        int ICell.RowSpan => 1;

        int ICell.ColSpan => 1;

        CellType ICell.CellType => CellType.ctChart;

        CellSet ICell.CellSet => CS;

        string ICell.Description => string.Empty;

        ICubeAddress IChartCell.Address => A;

        public IMemberCell RowMember =>
            CS.FFixedColumns > 0 ? CS.Cells(CS.FFixedColumns - 1, FRow) as IMemberCell : null;

        public IMemberCell ColumnMember => CS.FFixedRows > 0 ? CS.Cells(FCol, CS.FFixedRows - 1) as IMemberCell : null;

        ChartAxis IChartCell.AxisX => CS.XAxisDescriptor == null ? null : CS.XAxisDescriptor.Axis;

        ChartArea[] IChartCell.Charts => data;

        string ICell.Value => null;

        List<CubeAction> ICell.CubeActions => throw new NotSupportedException(
            "You cannot use the IChartCell.CubeActions property. Use the IChartCell.Actions method instead.");

        IList<Member> IChartCell.XMembers => CS.ColumnChartMembers(FCol);

        IList<Member> IChartCell.YMembers => CS.RowChartMembers(FRow);

        IList<CubeAction> IChartCell.Actions(ChartCellDetails details)
        {
            return CS.FGrid.Cube.RetrieveChartCellActions(details.Address, details.XMeasure, details.YMeasure);
        }

        int ICell.PagedStartRow => ((ICell) this).StartRow;

        int ICell.PagedStartColumn => ((ICell) this).StartColumn;

        #endregion


        #region ICell Members

        public ImagePosition ImagePosition => throw new NotImplementedException();

        public string ImageUri => throw new NotImplementedException();

        #endregion
    }
}