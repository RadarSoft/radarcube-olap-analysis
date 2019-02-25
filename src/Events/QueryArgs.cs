using System;

namespace RadarSoft.RadarCube.Events
{
    /// <summary>
    ///     An object passed as a parameter to the MOlapCube.OnQuery event which allows getting
    ///     the text of MDX-queries and the time of their execution.
    /// </summary>
    /// <seealso cref="OlapCube.OnQuery">OnQuery Event (RadarSoft.RadarCube.WinForms.MOlapCube)</seealso>
    public class QueryArgs : EventArgs
    {
        internal QueryArgs(string query, TimeSpan execTime)
        {
            MDXQuery = query;
            ExecutionTime = execTime;
        }

        /// <summary>The text of the MDX query.</summary>
        public string MDXQuery { get; }

        /// <summary>The time taken to fulfill an MDX query.</summary>
        public TimeSpan ExecutionTime { get; }
    }
}