using System.Collections.Generic;
using RadarSoft.RadarCube.CubeStructure;

namespace RadarSoft.RadarCube.State
{
    internal class ImmanentCube
    {
        internal bool Active;
        private CubeDimensions Dimensions;
        private List<CubeLevel> LevelsList;
        private CubeMeasures Measures;

        internal virtual void FromCube(Controls.Cube.RadarCube cube)
        {
            Dimensions = cube.Dimensions;
            cube.frcDimensions = null;
            Dimensions.FCube = null;
            foreach (var d in Dimensions)
                d.FCube = null;

            Measures = cube.Measures;
            cube.frcMeasures = null;
            Measures.FCube = null;
            foreach (var m in Measures)
                m.FCube = null;

            Active = cube.FActive;
            LevelsList = cube.FLevelsList;
            cube.FLevelsList = null;
            cube.FEngineList = null;
        }

        internal virtual void ToCube(Controls.Cube.RadarCube cube)
        {
            cube.frcDimensions = Dimensions;
            Dimensions = null;
            cube.Dimensions.FCube = cube;
            foreach (var d in cube.Dimensions)
                d.FCube = cube;

            cube.frcMeasures = Measures;
            Measures = null;
            cube.Measures.FCube = cube;
            foreach (var m in cube.Measures)
                m.FCube = cube;

            cube.FActive = Active;
            cube.FLevelsList = LevelsList;
            LevelsList = null;
        }
    }
}