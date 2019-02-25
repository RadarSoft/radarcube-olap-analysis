using System.Collections.Generic;
using System.Data;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.Interfaces
{
    internal interface IArrayDrilltrough
    {
        void Drillthrough(IList<ICubeAddress> addresses, IList<Measure> measures, DataTable dataTable, int rowsToFetch, ICollection<string> columns, DrillThroughMethod drillThroughMethod);
    }
}