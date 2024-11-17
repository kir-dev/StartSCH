using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;

namespace StartSch;

public static class Utils
{
    public static void AddModule<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors),
         MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
        TService>
        (this IServiceCollection serviceCollection) where TService : class, IModule
    {
        serviceCollection.AddSingleton<TService>();
        serviceCollection.AddSingleton<IModule, TService>(s => s.GetRequiredService<TService>());
    }

    // TODO: remove when updating to .NET 9
    public static JsonSerializerOptions JsonSerializerOptionsWeb { get; } = new(JsonSerializerDefaults.Web);

    public static CultureInfo HungarianCulture { get; } = new("hu-HU");

    // Used to match Pek names to Pincer names
    public static bool RoughlyMatches(this string a, string b)
    {
        a = a.Simplify();
        b = b.Simplify();
        return a.Contains(b) || b.Contains(a);
    }

    // Lowercase, remove diacritics, remove symbols
    public static string Simplify(this string str)
    {
        // TODO: use spans to avoid allocation
        str = str.ToLower(HungarianCulture);
        StringBuilder sb = new(str.Length);
        // ReSharper disable once ForCanBeConvertedToForeach
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i] switch
            {
                'á' => 'a',
                'é' => 'e',
                'í' => 'i',
                'ó' => 'o',
                'ö' => 'o',
                'ő' => 'o',
                'ú' => 'u',
                'ü' => 'u',
                'ű' => 'u',
                _ => str[i]
            };
            if (c is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
                sb.Append(c);
        }

        return sb.ToString();
    }
}