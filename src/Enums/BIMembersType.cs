namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Business intelligence type of hierarchy or hierarchy level members.
    /// </summary>
    public enum BIMembersType
    {
        /// <summary>
        ///     default type
        /// </summary>
        ltNone,

        /// <summary>
        ///     years
        /// </summary>
        ltTimeYear,

        /// <summary>
        ///     Half years
        /// </summary>
        ltTimeHalfYear,

        /// <summary>
        ///     Quarters
        /// </summary>
        ltTimeQuarter,

        /// <summary>
        ///     The full representaton of months
        /// </summary>
        ltTimeMonthLong,

        /// <summary>
        ///     The short representaton of months
        /// </summary>
        ltTimeMonthShort,

        /// <summary>
        ///     The number representaton of months
        /// </summary>
        ltTimeMonthNumber,

        /// <summary>
        ///     The week of year
        /// </summary>
        ltTimeWeekOfYear,

        /// <summary>
        ///     The day of year
        /// </summary>
        ltTimeDayOfYear,

        /// <summary>
        ///     day of the month
        /// </summary>
        ltTimeDayOfMonth,

        /// <summary>
        ///     day of the week
        /// </summary>
        ltTimeDayOfWeekLong,
        ltTimeDayOfWeekShort,

        /// <summary>
        ///     hours
        /// </summary>
        ltTimeHour,

        /// <summary>
        ///     minutes
        /// </summary>
        ltTimeMinute,

        /// <summary>
        ///     seconds
        /// </summary>
        ltTimeSecond
    }
}