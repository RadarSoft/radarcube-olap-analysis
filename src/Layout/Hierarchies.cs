using System;
using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>
    ///     An object of this type represents the collection of hierarchies on the Grid
    ///     level
    /// </summary>
    /// <moduleiscollection />
    public class Hierarchies : List<Hierarchy>, IStreamedObject
    {
        internal Hierarchies(Dimension ADimension)
        {
            Dimension = ADimension;
        }

        /// <summary>References to the Dimension object that contains the specified list.</summary>
        public Dimension Dimension { get; }

        public Hierarchy this[string UniqueName] => Find(UniqueName);

        /// <summary>
        ///     Returns from the collection an object of the Hierarchy type with a unique name
        ///     passed as the parameter or null, if there's no such object in the collection.
        /// </summary>
        public Hierarchy Find(string UniqueName)
        {
            foreach (var h in this)
                if (h.FUniqueName == UniqueName) return h;
            return null;
        }

        /// <summary>
        ///     Returns the first hierarchy with the name passed as DisplayName from the
        ///     list.
        /// </summary>
        public Hierarchy FindByDisplayName(string DisplayName)
        {
            foreach (var h in this)
                if (string.Compare(h.DisplayName, DisplayName, StringComparison.CurrentCultureIgnoreCase) ==
                    0) return h;
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
                        var h = new Hierarchy(Dimension);
                        // BeforeRead
                        StreamUtils.ReadStreamedObject(reader, h);
                        // AfterRead
                        Add(h);
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