using Microsoft.EntityFrameworkCore;
using StartSch.Data;

namespace StartSch.Services;

public class CategoryService(IDbContextFactory<Db> dbFactory)
{
    public async Task Initialize()
    {
        await using Db db = await dbFactory.CreateDbContextAsync();
        var categories = await db.Categories.ToListAsync();
        await db.CategoryIncludes.LoadAsync();
    }
}
