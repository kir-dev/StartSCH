using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.Data;

namespace StartSch.Services;

public class CategoryService(IDbContextFactory<Db> dbFactory, Db scopeDb, IMemoryCache cache)
{
    public const string CacheKey = nameof(CategoryIndex);
    
    public async Task<CategoryIndex> LoadIndex()
    {
        var cached = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            await using var tx = await db.BeginSnapshotTransactionAsync();
            
            var pages = await db.Pages
                .Include(p => p.Categories)
                .AsSplitQuery()
                .AsNoTrackingWithIdentityResolution()
                .ToListAsync();
            
            await tx.CommitAsync();
            
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            
            return new CategoryIndex(pages);
        });

        CategoryIndex index = cached!.DeepCopy();
        index.Attach(scopeDb);
        return index;
    }
}
