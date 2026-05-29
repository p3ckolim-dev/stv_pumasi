using Pumasi.Core.Chat;
using Pumasi.Core.Configuration;
using Xunit;

namespace Pumasi.Core.Tests.Chat;

public sealed class HelperChatFormatterTests
{
    [Fact]
    public void FormatAnswer_ReturnsNamedAnswerLine()
    {
        var lines = HelperChatFormatter.FormatAnswer("품앗이", "딸기 씨앗은 봄 달걀 축제에서 살 수 있어요.", Array.Empty<string>());

        var line = Assert.Single(lines);
        Assert.Equal("품앗이: 딸기 씨앗은 봄 달걀 축제에서 살 수 있어요.", line);
    }

    [Fact]
    public void FormatAnswer_HidesSourcesFromChatOutput()
    {
        var lines = HelperChatFormatter.FormatAnswer(
            "품앗이",
            "딸기 씨앗은 축제에서 살 수 있어요.",
            new[]
            {
                "딸기 - https://ko.stardewvalleywiki.com/딸기",
                "달걀 축제 - https://ko.stardewvalleywiki.com/달걀_축제",
                "봄 - https://ko.stardewvalleywiki.com/봄",
                "피에르네 잡화점 - https://ko.stardewvalleywiki.com/피에르네_잡화점"
            });

        var line = Assert.Single(lines);
        Assert.Equal("품앗이: 딸기 씨앗은 축제에서 살 수 있어요.", line);
    }

    [Fact]
    public void FormatAnswer_HidesEnglishSourcesFromChatOutput()
    {
        var lines = HelperChatFormatter.FormatAnswer(
            "Pumasi",
            "Strawberry seeds are sold at the Egg Festival.",
            new[] { "Strawberry", "Egg Festival", "Spring", "Pierre" },
            UiLanguage.English);

        var line = Assert.Single(lines);
        Assert.Equal("Pumasi: Strawberry seeds are sold at the Egg Festival.", line);
    }

    [Fact]
    public void FormatAnswer_CollapsesWhitespaceAndUsesFallbackName()
    {
        var lines = HelperChatFormatter.FormatAnswer(" ", "  지금은\n답변할 수 없어요.  ", Array.Empty<string>());

        var line = Assert.Single(lines);
        Assert.Equal("pumasi: 지금은 답변할 수 없어요.", line);
    }
}
