using System;
using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.CubeStructure
{
    /// <summary>Represents a collection of the Cube measures.</summary>
    /// <moduleiscollection />
    ////[Serializable]
    public class CubeMeasures : List<CubeMeasure>, IStreamedObject
    {
        [NonSerialized] internal Controls.Cube.RadarCube FCube;

        /// <summary>
        ///     Creates an instance of the CubeMeasures collection
        /// </summary>
        /// <param name="ACube"></param>
        public CubeMeasures(Controls.Cube.RadarCube ACube)
        {
            FCube = ACube;
        }

        /// <summary>
        ///     References to an instance of RadarCube containing the specified collection of
        ///     measures.
        /// </summary>
        public Controls.Cube.RadarCube Cube => FCube;

        /// <summary>
        ///     Returns the CubeMeasure object with a unique name passed as the parameter or
        ///     null, if there is no object in the collection.
        /// </summary>
        public CubeMeasure this[string UniqueName] => Find(UniqueName);

        /// <summary>
        ///     Returns a CubeMeasure object with a unique name passed as the parameter or null,
        ///     if there is no such object in the collection.
        /// </summary>
        /// <param name="UniqueName">The unique name of the measure</param>
        public CubeMeasure Find(string UniqueName)
        {
            foreach (var m in this)
                if (m.FUniqueName == UniqueName) return m;
            return null;
        }

        /// <summary>
        ///     Returns a CubeMeasure object with a display name passed as the parameter from
        ///     the Measures collection or null, if there is no such object in the collection.
        /// </summary>
        /// <param name="DisplayName">The caption of the measure</param>
        public CubeMeasure FindByDisplayName(string DisplayName)
        {
            foreach (var m in this)
                if (string.Compare(m.FDisplayName, DisplayName, StringComparison.CurrentCultureIgnoreCase) ==
                    0) return m;
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
                        var measure = new CubeMeasure();
                        // BeforeRead
                        measure.FCube = FCube;
                        StreamUtils.ReadStreamedObject(reader, measure);
                        // AfterRead
                        Add(measure);
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