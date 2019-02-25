using System;
using System.Diagnostics;
using System.IO;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Interfaces;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>Contains information about the measure display mode</summary>
    /// <remarks>
    ///     The custom calculation algorithm for display modes should be described in the
    ///     OlapGrid.OnShowMeasure event handler.
    /// </remarks>
    public class MeasureShowMode : IStreamedObject, IDescriptionable
    {
        internal string _DotUniqueName;
        internal string _UniqueName;

        internal Intelligence fIntelligence = null;
        internal Guid fUniqueName = Guid.NewGuid();
        internal bool fVisible;

        internal MeasureShowMode(Measure AMeasure, string ACaption, MeasureShowModeType AMode,
            string ABitmapURL)
            : this(AMeasure)
        {
            Caption = ACaption;
            Mode = AMode;

            BitmapURL = ABitmapURL;
        }

        internal MeasureShowMode(Measure AMeasure)
        {
            Measure = AMeasure;

            if (_UniqueName.IsNullOrEmpty())
            {
                _UniqueName = fUniqueName.ToString();
                _DotUniqueName = "." + _UniqueName;
            }
        }

        /// <summary>References to the measure the given display mode is specified for.</summary>
        public Measure Measure { get; }

        /// <summary>
        ///     Measure display mode caption displayed in the context menu.
        /// </summary>
        public string Caption { get; private set; }

        /// <summary>
        ///     The display mode type.
        /// </summary>
        /// <remarks>
        ///     For custom display modes the value of this property always equals
        ///     smSpecifiedByEvent.
        /// </remarks>
        public MeasureShowModeType Mode { get; private set; }

        /// <summary>A path to the icon in the context menu item.</summary>
        public string BitmapURL { get; private set; }

        /// <summary>
        ///     Indicates whether the current display mode for the specified measure in the
        ///     current OLAP slice is visible.
        /// </summary>
        public bool Visible
        {
            get { return fVisible; }
            set
            {
                fVisible = value;
                var m = Measure.Member;

                // WF, WEB ok, WPF ?
                var oldValueSortingDirection = new ValueSortingDirection();

                CellSet.CellSet cellSet = null;
                if (Measure != null && Measure.Grid != null)
                    cellSet = Measure.Grid.CellSet;

                Member lastMemberM = null;
                Member lastMeasureM = null;
                Member lastModeM = null;
                Level lastTotalLevel = null;

                if (cellSet != null && cellSet.FixedRows > 0 &&
                    cellSet.ValueSortedColumn >= cellSet.FixedColumns &&
                    cellSet.ValueSortedColumn < cellSet.ColumnCount)
                {
                    oldValueSortingDirection = cellSet.ValueSortingDirection;

                    var i = cellSet.FixedRows;
                    while (i > 0)
                    {
                        i--;
                        var gc = cellSet.Cells(cellSet.ValueSortedColumn, i);
                        if (gc != null)
                        {
                            var cell = gc as IMemberCell;
                            if (cell != null)
                                if (cell.Member != null)
                                    switch (cell.Member.MemberType)
                                    {
                                        case MemberType.mtMeasure:
                                            lastMeasureM = cell.Member;
                                            break;
                                        case MemberType.mtMeasureMode:
                                            lastModeM = cell.Member;
                                            break;
                                        default:
                                            if (lastMemberM == null)
                                                lastMemberM = cell.Member;
                                            break;
                                    }
                                else if (cell.IsTotal && cell.Level != null)
                                    lastTotalLevel = cell.Level.Level;
                        }
                    }
                }

                if (m != null)
                    m.Children[Measure.FShowModes.IndexOf(this)].FVisible = value;

                if (fIntelligence != null && !value)
                    fIntelligence.RemoveMeasure(Measure);

                if (Measure.Grid.Active && !Measure.Grid.IsUpdating && Measure.Grid.CellSet != null)
                    Measure.Grid.CellSet.Rebuild();

                if (cellSet != null)
                {
                    IMemberCell gcell = null;
                    if (lastMemberM != null)
                    {
                        gcell = cellSet.FindMember(lastMemberM);

#if DEBUG
                        if (gcell == null)
                            DebugLogging.WriteLine("MeasureShowMode.Visible_set({0}) - gcell == null", value);
#endif
                        Debug.Assert(gcell != null, "gcell != null");

                        var ims = gcell.StartColumn;
                        var imc = ims + gcell.ColSpan;

                        for (var i = ims; i < imc; i++)
                        {
                            var gc = cellSet.Cells(i, cellSet.FixedRows - 1);
                            if (gc != null)
                            {
                                var ccell = gc as IMemberCell;
                                if (ccell != null && ccell.Member != null)
                                {
                                    Member cmember;
                                    Member cmeasurem;
                                    Member cmodem;
                                    Level clevel;
                                    GetCSCellsM(cellSet, i, out cmember, out cmeasurem, out cmodem, out clevel);
                                    if (cmember == lastMemberM &&
                                        (cmeasurem == lastMeasureM || cmeasurem == null || lastMeasureM == null) &&
                                        (cmodem == lastModeM || cmodem == null || lastModeM == null) &&
                                        (clevel == lastTotalLevel || clevel == null || lastTotalLevel == null))
                                    {
                                        gcell = ccell;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else if (lastTotalLevel != null)
                    {
                        var ims = cellSet.FixedColumns;
                        var imc = cellSet.ColumnCount;
                        for (var i = ims; i < imc; i++)
                        {
                            var gc = cellSet.Cells(i, cellSet.FixedRows - 1);
                            if (gc != null)
                            {
                                var ccell = gc as IMemberCell;
                                if (ccell != null)
                                {
                                    Member cmember;
                                    Member cmeasurem;
                                    Member cmodem;
                                    Level clevel;
                                    GetCSCellsM(cellSet, i, out cmember, out cmeasurem, out cmodem, out clevel);
                                    if ((cmember == lastMemberM || cmember == null || lastMemberM == null) &&
                                        (cmeasurem == lastMeasureM || cmeasurem == null || lastMeasureM == null) &&
                                        (cmodem == lastModeM || cmodem == null || lastModeM == null) &&
                                        clevel == lastTotalLevel)
                                        gcell = ccell;
                                }
                            }
                        }
                    }
                    else if (lastModeM != null)
                    {
                        gcell = cellSet.FindMember(lastModeM);
                        if (gcell == null && lastMeasureM != null)
                            gcell = cellSet.FindMember(lastMeasureM);
                    }
                    else if (lastMeasureM != null)
                    {
                        gcell = cellSet.FindMember(lastMeasureM);
                        if (gcell == null && lastModeM != null)
                            gcell = cellSet.FindMember(lastModeM);
                    }

                    if (gcell != null)
                    {
                        cellSet.Grid.BeginUpdate();
                        cellSet.ValueSortingDirection = oldValueSortingDirection;
                        cellSet.ValueSortedColumn = gcell.StartColumn;
                        cellSet.Grid.EndUpdate();
                    }
                }
            }
        }

        public Intelligence LinkedIntelligence => fIntelligence;

        private static void GetCSCellsM(CellSet.CellSet cellSet, int col, out Member member, out Member measure,
            out Member mode, out Level level)
        {
            member = null;
            measure = null;
            mode = null;
            level = null;
            var i = cellSet.FixedRows;
            while (i > 0)
            {
                i--;
                var gc = cellSet.Cells(col, i);
                if (gc != null)
                {
                    var cell = gc as IMemberCell;
                    if (cell != null)
                        if (cell.Member != null)
                        {
                            if (cell.Member.MemberType == MemberType.mtMeasure)
                                measure = cell.Member;
                            else if (cell.Member.MemberType == MemberType.mtMeasureMode)
                                mode = cell.Member;
                            else if (member == null)
                                member = cell.Member;
                        }
                        else if (cell.IsTotal && cell.Level != null)
                        {
                            if (level == null)
                                level = cell.Level.Level;
                        }
                }
            }
        }

        public override string ToString()
        {
            return string.Format("Caption = \"{0}\"", Caption);
        }


        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgMeasureShowMode);

            StreamUtils.WriteTag(writer, Tags.tgMeasureShowMode_Mode);
            StreamUtils.WriteInt32(writer, (int) Mode);

            if (fVisible)
                StreamUtils.WriteTag(writer, Tags.tgMeasureShowMode_Visible);

            StreamUtils.WriteTag(writer, Tags.tgMeasureShowMode_Caption);
            StreamUtils.WriteString(writer, Caption);

            StreamUtils.WriteTag(writer, Tags.tgMeasureShowMode_UniqueName);
            StreamUtils.WriteGuid(writer, fUniqueName);

            if (!string.IsNullOrEmpty(BitmapURL))
            {
                StreamUtils.WriteTag(writer, Tags.tgMeasureShowMode_BitmapUrl);
                StreamUtils.WriteString(writer, BitmapURL);
            }

            StreamUtils.WriteTag(writer, Tags.tgMeasureShowMode_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgMeasureShowMode);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgMeasureShowMode_BitmapUrl:
                        BitmapURL = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgMeasureShowMode_Caption:
                        Caption = StreamUtils.ReadString(reader);
                        break;
                    case Tags.tgMeasureShowMode_Mode:
                        Mode = (MeasureShowModeType) StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgMeasureShowMode_Visible:
                        fVisible = true;
                        break;
                    case Tags.tgMeasureShowMode_UniqueName:
                        fUniqueName = StreamUtils.ReadGuid(reader);
                        _UniqueName = fUniqueName.ToString();
                        _DotUniqueName = "." + fUniqueName;
                        break;
                    case Tags.tgMeasureShowMode_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        #endregion

        #region IDescriptionable Members

        string IDescriptionable.DisplayName => Caption;

        string IDescriptionable.Description => "";

        string IDescriptionable.UniqueName => UniqueName;

        public string UniqueName => _UniqueName;

        internal string DotUniqueName => _DotUniqueName;

        #endregion
    }
}