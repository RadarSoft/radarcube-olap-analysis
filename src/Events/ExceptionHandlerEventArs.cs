using System;

namespace RadarSoft.RadarCube.Events
{
    /// <summary>
    ///     Provides data for the <see cref="TCustomOLAPControl.Error">OlapControl.Error</see> event.
    /// </summary>
    /// <example>
    ///     The simple OlapControl.Error event handler:
    ///     <code lang="CS">
    /// void Grid_Error(object sender, ExceptionHandlerEventArs e)
    /// {
    ///     MessageBox.Show(e.Exception.Message);
    ///     (sender as OlapControl).Serializer.XMLString = e.LastSuccessfulState;
    ///     e.Handled = true;
    /// }
    /// </code>
    ///     <code lang="VB">
    /// Private Sub Grid_Error(sender As Object, e As ExceptionHandlerEventArs)
    ///     MessageBox.Show(e.Exception.Message)
    ///     TryCast(sender, OlapControl).Serializer.XMLString = e.LastSuccessfulState
    ///     e.Handled = True
    /// End Sub
    /// </code>
    /// </example>
    public class ExceptionHandlerEventArs : EventArgs
    {
        /// <summary>
        ///     The ExceptionHandlerEventArs class constructor.
        /// </summary>
        public ExceptionHandlerEventArs(Exception exception, string savedXML)
        {
            Handled = false;
            LastSuccessfulState = savedXML;
            Exception = exception;
        }

        /// <summary>
        ///     Gets the Exception that represents the error that occurred.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///     The flag that indicates if the exception is handled
        ///     in the OlapControl.Error event handler.
        /// </summary>
        /// <remarks>
        ///     If this type of exception is handled in the
        ///     OlapControl.Error event handler, this flag shoud be set to True.
        /// </remarks>
        public bool Handled { get; set; }

        /// <summary>
        ///     The last successfully saved OLAP-slice
        ///     state.
        /// </summary>
        /// <example>
        ///     To apply this state, you need to execute the following
        ///     command:
        ///     <code lang="CS">
        /// TOLAPGid1.Serializer.XMLString = this.LastSuccessfulState;
        /// </code>
        /// </example>
        public string LastSuccessfulState { get; }
    }
}