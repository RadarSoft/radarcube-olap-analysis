using System;
using System.Collections.Generic;
using System.Text;

namespace RadarSoft.RadarCube.Tools
{
    internal class FileCacheDependency
    {
        internal FileCacheDependency(string filename)
        {
            FileName = filename;
        }

        internal string FileName { get; }
    }
}
