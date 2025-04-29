using Microsoft.EntityFrameworkCore;
using StartSch.Data;

namespace StartSch.Services;

public class CategoryRepository(Db db)
{
    
    private async Task<List<Category>> GetCategories()
    {
        db.Categories.Att
        var categories = await db.Categories.ToListAsync();
        await db.CategoryIncludes.LoadAsync();
        return categories;
    }
}
