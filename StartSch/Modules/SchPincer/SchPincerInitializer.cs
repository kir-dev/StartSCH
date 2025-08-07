using Microsoft.EntityFrameworkCore;
using StartSch.Data;

namespace StartSch.Modules.SchPincer;

public class SchPincerInitializer(SchPincerModule module, Db db) : IModuleInitializer
{
    public async Task Initialize()
    {
        Page schPincerPage =
            (await db.Pages
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.ExternalUrl == SchPincerModule.Url)
            )
            ?? db.Pages.Add(
                new()
                {
                    Name = "SCH-Pincér",
                    ExternalUrl = SchPincerModule.Url,
                    Categories =
                    {
                        new()
                        {
                            Interests =
                            {
                                new ShowEventsInCategory(),
                                new ShowPostsInCategory(),
                                new EmailWhenOrderingStartedInCategory(),
                                new EmailWhenPostPublishedInCategory(),
                                new PushWhenOrderingStartedInCategory(),
                                new PushWhenPostPublishedInCategory(),
                            }
                        }
                    }
                }
            ).Entity;
        await db.SaveChangesAsync();
        module.DefaultCategoryId = schPincerPage.Categories.Single().Id;
    }
}
