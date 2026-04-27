using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

[Index(nameof(AuthSchId), IsUnique = true)]
public class User : ICreatedUpdated
{
    public int Id { get; init; }
    public Guid? AuthSchId { get; init; }

    public Instant Created { get; set; }
    public Instant Updated { get; set; }
    [MaxLength(200)] public string? AuthSchEmail { get; set; } // only stored if verified
    [MaxLength(200)] public string? StartSchEmail { get; set; }
    public bool StartSchEmailVerified { get; set; }
    [MaxLength(100_000)] public string? PersonalCalendarConfiguration { get; set; }
    
    public List<Interest> Interests { get; } = [];
    public List<InterestSubscription> InterestSubscriptions { get; } = [];
    public List<PushSubscription> PushSubscriptions { get; } = [];
    
    public int? DefaultPersonalCalendarCategoryId { get; set; }
    public int? DefaultPersonalCalendarExamCategoryId { get; set; }
    public PersonalStartSchCalendar? DefaultPersonalCalendarCategory { get; set; }
    public PersonalStartSchCalendar? DefaultPersonalCalendarExamCategory { get; set; }
    public List<PersonalCalendar> PersonalCalendars { get; } = [];
}
