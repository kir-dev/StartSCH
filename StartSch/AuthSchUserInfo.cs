using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace StartSch;

[UsedImplicitly]
public record AuthSchUserInfo(
    string? Email,
    [property: JsonPropertyName("email_verified")]
    bool EmailVerified,
    [property: JsonPropertyName("pek.sch.bme.hu:activeMemberships/v1")]
    List<AuthSchActiveMembership>? PekActiveMemberships
);

[UsedImplicitly]
public record AuthSchActiveMembership(
    [property: JsonPropertyName("id")] int PekId,
    string Name,
    [property: JsonPropertyName("title")] List<string> Titles
);
