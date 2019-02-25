using RadarSoft.RadarCube.Controls;

namespace RadarSoft.RadarCube.Interfaces
{
    /// <exclude />
    public interface IMOlapCube
    {
        bool GetDatabasesList(string server, string restOfConnectionString, out string result);
        bool GetCubesList(string server, string database, string restOfConnectionString, out string result);
        string Activate(string server, string database, string cube, string restOfConnectionString);
        void ExecuteMDX(string query, OlapControl grid);
        void GetCurrentStatus(out string server, out string database, out string cube);
        string[] GetServersList();
    }
}