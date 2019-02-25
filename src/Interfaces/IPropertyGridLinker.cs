using System.Collections.Generic;

namespace RadarSoft.RadarCube.Interfaces
{
    internal interface IPropertyGridLinker
    {
        string DataTable { get; set; }
        string DisplayField { get; set; }
        IDictionary<string, IList<string>> TableToIDFields { get; set; }
    }
}