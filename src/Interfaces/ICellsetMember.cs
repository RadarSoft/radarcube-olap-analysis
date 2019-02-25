using System.Collections.Generic;
using RadarSoft.RadarCube.CellSet;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Interfaces
{
    internal interface ICellsetMember
    {
        Member FMember { get; set; }
        string FLineID { get; set; }
        int FHierID { get; set; }
        long FLineIdx { get; set; }
        int FModeID { get; set; }
        CellsetMember FParent { get; }
        CellSetMembers FChildren { get; }
        CellSetMembers FAttributes { get; }
        bool FIsTotal { get; set; }
        bool FIsPager { get; set; }
        CellsetLevel FLevel { get; }
        byte FIndent { get; }
        int FColSpan { get; set; }
        int FRowSpan { get; set; }
        int FStartRow { get; set; }
        int FStartColumn { get; set; }
        bool FIsRow { get; }
        int FSiblingsOrder { get; set; }
        InfoAttribute Attribute { get; set; }
        int CurrentPage { get; }
        bool IsInFrame { get; }
        string DisplayName { get; }
        Member[] MembersOfParent { get; }
        IEnumerable<CellsetMember> AllChildren();
        void PageTo(int page);
        int GetRowSpan();
        int GetColSpan();
        ICubeAddress GetAddress();
        CellSetMembers GetList();
        bool IsThisDrillAction(DrillAction a);
    }
}