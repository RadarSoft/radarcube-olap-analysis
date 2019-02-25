using System;
using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.CubeStructure
{
    /// <summary>
    ///     Represents a collection of CubeDimension objects (dimensions of the
    ///     Cube).
    /// </summary>
    /// <moduleiscollection />
    ////[Serializable]
    public class CubeDimensions : List<CubeDimension>, IStreamedObject
    {
        [NonSerialized] internal Controls.Cube.RadarCube FCube;

        /// <summary>
        ///     Creates an instance of CubeDimension type
        /// </summary>
        /// <param name="ACube">Owner of this collection</param>
        public CubeDimensions(Controls.Cube.RadarCube ACube)
        {
            FCube = ACube;
        }
        //  procedure CheckRemovedComponent(AComponent: TComponent);

        /// <summary>
        ///     Read only. References to an instance of RadarCube containing the specified
        ///     collection of dimensions.
        /// </summary>
        public Controls.Cube.RadarCube Cube => FCube;

        /// <summary>
        ///     Returns the CubeDimension object with the name passed as the parameter or null,
        ///     if there's no such object in the collection.
        /// </summary>
        /// <returns>The CubeDimension object</returns>
        public CubeDimension this[string UniqueName] => Find(UniqueName);

        internal void ClearMembers()
        {
            foreach (var d in this) d.ClearMembers();
        }

        internal CubeHierarchy FindHierarchy(string p1, string p2)
        {
            foreach (var dim in this)
            foreach (var h in dim.Hierarchies)
                if (h.FDataTable == p1)
                    if (h.FDisplayField == p2)
                        return h;
            return null;
        }

        internal CubeHierarchy FindHierarchy(string uniqueName)
        {
            foreach (var dim in this)
            foreach (var h in dim.Hierarchies)
                if (h.UniqueName == uniqueName)
                    return h;
            return null;
        }

        internal CubeMeasure FindByMeasure(string p1, string p2)
        {
            foreach (var m in Cube.Measures)
                if (m.FDataTable == p1 && m.SourceField == p2)
                    return m;
            return null;
        }

        internal CubeLevel FindLevel(string uniqueName)
        {
            foreach (var d in this)
            foreach (var h in d.Hierarchies)
            {
                if (h.FCubeLevels == null || h.FCubeLevels.Count == 0) continue;
                foreach (var l in h.Levels)
                    if (l.UniqueName == uniqueName) return l;
            }
            return null;
        }

        /// <summary>
        ///     Returns the CubeDimension object with the name passed as the UniqueName
        ///     parameter or null, if there's no such object in the collection.
        /// </summary>
        /// <returns>The CubeDimension object</returns>
        /// <param name="UniqueName">The unique name of the dimension</param>
        public CubeDimension Find(string UniqueName)
        {
            foreach (var d in this)
                if (d.UniqueName == UniqueName) return d;
            return null;
        }

        /// <summary>
        ///     Returns the CubeDimension object with the display name passed as the DisplayName
        ///     parameter or null, if there's no such object in the collection.
        /// </summary>
        /// <returns>The CubeDimension object</returns>
        /// <param name="DisplayName">The caption of the dimension</param>
        public CubeDimension FindByDisplayName(string DisplayName)
        {
            foreach (var d in this)
                if (string.Compare(d.FDisplayName, DisplayName, StringComparison.CurrentCultureIgnoreCase) ==
                    0) return d;
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
                        var d = new CubeDimension();
                        d.Init(FCube);
                        // BeforeRead
                        StreamUtils.ReadStreamedObject(reader, d);
                        // AfterRead
                        Add(d);
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