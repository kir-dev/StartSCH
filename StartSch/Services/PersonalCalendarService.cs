using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StartSch.Data;
using StartSch.Wasm.PersonalCalendars;

namespace StartSch.Services;

public class PersonalCalendarService(Db db, IcalendarCache icalendarCache,
    IOptions<StartSchOptions> startSchOptions,
    IDataProtectionProvider dataProtectionProvider)
{
    public async Task<PersonalCalendarContextDto> GetContextDto(User user, byte[] aesKey)
    {
        return new()
        {
            Calendars = await GetCalendarsWithEvents(user.Id, aesKey),
            ConfigJson = user.PersonalCalendarConfiguration,
            DefaultCategoryId = user.DefaultPersonalCalendarCategoryId!.Value,
            DefaultExamCategoryId = user.DefaultPersonalCalendarExamCategoryId!.Value,
        };
    }

    public async Task<List<PersonalCalendarLive>> GetCalendarsWithEvents(int userId, byte[] aesKey)
    {
        var calendars = (await db.PersonalCalendars
                .Where(c => c.UserId == userId)
                .ToListAsync())
            .Select(cal =>
            {
                PersonalCalendarLive liveCal = cal switch
                {
                    PersonalCalendarCategory category => new PersonalCalendarCategoryLive
                    {
                        IcsUrl = PersonalCalendarExportUrlExtensions.GenerateIcsUrl(
                            cal.Id, aesKey, startSchOptions.Value.PublicUrl, dataProtectionProvider
                        ),
                        Color = SharedUtils.RgbToCssColorString(category.Color),
                    },
                    PersonalNeptunCalendar => new PersonalNeptunCalendarLive(),
                    PersonalMoodleCalendar => new PersonalMoodleCalendarLive(),
                    _ => throw new NotImplementedException(),
                };
                liveCal.Id = cal.Id;
                liveCal.Name = cal.Name;
                (liveCal as ExternalPersonalCalendarLive)?.Url =
                    ((ExternalPersonalCalendar)cal).GetUrl(aesKey) ?? "";
                return liveCal;
            })
            .ToList();

        await Task.WhenAll(
            calendars
                .OfType<ExternalPersonalCalendarLive>()
                .Where(c => Uri.IsWellFormedUriString(c.Url, UriKind.Absolute))
                .Select(async c => c.Events.AddRange(await icalendarCache.GetEvents(c.Url, c.GetType())))
        );

        return calendars;
    }
}
