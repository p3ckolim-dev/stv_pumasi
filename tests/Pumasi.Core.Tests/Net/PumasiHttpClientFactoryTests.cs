using Pumasi.Core.Net;
using Xunit;

namespace Pumasi.Core.Tests.Net;

public sealed class PumasiHttpClientFactoryTests
{
    [Theory]
    [InlineData(0, 5)]
    [InlineData(3, 5)]
    [InlineData(30, 30)]
    public void Create_ClampsTimeoutToSafeMinimum(int configuredSeconds, int expectedSeconds)
    {
        using var client = PumasiHttpClientFactory.Create(configuredSeconds);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), client.Timeout);
    }

    [Fact]
    public void Create_ReturnsFreshClientForEachRequestScope()
    {
        using var first = PumasiHttpClientFactory.Create(30);
        using var second = PumasiHttpClientFactory.Create(30);

        Assert.NotSame(first, second);
    }
}
