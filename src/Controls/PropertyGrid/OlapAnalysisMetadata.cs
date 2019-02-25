using System;
using RadarSoft.RadarCube.Controls.Analysis;

namespace RadarSoft.RadarCube.Controls.PropertyGrid
{
    public class OlapAnalysisMetadata : PropertyGridMetadata
    {
        public PropertyMetadata Appearance_AnalysisType;
        public PropertyMetadata Appearance_EmptyCellString;
        public PropertyMetadata Appearance_HierarchiesDisplayMode;
        public PropertyMetadata Appearance_LinesInPage;
        public PropertyMetadata Appearance_MaxColumnsInGrid;
        public PropertyMetadata Appearance_MaxRowsInGrid;
        public PropertyMetadata Appearance_MaxTextLength;
        public PropertyMetadata Appearance_UseCheckboxesInTree;
        public PropertyMetadata Appearance_UseFixedHeaders;

        public PropertyMetadata Behavior_AllowDrilling;
        public PropertyMetadata Behavior_AllowEditing;
        public PropertyMetadata Behavior_AllowFiltering;
        public PropertyMetadata Behavior_AllowPaging;
        public PropertyMetadata Behavior_AllowScrolling;
        public PropertyMetadata Behavior_AllowResizing;
        public PropertyMetadata Behavior_AllowSelectionFormatting;
        public PropertyMetadata Behavior_ClientCallbackFunction;
        public PropertyMetadata Behavior_DelayPivoting;

        public PropertyMetadata Behavior_ErrorHandler;

        //public PropertyMetadata Behavior_IsSettingsEditable;
        public PropertyMetadata Behavior_MessageHandler;

        public PropertyMetadata Behavior_PivotingBehavior;

        public PropertyMetadata CubeStructureTree_ExpandDimensionsNode;
        public PropertyMetadata CubeStructureTree_ExpandMeasuresNode;
        public PropertyMetadata CubeStructureTree_HideDimensionNameIfPossible;
        public PropertyMetadata CubeStructureTree_StructureTreeWidth;

        //public PropertyMetadata Export_DrillthroughExportType;

        //public PropertyMetadata Export_IgnoreGridPaging;
        //public PropertyMetadata Localization_UsePreferredBrowserLanguage;

        public PropertyMetadata HierarchyEditorStyle_ItemsInPage;
        public PropertyMetadata HierarchyEditorStyle_Resizable;
        public PropertyMetadata HierarchyEditorStyle_TreeHeight;
        public PropertyMetadata HierarchyEditorStyle_Width;

        public PropertyMetadata Layout_FilterAreaVisible;
        public PropertyMetadata Layout_Height;
        public PropertyMetadata Layout_ShowAreasMode;
        public PropertyMetadata Layout_ShowFilterGrid;
        public PropertyMetadata Layout_ShowLegends;
        public PropertyMetadata Layout_ShowModificationAreas;
        public PropertyMetadata Layout_ShowToolbox;
        public PropertyMetadata Layout_Width;

        public PropertyMetadata Localization_CurrencyFormatString;
        //public PropertyMetadata Localization_DefaultLanguageCode;

        protected override Type RootType => typeof(OlapAnalysis);
    }
}