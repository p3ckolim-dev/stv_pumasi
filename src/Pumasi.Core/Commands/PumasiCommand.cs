namespace Pumasi.Core.Commands;

public sealed record PumasiCommand(PumasiCommandKind Kind, string Argument)
{
    public static PumasiCommand None { get; } = new(PumasiCommandKind.None, string.Empty);
}
