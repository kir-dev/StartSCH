using System.Globalization;

namespace StartSch.Tests;

[TestClass]
public sealed class DateFormatterTests
{
    [
        TestMethod,
        DataRow("2025-09-25T19:30:00", null, "2025-09-26T10:00:00", "tegnap 19:30"),
        DataRow("2025-09-25T19:30:00", null, "2025-09-25T10:00:00", "ma 19:30"),
        DataRow("2025-09-25T19:30:00", null, "2025-09-25T18:30:00", "ma 19:30 (1 óra múlva)"),
        DataRow("2025-09-25T19:30:00", null, "2025-09-25T19:20:00", "ma 19:30 (10 perc múlva)"),
        DataRow("2025-09-25T19:30:00", null, "2025-09-25T19:30:00", "ma 19:30 (most)"),
        DataRow("2025-09-25T19:30:00", null, "2025-09-25T19:40:00", "ma 19:30 (10 perccel ezelőtt)"),
        DataRow("2025-09-25T19:30:00", null, "2025-09-25T20:30:00", "ma 19:30 (1 órával ezelőtt)"),
        DataRow("2025-09-25T00:10:00", null, "2025-09-24T23:59:00", "holnap 00:10 (11 perc múlva)"),
        DataRow("2025-09-25T19:30:00", null, "2025-09-24T10:00:00", "holnap 19:30"),
        DataRow("2025-09-28T19:30:00", null, "2025-09-22T10:00:00", "vasárnap 19:30"),
        DataRow("2025-09-28T19:30:00", null, "2025-09-20T10:00:00", "jövő vasárnap 19:30"),
        DataRow("2025-09-24T19:30:00", null, "2025-01-01T10:00:00", "szept. 24., szerda, 19:30"),
        DataRow("2025-09-25T19:30:00", null, "2026-01-01T10:00:00", "2025. szept. 25., csütörtök, 19:30"),
        DataRow("2025-09-25T19:30:00", "2025-09-25T20:00:00", "2025-09-25T10:00:00", "ma 19:30–20:00"),
        DataRow("2025-09-25T19:30:00", "2025-09-25T20:00:00", "2025-09-25T18:30:00", "ma 19:30 (1 óra múlva) – 20:00"),
        DataRow("2025-09-25T19:30:00", "2025-09-26T00:10:00", "2025-09-25T19:20:00", "ma 19:30 (10 perc múlva) – holnap 00:10"),
        DataRow("2025-09-25T19:30:00", "2025-09-29T20:00:00", "2025-09-25T10:00:00", "ma 19:30 – jövő hétfő 20:00"),
        DataRow("2025-09-25T19:30:00", "2025-09-25T20:00:00", "2024-01-01T10:00:00", "2025. szept. 25., csütörtök, 19:30–20:00"),
        DataRow("2025-09-25T19:30:00", "2025-09-27T20:00:00", "2024-01-01T10:00:00", "2025. szept. 25., csütörtök, 19:30 – szept. 27., szombat, 20:00")
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
