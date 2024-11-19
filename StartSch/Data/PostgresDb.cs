using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

internal sealed class PostgresDb(DbContextOptions<PostgresDb> options) : Db(options);