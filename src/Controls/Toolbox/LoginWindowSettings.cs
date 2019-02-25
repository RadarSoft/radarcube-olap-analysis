using System.ComponentModel;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    /// <summary>
    ///     The settings of the login window displayed when the Connect button is
    ///     pressed.
    /// </summary>
    public class LoginWindowSettings
    {
        /// <summary>
        ///     "Server name" label string
        /// </summary>
        internal const string _ServerLabel = "Server name or path to the local Cube file:";

        private const string _ErrorString = "Connection error: {0}";
        internal const string _DatabaseLabel = "Database:";
        internal const string _CubeLabel = "Cube:";

        internal string fCubeLabel = _CubeLabel;

        internal string fDatabaseLabel = _DatabaseLabel;

        internal OlapToolboxBase fOwner;
        private string fServerLabel = _ServerLabel;

        [DefaultValue(_ServerLabel)]
        [Description("\"Server name\" label string")]
        public string ServerLabel
        {
            get => fServerLabel;
            set
            {
                if (string.IsNullOrEmpty(value))
                    fServerLabel = _ServerLabel;
                else
                    fServerLabel = value;
            }
        }

        /// <summary>
        ///     Data source for the MSAS database (name of the server or absolute path to the local cube file).
        /// </summary>
        [DefaultValue("")]
        [Description("Data source for the MSAS database (name of the server or absolute path to the local cube file)")]
        public string ServerName { get; set; } = "";

        [DefaultValue(true)]
        [Description("Visibility of \"Server name\" section")]
        [NotifyParentProperty(true)]
        public bool IsServerNameVisible { get; set; } = true;

        /// <summary>
        ///     The information displayed when it's a connection error.
        /// </summary>
        [DefaultValue(_ErrorString)]
        [Description("The information displayed when it's a connection error")]
        [NotifyParentProperty(true)]
        public string ErrorString { get; set; } = _ErrorString;

        /// <summary>
        ///     "Database" label string
        /// </summary>
        [DefaultValue(_DatabaseLabel)]
        [Description("\"Database\" label string")]
        [NotifyParentProperty(true)]
        public string DatabaseLabel
        {
            get => fDatabaseLabel;
            set
            {
                if (string.IsNullOrEmpty(value))
                    fDatabaseLabel = _DatabaseLabel;
                else
                    fDatabaseLabel = value;
            }
        }

        /// <summary>
        ///     The MSAS database name
        /// </summary>
        [DefaultValue("")]
        [Description("The MSAS database name")]
        [NotifyParentProperty(true)]
        public string DatabaseName { get; set; } = "";

        [DefaultValue(true)]
        [Description("Visibility of \"Database name\" section")]
        [NotifyParentProperty(true)]
        public bool IsDatabaseNameVisible { get; set; } = true;

        /// <summary>
        ///     "Cube" label string
        /// </summary>
        [DefaultValue(_CubeLabel)]
        [Description("\"Cube\" label string")]
        [NotifyParentProperty(true)]
        public string CubeLabel
        {
            get => fCubeLabel;
            set
            {
                if (string.IsNullOrEmpty(value))
                    fCubeLabel = _CubeLabel;
                else
                    fCubeLabel = value;
            }
        }

        /// <summary>
        ///     The MSAS Cube name
        /// </summary>
        [DefaultValue("")]
        [Description("The MSAS Cube name")]
        [NotifyParentProperty(true)]
        public string CubeName { get; set; } = "";

        [DefaultValue(true)]
        [Description("Visibility of \"Cube name\" section")]
        [NotifyParentProperty(true)]
        public bool IsCubeNameVisible { get; set; } = true;
    }
}