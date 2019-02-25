using System;
using System.Reflection;
using System.Xml;

namespace RadarSoft.RadarCube.Serialization
{
    internal class EventHandlerXmlReaderArgs : EventArgs
    {
        public EventHandlerXmlReaderArgs(XmlReader AXmlReader, PropertyInfo APropertyInfo)
        {
            XmlReader = AXmlReader;
            PropertyInfo = APropertyInfo;
            SuccessReading = false;
        }

        public bool Success { get; set; } = true;

        public XmlReader XmlReader { get; }

        public PropertyInfo PropertyInfo { get; }

        public bool SuccessReading { get; set; }
    }
}