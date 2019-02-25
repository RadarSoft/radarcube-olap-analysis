using System;
using System.Collections.Generic;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.State
{
    internal class ImmanentGrid
    {
        private CellSet.CellSet Cellset;
        private Dimensions Dimensions;
        private Engine.Engine Engine;
        private List<Hierarchy> FilteredHierarchies;
        private List<Level> FilteredHierarchies2;
        private AxesLayout Layout;
        private Measures Measures;
        private Guid MetadataGuid;
        private OlapGridMode Mode;

        internal virtual void FromGrid(OlapControl grid)
        {
            Mode = grid.FMode;
            Measures = grid.Measures;
            grid.fMeasures = null;
            Measures.FGrid = null;
            if (Measures.FLevel != null)
                Measures.FLevel.PagerSettings.FGrid = null;
            foreach (var m in Measures)
            {
                m.FGrid = null;
                m.FCubeMeasure = null;
            }
            Dimensions = grid.Dimensions;
            grid.FDimensions = null;
            Dimensions.FGrid = null;
            foreach (var d in Dimensions)
            {
                d.FGrid = null;
                d.FCubeDimension = null;
                foreach (var h in d.Hierarchies)
                {
                    h.FCubeHierarchy = null;
                    if (h.Levels != null)
                        foreach (var l in h.Levels)
                        {
                            l.FCubeLevel = null;
                            l.PagerSettings.FGrid = null;
                            foreach (var m in l.Members)
                                m.FCubeMember = null;
                        }
                }
            }
            Layout = grid.FLayout;
            grid.FLayout = null;
            Layout.fGrid = null;
            Engine = grid.Engine;
            grid.FEngine = null;
            if (Engine != null)
            {
                Engine.FCube = null;
                Engine.FGrid = null;
                foreach (var ml in Engine.FMetaLines.Values)
                {
                    ml.FGrid = null;
                    foreach (var ll in ml.fLines.Values)
                        ll.DoDeserialize(); // removes the lines data to cache 
                }
            }

            Cellset = grid.FCellSet;
            grid.FCellSet = null;
            if (Cellset != null)
            {
                Cellset.FGrid = null;
                if (Cellset.FSortingAddress != null)
                    Cellset.FSortingAddress.FGrid = null;
                foreach (var a in Cellset.fComments.Keys)
                    a.FGrid = null;
            }
            FilteredHierarchies = grid.FFilteredHierarchies;
            FilteredHierarchies2 = grid.FFilteredLevels;
            grid.FFilteredHierarchies = null;
            grid.FFilteredLevels = null;
        }

        internal virtual void ToGrid(OlapControl grid)
        {
            var cube = grid.Cube;
            grid.FMode = Mode;
            grid.fMeasures = Measures;
            Measures.FGrid = grid;
            Measures.FLevel.PagerSettings.FGrid = grid;

            foreach (var m in Measures)
            {
                m.FGrid = grid;
                m.FCubeMeasure = cube.Measures.Find(m.UniqueName);
            }
            Measures = null;

            grid.FDimensions = Dimensions;
            Dimensions.FGrid = grid;
            foreach (var d in Dimensions)
            {
                d.FGrid = grid;
                d.FCubeDimension = cube.Dimensions.Find(d.UniqueName);
                foreach (var h in d.Hierarchies)
                {
                    h.FCubeHierarchy = d.FCubeDimension.Hierarchies.Find(h.UniqueName);
                    if (h.Levels != null)
                        for (var i = 0; i < h.Levels.Count; i++)
                        {
                            var l = h.Levels[i];
                            l.FCubeLevel = h.CubeHierarchy.Levels[i];
                            l.PagerSettings.FGrid = grid;
                            foreach (var m in l.Members)
                                m.FCubeMember = l.CubeLevel.Members.Find(m.UniqueName);
                        }
                }
            }
            Dimensions = null;

            grid.FLayout = Layout;
            Layout.fGrid = grid;
            Layout = null;

            grid.FEngine = Engine;
            if (Engine != null)
            {
                Engine.FGrid = grid;
                Engine.FCube = cube;
            }
            if (cube != null)
            {
                cube.FEngineList.RemoveAll(item => item.FGrid == grid);
                cube.FEngineList.Add(Engine);
            }
            if (Engine != null)
            {
                foreach (var ml in Engine.FMetaLines.Values)
                    ml.FGrid = grid;
                Engine = null;
            }

            grid.FCellSet = Cellset;
            if (Cellset != null)
            {
                Cellset.FGrid = grid;
                if (Cellset.FSortingAddress != null)
                    Cellset.FSortingAddress.FGrid = grid;
                foreach (var a in Cellset.fComments.Keys)
                    a.FGrid = grid;
                Cellset = null;
            }

            grid.FFilteredHierarchies = FilteredHierarchies;
            grid.FFilteredLevels = FilteredHierarchies2;
            FilteredHierarchies = null;
            FilteredHierarchies2 = null;
        }
    }
}