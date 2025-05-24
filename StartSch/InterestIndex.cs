using StartSch.Data;

namespace StartSch;

public class InterestIndex
{
    private readonly Dictionary<int, Page> pages;
    private readonly Dictionary<int, Category> categories;
    private readonly List<Page> components = [];

    /// Must be called using data from EF, meaning all relationships are already set up
    public InterestIndex(IEnumerable<Page> pages)
    {
        this.pages = pages.ToDictionary(p => p.Id);

        HashSet<Page> visitedPages = [];
        HashSet<Category> visitedCategories = [];
        foreach (Page page in this.pages.Values)
            if (Explore(page, visitedPages, visitedCategories))
                components.Add(page);

        categories = visitedCategories.ToDictionary(c => c.Id);
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

        Explore(category.Page, visitedPages, visitedCategories);

        foreach (var c in category.IncludedCategories)
            Explore(c, visitedPages, visitedCategories);
        foreach (var c in category.IncluderCategories)
            Explore(c, visitedPages, visitedCategories);
    }

    public IEnumerable<Page> Pages => pages.Values;
    public IEnumerable<Category> Categories => categories.Values;

    public List<Category> GetCategories(List<int> categoryIds)
    {
        return categoryIds.Select(id => categories[id]).ToList();
    }

    public List<Page> GetPages(Func<Page, bool> predicate) => pages.Values.Where(predicate).ToList();

    public InterestIndex DeepCopy()
    {
        Dictionary<Page, Page> originalToClonePage = [];
        foreach (Page original in pages.Values)
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
        foreach (Category original in categories.Values)
        {
            Page page = originalToClonePage[original.Page];
            Category clone = new()
            {
                Id = original.Id,
                PageId = original.PageId,
                // Name = original.Name,
                Page = page,
            };
            clone.Interests.AddRange(original.Interests.Select(originalInterest =>
            {
                CategoryInterest cloneInterest = originalInterest switch
                {
                    EmailWhenOrderingStartedInCategory => new EmailWhenOrderingStartedInCategory(),
                    EmailWhenPostPublishedInCategory => new EmailWhenPostPublishedInCategory(),
                    PushWhenOrderingStartedInCategory => new PushWhenOrderingStartedInCategory(),
                    PushWhenPostPublishedInCategory => new PushWhenPostPublishedInCategory(),
                    ShowEventsInCategory => new ShowEventsInCategory(),
                    ShowPostsInCategory => new ShowPostsInCategory(),
                    _ => throw new NotImplementedException(),
                };
                cloneInterest.Id = originalInterest.Id;
                cloneInterest.CategoryId = clone.Id;
                cloneInterest.Category = clone;
                return cloneInterest;
            }));
            page.Categories.Add(clone);

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

        return new(originalToClonePage.Values.ToHashSet());
    }

    public void Attach(Db db)
    {
        db.Pages.AttachRange(components);
    }
}
