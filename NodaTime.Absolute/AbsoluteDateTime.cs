using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NodaTime.Calendars;
using NodaTime.Extensions;
using PureAttribute = System.Diagnostics.Contracts.PureAttribute;

namespace NodaTime;

/// <summary>
/// An absolute date time, including timezone
/// </summary>
/// <remarks>
/// This is a wrapper around around <see cref="ZonedDateTime"/>
/// that allows full comparison (converting to instant for all comparisons)
/// </remarks>
[JsonConverter(typeof(AbsoluteDateTimeJsonConverter))]
[DebuggerDisplay($"{{{nameof(_value)}}}")]
[PublicAPI]
public readonly struct AbsoluteDateTime :
    IEquatable<AbsoluteDateTime>,
    IComparable<AbsoluteDateTime>,
    IEquatable<ZonedDateTime>,
    IComparable<ZonedDateTime>,
    IEquatable<Instant>,
    IComparable<Instant>
{
    private readonly ZonedDateTime _value;

    public AbsoluteDateTime(ZonedDateTime value) => _value = value;

    [Pure] public override string ToString() => _value.ToString();

    #region Properties

    /// <summary>Gets the offset of the local representation of this value from UTC.</summary>
    /// <value>The offset of the local representation of this value from UTC.</value>
    public Offset Offset => _value.Offset;

    /// <summary>Gets the time zone associated with this value.</summary>
    /// <value>The time zone associated with this value.</value>
    public DateTimeZone Zone => _value.Zone;

    /// <summary>
    /// Gets the local date and time represented by this zoned date and time.
    /// </summary>
    /// <remarks>
    /// The returned
    /// <see cref="NodaTime.LocalDateTime"/> will have the same calendar system and return the same values for
    /// each of the calendar properties (Year, MonthOfYear and so on), but will not be associated with any
    /// particular time zone.
    /// </remarks>
    /// <value>The local date and time represented by this zoned date and time.</value>
    public LocalDateTime LocalDateTime => _value.LocalDateTime;

    /// <summary>Gets the calendar system associated with this zoned date and time.</summary>
    /// <value>The calendar system associated with this zoned date and time.</value>
    public CalendarSystem Calendar => _value.Calendar;

    /// <summary>
    /// Gets the local date represented by this zoned date and time.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="LocalDate"/>
    /// will have the same calendar system and return the same values for each of the date-based calendar
    /// properties (Year, MonthOfYear and so on), but will not be associated with any particular time zone.
    /// </remarks>
    /// <value>The local date represented by this zoned date and time.</value>
    public LocalDate Date => _value.Date;

    /// <summary>
    /// Gets the time portion of this zoned date and time.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="LocalTime"/> will
    /// return the same values for each of the time-based properties (Hour, Minute and so on), but
    /// will not be associated with any particular time zone.
    /// </remarks>
    /// <value>The time portion of this zoned date and time.</value>
    public LocalTime TimeOfDay => _value.TimeOfDay;

    /// <summary>Gets the era for this zoned date and time.</summary>
    /// <value>The era for this zoned date and time.</value>
    public Era Era => _value.Era;

    /// <summary>Gets the year of this zoned date and time.</summary>
    /// <remarks>This returns the "absolute year", so, for the ISO calendar,
    /// a value of 0 means 1 BC, for example.</remarks>
    /// <value>The year of this zoned date and time.</value>
    public int Year => _value.Year;

    /// <summary>Gets the year of this zoned date and time within its era.</summary>
    /// <value>The year of this zoned date and time within its era.</value>
    public int YearOfEra => _value.YearOfEra;

    /// <summary>Gets the month of this zoned date and time within the year.</summary>
    /// <value>The month of this zoned date and time within the year.</value>
    public int Month => _value.Month;

    /// <summary>Gets the day of this zoned date and time within the year.</summary>
    /// <value>The day of this zoned date and time within the year.</value>
    public int DayOfYear => _value.DayOfYear;

    /// <summary>
    /// Gets the day of this zoned date and time within the month.
    /// </summary>
    /// <value>The day of this zoned date and time within the month.</value>
    public int Day => _value.Day;

    /// <summary>
    /// Gets the week day of this zoned date and time expressed as an <see cref="NodaTime.IsoDayOfWeek"/> value.
    /// </summary>
    /// <value>The week day of this zoned date and time expressed as an <c>IsoDayOfWeek</c> value.</value>
    public IsoDayOfWeek DayOfWeek => _value.DayOfWeek;

    /// <summary>
    /// Gets the hour of day of this zoned date and time, in the range 0 to 23 inclusive.
    /// </summary>
    /// <value>The hour of day of this zoned date and time, in the range 0 to 23 inclusive.</value>
    public int Hour => _value.Hour;

    /// <summary>
    /// Gets the hour of the half-day of this zoned date and time, in the range 1 to 12 inclusive.
    /// </summary>
    /// <value>The hour of the half-day of this zoned date and time, in the range 1 to 12 inclusive.</value>
    public int ClockHourOfHalfDay => _value.ClockHourOfHalfDay;

    /// <summary>
    /// Gets the minute of this zoned date and time, in the range 0 to 59 inclusive.
    /// </summary>
    /// <value>The minute of this zoned date and time, in the range 0 to 59 inclusive.</value>
    public int Minute => _value.Minute;

    /// <summary>
    /// Gets the second of this zoned date and time within the minute, in the range 0 to 59 inclusive.
    /// </summary>
    /// <value>The second of this zoned date and time within the minute, in the range 0 to 59 inclusive.</value>
    public int Second => _value.Second;

    /// <summary>
    /// Gets the millisecond of this zoned date and time within the second, in the range 0 to 999 inclusive.
    /// </summary>
    /// <value>The millisecond of this zoned date and time within the second, in the range 0 to 999 inclusive.</value>
    public int Millisecond => _value.Millisecond;

    /// <summary>
    /// Gets the tick of this zoned date and time within the second, in the range 0 to 9,999,999 inclusive.
    /// </summary>
    /// <value>The tick of this zoned date and time within the second, in the range 0 to 9,999,999 inclusive.</value>
    public int TickOfSecond => _value.TickOfSecond;

    /// <summary>
    /// Gets the tick of this zoned date and time within the day, in the range 0 to 863,999,999,999 inclusive.
    /// </summary>
    /// <remarks>
    /// This is the TickOfDay portion of the contained <see cref="_value"/>.
    /// On daylight saving time transition dates, it may not be the same as the number of ticks elapsed since the beginning of the day.
    /// </remarks>
    /// <value>The tick of this zoned date and time within the day, in the range 0 to 863,999,999,999 inclusive.</value>
    public long TickOfDay => _value.TickOfDay;

    /// <summary>
    /// Gets the nanosecond of this zoned date and time within the second, in the range 0 to 999,999,999 inclusive.
    /// </summary>
    /// <value>The nanosecond of this zoned date and time within the second, in the range 0 to 999,999,999 inclusive.</value>
    public int NanosecondOfSecond => _value.NanosecondOfSecond;

    /// <summary>
    /// Gets the nanosecond of this zoned date and time within the day, in the range 0 to 86,399,999,999,999 inclusive.
    /// </summary>
    /// <remarks>
    /// This is the NanosecondOfDay portion of the contained <see cref="_value"/>.
    /// On daylight saving time transition dates, it may not be the same as the number of nanoseconds elapsed since the beginning of the day.
    /// </remarks>
    /// <value>The nanosecond of this zoned date and time within the day, in the range 0 to 86,399,999,999,999 inclusive.</value>
    public long NanosecondOfDay => _value.NanosecondOfDay;

    #endregion

    #region Conversion

    /// <summary>
    /// Converts this value to the instant it represents on the time line.
    /// </summary>
    /// <remarks>
    /// This is always an unambiguous conversion. Any difficulties due to daylight saving
    /// transitions or other changes in time zone are handled when converting from a
    /// <see cref="NodaTime.LocalDateTime" /> to a <see cref="ZonedDateTime"/>; the <c>ZonedDateTime</c> remembers
    /// the actual offset from UTC to local time, so it always knows the exact instant represented.
    /// </remarks>
    /// <returns>The instant corresponding to this value.</returns>
    [Pure]
    public Instant ToInstant() => _value.ToInstant();

    /// <summary>
    /// Gets the <see cref="ZonedDateTime"/> value of this <see cref="AbsoluteDateTime"/>.
    /// </summary>
    [Pure]
    public ZonedDateTime ToZonedDateTime() => _value;

    public static implicit operator AbsoluteDateTime(ZonedDateTime value) => new(value);
    public static implicit operator ZonedDateTime(AbsoluteDateTime value) => value._value;

    #endregion

    #region Calendar

    /// <summary>
    /// Returns the next <see cref="AbsoluteDateTime" /> falling on the specified <see cref="IsoDayOfWeek"/>,
    /// at the same time of day as this value.
    /// This is a strict "next" - if this value on already falls on the target
    /// day of the week, the returned value will be a week later.
    /// </summary>
    /// <param name="targetDayOfWeek">The ISO day of the week to return the next date of.</param>
    /// <returns>The next <see cref="LocalDateTime"/> falling on the specified day of the week.</returns>
    /// <exception cref="InvalidOperationException">The underlying calendar doesn't use ISO days of the week.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="targetDayOfWeek"/> is not a valid day of the
    /// week (Monday to Sunday).</exception>
    [Pure]
    public AbsoluteDateTime Next(IsoDayOfWeek targetDayOfWeek) =>
        _value.LocalDateTime.Next(targetDayOfWeek).InZoneStrictly(_value.Zone);

    #endregion

    #region Arithmetic

    /// <summary>
    /// Returns the result of adding a duration to this zoned date and time.
    /// </summary>
    /// <remarks>
    /// This is an alternative way of calling <see cref="op_Addition(AbsoluteDateTime, Duration)"/>.
    /// </remarks>
    /// <param name="duration">The duration to add</param>
    /// <returns>A new <see cref="AbsoluteDateTime" /> representing the result of the addition.</returns>
    [Pure]
    public AbsoluteDateTime Plus(Duration duration) => this + duration;

    /// <summary>
    /// Adds a period to this absolute date/time.
    /// Fields are added in descending order of significance (years first, then months, and so on).
    /// </summary>
    /// <param name="period">Period to add</param>
    /// <returns>The resulting local date and time</returns>
    [Pure]
    public AbsoluteDateTime Plus(Period period) => _value.LocalDateTime.Plus(period).InZoneStrictly(_value.Zone);

    /// <summary>
    /// Returns the result of adding an increment of seconds to this zoned date and time
    /// </summary>
    /// <param name="seconds">The number of seconds to add</param>
    /// <returns>A new <see cref="AbsoluteDateTime" /> representing the result of the addition.</returns>
    public AbsoluteDateTime PlusSeconds(long seconds) => _value.PlusSeconds(seconds);

    #endregion

    #region Operators

    public static Duration operator -(AbsoluteDateTime end, AbsoluteDateTime start) => end._value - start._value;
    public static AbsoluteDateTime operator +(AbsoluteDateTime start, Duration duration) => start._value + duration;

    public static bool operator <(AbsoluteDateTime left, AbsoluteDateTime right) => left.ToInstant() < right.ToInstant();
    public static bool operator >(AbsoluteDateTime left, AbsoluteDateTime right) => left.ToInstant() > right.ToInstant();

    public static bool operator <=(AbsoluteDateTime left, AbsoluteDateTime right) => left.ToInstant() <= right.ToInstant();
    public static bool operator >=(AbsoluteDateTime left, AbsoluteDateTime right) => left.ToInstant() >= right.ToInstant();

    public static bool operator ==(AbsoluteDateTime left, AbsoluteDateTime right) => left.ToInstant() == right.ToInstant();
    public static bool operator !=(AbsoluteDateTime left, AbsoluteDateTime right) => left.ToInstant() != right.ToInstant();

    #endregion

    #region Comparison

    [Pure]
    public int CompareTo(ZonedDateTime other) => _value.ToInstant().CompareTo(other.ToInstant());

    //For equality, we compare the values directly (not the instants)
    [Pure]
    public bool Equals(ZonedDateTime other) => _value.ToInstant() == other.ToInstant();

    [Pure]
    public override int GetHashCode() => _value.GetHashCode();

    [Pure]
    public bool Equals(Instant other) => _value.ToInstant() == other;

    [Pure]
    public int CompareTo(Instant other) => _value.ToInstant().CompareTo(other);

    [Pure]
    public bool Equals(AbsoluteDateTime other) => _value.ToInstant() == other._value.ToInstant();

    [Pure]
    public int CompareTo(AbsoluteDateTime other) => _value.ToInstant().CompareTo(other._value.ToInstant());

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj switch
        {
            Instant x => _value.ToInstant() == x,
            ZonedDateTime x => _value.ToInstant() == x.ToInstant(),
            AbsoluteDateTime x => _value.ToInstant() == x.ToInstant(),
            _ => false
        };

    #endregion

    #region Helpers
    
    /// <summary>
    /// Returns the maximum of absolute dates
    /// </summary>
    public static AbsoluteDateTime Max(AbsoluteDateTime x, AbsoluteDateTime y) =>
        x > y ? x : y;

    /// <summary>
    /// Returns the minimum of 2 absolute dates
    /// </summary>
    public static AbsoluteDateTime Min(AbsoluteDateTime x, AbsoluteDateTime y) =>
        x < y ? x : y;

    #endregion

    /// <summary>
    /// Gets the <see cref="DateTimeZone.Id"/> of the time zone associated with this value.
    /// </summary>
    public string GetZoneId() => _value.Zone.Id;

    /// <summary>
    /// Returns the current date and time in the UTC time zone.
    /// </summary>
    public static AbsoluteDateTime UtcNow() => SystemClock.Instance.GetCurrentInstant().InUtc();
}