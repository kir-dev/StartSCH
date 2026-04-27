using Microsoft.EntityFrameworkCore;
using StartSch.Data;

namespace StartSch.Services;

public class PersonalCalendarService(Db db, IcalendarCache icalendarCache)
{
    public async Task<List<PersonalCalendarLive>> GetCalendarsWithEvents(int userId)
    {
        var calendars = (await db.PersonalCalendars
                .Where(c => c.UserId == userId)
                .ToListAsync())
            .Select(c =>
            {
                PersonalCalendarLive l = c switch
                {
                    PersonalStartSchCalendar => new PersonalStartSchCalendarLive
                    {
                        IcsUrl = PersonalCalendarExportUrlExtensions.GenerateIcsUrl(
                            c.Id, encryptionToken.AesKey, StartSchOptions.Value.PublicUrl, DataProtectionProvider
                        ),
                    },
                    PersonalNeptunCalendar => new PersonalNeptunCalendarLive(),
                    PersonalMoodleCalendar => new PersonalMoodleCalendarLive(),
                    _ => throw new NotImplementedException(),
                };
                l.Id = c.Id;
                l.Name = c.Name;
                (l as ExternalPersonalCalendarLive)?.Url =
                    ((ExternalPersonalCalendar)c).GetUrl(encryptionToken.AesKey);
                return l;
            })
            .ToList();

        await Task.WhenAll(
            calendars
                .OfType<ExternalPersonalCalendarLive>()
                .Select(async c => c.Events.AddRange(await icalendarCache.GetEvents(c.Url, c.GetType())))
        );
        
    }
}
