namespace Pumasi.Core.Clock;

public sealed class SystemClock : IClock
{
    public static SystemClock Instance { get; } = new();

    private SystemClock()
    {
    }

    public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
}
