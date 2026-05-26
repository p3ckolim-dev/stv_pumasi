using Pumasi.Core.Knowledge;
using Xunit;

namespace Pumasi.Core.Tests.Knowledge;

public sealed class WikiMemoryCacheTests
{
    [Fact]
    public void TryGetSearch_NormalizesQueryWhitespace()
    {
        var cache = new WikiMemoryCache();
        var results = new[] { new WikiSearchResult("딸기", "https://ko.stardewvalleywiki.com/딸기", "봄 작물") };

        cache.SetSearch(" 딸기   씨앗 ", results);

        Assert.True(cache.TryGetSearch("딸기 씨앗", out var cached));
        Assert.Equal("딸기", Assert.Single(cached).Title);
    }

    [Fact]
    public void TryGetExtract_NormalizesTitleWhitespace()
    {
        var cache = new WikiMemoryCache();
        var extract = new WikiPageExtract("딸기", "https://ko.stardewvalleywiki.com/딸기", "딸기 설명");

        cache.SetExtract(" 딸기 ", extract);

        Assert.True(cache.TryGetExtract("딸기", out var cached));
        Assert.Equal("딸기 설명", cached.Extract);
    }
}
