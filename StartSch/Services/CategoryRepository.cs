using Microsoft.EntityFrameworkCore;
using StartSch.Data;

namespace StartSch.Services;

public class CategoryIndex
{
    /// Must be called using data from EF, meaning all relationships are already set up
    public CategoryIndex(HashSet<Category> categories, HashSet<Page> pages)
    {
        
    }

    private static void Explore(Page page, HashSet<Category> visitedCategories, HashSet<Page> visitedPages)
    {
        if (!visitedPages.Add(page))
            return;

        foreach (var category in page.Categories)
            Explore(category, visitedCategories, visitedPages);
    }

    private static void Explore(Category category, HashSet<Category> visitedCategories, HashSet<Page> visitedPages)
    {
        if (!visitedCategories.Add(category))
            return;

        Explore(category.Owner, visitedCategories, visitedPages);
        
        foreach (var c in category.IncludedCategories)
            Explore(c, visitedCategories, visitedPages);
        foreach (var c in category.IncluderCategories)
            Explore(c, visitedCategories, visitedPages);
    }

    public CategoryIndex Clone() {}
    
    public void Attach(Db db) {}
}

public static class CategoryUtils
{
    /// Recursively finds categories that include the specified categories.
    /// When publishing a post, this finds targets for notifications.
    public static HashSet<Category> FlattenIncludingCategories(List<Category> categories)
    {
        HashSet<Category> res = [];
        ExploreUp(res, categories);
        return res;

        static void ExploreUp(HashSet<Category> includers, List<Category> currentLevel)
        {
            foreach (var category in currentLevel)
            {
                includers.Add(category);
                ExploreUp(includers, category.IncluderCategories);
            }
        }
    }

    /// Recursively finds categories that are included by the specified categories.
    /// When loading a page, this finds categories to include in the database query.
    public static HashSet<Category> FlattenIncludedCategories(List<Category> categories)
    {
        HashSet<Category> res = [];
        ExploreDown(res, categories);
        return res;

        static void ExploreDown(HashSet<Category> includers, List<Category> currentLevel)
        {
            foreach (var category in currentLevel)
            {
                includers.Add(category);
                ExploreDown(includers, category.IncludedCategories);
            }
        }
    }

    /// Removes categories that are implicitly selected (are included by another selected category).
    public static HashSet<Category> OptimizeSelection(HashSet<Category> selectedCategories)
    {
        return selectedCategories
            .Where(c => !c.IncluderCategories.Any(selectedCategories.Contains))
            .ToHashSet();
    }
}

public class CategoryNode
{
    public int Id { get; set; }
    public List<CategoryNode> Includes { get; } = [];
    public List<CategoryNode> IncludedBy { get; } = [];
}

public class CategoryRepository(Db db)
{
    
    private async Task<List<Category>> GetCategories()
    {
        var categories = await db.Categories.ToListAsync();
        await db.CategoryIncludes.LoadAsync();
        db.Categories.AttachRange();
        return categories;
    }
}
