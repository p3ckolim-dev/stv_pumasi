namespace Pumasi.Core.Clock;

public interface IClock
{
    DateTimeOffset GetUtcNow();
}
