using System.Collections.Generic;
using System.Linq;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.CellSet
{
    internal class CellsetMember : ICellsetMember
    {
#if DEBUG
        public override string ToString()
        {
            var leveldn = "{x:null}";
            if (FLevel != null && FLevel.FLevel != null)
                leveldn = FLevel.FLevel.DisplayName;

            return string.Format(
                "CellsetMember DisplayName=\"{0}\" Level.DisplayName=\"{1}\"",
                DisplayName, leveldn);
        }
#endif
        public PossibleDrillActions FExpandStatus = PossibleDrillActions.esCollapsed;
        public Member FMember { get; set; }
        public string FLineID { get; set; }
        public int FHierID { get; set; }
        public long FLineIdx { get; set; }
        public string FMeasureID;
        public int FModeID { get; set; }
        public CellsetMember FParent { get; }
        public CellSetMembers FChildren { get; }
        public CellSetMembers FAttributes { get; }
        public bool FIsTotal { get; set; }
        public bool FIsPager { get; set; }
        public CellsetLevel FLevel { get; }
        public byte FIndent { get; set; }
#if DEBUG
        private int _fFColSpan;
        public int FColSpan
        {
            get => _fFColSpan;
            set
            {
                if (_fFColSpan == value)
                    return;

                //if (value == 2 && DisplayName != "Total" && DisplayName != "1997")
                //{
                //}

                _fFColSpan = value;
            }
        }
#else
        public int FColSpan { get; set; }
#endif

#if DEBUG
        private int _fFRowSpan;
        public int FRowSpan
        {
            get => _fFRowSpan;
            set
            {
                if (_fFRowSpan == value)
                    return;

                //if (value == 2 && DisplayName != "Total" && DisplayName != "1997")
                //{
                //}

                _fFRowSpan = value;
            }
        }
#else
        public int FRowSpan { get; set; }
#endif

        public int FStartRow { get; set; }
        public int FStartColumn { get; set; }
        public bool FIsRow { get; }
        public bool FAtypicalBehavior = false;
        public int FSiblingsOrder { get; set; }
        public bool Rendered;
        public InfoAttribute Attribute { get; set; }

        public IEnumerable<CellsetMember> AllChildren()
        {
            return FChildren.SelectMany(item => item.AllChildren()).Union(new CellsetMember[1] {this});
        }

        public int CurrentPage
        {
            get
            {
                var s = FLevel.FLevel.UniqueName + "|";
                if (FParent != null)
                    foreach (var m in FParent.FChildren)
                        //TODO (Stepanov): The Grid pagination didn't work at the low levels of detail.
                        //if (m.FIsPager || m.FIsTotal)
                        if (!m.FIsPager && !m.FIsTotal)
                        {
                            s = m.FLevel.FLevel.UniqueName + "|";
                            break;
                        }
                if (FParent != null && FParent.FMember != null)
                    s += FParent.FMember.UniqueName;
                var page = 1;
                return !FLevel.FLevel.GetGrid().FCellSet.ScrolledNodes.TryGetValue(s, out page) ? 1 : page;
            }
        }

        public void PageTo(int page)
        {
            if (!FIsPager)
                return;

            if (CurrentPage == page)
                return;

            var s = FLevel.FLevel.UniqueName + "|";
            if (FParent != null)
                foreach (var m in FParent.FChildren)
                    if (!m.FIsPager && !m.FIsTotal)
                    {
                        s = m.FLevel.FLevel.UniqueName + "|";
                        break;
                    }
            if (FMember != null)
                s += FMember.UniqueName;
            var cs = FLevel.FLevel.GetGrid().FCellSet;
            cs.ScrolledNodes.Remove(s);
            if (page > 1)
                cs.ScrolledNodes.Add(s, page);
            cs.Rebuild();
        }

        public int GetRowSpan()
        {
            if (!FIsRow)
                return FRowSpan;
            if (!IsInFrame)
                return 0;
            if (FRowSpan <= 1 || Attribute != null)
                return FRowSpan;
            return FChildren.Sum(m => m.GetRowSpan());
        }

        public int GetColSpan()
        {
            if (FIsRow)
                return FColSpan;
            if (!IsInFrame)
                return 0;
            if (FColSpan <= 1 || Attribute != null)
                return FColSpan;
            return FChildren.Sum(m => m.GetColSpan());
        }

        public bool IsInFrame
        {
            get
            {
                if (FLevel == null) return true;

                if (Attribute != null)
                {
                    var cm = FParent;
                    while (cm.Attribute != null) cm = cm.FParent;
                    return cm.IsInFrame;
                }

                if (FIsPager || FIsTotal) return true;
                if (FLevel.FLevel.PagerSettings.AllowPaging)
                {
                    var ps = FLevel.FLevel.PagerSettings;

                    return FSiblingsOrder / ps.LinesInPage == CurrentPage - 1;
                }
                return !FIsPager;
            }
        }

        public object Data
        {
            get
            {
                if (FIsPager || FLevel == null)
                    return null;
                if (FMember == null)
                    return FLevel.FLevel == null ? null : FLevel.FLevel.TotalCaption;
                if (Attribute == null || FMember.FCubeMember == null)
                    return FMember.Data;
                if (!FMember.FLevel.FCubeLevel.InfoAttributes.Contains(Attribute))
                    return FMember.Data;
                return FMember.FCubeMember.GetAttributeAsObject(Attribute.DisplayName);
            }
        }

        public string DisplayName
        {
            get
            {
                if (FIsPager || FLevel == null) return "";
                if (FMember == null)
                    return FLevel.FLevel == null ? "" : FLevel.FLevel.TotalCaption;
                if (Attribute == null || FMember.FCubeMember == null)
                    return FMember.DisplayName;
                if (!FMember.FLevel.FCubeLevel.InfoAttributes.Contains(Attribute))
                    return FMember.DisplayName;
                return FMember.FCubeMember.GetAttributeValue(Attribute.DisplayName);
            }
        }

        public ICubeAddress GetAddress()
        {
            var MID = FMeasureID;
            if (MID == string.Empty && FLevel.FLevel.Grid.CellSet.FDefaultMeasure != null)
                MID = FLevel.FLevel.Grid.CellSet.FDefaultMeasure.UniqueName;
            var a = new ICubeAddress(FLevel.FLevel.Grid,
                FLineID, FHierID, FLineIdx, MID, FModeID, 1);
            if (FMember != null)
                switch (FMember.MemberType)
                {
                    case MemberType.mtMeasure:
                        a.Tag = 2;
                        break;
                    case MemberType.mtMeasureMode:
                        a.Tag = 3;
                        break;
                }
            return a;
        }

        public CellSetMembers GetList()
        {
            var parent = FParent;
            if (FIsTotal && parent != null) parent = parent.FParent;
            if (parent != null)
                return parent.FChildren;
            return FStartRow >= FLevel.FLevel.Grid.FCellSet.FFixedRows
                ? FLevel.FLevel.Grid.FCellSet.FRowMembers
                : FLevel.FLevel.Grid.FCellSet.FColumnMembers;
        }

        internal CellsetMember(Member AMember, CellsetMember AParent, CellsetLevel ALevel, bool IsRowArea)
            // : base(AMember)
        {
            FIndent = 0;
            FMeasureID = string.Empty;
            Rendered = false;

            FChildren = new CellSetMembers();
            FAttributes = new CellSetMembers();

            FMember = AMember;
            FParent = AParent;
            FLevel = ALevel;
            FIsRow = IsRowArea;
            if (FLevel != null)
                FLevel.FCellsCount++;

#if DEBUG
            if (AMember != null && AParent != null)
                if (AMember.DisplayName == "Household" && AParent.DisplayName == "Household")
                {
                }
            if (AMember != null && AMember.DisplayName == "1996")
            {
            }
            if (AMember != null && AMember.DisplayName == "Warehouse Sales")
            {
            }
#endif

            if (AMember != null)
                if (AMember.FLevel != ALevel.FLevel)
                {
                    var g = AMember.FLevel.GetGrid();
                    var ll = IsRowArea ? g.CellSet.FRowLevels : g.CellSet.FColumnLevels;

                    foreach (var l in ll)
                        if (l.FLevel == AMember.FLevel)
                        {
                            FLevel = l;
                            break;
                        }
                }
        }

        private IEnumerable<CellsetMember> GetParents()
        {
            var res = FParent;
            while (res != null)
            {
                yield return res;
                res = res.FParent;
            }
        }

        private Member[] _MembersOfParent;

        public Member[] MembersOfParent
        {
            get
            {
                if (_MembersOfParent == null)
                    _MembersOfParent = new[] {FMember}
                        .Concat(GetParents()
                            .Select(x1 => x1.FMember))
                        .ToArray();
                return _MembersOfParent;
            }
        }

        private readonly Dictionary<DrillAction, bool> _dict = new Dictionary<DrillAction, bool>();

        public bool IsThisDrillAction(DrillAction a)
        {
            //DebugLogging.WriteLine("CellsetMember.IsThisDrillAction(cellsetmember={0}; da={1})", 
            //    DisplayName, a);

            var res = false;
            if (_dict.TryGetValue(a, out res) == false)
            {
                res = IsThisDrillAction_Inner(a);
            }
#if DEBUG
#endif

            return res;
        }

        private bool IsThisDrillAction_Inner(DrillAction a)
        {
            if (a.Members.Count != MembersOfParent.Length)
                return false;

            // all members are 

            var res1 = a.Members
                           .Where((t, i) => t != MembersOfParent[i])
                           .Any() == false;

            //res1 = true;

            //for (int i = 0; i < MembersOfParent.Length; i++)
            //{
            //    res1 &= a.Members.(i) != MembersOfParent;
            //}

            var res2 = a.Members
                           .Where((t, i) => t != MembersOfParent[i])
                           .FirstOrDefault() == null;

            if (res1 != res2)
            {
            }
            return res1;
        }
    }
}