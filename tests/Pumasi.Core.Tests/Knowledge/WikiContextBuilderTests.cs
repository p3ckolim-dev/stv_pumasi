using Pumasi.Core.Knowledge;
using Xunit;

namespace Pumasi.Core.Tests.Knowledge;

public sealed class WikiContextBuilderTests
{
    [Fact]
    public void Build_RespectsMaxPages()
    {
        var builder = new WikiContextBuilder(new WikiContextOptions(MaxPages: 2, CharacterLimit: 500));
        var pages = new[]
        {
            new WikiPageExtract("딸기", "https://ko.stardewvalleywiki.com/딸기", "딸기는 봄 작물입니다."),
            new WikiPageExtract("블루베리", "https://ko.stardewvalleywiki.com/블루베리", "블루베리는 여름 작물입니다."),
            new WikiPageExtract("크랜베리", "https://ko.stardewvalleywiki.com/크랜베리", "크랜베리는 가을 작물입니다.")
        };

        var context = builder.Build(pages);

        Assert.Contains("딸기", context.ContextText);
        Assert.Contains("블루베리", context.ContextText);
        Assert.DoesNotContain("크랜베리", context.ContextText);
        Assert.Equal(2, context.Sources.Count);
    }

    [Fact]
    public void Build_RespectsCharacterLimit()
    {
        var builder = new WikiContextBuilder(new WikiContextOptions(MaxPages: 3, CharacterLimit: 120));
        var pages = new[]
        {
            new WikiPageExtract("긴 문서", "https://ko.stardewvalleywiki.com/긴_문서", new string('가', 500))
        };

        var context = builder.Build(pages);

        Assert.True(context.ContextText.Length <= 120);
        Assert.Single(context.Sources);
    }
}
