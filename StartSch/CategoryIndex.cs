using StartSch.Data;

namespace StartSch;

public class CategoryIndex
{
    private readonly HashSet<Page> pages;
    private readonly HashSet<Category> categories;
    private readonly List<Page> components = [];
    
    /// Must be called using data from EF, meaning all relationships are already set up, and there are no new entities
    /// reachable by exploring the graph
    public CategoryIndex(IEnumerable<Page> pages, IEnumerable<Category> categories)
    {
        this.categories = new(categories);
        this.pages = new(pages);

        HashSet<Page> visitedPages = [];
        HashSet<Category> visitedCategories = [];
        foreach (Page page in this.pages)
            if (Explore(page, visitedPages, visitedCategories))
                components.Add(page);
    }

    private static bool Explore(Page page, HashSet<Page> visitedPages, HashSet<Category> visitedCategories)
    {
        if (!visitedPages.Add(page))
            return false;
    
        foreach (var category in page.Categories)
            Explore(category, visitedPages, visitedCategories);

        return true;
    }
    
    private static void Explore(Category category, HashSet<Page> visitedPages, HashSet<Category> visitedCategories)
    {
        if (!visitedCategories.Add(category))
            return;

        Explore(category.Owner, visitedPages, visitedCategories);
        
        foreach (var c in category.IncludedCategories)
            Explore(c, visitedPages, visitedCategories);
        foreach (var c in category.IncluderCategories)
            Explore(c, visitedPages, visitedCategories);
    }

    public CategoryIndex DeepCopy()
    {
        Dictionary<Page, Page> originalToClonePage = [];
        foreach (Page original in pages)
        {
            Page clone = new()
            {
                Id = original.Id,
                PekId = original.PekId,
                PekName = original.PekName,
                PincerId = original.PincerId,
                PincerName = original.PincerName,
            };
            originalToClonePage.Add(original, clone);
        }
        
        Dictionary<Category, Category> originalToCloneCategory = [];
        foreach (Category original in categories)
        {
            Category clone = new()
            {
                Id = original.Id,
                OwnerId = original.OwnerId,
                Name = original.Name,
                Owner = originalToClonePage[original.Owner],
            };
            
            originalToCloneCategory.Add(original, clone);

            foreach (Category originalIncludedCategory in original.IncludedCategories)
            {
                if (originalToCloneCategory.TryGetValue(originalIncludedCategory, out Category? cloneIncludedCategory))
                {
                    clone.IncludedCategories.Add(cloneIncludedCategory);
                    cloneIncludedCategory.IncluderCategories.Add(clone);
                }
            }

            foreach (Category originalIncluderCategory in original.IncluderCategories)
            {
                if (originalToCloneCategory.TryGetValue(originalIncluderCategory, out Category? cloneIncluderCategory))
                {
                    clone.IncluderCategories.Add(cloneIncluderCategory);
                    cloneIncluderCategory.IncludedCategories.Add(clone);
                }
            }
        }

        return new(originalToClonePage.Values.ToHashSet(), originalToCloneCategory.Values.ToHashSet());
    }

    public void Attach(Db db)
    {
        db.Pages.AttachRange(components);
    }
}
