namespace Pumasi.Core.Tasks;

public sealed record TaskTarget(
    string Location,
    int X,
    int Y,
    string? EntityId = null,
    string? ObjectName = null)
{
    public string NormalizedLocation => Normalize(Location);

    public static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "Unknown"
            : value.Trim().Replace(" ", "", StringComparison.Ordinal);
    }
}
