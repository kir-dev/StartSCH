using Microsoft.EntityFrameworkCore;

namespace StartSch.Data.Migrations;

internal sealed class PostgresDb(DbContextOptions<PostgresDb> options) : Db(options);
