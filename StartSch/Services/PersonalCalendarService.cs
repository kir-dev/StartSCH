using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StartSch.Data;

namespace StartSch.Services;

public class PersonalCalendarService(Db db, IcalendarCache icalendarCache,
    IOptions<StartSchOptions> startSchOptions,
    IDataProtectionProvider dataProtectionProvider)
{
    public async Task<List<PersonalCalendarLive>> GetCalendarsWithEvents(int userId, PersonalCalendarEncryptionToken encryptionToken)
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
                            c.Id, encryptionToken.AesKey, startSchOptions.Value.PublicUrl, dataProtectionProvider
                        ),
                    },
                    PersonalNeptunCalendar => new PersonalNeptunCalendarLive(),
                    PersonalMoodleCalendar => new PersonalMoodleCalendarLive(),
                    _ => throw new NotImplementedException(),
                };
                l.Id = c.Id;
                l.Name = c.Name;
                (l as ExternalPersonalCalendarLive)?.Url =
                    ((ExternalPersonalCalendar)c).GetUrl(encryptionToken.AesKey) ?? "";
                return l;
            })
            .ToList();

        await Task.WhenAll(
            calendars
                .OfType<ExternalPersonalCalendarLive>()
                .Select(async c => c.Events.AddRange(await icalendarCache.GetEvents(c.Url, c.GetType())))
        );

        return calendars;
    }
}
