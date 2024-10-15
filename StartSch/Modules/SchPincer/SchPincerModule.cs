using StartSch.Wasm;

namespace StartSch.Modules.SchPincer;

public class SchPincerModule : IModule
{
    public string Id => "pincer";
    public IEnumerable<Post> Posts => [];
    public IEnumerable<Event> Events => [];
    public IEnumerable<Instance> Instances => [new("https://schpincer.sch.bme.hu", "SCH-Pincér")];

    public IEnumerable<Opening> Openings =>
    [
        new("lángosch", "Goofy Pitbull nyitás", null, DateTime.Now, DateTime.Now),
        new("magyarosch", "Harcsapaprikás túrós csuszával, bukta", null, DateTime.Now, DateTime.Now),
    ];

    public IEnumerable<TagGroup> Tags =>
    [
        new("push", null, [
            new("pincér", "Pincer", [
                new("hírek"),
                new("rendelés"),
                new("nyitás"),
                new("lángosch", "lang", [
                    new("hírek"),
                    new("rendelés"),
                    new("nyitás"),
                ]),
                new("magyarosch", "hungry", [
                    new("hírek"),
                    new("rendelés"),
                    new("nyitás"),
                ]),
            ]),
            new("esemény"),
        ]),
        new("email", null, [
            new("pincér", "Pincer", [
                new("hírek"),
                new("rendelés"),
                new("nyitás"),
                new("lángosch", "lang", [
                    new("hírek"),
                    new("rendelés"),
                    new("nyitás"),
                ]),
                new("magyarosch", "hungry", [
                    new("hírek"),
                    new("rendelés"),
                    new("nyitás"),
                ]),
            ]),
            new("esemény"),
        ]),
    ];
}