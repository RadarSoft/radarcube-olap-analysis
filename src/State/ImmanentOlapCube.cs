using RadarSoft.RadarCube.Controls.Cube.Md;

namespace RadarSoft.RadarCube.State
{
    internal class ImmanentOlapCube : ImmanentCube
    {
        private string ConnectionString;
        private string CubeName;
        private MOlapCube.RadarCellset rcs;

        internal override void FromCube(Controls.Cube.RadarCube cube)
        {
            base.FromCube(cube);
            var c = (MOlapCube) cube;
            rcs = c.rcs;
            c.rcs = null;

            ConnectionString = c.ConnectionString;
            CubeName = c.CubeName;
        }

        internal override void ToCube(Controls.Cube.RadarCube cube)
        {
            base.ToCube(cube);
            var c = (MOlapCube) cube;
            c.rcs = rcs;
            rcs = null;

            c.ConnectionString = ConnectionString;
            c.CubeName = CubeName;
        }
    }
}