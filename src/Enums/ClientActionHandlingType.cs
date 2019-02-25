namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Enumerates the ways of handling some user action on some web control.
    /// </summary>
    public enum ClientActionHandlingType
    {
        /// <summary>
        ///     Handling with a client script only
        /// </summary>
        ClientOnly,

        /// <summary>
        ///     Handling with a postback requst
        /// </summary>
        Postback,

        /// <summary>
        ///     Handling with a callback request and a client script after it
        /// </summary>
        Callback,
        Inherent
    }
}