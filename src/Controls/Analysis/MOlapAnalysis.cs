using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using RadarSoft.RadarCube.Controls.Cube.Md;

namespace RadarSoft.RadarCube.Controls.Analysis
{
    public class MOlapAnalysis : OlapAnalysis
    {
        public MOlapAnalysis(HttpContext context, IHostingEnvironment hosting, IMemoryCache cache)
            : this(null, context, hosting, cache)
        {
        }

        public MOlapAnalysis(string id, HttpContext context, IHostingEnvironment hosting, IMemoryCache cache)
            : base(context, hosting, cache)
        {
            Cube = new MOlapCube(context, hosting, cache);
            ID = id;
        }

        public MOlapCube MCube => Cube as MOlapCube;

        public override string ID
        {
            get => base.ID;
            set
            {
                Cube.ID = "MOlapCube_" + value;
                base.ID = value;
            }
        }


        /// <summary>
        ///     Gets or sets the connection string to open an OLAP server connection.
        /// </summary>
        public override string ConnectionString
        {
            get => Cube.ConnectionString;
            set => Cube.ConnectionString = value;
        }

        /// <summary>
        ///     Allows the Grid to use the OLAP server cell color formatting option.
        /// </summary>
        public bool UseOlapServerColorFormatting
        {
            get => MCube.UseOlapServerColorFormatting;
            set => MCube.UseOlapServerColorFormatting = value;
        }

        /// <summary>
        ///     Allows the Grid to use the OLAP server cell font formatting option.
        /// </summary>
        public bool UseOlapServerFontFormatting
        {
            get => MCube.UseOlapServerFontFormatting;
            set => MCube.UseOlapServerFontFormatting = value;
        }

        /// <summary>Allows the use of the MDX subcube expressions.</summary>
        public bool DisableSubcubeFeatures
        {
            get => MCube.DisableSubcubeFeatures;
            set => MCube.DisableSubcubeFeatures = value;
        }

        /// <summary>Name of the current OLAP Cube.</summary>
        public string CubeName
        {
            get => Cube.CubeName;
            set => Cube.CubeName = value;
        }

        /// <summary>Indicates whether the Cube is active.</summary>
        public override bool Active
        {
            get => Cube.Active;
            set
            {
                if (value == Cube.Active)
                    return;

                Cube.Active = value;
                SetActive(value);
            }
        }
    }
}