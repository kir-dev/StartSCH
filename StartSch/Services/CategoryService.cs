using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.Data;
using StartSch.Data.Migrations;

namespace StartSch.Services;

public class CategoryService(IDbContextFactory<Db> dbFactory, Db scopeDb, IMemoryCache cache)
{
    public const string CacheKey = nameof(CategoryIndex);
    
    public async Task<CategoryIndex> LoadIndex()
    {
        var cached = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            List<Page> pages;
            
            // SQLite doesn't really support parallel transactions. The current method might be called from inside
            // another one, let's just reuse that transaction. `AsNoTrackingWithIdentityResolution` ensures the
            // results from the query won't be leaked to the calling code through the DbContext.
            if (scopeDb is SqliteDb && scopeDb.Database.CurrentTransaction != null)
            {
                pages = await GetPages(scopeDb);
            }
            else
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                await using var tx = await db.BeginSnapshotTransactionAsync();

                pages = await GetPages(db);
                
                await tx.CommitAsync();
            }
            
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            
            return new CategoryIndex(pages);
        });

        CategoryIndex index = cached!.DeepCopy();
        index.Attach(scopeDb);
        return index;
    }
    
    private static async Task<List<Page>> GetPages(Db db)
    {
        return await db.Pages
            .Include(p => p.Categories)
            .AsSplitQuery()
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync();
    }
}
