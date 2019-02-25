using System;
using System.ComponentModel;
using System.IO;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.CubeStructure
{
    /// <summary>Represents the Cube dimension responsible for its structure.</summary>
    /// <remarks>
    ///     <para>
    ///         For users, dimensions organize data with relation to the area of interest,
    ///         such as Customers, Stores, or Employees. Dimensions contain attributes that
    ///         correspond to columns in dimension tables. These attributes appear as attribute
    ///         hierarchies and can be organized into multilevel hierarchical structures.
    ///     </para>
    ///     <para>
    ///         Objects of this class are responsible for the information specific for the
    ///         structure of the Cube. Data stored by instances of this class remains unchangeable
    ///         while working with the active Cube.
    ///     </para>
    /// </remarks>
    ////[Serializable]
    public class CubeDimension : IStreamedObject, IDescriptionable
    {
        [NonSerialized] internal Controls.Cube.RadarCube FCube;

        internal string FDescription = string.Empty;
        internal DimensionType fDimensionType = DimensionType.dtNormal;
        internal string FDisplayName;
        internal string FKeyHierarchyName;
        private string FUniqueName = string.Empty;

        /// <summary>
        ///     Creates the instance of CubeDimension type
        /// </summary>
        public CubeDimension()
        {
            Hierarchies = new CubeHierarchies(this);
        }

        public CubeDimension(Controls.Cube.RadarCube ACube)
            : this()
        {
            Init(ACube);
        }

        /// <summary>
        ///     Returns True if this dimension is a time dimension.
        /// </summary>
        [Browsable(false)]
        public bool IsTimeDimension => fDimensionType == DimensionType.dtTime;

        /// <summary>
        ///     <strong>Read only</strong>. References to the instance of the RadarCube
        ///     containing the specified dimension.
        /// </summary>
        [Browsable(false)]
        public Controls.Cube.RadarCube Cube => FCube;

        /// <summary>
        ///     Specifies a dimension caption for an end-user.
        /// </summary>
        [Description("Specifies a dimension caption for an end-user.")]
        [Category("Appearance")]
        [NotifyParentProperty(true)]
        public string DisplayName
        {
            get => FDisplayName;
            set => FDisplayName = value;
        }

        /// <summary>
        ///     Description of the dimension.
        /// </summary>
        /// <remarks>
        ///     Text assigned to this property will pop up as a tooltip when pointing the cursor
        ///     at a dimension.
        /// </remarks>
        [Description("Description of the dimension."),
        Category("Appearance"), DefaultValue(""),
        NotifyParentProperty(true)]
        public string Description
        {
            get { return FDescription; }
            set { FDescription = value; }
        }

        /// <summary>
        ///     Contains a unique string dimension identifier.
        /// </summary>
        /// <remarks>
        ///     Text assigned to this property will pop up as a tooltip when pointing the cursor
        ///     at a dimension.
        /// </remarks>
        [Description("Contains a unique string dimension identifier"),
        Category("Behavior")]
        public string UniqueName
        {
            get
            {
                if (!string.IsNullOrEmpty(FUniqueName)) return FUniqueName;
                if (!string.IsNullOrEmpty(FDisplayName))
                    FUniqueName = "[" + FDisplayName + "]";
                else
                    FUniqueName = Guid.NewGuid().ToString();
                return FUniqueName;
            }
            set { FUniqueName = value; }
        }

        /// <summary>A collection of hierarchies that belong to the specified dimension.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
        Category("Behavior"),
        Description("A collection of hierarchies that belong to the specified dimension.")]
        public CubeHierarchies Hierarchies { get; }

        internal void ClearMembers()
        {
            foreach (var h in Hierarchies)
            {
                h.FCubeLevels.Clear();
                h.FMDXLevelNames.Clear();
            }
        }

        /// <summary>
        ///     Returns an unique name of the dimension
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return UniqueName;
        }

        internal void Init(Controls.Cube.RadarCube ACube)
        {
            FCube = ACube;
        }


        internal CubeHierarchy FindByDataTable(string p, string p_2)
        {
            foreach (var h in Hierarchies)
                if (h.FDataTable == p && h.FDisplayField == p_2)
                    return h;
            return null;
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgCubeDimension);

            StreamUtils.WriteTag(writer, Tags.tgCubeDimension_DisplayName);
            StreamUtils.WriteString(writer, FDisplayName);

            StreamUtils.WriteTag(writer, Tags.tgCubeDimension_UniqueName);
            StreamUtils.WriteString(writer, FUniqueName);

            if (!string.IsNullOrEmpty(FDescription))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeDimension_Description);
                StreamUtils.WriteString(writer, FDescription);
            }

            if (!string.IsNullOrEmpty(FKeyHierarchyName))
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeDimension_KeyHierName);
                StreamUtils.WriteString(writer, FKeyHierarchyName);
            }

            if (fDimensionType != DimensionType.dtNormal)
            {
                StreamUtils.WriteTag(writer, Tags.tgCubeDimension_DimensionType);
                StreamUtils.WriteInt32(writer, (int) fDimensionType);
            }

            StreamUtils.WriteStreamedObject(writer, Hierarchies, Tags.tgCubeDimension_Hierarchies);

            StreamUtils.WriteTag(writer, Tags.tgCubeDimension_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgCubeDimension);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgCubeDimension_Description:
                        FDescription = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeDimension_KeyHierName:
                        FKeyHierarchyName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeDimension_DimensionType:
                        fDimensionType = (DimensionType) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgCubeDimension_DisplayName:
                        FDisplayName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeDimension_Hierarchies:
                        StreamUtils.ReadStreamedObject(reader, Hierarchies);
                        break;
                    case Tags.tgCubeDimension_UniqueName:
                        FUniqueName = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgCubeDimension_EOT:
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