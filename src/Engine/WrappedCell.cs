using System;
using RadarSoft.XmlaClient.Metadata;

namespace RadarSoft.RadarCube.Engine
{
    internal class WrappedCell
    {
        private const string _error = "#ERROR!";
        private readonly Cell fCell;

        internal WrappedCell(Cell c)
        {
            fCell = c;
        }

        public object Value
        {
            get
            {
                try
                {
                    return fCell.Value;
                }
                catch (Exception e)
                {
                    return _error + e.Message;
                }
            }
        }

        public string FormattedValue
        {
            get
            {
                try
                {
                    return fCell.FormattedValue;
                }
                catch
                {
                    return _error;
                }
            }
        }

        public CellPropertyCollection CellProperties => fCell.CellProperties;
    }
}