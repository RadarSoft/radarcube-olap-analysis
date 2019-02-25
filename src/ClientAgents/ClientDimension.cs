using System.ComponentModel;
using System.Linq;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Layout;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class ClientDimension
    {
        [DefaultValue("")] public string Description = "";

        public string DisplayName;
        public ClientHierarchy[] Hierarchies;

        public ClientDimension()
        {
        }

        internal ClientDimension(Dimension d, OlapControl grid)
        {
            DisplayName = d.DisplayName;
            Description = d.Description;
            Hierarchies = d.Hierarchies.Where(item => item.Visible).Select(item => new ClientHierarchy(item, grid))
                .ToArray();
        }
    }
}