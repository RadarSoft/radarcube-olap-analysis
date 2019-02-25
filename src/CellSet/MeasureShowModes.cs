using System;
using System.Collections.Generic;
using System.IO;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Layout;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.CellSet
{
    /// <summary>The list of display modes for measures</summary>
    public class MeasureShowModes : List<MeasureShowMode>, IStreamedObject
    {
        private readonly Measure fMeasure;

        internal MeasureShowModes(Measure AMeasure)
        {
            fMeasure = AMeasure;
            RestoreDefaults();
        }

        /// <summary>
        ///     Finds the measure show mode.
        /// </summary>
        /// <param name="caption">The display mode's caption</param>
        public MeasureShowMode Find(string caption)
        {
            foreach (var m in this)
                if (m.Caption == caption) return m;
            return null;
        }

        /// <summary>
        ///     Finds the measure show mode.
        /// </summary>
        /// <param name="type">A diaplay mode type</param>
        /// <returns></returns>
        public MeasureShowMode Find(MeasureShowModeType type)
        {
            foreach (var m in this)
                if (m.Mode == type) return m;
            return null;
        }

        internal MeasureShowMode ShowModeById(Guid guid)
        {
            foreach (var m in this)
                if (m.fUniqueName == guid) return m;
            return null;
        }

        public MeasureShowMode FirstVisibleMode
        {
            get
            {
                foreach (var m in this)
                    if (m.Visible) return m;
                return null;
            }
        }

        public IList<MeasureShowMode> NativeModes
        {
            get
            {
                var l = new List<MeasureShowMode>();
                foreach (var m in this)
                    if (fMeasure.FGrid.FEngine.IsNativeDataPresent(m))
                        l.Add(m);
                return l.AsReadOnly();
            }
        }

        internal int CountVisible
        {
            get
            {
                var c = 0;
                for (var i = 0; i < Count; i++)
                    if (this[i].Visible) c++;
                return c;
            }
        }

        /// <summary>
        ///     Adds a new display mode to the list. This only can be called from
        ///     OlapGrid.OnInitMeasures event handler.
        /// </summary>
        public MeasureShowMode Add(string ModeCaption)
        {
            return Add(ModeCaption, null);
        }

        /// <summary>
        ///     Adds a new display mode to the list. This method can be called only from
        ///     OlapGrid.OnInitMeasures event handler.
        /// </summary>
        public MeasureShowMode Add(string ModeCaption, string BitmapURL)
        {
            var m = new MeasureShowMode(fMeasure, ModeCaption,
                MeasureShowModeType.smSpecifiedByEvent, BitmapURL);

            Add(m);
            return m;
        }

        /// <summary>
        ///     Adds a new display mode in a certain position of the display mode list.
        /// </summary>
        /// <remarks>
        ///     The order of the display modes in the context menu coincides with their order in
        ///     the ShowModes list
        /// </remarks>
        public MeasureShowMode Insert(int Index, string ModeCaption, string BitmapURL)
        {
            var m = new MeasureShowMode(fMeasure, ModeCaption,
                MeasureShowModeType.smSpecifiedByEvent, BitmapURL);

            Insert(Index, m);
            return m;
        }

        public void Remove(string Caption)
        {
            var m = Find(Caption);
            if (m != null) Remove(m);
        }

        /// <summary>
        ///     Restores the standard display modes by deleting all the changes added by a
        ///     programmer.
        /// </summary>
        public void RestoreDefaults()
        {
            Clear();
            if (fMeasure.IsKPI)
            {
                Add(new MeasureShowMode(fMeasure, RadarUtils.GetResStr("rssmKPIValue"),
                    MeasureShowModeType.smKPIValue, null));

                Add(new MeasureShowMode(fMeasure, RadarUtils.GetResStr("rssmKPIGoal"),
                    MeasureShowModeType.smKPIGoal, null));

                Add(new MeasureShowMode(fMeasure, RadarUtils.GetResStr("rssmKPIStatus"),
                    MeasureShowModeType.smKPIStatus, null));

                Add(new MeasureShowMode(fMeasure, RadarUtils.GetResStr("rssmKPITrend"),
                    MeasureShowModeType.smKPITrend, null));

                Add(new MeasureShowMode(fMeasure, RadarUtils.GetResStr("rssmKPIWeight"),
                    MeasureShowModeType.smKPIWeight, null));
                for (var i = 0; i < 4; i++) this[i].Visible = true;
            }
            else
            {
                Add(new MeasureShowMode(fMeasure, RadarUtils.GetResStr("rssmNormal"),
                    MeasureShowModeType.smNormal, null));

                Add(new MeasureShowMode(fMeasure, RadarUtils.GetResStr("rssmPercentRowTotal"),
                    MeasureShowModeType.smPercentRowTotal, null));

                Add(new MeasureShowMode(fMeasure, RadarUtils.GetResStr("rssmPercentColTotal"),
                    MeasureShowModeType.smPercentColTotal, null));

                Add(new MeasureShowMode(fMeasure, RadarUtils.GetResStr("rssmPercentParentRowItem"),
                    MeasureShowModeType.smPercentParentRowItem, null));

                Add(new MeasureShowMode(fMeasure, RadarUtils.GetResStr("rssmPercentParentColItem"),
                    MeasureShowModeType.smPercentParentColItem, null));

                Add(new MeasureShowMode(fMeasure, RadarUtils.GetResStr("rssmRowRank"),
                    MeasureShowModeType.smRowRank, null));

                Add(new MeasureShowMode(fMeasure, RadarUtils.GetResStr("rssmColumnRank"),
                    MeasureShowModeType.smColumnRank, null));

                Add(new MeasureShowMode(fMeasure, RadarUtils.GetResStr("rssmPercentGrandTotal"),
                    MeasureShowModeType.smPercentGrandTotal, null));
                this[0].Visible = true;
            }
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteList(writer, this);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgList);
            Clear();
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgList_Count:
                        var c = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgList_Item:
                        var m = new MeasureShowMode(fMeasure);
                        // BeforeRead
                        StreamUtils.ReadStreamedObject(reader, m);
                        // AfterRead
                        Add(m);
                        break;
                    case Tags.tgList_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        #endregion
    }
}