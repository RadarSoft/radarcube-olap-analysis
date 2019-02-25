using RadarSoft.RadarCube.Enums;

namespace RadarSoft.RadarCube.CubeStructure
{
    /// <summary>
    ///     Describes the MSAS Action for the specified item (cell, member, level, hierarchy
    ///     and so on).
    /// </summary>
    public class CubeAction
    {
        internal CubeAction(CubeActionType type, string name, string caption, string description,
            string expression, string application)
        {
            ActionType = type;
            ActionName = name;
            Caption = caption;
            Description = description;
            Expression = expression;
            Application = application;
        }

        /// <summary>Specifies an action's triggering method.</summary>
        public CubeActionType ActionType { get; }

        /// <summary>
        ///     The name of this action.
        /// </summary>
        public string ActionName { get; }

        /// <summary>The action name.</summary>
        public string Caption { get; }

        /// <summary>A detailed description of the action.</summary>
        public string Description { get; }

        /// <summary>The expression or content of the action to be run.</summary>
        public string Expression { get; }

        /// <summary>
        ///     The name of the application that is to be used to run the action.
        /// </summary>
        public string Application { get; }
    }
}