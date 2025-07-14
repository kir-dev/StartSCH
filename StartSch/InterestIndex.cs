using System.Runtime.InteropServices;
using StartSch.Data;

namespace StartSch;

public class InterestIndex
{
    private readonly Dictionary<int, Page> pages = [];
    private readonly Dictionary<int, Category> categories = [];
    private readonly Dictionary<int, Interest> _interests = [];
    private readonly List<Page> components = [];

    /// Must be called using data from EF, meaning all relationships are already set up
    public InterestIndex(IEnumerable<Page> pages)
    {
        foreach (Page page in pages)
            if (Explore(page))
                components.Add(page);
    }

    private bool Explore(Page page)
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(pages, page.Id, out bool exists);
        if (exists) return false;
        entry = page;

        foreach (var category in page.Categories) Explore(category);

        return true;
    }

    private void Explore(Category category)
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(categories, category.Id, out bool exists);
        if (exists) return;
        entry = category;

        Explore(category.Page);
        foreach (var c in category.IncludedCategories) Explore(c);
        foreach (var c in category.IncluderCategories) Explore(c);
        foreach (var i in category.Interests) _interests.Add(i.Id, i);
    }

    public IEnumerable<Page> Pages => pages.Values;

    public List<Interest> GetInterests(IEnumerable<int> interestIds)
    {
        return interestIds.Select(id => _interests[id]).ToList();
    }

    public InterestIndex DeepCopy()
    {
        Dictionary<Page, Page> originalToClonePage = [];
        foreach (Page original in pages.Values)
        {
            Page clone = new()
            {
                Id = original.Id,
                Url = original.Url,
                Name = original.Name,
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
                Name = original.Name,
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
