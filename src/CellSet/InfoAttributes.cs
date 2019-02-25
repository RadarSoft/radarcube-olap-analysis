using System;
using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.CellSet
{
    /// <exclude />
    /// <moduleiscollection />
    public class InfoAttributes : List<InfoAttribute>, IStreamedObject
    {
        // ***
        internal CubeHierarchy fHierarchy;

        public InfoAttributes(CubeHierarchy AHierarchy)
        {
            fHierarchy = AHierarchy;
        }

        internal InfoAttributes()
            : this(null)
        {
        }

        // ***
        internal InfoAttributes Clone()
        {
            var target = new InfoAttributes(fHierarchy);
            foreach (var item in this)
            {
                var newItem = new InfoAttribute();
                newItem.fSourceField = item.fSourceField;
                newItem.fDisplayName = item.fDisplayName;
                newItem.fFieldType = item.fFieldType;
                newItem.DisplayMode = item.DisplayMode;
                newItem.fUniqueName = item.fUniqueName;
                target.Add(newItem);
            }
            return target;
        }

        /// <summary>
        ///     Returns from the collection the object of the InfoAttribute type
        ///     with a unique name passed as the UniqueName parameter or nil, if that object doesn't exist in the collection.
        /// </summary>
        /// <param name="UniqueName"></param>
        /// <returns></returns>
        public InfoAttribute Find(string uniqueName)
        {
            foreach (var i in this)
                if (i.fUniqueName == uniqueName)
                    return i;
            foreach (var i in this)
                if (i.fDisplayName == uniqueName)
                    return i;
            return null;
        }

        /// <summary>
        ///     Seeks the attribute with the assigned DisplayName in the list of attributes.
        /// </summary>
        /// <param name="DisplayName"></param>
        /// <returns></returns>
        public InfoAttribute FindByDisplayName(string DisplayName)
        {
            foreach (var i in this)
                if (string.Compare(i.fDisplayName, DisplayName, StringComparison.CurrentCultureIgnoreCase) == 0)
                    return i;
            return null;
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteList(writer, this);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgList);
            Clear();
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgList_Count:
                        var c = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgList_Item:
                        var item = new InfoAttribute();
                        StreamUtils.ReadStreamedObject(reader, item);
                        // AfterRead
                        Add(item);
                        break;
                    case Tags.tgList_EOT:
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