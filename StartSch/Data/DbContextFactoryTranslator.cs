using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

// Turns a IDbContextFactory<TDb> into a IDbContextFactory<Db>
internal class DbContextFactoryTranslator<TDb>(IDbContextFactory<TDb> factory)
    : IDbContextFactory<Db> where TDb : Db
{
    public Db CreateDbContext() => factory.CreateDbContext();
}