using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

internal class DbContextFactoryTranslator(IDbContextFactory<PostgresDb> factory) : IDbContextFactory<Db>
{
    public Db CreateDbContext() => factory.CreateDbContext();
}