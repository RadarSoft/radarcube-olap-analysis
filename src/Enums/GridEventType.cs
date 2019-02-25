namespace RadarSoft.RadarCube.Enums
{
    /// <summary>The types of events that appear in the RadarCube library code.</summary>
    public enum GridEventType
    {
        geEndUpdate,
        geRebuildNodes,
        geAddCalculatedMember,
        geAddGroup,
        geDeleteCustomMember,
        geClearGroup,
        geMoveMemberToGroup,
        geMoveMemberFromGroup,
        geDrillAction,
        geSwapMember,
        gePivotAction,
        geFilterAction,
        geActiveChanged,
        geValueSortingChanged,
        geCancelHistory,
        geChangeCubeStructure,
        geAnalysisTypeChanged,
        geActiveSliceChanged
    }
}