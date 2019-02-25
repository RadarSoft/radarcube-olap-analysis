using System;
using System.IO;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Serialization;

namespace RadarSoft.RadarCube.CellSet
{
    /// <summary>Determines the page view settings for the specified hierarchy level.</summary>
    [Serializable]
    public class PagerSettings : IStreamedObject
    {
        private bool? FAllowPaging;

        [NonSerialized] internal OlapControl FGrid;

        private int? FLinesInFrame;

        internal PagerSettings(bool allowPaging, int linesInFrame, OlapControl grid)
        {
            FAllowPaging = allowPaging;
            FLinesInFrame = linesInFrame;
            FGrid = grid;
        }

        /// <summary>Permits or prohibits a hierarchy page view mode for the specified level.</summary>
        /// <remarks>
        ///     <para>
        ///         Even if the page view mode is switched on for an individual level, it won't
        ///         operate in case the Grid's OlapControl.AllowPaging property is set to
        ///         False.
        ///     </para>
        /// </remarks>
        public bool AllowPaging
        {
            get
            {
                if (FAllowPaging.HasValue == false)
                    FGrid.UpdateLevelsPageState();
                return FAllowPaging.Value && FGrid.AllowPaging;
            }
            set => FAllowPaging = value;
        }

        /// <summary>
        ///     Determines a number of items in a "page" for the specified hierarchy
        ///     level.
        /// </summary>
        /// <remarks>
        ///     The property overrides the OlapControl.LinesInPage property. Thus, the number
        ///     of lines for an individual level may be different from the number set for the Grid's
        ///     page view mode.
        /// </remarks>
        public int LinesInPage
        {
            get
            {
                if (FLinesInFrame.HasValue == false)
                    FGrid.UpdateLevelsPageState();
                return FLinesInFrame.Value;
            }
            set => FLinesInFrame = value;
        }

        internal void InvalidatePagingState()
        {
            FAllowPaging = null;
            FLinesInFrame = null;
        }

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgPagerSettings);

            StreamUtils.WriteTag(writer, Tags.tgPagerSettings_AllowPaging);
            StreamUtils.WriteBoolean(writer, AllowPaging);

            StreamUtils.WriteTag(writer, Tags.tgPagerSettings_LinesInFrame);
            StreamUtils.WriteInt32(writer, LinesInPage);

            StreamUtils.WriteTag(writer, Tags.tgPagerSettings_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgPagerSettings);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgPagerSettings_AllowPaging:
                        FAllowPaging = StreamUtils.ReadBoolean(reader);
                        break;
                    case Tags.tgPagerSettings_LinesInFrame:
                        FLinesInFrame = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgPagerSettings_EOT:
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