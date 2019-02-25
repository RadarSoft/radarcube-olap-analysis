using System.ComponentModel;
using System.Xml.Serialization;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    /// <summary>Used in the Ria control for expanding its standard context menus.</summary>
    /// <seealso cref="RiaOLAPGrid.OnShowContextMenu">OnShowContextMenu Event (RadarSoft.RadarCube.Web.RiaOLAPGrid)</seealso>
    public class GenericMenuItem
    {
        public GenericMenuItem()
        {
        }

        public GenericMenuItem(GenericMenuActionType actionType, string caption, string imageUrl,
            string actionString)
        {
            ActionType = actionType;
            Caption = caption;
            ImageUrl = imageUrl;
            MenuItemValue = actionString;
        }

        public GenericMenuItem(GenericMenuActionType actionType)
        {
            ActionType = actionType;
        }

        public GenericMenuItem(string caption)
        {
            ActionType = GenericMenuActionType.Nothing;
            Caption = caption;
        }

        public GenericMenuItem(GenericMenuActionType actionType, string caption, bool isChecked,
            string actionString)
        {
            ActionType = actionType;
            Caption = caption;
            IsChecked = isChecked;
            MenuItemValue = actionString;
        }

        internal string ExtraCommand { get; set; } = "";

        /// <summary>For the ClientOnly handling type – the JScript-function to be called.</summary>
        public string ClientScript { get; set; } = string.Empty;

        /// <summary>
        ///     The menu item click handling type: it can be handled on the server through a
        ///     Callback-request (AJAX), Postback-request, or a client-side JScript function can be
        ///     called (ClientOnly).
        /// </summary>
        public ClientActionHandlingType HandlingType { get; set; } = ClientActionHandlingType.Postback;

        /// <summary>
        ///     For TGenericMenuItems, created in the OnShowContextMenu events handler, this
        ///     property must be set into CustomAction (with the exception of menu items with children
        ///     – in this case it should be Nothing).
        /// </summary>
        /// <summary>
        ///     For TGenericMenuItems, created in the OnShowContextMenu events handler, this
        ///     property must be set into CustomAction (with the exception of menu items with children
        ///     – in this case it should be Nothing).
        /// </summary>
        public GenericMenuActionType ActionType { get; set; }

        /// <summary>A caption of the context menu item.</summary>
        [DefaultValue("")]
        public string Caption { get; set; } = string.Empty;

        [DefaultValue("")]
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        ///     For the Callback and Postback handling types – the unique value, passed to the
        ///     OnContextMenuClick event handler, that allows identifying which menu item was
        ///     clicked.
        /// </summary>
        [DefaultValue("")]
        public string MenuItemValue { get; set; } = string.Empty;

        /// <summary>
        ///     True or False, depending of the required status of the “Checked” menu
        ///     item.
        /// </summary>
        [DefaultValue(false)]
        public bool IsChecked { get; set; }

        [DefaultValue("")]
        public string TargetPage { get; set; } = string.Empty;

        /// <summary>The list of “children” menu items – in this case, the menu is multi-level.</summary>
        /// <summary>The list of “children” menu items – in this case, the menu is multi-level.</summary>
        [XmlIgnore]
        public GenericMenu ChildItems
        {
            get
            {
                if (Items == null)
                    Items = new GenericMenu();
                return Items;
            }
        }

        /// <summary>Do not use it directly. Use ChildItems property instead.</summary>
        /// <exclude />
        [DefaultValue(null)]
        public GenericMenu Items { get; set; }

        internal void JScriptCorrection(OlapControl grid)
        {
            var s = "javascript:{RadarSoft.$('#" + grid.ClientID + "').data('grid')}";
            if (MenuItemValue.StartsWith(s))
                MenuItemValue = MenuItemValue.Substring(s.Length);
            MenuItemValue = MenuItemValue.Replace("javascript:olapgrid_manager", "");
            foreach (var mi in ChildItems)
                mi.JScriptCorrection(grid);
        }
    }
}