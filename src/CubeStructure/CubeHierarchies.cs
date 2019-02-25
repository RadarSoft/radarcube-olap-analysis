using System;
using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.CubeStructure
{
    /// <summary>Represents a collection of the dimension hierarchies.</summary>
    /// <moduleiscollection />
    //[Serializable]
    public class CubeHierarchies : List<CubeHierarchy>, IStreamedObject
    {
        internal CubeHierarchies(CubeDimension ADimension)
        {
            Dimension = ADimension;
        }

        /// <summary>References to a dimension containing the specified hierarchy.</summary>
        public CubeDimension Dimension { get; }

        /// <summary>
        ///     Returns the CubeHierarchy object with a unique name passed as the parameter or
        ///     null, if there is no such object in the collection.
        /// </summary>
        /// <returns>The CubeHierarchy object</returns>
        public CubeHierarchy this[string UniqueName] => Find(UniqueName);

        /// <summary>
        ///     Returns a CubeHierarchy object with a name passed as the parameter or null, if
        ///     there's no such object in the collection.
        /// </summary>
        /// <returns>The CubeHierarchy object</returns>
        /// <param name="UniqueName">The unique name of the hierarchy</param>
        public CubeHierarchy Find(string UniqueName)
        {
            foreach (var h in this)
                if (h.FUniqueName == UniqueName) return h;
            return null;
        }

        /// <summary>
        ///     Returns the CubeHierarchy object with a display name passed as the parameter or
        ///     null, if there's no such object in the collection.
        /// </summary>
        /// <returns>The CubeHierarchy object</returns>
        /// <param name="DisplayName">The caption of the hierarchy</param>
        public CubeHierarchy FindByDisplayName(string DisplayName)
        {
            foreach (var h in this)
                if (string.Compare(h.FDisplayName, DisplayName, StringComparison.CurrentCultureIgnoreCase) ==
                    0) return h;
            return null;
        }

        /// <summary>Returns a multilevel hierarchy that has the specified DisplayName.</summary>
        /// <returns>The CubeHierarchy object</returns>
        /// <param name="DisplayName">The caption of the hierarchy</param>
        public CubeHierarchy FindComposite(string DisplayName)
        {
            foreach (var h in this)
                if (h.FOrigin == HierarchyOrigin.hoUserDefined && h.FDisplayName == DisplayName) return h;
            return null;
        }

        /// <summary>Returns a non-multilevel hierarchy with the specified DisplayName.</summary>
        /// <returns>The CubeHierarchy object</returns>
        /// <param name="DisplayName">The caption of the hierarchy</param>
        public CubeHierarchy FindNonComposite(string DisplayName)
        {
            foreach (var h in this)
                if (h.FOrigin != HierarchyOrigin.hoUserDefined && h.FDisplayName == DisplayName) return h;
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
                        var h = new CubeHierarchy();
                        h.Init(Dimension);
                        // BeforeRead
                        StreamUtils.ReadStreamedObject(reader, h);
                        // AfterRead
                        Add(h);
                        break;
                    case Tags.tgList_EOT:
                        exit = true;
                        foreach (var h1 in this)
                            h1.ResolveChildrenAndSource();
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