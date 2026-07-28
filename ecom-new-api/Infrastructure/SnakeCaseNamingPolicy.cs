using System.Text;
using System.Text.Json;

namespace ecom_new_api.Infrastructure;

/// <summary>
/// Converts PascalCase/camelCase property names to snake_case for JSON serialisation.
/// E.g. "SiteId" → "site_id", "UserIp" → "user_ip", "SubTotalAmount" → "sub_total_amount".
/// </summary>
public sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public static readonly SnakeCaseNamingPolicy Instance = new();

    private SnakeCaseNamingPolicy() { }

    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c))
            {
                var prevIsLower = !char.IsUpper(name[i - 1]);
                var nextIsLower = i + 1 < name.Length && !char.IsUpper(name[i + 1]);
                if (prevIsLower || nextIsLower)
                    sb.Append('_');
            }
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
