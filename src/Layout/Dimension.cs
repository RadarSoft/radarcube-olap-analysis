using System.IO;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.CubeStructure;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>
    ///     Creates an instance of Dimension class
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In comparison to the CubeDimension class, Dimension has the Visible
    ///         property that defines the visible/invisible of a hierarchy group in the Cube
    ///         structure panel.
    ///     </para>
    /// </remarks>
    public class Dimension : IStreamedObject, IDescriptionable
    {
        internal Hierarchies FHierarchies;
        internal CubeDimension FCubeDimension;
#if DEBUG
        internal string _FUniqueName;
        internal string FUniqueName
        {
            get => _FUniqueName;
            set => _FUniqueName = value;
        }
#else
        internal string FUniqueName;
#endif

        internal bool FVisible = true;
        internal OlapControl FGrid;

        internal void RestoreAfterSerialization(OlapControl grid)
        {
            FGrid = grid;
            if (grid.Cube == null) return;
            FCubeDimension = grid.Cube.Dimensions.Find(FUniqueName);
            foreach (var h in Hierarchies) h.RestoreAfterSerialization();
        }

        internal void InitDimension(CubeDimension CubeDimension)
        {
            FCubeDimension = CubeDimension;
            FUniqueName = CubeDimension.UniqueName;
            foreach (var h in CubeDimension.Hierarchies)
            {
                var H = new Hierarchy(this);
                FHierarchies.Add(H);
                H.InitHierarchyProperties(h);
            }
        }

        internal void ClearMembers()
        {
            foreach (var h in FHierarchies)
            {
                if (h.FLevels != null)
                {
                    h.FLevels.Clear();
                    h.FLevels = null;
                    h.FFiltered = false;
                }
                h.FInitialized = false;
            }
        }

        /// <summary>Returns a unique name of the dimension</summary>
        public override string ToString()
        {
            return UniqueName;
        }

        /// <summary>
        ///     Removes all calculated members from every hierarchy in the dimension
        /// </summary>
        public void DeleteCalculatedMembers()
        {
            foreach (var h in FHierarchies) h.DeleteCalculatedMembers();
        }

        /// <summary>
        ///     Removes all groups from every hierarchy in the dimension
        /// </summary>
        public void DeleteGroups()
        {
            foreach (var h in FHierarchies) h.DeleteGroups();
        }

        /// <summary>
        ///     References to the corresponding object of the CubeDimension on the Cube
        ///     level.
        /// </summary>
        public CubeDimension CubeDimension => FCubeDimension;

        /// <summary>
        ///     A dimension name as it will be displayed for an end user.
        /// </summary>
        public string DisplayName => FCubeDimension != null ? FCubeDimension.FDisplayName : "";

        /// <summary>
        ///     Description of a dimension that appears as a pop-up window (tooltip) when the
        ///     cursor is pointed at the node with the dimention name in the Cube structure
        ///     panel.
        /// </summary>
        public string Description => FCubeDimension != null ? FCubeDimension.FDescription : "";

        /// <summary>
        ///     Reference to an object of the OlapControl type containing the specified
        ///     dimension.
        /// </summary>
        public OlapControl Grid => FGrid;

        /// <summary>
        ///     Creates an instance of Dimension class. Do not use this method in
        ///     applications.
        /// </summary>
        /// <param name="AGrid">Owner of the dimension</param>
        public Dimension(OlapControl AGrid)
        {
            FGrid = AGrid;
            FHierarchies = new Hierarchies(this);
        }

        /// <summary>Lists of hierarchies of the specified dimension.</summary>
        public Hierarchies Hierarchies => FHierarchies;

        /// <summary>A unique string dimension identifier.</summary>
        /// <remarks>Never visible to an end user.</remarks>
        public string UniqueName
        {
            get => FUniqueName;
            set => FUniqueName = value;
        }

        /// <summary>
        ///     <para>Defines whether the specified dimention is visible for an end user</para>
        /// </summary>
        public bool Visible
        {
            get => FVisible;
            set => FVisible = value;
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgDimension);

            if (!FVisible)
                StreamUtils.WriteTag(writer, Tags.tgDimension_NotVisible);

            StreamUtils.WriteTag(writer, Tags.tgDimension_UniqueName);
            StreamUtils.WriteString(writer, FUniqueName);

            StreamUtils.WriteStreamedObject(writer, FHierarchies, Tags.tgDimension_Hierarchies);

            StreamUtils.WriteTag(writer, Tags.tgDimension_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgDimension);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgDimension_Hierarchies:
                        StreamUtils.ReadStreamedObject(reader, FHierarchies);
                        break;
                    case Tags.tgDimension_NotVisible:
                        FVisible = false;
                        break;
                    case Tags.tgDimension_UniqueName:
                        FUniqueName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgDimension_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        #endregion

        #region IDescriptionable Members

        string IDescriptionable.DisplayName => DisplayName;

        string IDescriptionable.Description => Description;

        string IDescriptionable.UniqueName => UniqueName;

        #endregion
    }
}