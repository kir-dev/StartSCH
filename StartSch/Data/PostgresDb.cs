using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

// Needed for having separate migrations for SQLite and PostgreSQL
// https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers#using-multiple-context-types
internal sealed class PostgresDb(DbContextOptions options) : Db(options)
{
}