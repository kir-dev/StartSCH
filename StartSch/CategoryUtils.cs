using StartSch.Data;

namespace StartSch;

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
