using StartSch.Wasm;

namespace StartSch.Modules.Cmsch;

public class CmschModule : IModule
{
    public string Id => "cmsch";

    public IEnumerable<Post> Posts =>
    [
        new(
            "Megnyílt a QR-fight utolsó szakasza!",
            "Megnyílt a QR-fight utolsó körzete, Budapest! A legtöbb QR kódot a belváros tartogatja magában. Jó keresgélést!",
            "Megnyílt a QR-fight utolsó körzete, Budapest! A legtöbb QR kódot a belváros tartogatja magában. Jó keresgélést!",
            "https://qpa.sch.bme.hu/news/qr-fight-budapest",
            DateTime.Now,
            ["qpa"]),
    ];

    public IEnumerable<Event> Events =>
    [
    ];

    public IEnumerable<Instance> Instances =>
    [
        new("https://qpa.sch.bme.hu", "QPA")
    ];

    public IEnumerable<SelectableGroup<TagDetails>> Tags =>
    [
        new("push", null, [
            new("qpa", "QPA"),
            new("esemény"),
        ]),
        new("email", null, [
            new("qpa", "QPA"),
            new("esemény"),
        ]),
    ];
}