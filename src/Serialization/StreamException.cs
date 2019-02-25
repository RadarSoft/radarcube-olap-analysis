using System;

namespace RadarSoft.RadarCube.Serialization
{
    /// <summary>
    ///     The exception is fired when an error occurs on reading cube data from / writing to the stream.
    /// </summary>
    public class StreamException : Exception
    {
        /// <exclude />
        public StreamException()
        {
        }

        /// <exclude />
        public StreamException(string message) : base(message)
        {
        }
    }
}