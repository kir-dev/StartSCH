namespace StartSch.Modules.SchPincer;

public class SchPincerModule : IModule
{
    public string Id => "pincer";
    public IEnumerable<Post> Posts => [];
    public IEnumerable<Event> Events => [];
    public IEnumerable<Instance> Instances => [new("https://schpincer.sch.bme.hu", "SCH-Pincér")];
    public IEnumerable<Opening> Openings => [
        new("lángosch", "Goofy Pitbull nyitás", null, DateTime.Now, DateTime.Now),
        new("magyarosch", "Harcsapaprikás túrós csuszával, bukta", null, DateTime.Now, DateTime.Now),
    ];
}