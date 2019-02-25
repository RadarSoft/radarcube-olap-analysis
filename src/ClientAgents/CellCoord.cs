namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class CellCoord
    {
        public int CellIndex;
        public int Col;
        public int Row;

        public CellCoord()
        {
        }

        public CellCoord(int idx, int r, int c)
        {
            CellIndex = idx;
            Row = r;
            Col = c;
        }
    }
}