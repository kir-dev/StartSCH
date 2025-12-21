using NodaTime;
using NodaTime.Text;

namespace StartSch.Tests;

[TestClass]
public sealed class DateFormatterTests
{
    [
        TestMethod,
        DataRow("2025-09-26T10:00:00", "2025-09-25T19:30:00", null, "tegnap 19:30"),
        DataRow("2025-09-25T10:00:00", "2025-09-25T19:30:00", null, "ma 19:30"),
        DataRow("2025-09-25T18:30:00", "2025-09-25T19:30:00", null, "ma 19:30 (1 óra múlva)"),
        DataRow("2025-09-25T19:20:00", "2025-09-25T19:30:00", null, "ma 19:30 (10 perc múlva)"),
        DataRow("2025-09-25T19:30:00", "2025-09-25T19:30:00", null, "ma 19:30 (most)"),
        DataRow("2025-09-25T19:40:00", "2025-09-25T19:30:00", null, "ma 19:30 (10 perccel ezelőtt)"),
        DataRow("2025-09-25T20:30:00", "2025-09-25T19:30:00", null, "ma 19:30 (1 órával ezelőtt)"),
        DataRow("2025-09-24T23:59:00", "2025-09-25T00:10:00", null, "holnap 00:10 (11 perc múlva)"),
        DataRow("2025-09-24T10:00:00", "2025-09-25T19:30:00", null, "holnap 19:30"),
        DataRow("2025-09-22T10:00:00", "2025-09-28T19:30:00", null, "vasárnap 19:30"),
        DataRow("2025-09-20T10:00:00", "2025-09-28T19:30:00", null, "jövő vasárnap 19:30"),
        DataRow("2025-01-01T10:00:00", "2025-09-24T19:30:00", null, "szept. 24., sze., 19:30"),
        DataRow("2026-01-01T10:00:00", "2025-09-25T19:30:00", null, "2025. szept. 25., cs., 19:30"),
        DataRow("2025-09-25T10:00:00", "2025-09-25T19:30:00", "2025-09-25T20:00:00", "ma 19:30–20:00"),
        DataRow("2025-09-25T18:30:00", "2025-09-25T19:30:00", "2025-09-25T20:00:00", "ma 19:30 (1 óra múlva) – 20:00"),
        DataRow("2025-09-25T19:20:00", "2025-09-25T19:30:00", "2025-09-26T00:10:00", "ma 19:30 (10 perc múlva) – holnap 00:10"),
        DataRow("2025-09-25T10:00:00", "2025-09-25T19:30:00", "2025-09-29T20:00:00", "ma 19:30 – jövő hétfő 20:00"),
        DataRow("2024-01-01T10:00:00", "2025-09-25T19:30:00", "2025-09-25T20:00:00", "2025. szept. 25., cs., 19:30–20:00"),
        DataRow("2024-01-01T10:00:00", "2025-09-25T19:30:00", "2025-09-27T20:00:00", "2025. szept. 25., cs., 19:30 – szept. 27., szo., 20:00")
    ]
    public void Test(string nowS, string dateS, string? endS, string expected)
    {
        LocalDateTime date = LocalDateTimePattern.GeneralIso.Parse(dateS).Value;
        LocalDateTime? end = endS == null ? null : LocalDateTimePattern.GeneralIso.Parse(endS).Value;
        LocalDateTime now = LocalDateTimePattern.GeneralIso.Parse(nowS).Value;
        
        string result = DateFormatter.FormatHungarianTime(
            date.InZoneStrictly(Utils.HungarianTimeZone),
            end?.InZoneStrictly(Utils.HungarianTimeZone),
            now.InZoneStrictly(Utils.HungarianTimeZone)
        );
        
        Assert.AreEqual(expected, result);
    }
}
