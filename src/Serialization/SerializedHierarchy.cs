using System;
using System.ComponentModel;
using System.Linq;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    public class SerializedHierarchy
    {
        [DefaultValue(SerializedAlignment.Left)] public SerializedAlignment Alignment = SerializedAlignment.Left;

        [DefaultValue(true)] public bool AllowChangeTotalAppearance = true;

        [DefaultValue(true)] public bool AllowFilter = true;

        [DefaultValue(true)] public bool AllowHierarchyEditor = true;

        [DefaultValue(true)] public bool AllowMultiselect = true;

        [DefaultValue(true)] public bool AllowPopupOnLevelCaptions = true;

        [DefaultValue(true)] public bool AllowPopupOnMembers = true;

        [DefaultValue(true)] public bool AllowRegroup = true;

        [DefaultValue(true)] public bool AllowResort = true;

        [DefaultValue(true)] public bool AllowSwapMembers = true;

        [DefaultValue("")] public string DisplayName = "";

        [DefaultValue("Currency")] public string FormatString = "Currency";

        public SerializedLevel[] Levels;

        [DefaultValue(false)] public bool OverrideSortMethods;

        [DefaultValue(false)] public bool ShowEmptyLines;

        [DefaultValue(MembersSortType.msTypeRelated)] public MembersSortType SortType = MembersSortType.msTypeRelated;

        [DefaultValue(true)] public bool TakeFiltersIntoCalculations = true;

        [DefaultValue(TotalAppearance.taLast)] public TotalAppearance TotalAppearance = TotalAppearance.taLast;

        [DefaultValue("")] public string TotalCaption = "";

        [DefaultValue(true)] public bool UnfetchedMembersVisibile = true;

        public string UniqueName;

        [DefaultValue(true)] public bool Visible = true;

        public SerializedHierarchy()
        {
        }

        public SerializedHierarchy(Hierarchy h)
        {
            UnfetchedMembersVisibile = h.UnfetchedMembersVisible;
            UniqueName = h.UniqueName;
            if (h.DisplayName != h.CubeHierarchy.DisplayName) DisplayName = h.DisplayName;

            if (h.TotalCaption != RadarUtils.GetResStr("rsTotalCaption")) TotalCaption = h.TotalCaption;

            SortType = h.SortType;
            FormatString = h.FormatString;
            TotalAppearance = h.TotalAppearance;
            AllowResort = h.AllowResort;
            AllowFilter = h.AllowFilter;
            AllowPopupOnLevelCaptions = h.AllowPopupOnLevelCaptions;
            AllowHierarchyEditor = h.AllowHierarchyEditor;
            AllowPopupOnMembers = h.AllowPopupOnMembers;
            AllowRegroup = h.AllowRegroup;
            AllowChangeTotalAppearance = h.AllowChangeTotalAppearance;
            Visible = h.Visible;
            TakeFiltersIntoCalculations = h.TakeFiltersIntoCalculations;
            AllowSwapMembers = h.AllowSwapMembers;
            AllowMultiselect = h.AllowMultiselect;
            OverrideSortMethods = h.OverrideSortMethods;
            ShowEmptyLines = h.FShowEmptyLines;
            if (h.FLevels != null)
            {
                var ll = h.FLevels.Select(L => new SerializedLevel(L)).ToList();
                if (ll.Count > 0)
                    Levels = ll.ToArray();
            }
        }

        public override string ToString()
        {
            return string.Format("DisplayName={0} UniqueName={1}", DisplayName, UniqueName);
        }

        internal void Restore(Hierarchy h)
        {
            h.DeleteCalculatedMembers();
            h.DeleteGroups();
            h.UnfetchedMembersVisible = UnfetchedMembersVisibile;
            if (DisplayName != "") h.FDisplayName = DisplayName;
            if (TotalCaption != "") h.TotalCaption = TotalCaption;
            h.FSortType = SortType;
            h.FormatString = FormatString;

            h.FTotalAppearance = TotalAppearance;
            h.FAllowResort = AllowResort;
            h.FAllowFilter = AllowFilter;
            h.FAllowPopupOnCaptions = AllowPopupOnLevelCaptions;
            h.AllowHierarchyEditor = AllowHierarchyEditor;
            h.AllowPopupOnMembers = AllowPopupOnMembers;
            h.AllowRegroup = AllowRegroup;
            h.AllowChangeTotalAppearance = AllowChangeTotalAppearance;
            h.Visible = Visible;
            h.FTakeFiltersIntoCalculations = TakeFiltersIntoCalculations;
            h.AllowSwapMembers = AllowSwapMembers;
            h.AllowMultiselect = AllowMultiselect;
            h.FOverrideSortMethods = OverrideSortMethods;
            h.FShowEmptyLines = ShowEmptyLines;
            if (Levels != null && !h.FInitialized)
                h.InitHierarchy(h.IsDate ? -1 : 0);

            RestoreFilter(h);
        }

        internal void RestoreFilter(Hierarchy h)
        {
            if (h.FLevels != null && Levels != null)
            {
                var UpdateFilter = false;
                h.BeginUpdate();

                foreach (var m in h.FLevels[0].Members)
                    m.Visible = h.UnfetchedMembersVisible;

                for (var i = 0; i < Math.Min(h.FLevels.Count, Levels.Length); i++)
                    if (Levels[i].Restore(h.FLevels[i]))
                        UpdateFilter = true;
                if (UpdateFilter)
                    h.UpdateFilterState(true);
                h.EndUpdate();

                if (!UnfetchedMembersVisibile && h.FLevels.Count > 0)
                    h.EndUpdate();

                h.Sort();
            }
        }
    }
}