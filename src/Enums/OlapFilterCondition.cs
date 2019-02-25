namespace RadarSoft.RadarCube.Enums
{
    /// <summary>
    ///     Enumerates the possible context filter conditions.
    /// </summary>
    public enum OlapFilterCondition
    {
        /// <summary>
        ///     The caption, date or value equals the specified value.
        /// </summary>
        fcEqual = 1,

        /// <summary>
        ///     The caption, date or value doesn't equal the specified value.
        /// </summary>
        fcNotEqual = 2,

        /// <summary>
        ///     The member caption starts with the specified substring.
        /// </summary>
        fcStartsWith = 101,

        /// <summary>
        ///     The member caption doesn't start with the specified substring.
        /// </summary>
        fcNotStartsWith = 102,

        /// <summary>
        ///     The member caption ends with the specified substring.
        /// </summary>
        fcEndsWith = 103,

        /// <summary>
        ///     The member caption doesn't end with the specified substring.
        /// </summary>
        fcNotEndsWith = 104,

        /// <summary>
        ///     The member caption contains the specified substring.
        /// </summary>
        fcContains = 111,

        /// <summary>
        ///     The member caption doesn't contain the specified substring.
        /// </summary>
        fcNotContains = 112,

        /// <summary>
        ///     The caption, date or value is less than the specified value.
        /// </summary>
        fcLess = 11,

        /// <summary>
        ///     The caption, date or value isn't less than the specified value.
        /// </summary>
        fcNotLess = 12,

        /// <summary>
        ///     The caption, date or value is greater than the specified value.
        /// </summary>
        fcGreater = 13,

        /// <summary>
        ///     The caption, date or value isn't greater than the specified value.
        /// </summary>
        fcNotGreater = 14,

        /// <summary>
        ///     The caption, date or value is between the two specified values.
        /// </summary>
        fcBetween = 21,

        /// <summary>
        ///     The caption, date or value is out of range between the two specified values.
        /// </summary>
        fcNotBetween = 22,

        /// <summary>
        ///     Shows the first/last N members meeting the specified criteria.
        /// </summary>
        fcFirstTen = 201
    }
}