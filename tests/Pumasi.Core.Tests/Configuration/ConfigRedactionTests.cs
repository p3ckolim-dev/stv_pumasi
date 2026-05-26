using System.Text.Json;
using Pumasi.Core.Configuration;
using Xunit;

namespace Pumasi.Core.Tests.Configuration;

public sealed class ConfigRedactionTests
{
    [Fact]
    public void ToSharedSnapshot_DoesNotIncludeGeminiApiKey()
    {
        var config = new ModConfig
        {
            Gemini = { ApiKey = "super-secret-gemini-key" },
            Assistant = { Name = "Miso" }
        };

        var snapshot = config.ToSharedSnapshot();
        var json = JsonSerializer.Serialize(snapshot);

        Assert.Contains("Miso", json);
        Assert.DoesNotContain("super-secret-gemini-key", json);
        Assert.DoesNotContain("ApiKey", json);
    }

    [Fact]
    public void ToString_RedactsGeminiApiKey()
    {
        var config = new ModConfig
        {
            Gemini = { ApiKey = "super-secret-gemini-key" }
        };

        Assert.DoesNotContain("super-secret-gemini-key", config.ToString());
        Assert.Contains("ApiKey=set", config.ToString());
    }
}
