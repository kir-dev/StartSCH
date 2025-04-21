using Microsoft.EntityFrameworkCore;

namespace StartSch.Data.Migrations;

internal sealed class SqliteDb(DbContextOptions<SqliteDb> options) : Db(options);
