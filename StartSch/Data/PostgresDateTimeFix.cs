using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace StartSch.Data;

// Ensure EF change tracking ignores sub-microsecond differences for DateTime/DateTime? properties
//
// DateTime has a resolution of 100 nanoseconds, while Postgres can only store microseconds.
// When setting the value of a DateTime property, EF always sees an update, as the original version's nanoseconds
// component is 0, while the incoming has it set to something else.
//
// https://github.com/npgsql/npgsql/blob/332ce0b2f/src/Npgsql/Internal/Converters/Temporal/PgTimestamp.cs#L20
public static class PostgresDateTimeFix
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        ValueComparer<DateTime> dateTimeComparer = new(
            (l, r) => NormalizeToMicroseconds(l).Ticks == NormalizeToMicroseconds(r).Ticks,
            x => NormalizeToMicroseconds(x).Ticks.GetHashCode()
        );
        ValueComparer<DateTime?> nullableDateTimeComparer = new(
            (l, r) =>
                (!l.HasValue && !r.HasValue)
                || (l.HasValue && r.HasValue && NormalizeToMicroseconds(l.Value).Ticks == NormalizeToMicroseconds(r.Value).Ticks),
            x => x.HasValue ? NormalizeToMicroseconds(x.Value).Ticks.GetHashCode() : 0
        );

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetValueComparer(dateTimeComparer);
                else if (property.ClrType == typeof(DateTime?))
                    property.SetValueComparer(nullableDateTimeComparer);
            }
        }
    }

    private static DateTime NormalizeToMicroseconds(DateTime dateTime)
    {
        long ticks = dateTime.Ticks - (dateTime.Ticks % 10L);
        return new DateTime(ticks, dateTime.Kind);
    }
}
