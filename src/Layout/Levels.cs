using System;
using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>The list of hierarchy levels</summary>
    /// <remarks>
    ///     The hierarchy of any type has at least one level that contains the list of its
    ///     members
    /// </remarks>
    public class Levels : List<Level>, IStreamedObject
    {
        [NonSerialized] private CubeLevels fCubeLevels;

        internal Levels(Hierarchy AHierarchy, CubeLevels ACubeLevels)
        {
            Hierarchy = AHierarchy;
            Hierarchy.FLevels = this;
            fCubeLevels = ACubeLevels;
            foreach (var l in ACubeLevels)
            {
                var L = new Level(AHierarchy, l, null);
                L.fIndex = Convert.ToInt16(Count);
                Add(L);
                L.FMembers.Initialize(L.FCubeLevel.FMembers, L, null);
                L.CreateNewMembers();
            }
        }

        internal Levels(Hierarchy AHierarchy)
        {
            Hierarchy = AHierarchy;
        }

        /// <summary>References to the hierarchy containing the specified list of levels.</summary>
        public Hierarchy Hierarchy { get; }

        /// <summary>References to the corresponding CubeLevels object.</summary>
        public CubeLevels CubeLevels => fCubeLevels;

        internal void RestoreAfterSerialization(CubeLevels levels)
        {
            fCubeLevels = levels;
            foreach (var l in this) l.RestoreAfterSerialization(Hierarchy.Dimension.Grid);
        }

        /// <summary>
        ///     Returns from the list the object of the Level type with a unique name passed as
        ///     the parameter or null, if there's no such object in the list.
        /// </summary>
        public Level Find(string UniqueName)
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
                        var l = new Level(Hierarchy, null);
                        // BeforeRead
                        StreamUtils.ReadStreamedObject(reader, l);
                        // AfterRead
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