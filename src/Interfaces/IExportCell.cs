using System.Collections.Generic;
using System.Drawing;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.Interfaces
{
    internal interface IExportCell : IImagable
    {
        object Data { get; }

        //System.Drawing.Image Image { get; }
        Color Background { get; }

        CellSet.CellSet CellSet { get; }
        CellType CellType { get; }
        int ColSpan { get; }
        List<CubeAction> CubeActions { get; }
        string Description { get; }
        int PagedStartColumn { get; }
        int PagedStartRow { get; }
        int RowSpan { get; }
        int StartColumn { get; }
        int StartRow { get; }
        string Value { get; }
    }
}