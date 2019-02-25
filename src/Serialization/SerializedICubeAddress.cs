using System.ComponentModel;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Serialization
{
    /// <exclude />
    public class SerializedICubeAddress
    {
        public string[] Levels;

        [DefaultValue("")] public string MeasureDisplayName = "";

        [DefaultValue("")] public string MeasureMode = "";

        [DefaultValue("")] public string MeasureUniqueName = "";

        public string[] Members;

        [DefaultValue(0)] public int Tag;

        public SerializedICubeAddress()
        {
        }

        public SerializedICubeAddress(ICubeAddress address)
        {
            if (address.Measure != null)
            {
                MeasureDisplayName = address.Measure.DisplayName;
                MeasureUniqueName = address.Measure.UniqueName;
            }

            if (address.MeasureMode != null)
                MeasureMode = address.MeasureMode.Caption;

            if (address.LevelsCount > 0)
            {
                Levels = new string[address.LevelsCount];
                Members = new string[address.LevelsCount];

                for (var i = 0; i < address.LevelsCount; i++)
                {
                    Levels[i] = address.Levels(i).UniqueName;
                    Members[i] = address.Members(i).UniqueName;
                }
            }

            Tag = address.Tag;
        }

        public ICubeAddress GetCubeAddress(OlapControl grid)
        {
            var a = new ICubeAddress(grid);
            if (!string.IsNullOrEmpty(MeasureUniqueName))
            {
                a.Measure = grid.Measures.Find(MeasureUniqueName);
                if (a.Measure == null)
                    a.Measure = grid.Measures.FindByDisplayName(MeasureDisplayName);

                if (a.Measure != null)
                    a.MeasureMode = a.Measure.ShowModes.Find(MeasureMode);
            }

            if (Levels != null)
                for (var i = 0; i < Levels.Length; i++)
                {
                    var l = grid.Dimensions.FindLevel(Levels[i]);
                    Member m = null;
                    if (l != null)
                        m = l.FindMember(Members[i]);
                    if (m != null) a.AddMember(m);
                }

            a.Tag = Tag;

            return a;
        }
    }
}