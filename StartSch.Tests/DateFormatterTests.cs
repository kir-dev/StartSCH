using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace StartSch.Tests;

[TestClass]
public sealed class DateFormatterTests
{
    [
        TestMethod,
        DataRow(
            "2025-09-25T19:30:00",
            "2025-09-25T19:30:00",
            "2025-09-25T19:30:00", "most-most"
        )
    ]
    public void Test(string dateS, string? endS, string nowS, string expected)
    {
        DateTime date = DateTime.ParseExact(dateS, "s", CultureInfo.InvariantCulture);
        DateTime? end = endS == null ? null : DateTime.ParseExact(endS, "s", CultureInfo.InvariantCulture);
        DateTime now = DateTime.ParseExact(nowS, "s", CultureInfo.InvariantCulture);
        
        string result = DateFormatter.FormatHungarianTime(date, end, now);
        
        Assert.AreEqual(expected, result);
    }
}
