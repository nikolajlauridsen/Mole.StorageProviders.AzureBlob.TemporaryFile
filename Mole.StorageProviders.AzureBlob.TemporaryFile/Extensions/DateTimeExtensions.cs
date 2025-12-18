using System.Globalization;

namespace Mole.StorageProviders.AzureBlob.TemporaryFile.Extensions;

/// <summary>
/// Extension methods for DateTime conversion to and from roundtrip format strings.
/// </summary>
internal static class DateTimeExtensions
{
    /// <summary>
    /// Converts a DateTime to a roundtrip format string ("O" format specifier).
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>A string representation in ISO 8601 roundtrip format.</returns>
    internal static string ToRoundtripString(this DateTime dateTime) =>
        dateTime.ToString("O");

    /// <summary>
    /// Parses a roundtrip format string back to a DateTime.
    /// </summary>
    /// <param name="roundtripString">The string to parse.</param>
    /// <returns>The parsed DateTime with proper DateTimeKind preserved.</returns>
    internal static DateTime ToRoundtripDateTime(this string roundtripString) =>
        DateTime.Parse(roundtripString, null, DateTimeStyles.RoundtripKind);
}
