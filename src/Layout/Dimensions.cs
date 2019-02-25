using System;
using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>Represents the Grid-level collection of Cube dimensions.</summary>
    /// <moduleiscollection />
    public class Dimensions : List<Dimension>, IStreamedObject
    {
        internal OlapControl FGrid;

        /// <summary>Creates an instance of the Dimensions type.</summary>
        /// <param name="AGrid">An owner of this collection</param>
        public Dimensions(OlapControl AGrid)
        {
            FGrid = AGrid;
        }

        /// <summary>
        ///     References to an instance of OlapControl containing the specified
        ///     dimension.
        /// </summary>
        public OlapControl Grid => FGrid;

        /// <summary>
        ///     Returns a dimension with the specified unique name.
        /// </summary>
        /// <param name="UniqueName">An <see cref="TDimension.UniqueName">unique name</see> of the dimension</param>
        /// <returns></returns>
        public Dimension this[string UniqueName] => Find(UniqueName);

        internal void RestoreAfterSerialization(OlapControl grid)
        {
            FGrid = grid;
            foreach (var d in this)
                d.RestoreAfterSerialization(grid);
        }

        internal void ClearMembers()
        {
            foreach (var d in this) d.ClearMembers();
        }

        /// <summary>
        ///     Removes all calculated members from every hierarchy of every dimension in the list
        /// </summary>
        public void DeleteCalculatedMembers()
        {
            foreach (var d in this) d.DeleteCalculatedMembers();
        }

        /// <summary>
        ///     Removes all groups from every hierarchy of every dimension in the list
        /// </summary>
        public void DeleteGroups()
        {
            foreach (var d in this) d.DeleteGroups();
        }

        /// <summary>
        ///     Returns an object of the Dimension type with a unique name passed as the
        ///     parameter or null, if there's no such object in the collection.
        /// </summary>
        public Dimension Find(string UniqueName)
        {
            foreach (var d in this)
                if (d.FUniqueName == UniqueName) return d;
            return null;
        }

        /// <summary>
        ///     Returns an object of the Dimension type with a display name passed as the
        ///     parameter or null, if there's no such object in the collection.
        /// </summary>
        public Dimension FindByDisplayName(string DisplayName)
        {
            foreach (var d in this)
                if (string.Compare(d.DisplayName, DisplayName, StringComparison.CurrentCultureIgnoreCase) ==
                    0) return d;
            return null;
        }

        /// <summary>
        ///     Returns an object of the Hierarchy type with the UniqueName passed as the
        ///     parameter.
        /// </summary>
        public Hierarchy FindHierarchy(string UniqueName)
        {
            var un = UniqueName.ToLower();
            foreach (var d in this)
            foreach (var h in d.Hierarchies)
            {
                if (h.FUniqueName == UniqueName)
                    return h;
                if (un == ("[" + d.DisplayName + "].[" + h.DisplayName + "]").ToLower())
                    return h;
            }
            return null;
        }

        /// <summary>
        ///     <para></para>
        ///     <para>
        ///         Returns an object of the Hierarchy type with the display name passed as the
        ///         parameter.
        ///     </para>
        /// </summary>
        /// <param name="HierarchyName">
        ///     The <see cref="THierarchy.DisplayName">display name</see> of the hierarchy which is looking
        ///     for.
        /// </param>
        public Hierarchy FindHierarchyByDisplayName(string HierarchyName)
        {
            foreach (var dim in this)
            {
                var res = dim.Hierarchies.FindByDisplayName(HierarchyName);
                if (res != null) return res;
            }
            return null;
        }

        internal Level FindLevel(string uniqueName)
        {
            var un = uniqueName.ToLower();
            foreach (var d in this)
            foreach (var h in d.Hierarchies)
            {
                if (h.Levels == null || h.Levels.Count == 0)
                    continue;
                foreach (var l in h.Levels)
                {
                    if (l.UniqueName == uniqueName)
                        return l;
                    if (un == (
                            "[" + d.DisplayName +
                            "].[" + h.DisplayName +
                            "].[" + l.DisplayName + "]").ToLower())
                        return l;
                }
            }
            foreach (var d in this)
            foreach (var h in d.Hierarchies)
                if (h.CubeHierarchy.FMDXLevelNames.Contains(uniqueName))
                    return h.Levels[0];
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
                        var d = new Dimension(FGrid);
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