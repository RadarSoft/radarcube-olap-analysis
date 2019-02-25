using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.CubeStructure
{
    /// <summary>Represents the list of hierarchy levels.</summary>
    /// <moduleiscollection />
    //[Serializable]
    public class CubeLevels : List<CubeLevel>, IStreamedObject
    {
        private readonly CubeHierarchy FHierarchy;

        internal CubeLevels(CubeHierarchy hierarchy)
        {
            FHierarchy = hierarchy;
        }

        public new void Add(CubeLevel item)
        {
            base.Add(item);
        }

        /// <summary>
        ///     Returns from the list the object of the CubeLevel type with a unique name passed
        ///     as the parameter or null, if there's no such object in the list.
        /// </summary>
        /// <param name="UniqueName">The unique name of the level</param>
        public CubeLevel Find(string UniqueName)
        {
            foreach (var l in this)
                if (l.UniqueName == UniqueName) return l;
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
                        var l = new CubeLevel(FHierarchy);
                        StreamUtils.ReadStreamedObject(reader, l);
                        Add(l);
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