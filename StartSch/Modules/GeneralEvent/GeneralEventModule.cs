using StartSch.Wasm;

namespace StartSch.Modules.GeneralEvent;

public class GeneralEventModule : IModule
{
    public string Id => "general";

    public IEnumerable<Post> Posts =>
    [
        new(
            "Programozói Konferencia 2024",
            "Hagyományunkhoz híven idén is megrendeztük az őszi konferenciánkat, idén tizenkettedjére! Szokásunkhoz híven ez is egy családias hangulatú és érdekes konferencia volt, ahol hallgatótársaid által prezentált előadásokon vehettél részt a programozás minden területéről! ",
            "Hagyományunkhoz híven idén is megrendeztük az őszi konferenciánkat, idén tizenkettedjére! Szokásunkhoz híven ez is egy családias hangulatú és érdekes konferencia volt, ahol hallgatótársaid által prezentált előadásokon vehettél részt a programozás minden területéről! ",
            "https://progkonf.sch.bme.hu",
            DateTime.Now,
            ["progkonf", "esemény"]),
    ];

    public IEnumerable<Event> Events => [
        new(
            "Programozói Konferencia 2024",
            "Hagyományunkhoz híven idén is megrendeztük az őszi konferenciánkat, idén tizenkettedjére! Szokásunkhoz híven ez is egy családias hangulatú és érdekes konferencia volt, ahol hallgatótársaid által prezentált előadásokon vehettél részt a programozás minden területéről! ",
            "Hagyományunkhoz híven idén is megrendeztük az őszi konferenciánkat, idén tizenkettedjére! Szokásunkhoz híven ez is egy családias hangulatú és érdekes konferencia volt, ahol hallgatótársaid által prezentált előadásokon vehettél részt a programozás minden területéről! ",
            DateTime.Today,
            DateTime.Now,
            "https://progkonf.sch.bme.hu",
            ["progkonf", "esemény"]),
    ];

    public IEnumerable<Instance> Instances =>
    [
        new("https://progkong.sch.bme.hu", "Programozói Konferencia"),
    ];

    public IEnumerable<TagGroup> Tags =>
    [
        new("push", null, [
            new("tanfolyam"),
            new("esemény"),
        ]),
        new("email", null, [
            new("tanfolyam"),
            new("esemény"),
        ]),
    ];
}