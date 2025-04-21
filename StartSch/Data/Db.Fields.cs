using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

public partial class Page
{
    public int Id { get; init; }
}

public partial class Category
{
    public int Id { get; init; }
}

public partial class Event
{
    public int Id { get; init; }
}

public partial class Post
{
    public int Id { get; init; }
}

public partial class User
{
    public int Id { get; init; }
    public Guid? AuthSchId { get; set; }

    [MaxLength(200)] public string? AuthSchEmail { get; set; } // only stored if verified
    [MaxLength(200)] public string? StartSchEmail { get; set; }
    public bool StartSchEmailVerified { get; set; }
}

public abstract partial class Interest
{
    public int Id { get; init; }
}

public abstract partial class InterestSubscription
{
    public int Id { get; init; }
}

[Index(nameof(Url), IsUnique = true)]
public partial class Post
{
    public int Id { get; init; }
    public int? EventId { get; set; }

    [MaxLength(130)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? ExcerptMarkdown { get; set; }
    [MaxLength(50000)] public string? ContentMarkdown { get; set; }
    [MaxLength(500)] public string? Url { get; init; }
    public DateTime? PublishedUtc { get; set; }
    public required DateTime CreatedUtc { get; init; }
}

[Index(nameof(PekId), IsUnique = true)]
[Index(nameof(PekName), IsUnique = true)]
[Index(nameof(PincerId), IsUnique = true)]
[Index(nameof(PincerName), IsUnique = true)]
public partial class Page
{
    public int Id { get; init; }

    [NotNullIfNotNull(nameof(PekName))] public int? PekId { get; set; }

    [NotNullIfNotNull(nameof(PekId)), MaxLength(40)]
    public string? PekName { get; set; }

    [NotNullIfNotNull(nameof(PincerName))] public int? PincerId { get; set; }

    [NotNullIfNotNull(nameof(PincerId)), MaxLength(40)]
    public string? PincerName { get; set; }
}

public partial class Category
{
    public int Id { get; set; }
}

public class Opening : Event
{
    public DateTime? OrderingStartUtc { get; set; }
    public DateTime? OrderingEndUtc { get; set; }
    public DateTime? OutOfStockUtc { get; set; }
}

public partial class Event
{
    public int Id { get; init; }
    public int? ParentId { get; set; }
    public required DateTime CreatedUtc { get; init; }
    public required DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    [MaxLength(130)] public required string Title { get; set; }
    [MaxLength(50000)] public string? DescriptionMarkdown { get; set; }
}

[Index(nameof(Endpoint), IsUnique = true)]
public partial class PushSubscription
{
    public int Id { get; init; }

    public Guid UserId { get; set; }

    // max lengths are arbitrary, may need to be adjusted
    [MaxLength(2000)] public required string Endpoint { get; init; }
    [MaxLength(100)] public required string P256DH { get; init; }
    [MaxLength(50)] public required string Auth { get; init; }
}

// NOTIFICATION QUEUE //

