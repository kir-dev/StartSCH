using StartSch.Data;

namespace StartSch.Modules.Schonherz;

// ImpulzusModule instead?
// public class SchonherzModule : IModule, IModuleInitializer
// {
//     public static void Register(IServiceCollection services)
//     {
//         services.RegisterModuleInitializer<SchonherzModuleInitializer>();
//     }
// }
//
// public class SchonherzModuleInitializer(Db db) : IModuleInitializer
// {
//     public async Task Initialize()
//     {
//         var sch = db.Pages.Add(new()
//         {
//             Name = "Schönherz",
//         }).Entity;
//         Category defaultSch = new();
//         sch.Categories.Add(defaultSch);
//         Category schTanfolyamok = new(){Name = "Tanfolyamok"};
//         defaultSch.IncludedCategories.Add(schTanfolyamok);
//         Category schSzakmaiTanfolyamok = new() { Name = "Szakmai tanfolyamok", Page = sch};
//         schTanfolyamok.IncludedCategories.Add(schSzakmaiTanfolyamok);
//         schTanfolyamok.IncludedCategories.Add(new() { Name = "Kajás tanfolyamok", Page = sch });
//         schTanfolyamok.IncludedCategories.Add(new() { Name = "Házszintű tanfolyamok", Page = sch});
//
//         Page kirDevPage = GetKirDevPage();
//         Category kirDevCategory = new();
//         Category kirDevTanfolyam = new() { Name = "Tanfolyam" };
//         kirDevCategory.IncludedCategories.Add(kirDevTanfolyam);
//         schSzakmaiTanfolyamok.IncludedCategories.Add(kirDevTanfolyam);
//         kirDevPage.Categories.Add(kirDevCategory);
//         kirDevPage.Categories.Add(kirDevTanfolyam);
//        
//     }
// }
