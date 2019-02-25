using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using RadarSoft.RadarCube.Attributes;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Tools;
using System.Linq;

namespace RadarSoft.RadarCube.Serialization
{
    public static class XmlSerializator
    {
        public static bool OLD_XMLMODE = true;

        public static bool MODE_SAVE_EMPTY_COLLECTION = false;
        public static bool MODE_SAVE_NULL_ELEMENT = false;

        internal static Stack<object> RootElementStack = new Stack<object>();

        internal static Stack<string> _Main_NameSpace = new Stack<string>();

        internal static EventHandlerXmlReader OnReadElement;
        internal static EventPropertyListGetter OnEventPropertyListGetter;
        internal static EventSerializeProcessing OnSerializeProcessing;

        internal static List<EventHandler> SerializeObject;

        [XmlIgnore]
        private static readonly Dictionary<Type, XmlSerializer> _Dict = new Dictionary<Type, XmlSerializer>();

        [XmlIgnore] internal static object lockobj = new object();

        [XmlIgnore] internal static Dictionary<Type, Type> _ConvertTo = new Dictionary<Type, Type>();

        [XmlIgnore] private static readonly List<Type> _ExcludeTypes = new List<Type>();

        private static object serializerLockObject;

        private static readonly Dictionary<Type, XmlSerializer> _XmlSerializers = new Dictionary<Type, XmlSerializer>();

        private static readonly Dictionary<PropertyInfo, IEnumerable<Attribute>> _prop_GetCustomAttributes =
            new Dictionary<PropertyInfo, IEnumerable<Attribute>>();

        private static readonly Dictionary<Type, FieldInfo[]> _FillLists_f = new Dictionary<Type, FieldInfo[]>();

        private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> _FillLists_p =
            new Dictionary<Type, IEnumerable<PropertyInfo>>();

        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> _FillLists_f_d =
            new Dictionary<Type, Dictionary<string, FieldInfo>>();

        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> _FillLists_p_d =
            new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        private static readonly Dictionary<string, Type> _Types = new Dictionary<string, Type>();

        static XmlSerializator()
        {
            SerializeObject = new List<EventHandler>();
        }

        internal static object RootElement
        {
            get
            {
                if (RootElementStack.Count == 0)
                    return null;
                return RootElementStack.Peek();
            }
            set
            {
                if (value == null)
                    RootElementStack.Pop();
                else
                    RootElementStack.Push(value);
            }
        }

        internal static string Main_NameSpace
        {
            get
            {
                if (_Main_NameSpace.Count == 0)
                    return null;
                return _Main_NameSpace.Peek();
            }
            set
            {
                if (value == null)
                    _Main_NameSpace.Pop();
                else
                    _Main_NameSpace.Push(value);
            }
        }

        internal static object SerializerLockObject
        {
            get
            {
                if (serializerLockObject == null)
                    Interlocked.CompareExchange<object>(ref serializerLockObject, new object(), null);
                return serializerLockObject;
            }
        }

        public static List<Type> ExcludeTypes => _ExcludeTypes;

        internal static XmlSerializer GetXmlSerializer(Type AType)
        {
            XmlSerializer res = null;
            lock (SerializerLockObject)
            {
#if DEBUG
                if (AType == null)
                {
                }
#endif
                try
                {
                    if (_XmlSerializers.TryGetValue(AType, out res) == false)
                    {
                        res = new XmlSerializer(AType);
                        _XmlSerializers.Add(AType, res);
                    }
                    return res;
                }
                catch (Exception e)
                {
                    Common_ExceptionMessage(e);
                }
            }
            return res;
        }

        public static void Serialize(XmlWriter writer, object ASource)
        {
            SerializeOLDSTYLE(writer, ASource);
            OnSerializeObject(ASource);
        }

        private static void OnSerializeObject(object ASource)
        {
            foreach (var h in SerializeObject)
                h(ASource, EventArgs.Empty);
        }

        public static void SerializeOLDSTYLE(XmlWriter writer, object ASource)
        {
#if DEBUG
            try
            {
#endif
                if (ASource == null)
                    return;

                var _Type = ASource.GetType();

                if (ExcludeTypes.Contains(_Type))
                    return;

                XmlSerializer xml = null;
                var props = _Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in props)
                {
                    var attrs = prop_GetCustomAttributes(prop, !true);

                    if (PresentAttribute(attrs, typeof(XmlIgnoreAttribute)))
                        continue;

                    if (prop.CanWrite == false)
                    {
                        if (prop.PropertyType == typeof(bool) ||
                            prop.PropertyType == typeof(string) ||
                            prop.PropertyType.GetTypeInfo().IsEnum)
                            continue;

                        var list = prop.GetValue(ASource, null) as IList;
                        if (list != null && list.Count == 0)
                            continue;
                    }

                    var val = prop.GetValue(ASource, null);
                    var valstart = val;

#if DEBUG
                    if (prop.Name == "Background")
                    {
                    }
#endif

                    if (val != null)
                    {
                        var ia = GetAttribute(attrs, typeof(XmlIntellegenceAttribute)) as XmlIntellegenceAttribute;

                        if (ia != null)
                            switch (ia.Func(val))
                            {
                                case TargetSerialization.Usial:
                                    var res = ia.Save(val);
                                    if (string.IsNullOrEmpty(res) == false)
                                    {
                                        writer.WriteStartElement(prop.Name);
                                        writer.WriteRaw(res);
                                        writer.WriteEndElement();
                                    }
                                    continue;
                                case TargetSerialization.Attached:
                                case TargetSerialization.Auto:
                                    continue;
                            }
                    }

                    if (PresentAttribute(attrs, typeof(XmlAttributeAttribute)))
                        continue;

                    var type = ConvertTo(prop.PropertyType);

                    if (ExcludeTypes.Contains(type))
                        continue;

                    if (type != prop.PropertyType)
                    {
                        object[] args = {val};
                        val = Activator.CreateInstance(type, args);
                    }

                    object defval = null;

                    if (GetDefaultValue(attrs, out defval))
                    {
                        if (valstart != null && defval != null &&
                            string.Compare(valstart.ToString(), defval.ToString()) == 0)
                            continue;
                        if (valstart == null && defval == null)
                            continue;
                        var v1 = Convert.ToString(valstart);
                        var v2 = Convert.ToString(defval);
                        if (string.Compare(v1, v2) == 0)
                            continue;
                    }

                    var ixml = val as IXmlSerializable;
                    if (ixml != null)
                    {
                        //if (val != null && val.GetType().IsAbstract || (val is IList == false))
                        //    writer.WriteStartElement(val.GetType().Name);
                        //else
                        writer.WriteStartElement(prop.Name);
                        ixml.WriteXml(writer);
                        writer.WriteEndElement();
                    }
                    else
                    {
                        val = ConvertTo(val);
                        if (type.GetTypeInfo().IsAbstract == false)
                        {
                            writer.WriteStartElement(prop.Name);
                            xml = GetXmlSerializer(type);
                            xml.Serialize(writer, val);
                            writer.WriteEndElement();
                        }
                    }
                }
                var fields = _Type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    var type = ConvertTo(field.FieldType);

                    if (ExcludeTypes.Contains(type))
                        continue;

                    var attrs = field.GetCustomAttributes(true);

                    if (PresentAttribute(attrs.Cast<Attribute>(), typeof(XmlIgnoreAttribute)))
                        continue;

                    var val = field.GetValue(ASource);
                    var valstart = val;

                    if (type != field.FieldType)
                    {
                        object[] args = {val};
                        val = Activator.CreateInstance(type, args);
                    }

                    object defval = null;

                    if (GetDefaultValue(attrs.Cast<Attribute>(), out defval))
                    {
                        if (valstart != null && defval != null &&
                            string.Compare(valstart.ToString(), defval.ToString()) == 0)
                            continue;
                        if (valstart == null && defval == null)
                            continue;
                        var v1 = Convert.ToString(valstart);
                        var v2 = Convert.ToString(defval);
                        if (string.Compare(v1, v2) == 0)
                            continue;
                    }

                    var ixml = val as IXmlSerializable;
                    if (ixml != null)
                    {
                        writer.WriteStartElement(field.Name);
                        ixml.WriteXml(writer);
                        writer.WriteEndElement();
                        continue;
                    }

                    val = ConvertTo(val);

                    if (type.GetTypeInfo().IsAbstract == false)
                    {
                        writer.WriteStartElement(field.Name);
                        xml = GetXmlSerializer(type);
                        xml.Serialize(writer, val);
                        writer.WriteEndElement();
                    }
                }
#if DEBUG
            }
            catch (Exception e)
            {
                Common_ExceptionMessage(e);
            }
#endif
        }

        private static IEnumerable<Attribute> prop_GetCustomAttributes(PropertyInfo pi, bool p)
        {
            IEnumerable<Attribute> res = null;
            if (_prop_GetCustomAttributes.TryGetValue(pi, out res) == false)
            {
                res = pi.GetCustomAttributes(p).Cast<Attribute>();
                _prop_GetCustomAttributes.Add(pi, res);
            }
            return res;
        }

        public static void SerializeNEWSTYLE(XmlWriter writer, object ASource)
        {
#if DEBUG
            try
            {
#endif
                var _Type = ASource.GetType();
                if (ExcludeTypes.Contains(_Type))
                    return;

                XmlSerializer xml = null;
                var props = GetPropertyInfos(_Type);

                foreach (var prop in props)
                {
                    var attrs = prop_GetCustomAttributes(prop, !true);

                    if (PresentAttribute(attrs, typeof(XmlIgnoreAttribute)) ||
                        PresentAttribute(attrs, typeof(XmlAttributeAttribute)))
                        continue;

                    if (IsContinueByReadOnly(ASource, prop))
                        continue;

#if DEBUG
                    if (prop.Name == "Background")
                    {
                    }
#endif

                    var val = prop.GetValue(ASource, null);
                    var valstart = val;

                    if (val != null && TryWriteIntellegenceData(writer, attrs, val, prop))
                        continue;

                    var type = ConvertTo(prop.PropertyType);

                    if (type != prop.PropertyType)
                        val = Activator.CreateInstance(type, val);

                    if (IsDefaultValuePresent(attrs, valstart, prop, ASource))
                        continue;

                    var ixml = val as IXmlSerializable;
                    if (ixml != null)
                    {
                        if (valstart == null)
                        {
                        }
                        else
                        {
                            writer.WriteStartElement(prop.Name);
                            if (IsCollectionProperty(prop, val) != -1)
                                ixml.WriteXml(writer);
                            else
                                ixml.WriteXml(writer);
                            writer.WriteEndElement();
                        }
                    }
                    else
                    {
                        val = ConvertTo(val);
                        //if (type.IsAbstract == false && (val != null && MODE_SAVE_NULL_ELEMENT))
                        {
                            writer.WriteStartElement(prop.Name);
                            xml = GetXmlSerializer(type);
                            xml.Serialize(writer, val);
                            writer.WriteEndElement();
                        }
                    }
                }
#if DEBUG
            }
            catch (Exception e)
            {
                Common_ExceptionMessage(e);
            }
#endif
        }

        private static int IsCollectionProperty(PropertyInfo prop, object val)
        {
            return IsCollectionProperty(prop.PropertyType, val);
        }

        internal static int IsCollectionProperty(Type AType, object val)
        {
            return -1;
        }

        public static string GetPropertyNameForWrite(object val, PropertyInfo prop)
        {
            if (val != null && val.GetType().GetTypeInfo().IsAbstract
                || IsCollectionProperty(prop, val) == -1
            )
                return val.GetType().Name;
            return prop.Name;
        }

        private static bool IsDefaultValuePresent(IEnumerable<Attribute> attrs, object valstart, PropertyInfo pi,
            object source)
        {
            object defval = null;
            if (GetDefaultValue(attrs, out defval, pi, source))
            {
                if (valstart != null &&
                    defval != null &&
                    string.Compare(valstart.ToString(), defval.ToString()) == 0)
                    return true;
                if (valstart == null && defval == null)
                    return true;

                if (valstart != null && defval == null || valstart == null && defval != null)
                    return false;

                var v1 = Convert.ToString(valstart);
                var v2 = Convert.ToString(defval);
                if (string.Compare(v1, v2) == 0)
                    return true;
            }
            return false;
        }

        private static bool TryWriteIntellegenceData(XmlWriter writer, IEnumerable<Attribute> attrs, object val,
            PropertyInfo prop)
        {
            var ia = GetAttribute(attrs, typeof(XmlIntellegenceAttribute)) as XmlIntellegenceAttribute;

            if (ia != null)
                switch (ia.Func(val))
                {
                    case TargetSerialization.Usial:
                        var res = ia.Save(val);
                        if (string.IsNullOrEmpty(res) == false)
                        {
                            writer.WriteStartElement(prop.Name);
                            writer.WriteRaw(res);
                            writer.WriteEndElement();
                        }
                        return true;
                    case TargetSerialization.Attached:
                    case TargetSerialization.Auto:
                        return true;
                }
            return false;
        }

        private static bool IsContinueByReadOnly(object ASource, PropertyInfo prop)
        {
            var list = prop.GetValue(ASource, null) as IList;

            if (prop.CanWrite == false)
                if (prop.PropertyType == typeof(bool) ||
                    prop.PropertyType == typeof(string) ||
                    prop.PropertyType.GetTypeInfo().IsEnum)
                    return true;
            if (list != null && list.Count == 0 && !MODE_SAVE_EMPTY_COLLECTION)
                return true;

            return false;
        }

        private static IEnumerable<PropertyInfo> GetPropertyInfos(Type _Type)
        {
            var props = _Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return props;
        }

        internal static bool PresentAttribute(IEnumerable<Attribute> attrs, Type type)
        {
            foreach (var obj in attrs)
                if (obj.GetType() == type || obj.GetType().GetTypeInfo().IsSubclassOf(type))
                    return true;
            return false;
        }

        internal static object GetAttribute(IEnumerable<Attribute> attrs, Type type)
        {
            if (attrs == null)
                return null;
            foreach (object obj in attrs)
                if (obj.GetType() == type || obj.GetType().GetTypeInfo().IsSubclassOf(type))
                    return obj;
            return null;
        }

        private static bool GetDefaultValue(IEnumerable<Attribute> attrs, out object Value)
        {
            return GetDefaultValue(attrs, out Value, null, null);
        }

        private static bool GetDefaultValue(IEnumerable<Attribute> attrs, out object Value, PropertyInfo pi,
            object sender)
        {
            Value = null;
            foreach (object obj in attrs)
                if (obj is DefaultValueAttribute)
                {
                    var def = obj as DefaultValueAttribute;
                    Value = def.Value;
                    return true;
                }
            return false;
        }

        private static void Common_ExceptionMessage(Exception e)
        {
            WorkException(e);
        }

        private static void WorkException(Exception e)
        {
            throw e;
        }

        internal static void DeSerialize(XmlReader reader, object ATarget)
        {
            DeSerializeOLDSTYLE(reader, ATarget);
            OnSerializeObject(ATarget);
        }

        public static void DeSerializeWPF(XmlReader reader, object ATarget)
        {
            DeSerializeOLDSTYLE(reader, ATarget);
        }

        public static void DeSerializeOLDSTYLE(XmlReader reader, object ATarget)
        {
            if (ATarget == null)
                return;
#if DEBUG
            try
            {
#endif
                var _Type = ATarget.GetType();

                var xml = GetXmlSerializer(_Type);
                object val = null;

                List<PropertyInfo> listprops;
                List<FieldInfo> listfields;
                Dictionary<string, FieldInfo> ddd;
                Dictionary<string, PropertyInfo> ppp;

                FillLists(_Type, out listprops, out listfields, out ddd, out ppp);

                while (listprops.Count > 0 || listfields.Count > 0)
                {
                    PropertyInfo prop = null;

                    var field = FindPropORField(reader, ddd, ppp, ref prop);

                    if (reader.NodeType == XmlNodeType.EndElement ||
                        reader.NodeType == XmlNodeType.None)
                        break;
                    if (reader.IsEmptyElement && reader.AttributeCount == 0)
                    {
                        reader.Read();
                        continue;
                    }

                    // no this property or no this text in XML
                    if (prop == null && field == null)
                        if (reader.NodeType == XmlNodeType.EndElement)
                        {
                            break;
                        }
                        else
                        {
                            if (reader.IsEmptyElement)
                                break;
                            reader.Skip();
                            continue;
                        }
                    if (prop != null)
                    {
                        var attrs = prop_GetCustomAttributes(prop, true);
                        if (PresentAttribute(attrs, typeof(XmlIgnoreAttribute)))
                        {
                            reader.Skip();
                            continue;
                        }
                        if (PresentAttribute(attrs, typeof(XmlAttributeAttribute)))
                        {
                            //if (reader.AttributeCount != 0)
                            //{
                            //    if (XmlSerializator.OnReadElement != null)
                            //        XmlSerializator.OnReadElement(ATarget, new EventHandlerXmlReaderArgs(reader, prop));
                            //}

                            reader.ReadAttributeValue();
                            reader.Read();
                            continue;
                        }

                        var ia = GetAttribute(attrs, typeof(XmlIntellegenceAttribute)) as XmlIntellegenceAttribute;
                        if (ia != null)
                        {
                            var res = reader.ReadInnerXml();
                            val = ia.Read(res);
                            SetValueToProperty(ATarget, val, prop);
                            listprops.Remove(prop);
                            continue;
                        }

                        var type = ConvertTo(prop.PropertyType);

                        val = prop.GetValue(ATarget, null);

                        if (val == null)
                        {
                            object defvalue;
                            if (GetDefaultValue(attrs, out defvalue))
                                SetValueToProperty(ATarget, defvalue, prop);
                        }

                        if (type != prop.PropertyType)
                        {
                            object[] args = {val};
                            val = Activator.CreateInstance(type, args);
                        }
                        if (val == null && type.GetTypeInfo().GetInterface(typeof(IXmlSerializable).Name) != null)
                            val = Activator.CreateInstance(type);

                        //bool IsIEnumerable = type.GetInterface(typeof(IEnumerable).Name) != null;

                        if ((reader.IsEmptyElement == false || reader.AttributeCount > 0)
                            && reader.NodeType != XmlNodeType.EndElement)
                        {
                            if ((reader.IsEmptyElement && reader.AttributeCount > 0) == false)
                                reader.ReadStartElement(prop.Name);
                            if (prop.PropertyType == typeof(Image))
                            {
                                if (reader.Name == "TxmlImage")
                                {
                                    // new method
                                    val = GetXmlSerializer(type).Deserialize(reader);
                                    SetValueToProperty(ATarget, val, prop);
                                }
                                else
                                {
                                    // old method
                                    val = GetXmlSerializer(typeof(Image)).Deserialize(reader);
                                }
                            }
                            else
                            {
                                if (val is IXmlSerializable && val != null)
                                    ((IXmlSerializable) val).ReadXml(reader);
                                else
                                    val = GetXmlSerializer(type).Deserialize(reader);
                                SetValueToProperty(ATarget, val, prop);
                            }
                            //if (IsIEnumerable == false)
                            //if (attributecount == 0)
                            if (reader.NodeType == XmlNodeType.EndElement)
                                reader.ReadEndElement();
                        }
                        else if (reader.NodeType != XmlNodeType.EndElement)
                        {
                            reader.Skip();
                        }

                        if (OnSerializeProcessing != null)
                            OnSerializeProcessing(val,
                                new EventSerializeProcessingArgs(val, SerializeAction.AfterRead));

                        listprops.Remove(prop);
                    }

                    if (field != null)
                    {
#if DEBUG
                        if (field.Name == "OLAPDocuments")
                        {
                        }
                        if (field.Name == "OLAPGridSettings")
                        {
                        }
                        if (field.Name == "AllowSelectionInfoCount")
                        {
                        }

#endif
                        val = field.GetValue(ATarget);

                        var type = ConvertTo(field.FieldType);

                        if (!reader.IsEmptyElement && val == null && type.GetTypeInfo().IsClass &&
                            type.GetConstructor(new Type[] { }) != null)
                        {
                            object[] args = { };
                            val = Activator.CreateInstance(type, args);
                        }

                        if (!reader.IsEmptyElement)
                        {
                            if (field.Name == reader.Name)
                                reader.ReadStartElement(field.Name);


                            if (val is IXmlSerializable)
                                ((IXmlSerializable) val).ReadXml(reader);
                            else
                                val = GetXmlSerializer(type).Deserialize(reader);

                            if (reader.AttributeCount > 0)
                            {
                                var nilattr = reader.GetAttribute("xsi:nil");
                                if (string.IsNullOrEmpty(nilattr) == false && nilattr == "true")
                                    reader.Read();
                            }

                            if (field.Name == reader.Name)
                                reader.ReadEndElement();
                        }
                        else
                        {
                            reader.Skip();
                        }

                        SetValueToField(ATarget, val, field);
                        if (listfields.Contains(field))
                            listfields.Remove(field);
                    }
                }
#if DEBUG
            }
            catch (Exception e)
            {
                Common_ExceptionMessage(e);
            }
#endif
        }

        private static bool TryIntellegence(
            IEnumerable<Attribute> attrs,
            XmlReader reader,
            List<PropertyInfo> listprops,
            PropertyInfo prop,
            object ATarget,
            ref object val
        )
        {
            var ia = GetAttribute(attrs, typeof(XmlIntellegenceAttribute)) as XmlIntellegenceAttribute;
            if (ia != null)
            {
                var res = reader.ReadInnerXml();
                val = ia.Read(res);
                SetValueToProperty(ATarget, val, prop);
                listprops.Remove(prop);
                return true;
            }
            return false;
        }

        private static void FillLists(Type _Type,
            out List<PropertyInfo> listprops,
            out List<FieldInfo> listfields,
            out Dictionary<string, FieldInfo> ddd,
            out Dictionary<string, PropertyInfo> ppp)
        {
            //
            // PropertyInfo
            //

            IEnumerable<PropertyInfo> props;

            if (_FillLists_p.TryGetValue(_Type, out props) == false)
            {
                props = GetPropertyInfos(_Type);
                listprops = new List<PropertyInfo>();
                listprops.AddRange(props);

                _FillLists_p.Add(_Type, props);

                ppp = new Dictionary<string, PropertyInfo>();
                foreach (var p in props)
                    if (!ppp.ContainsKey(p.Name))
                        ppp.Add(p.Name, p);

                _FillLists_p_d.Add(_Type, ppp);
            }
            else
            {
                ppp = _FillLists_p_d[_Type];
            }

            listprops = new List<PropertyInfo>(props);

            //
            // FieldInfo
            //

            FieldInfo[] fields;
            if (_FillLists_f.TryGetValue(_Type, out fields) == false)
            {
                fields = _Type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                listfields = new List<FieldInfo>();
                listfields.AddRange(fields);

                _FillLists_f.Add(_Type, fields);

                ddd = new Dictionary<string, FieldInfo>();
                foreach (var f in fields)
                    if (!ddd.ContainsKey(f.Name))
                        ddd.Add(f.Name, f);

                _FillLists_f_d.Add(_Type, ddd);
            }
            else
            {
                ddd = _FillLists_f_d[_Type];
            }

            listfields = new List<FieldInfo>(fields);
        }

        private static PropertyInfo FindProperty(XmlReader reader, Dictionary<string, PropertyInfo> ppp)
        {
            if (ppp.ContainsKey(reader.Name))
                return ppp[reader.Name];
            return null;
        }

        private static FieldInfo FindPropORField(XmlReader reader, Dictionary<string, FieldInfo> ddd,
            Dictionary<string, PropertyInfo> ppp, ref PropertyInfo prop)
        {
            if (ppp.ContainsKey(reader.Name))
                prop = ppp[reader.Name];

            FieldInfo field = null;
            if (prop == null)
                if (ddd.ContainsKey(reader.Name))
                    field = ddd[reader.Name];
            return field;
        }

        internal static Type FindTypeByName(string ATypeName, string ns)
        {
            if (_Types.ContainsKey(ATypeName))
                return _Types[ATypeName];
            var assemblies = RadarUtils.GetReferencingAssemblies();
            //Microsoft.Extensions.DependencyModel.DependencyContext.Default.CompileLibraries;
            //System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(path)
            foreach (var a in assemblies)
            foreach (var t in a.GetExportedTypes())
                if (t.Name == ATypeName && t.Namespace == ns)
                {
                    _Types.Add(ATypeName, t);
                    return t;
                }
            return null;
        }

        private static void SetValueToProperty(object owner, object val, PropertyInfo prop)
        {
            if (prop.CanWrite == false)
                return;
#if DEBUG
            try
            {
                var iback = val as IxmlGetBack;
                if (iback != null)
                    prop.SetValue(owner, iback.GetBack(), null);
                else
                    prop.SetValue(owner, val, null);
            }
            catch (Exception e)
            {
                Common_ExceptionMessage(e);
            }
#else

            IxmlGetBack iback = val as IxmlGetBack;
            if (iback != null)
                prop.SetValue(owner, iback.GetBack(), null);
            else
                prop.SetValue(owner, val, null);
#endif
        }

        private static void SetValueToField(object owner, object val, FieldInfo field)
        {
            var iback = val as IxmlGetBack;
            if (iback != null)
                field.SetValue(owner, iback.GetBack());
            else
                field.SetValue(owner, val);
        }

        private static Type ConvertTo(Type type)
        {
            return type;
        }

        private static object ConvertTo(object AData)
        {
            if (AData == null)
                return null;
            var type = AData.GetType();

            var iback = AData as IxmlGetBack;
            if (iback != null)
                return AData;

            var txml = ConvertTo(type);

            if (txml != type)
            {
                var res = Activator.CreateInstance(txml, AData);
                return res;
            }
            return AData;
        }

        internal static void Close()
        {
            _Dict.Clear();
            _ExcludeTypes.Clear();
            lockobj = new object();
        }
    }
}