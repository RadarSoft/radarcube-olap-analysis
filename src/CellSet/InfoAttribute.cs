using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.CellSet
{
    /// <exclude />
    [DebuggerDisplay("InfoAttribute: DisplayMode={DisplayMode} DisplayName={DisplayName}")]
    public class InfoAttribute : IStreamedObject, IPropertyGridLinker
    {
        internal string fDescription;
        internal AttributeDispalyMode FDisplayMode = AttributeDispalyMode.None;
        internal string fDisplayName;
        internal Type fFieldType;

        internal string fSourceField;
        internal string fUniqueName = Guid.NewGuid().ToString();
        internal IPropertyGridLinker LinkerHierarchy { get; set; }

        [NotifyParentProperty(true)]
        public string SourceField
        {
            get => fSourceField;
            set
            {
                fSourceField = value;
                if (value == "") fFieldType = null;
            }
        }

        /// <summary>
        ///     Hierarchy attribute caption. A value of a given property will be displayed for end users as an attribute name.
        /// </summary>
        [NotifyParentProperty(true)]
        public string DisplayName
        {
            get => fDisplayName;
            set => fDisplayName = value;
        }

        /// <summary>
        ///     An unique name of the attribute.
        /// </summary>
        [NotifyParentProperty(true)]
        public string UniqueName
        {
            get => fUniqueName;
            set => fUniqueName = value;
        }

        [NotifyParentProperty(true)]
        public Type SourceFieldType
        {
            get => fFieldType;
            set => fFieldType = value;
        }

        /// <summary>
        ///     Defines the method of displaying the attribute value in the Grid.
        /// </summary>
        [DefaultValue(AttributeDispalyMode.None)]
        [Description("Defines the method of displaying the attribute value in the Grid.")]
        [NotifyParentProperty(true)]
        public AttributeDispalyMode DisplayMode
        {
            get => FDisplayMode;
            set => FDisplayMode = value;
        }

        internal bool IsDisplayModeNone => FDisplayMode == AttributeDispalyMode.None;

        [DefaultValue(false)]
        public bool IsDisplayModeAsColumn
        {
            get => FDisplayMode.HasFlag(AttributeDispalyMode.AsColumn);
            set
            {
                if (value)
                    FDisplayMode |= AttributeDispalyMode.AsColumn;
                else
                    FDisplayMode &= ~AttributeDispalyMode.AsColumn;
            }
        }

        [DefaultValue(false)]
        public bool IsDisplayModeAsTooltip
        {
            get => FDisplayMode.HasFlag(AttributeDispalyMode.AsTooltip);
            set
            {
                if (value)
                    FDisplayMode |= AttributeDispalyMode.AsTooltip;
                else
                    FDisplayMode &= ~AttributeDispalyMode.AsTooltip;
            }
        }

        IDictionary<string, IList<string>> IPropertyGridLinker.TableToIDFields
        {
            get => LinkerHierarchy.TableToIDFields;
            set
            {
                if (LinkerHierarchy != null)
                    LinkerHierarchy.TableToIDFields = value;
                if (value == null)
                    LinkerHierarchy = null;
            }
        }

        string IPropertyGridLinker.DataTable
        {
            get
            {
                if (LinkerHierarchy == null)
                    return null;
                return LinkerHierarchy.DataTable;
            }
            set
            {
                ;
            }
        }

        string IPropertyGridLinker.DisplayField
        {
            get => SourceField;
            set => SourceField = value;
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgInfoAttribute);

            if (!string.IsNullOrEmpty(fSourceField))
            {
                StreamUtils.WriteTag(writer, Tags.tgInfoAttribute_SourceField);
                StreamUtils.WriteString(writer, fSourceField);
            }

            StreamUtils.WriteTag(writer, Tags.tgInfoAttribute_DisplayName);
            StreamUtils.WriteString(writer, fDisplayName);

            StreamUtils.WriteTag(writer, Tags.tgInfoAttribute_UniqueName);
            StreamUtils.WriteString(writer, fUniqueName);

            if (fFieldType != null)
            {
                StreamUtils.WriteTag(writer, Tags.tgInfoAttribute_FieldType);
                StreamUtils.WriteType(writer, fFieldType);
            }

            if (DisplayMode != AttributeDispalyMode.None)
            {
                StreamUtils.WriteTag(writer, Tags.tgInfoAttribute_DisplayMode);
                StreamUtils.WriteInt32(writer, (int) DisplayMode);
            }

            StreamUtils.WriteTag(writer, Tags.tgInfoAttribute_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgInfoAttribute);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgInfoAttribute_SourceField:
                        fSourceField = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgInfoAttribute_DisplayName:
                        fDisplayName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgInfoAttribute_UniqueName:
                        fUniqueName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgInfoAttribute_FieldType:
                        fFieldType = StreamUtils.ReadType(reader);
                        break;
                    case Tags.tgInfoAttribute_DisplayMode:
                        DisplayMode = (AttributeDispalyMode) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgInfoAttribute_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        #endregion
    }
}