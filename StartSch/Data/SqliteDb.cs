using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

internal sealed class SqliteDb(DbContextOptions<SqliteDb> options) : Db(options);