using Pumasi.Core.Commands;
using Xunit;

namespace Pumasi.Core.Tests.Commands;

public sealed class PumasiCommandParserTests
{
    [Fact]
    public void ParseChatInput_RoutesBarePmsTextToAsk()
    {
        var command = PumasiCommandParser.ParseChatInput("/pms 딸기 씨앗은 어디서 사?");

        Assert.Equal(PumasiCommandKind.Ask, command.Kind);
        Assert.Equal("딸기 씨앗은 어디서 사?", command.Argument);
    }

    [Fact]
    public void ParsePmsArguments_RoutesAskVerbToAsk()
    {
        var command = PumasiCommandParser.ParsePmsArguments(new[] { "ask", "온실", "수확해줘" });

        Assert.Equal(PumasiCommandKind.Ask, command.Kind);
        Assert.Equal("온실 수확해줘", command.Argument);
    }

    [Theory]
    [InlineData("/pms todo up 2", "up 2")]
    [InlineData("/pms_todo move 3 1", "move 3 1")]
    public void ParseChatInput_PreservesTodoArguments(string input, string expectedArgument)
    {
        var command = PumasiCommandParser.ParseChatInput(input);

        Assert.Equal(PumasiCommandKind.Todo, command.Kind);
        Assert.Equal(expectedArgument, command.Argument);
    }

    [Theory]
    [InlineData("/pms animals on", "animals on")]
    [InlineData("/pms work animals off", "animals off")]
    [InlineData("/pms_work animals on", "animals on")]
    public void ParseChatInput_RoutesWorkCategoryCommands(string input, string expectedArgument)
    {
        var command = PumasiCommandParser.ParseChatInput(input);

        Assert.Equal(PumasiCommandKind.WorkCategory, command.Kind);
        Assert.Equal(expectedArgument, command.Argument);
    }

    [Theory]
    [InlineData("/pms status", PumasiCommandKind.Status)]
    [InlineData("/pms_status", PumasiCommandKind.Status)]
    [InlineData("/pms scan", PumasiCommandKind.Scan)]
    [InlineData("/pms_scan", PumasiCommandKind.Scan)]
    [InlineData("/pms todo", PumasiCommandKind.Todo)]
    [InlineData("/pms_todo", PumasiCommandKind.Todo)]
    [InlineData("/pms help", PumasiCommandKind.Help)]
    public void ParseChatInput_RoutesKnownVerbs(string input, PumasiCommandKind expected)
    {
        var command = PumasiCommandParser.ParseChatInput(input);

        Assert.Equal(expected, command.Kind);
        Assert.Equal(string.Empty, command.Argument);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("/other status")]
    [InlineData("")]
    public void ParseChatInput_IgnoresNonPmsInput(string input)
    {
        var command = PumasiCommandParser.ParseChatInput(input);

        Assert.Equal(PumasiCommandKind.None, command.Kind);
    }

    [Fact]
    public void ParsePmsArguments_RejectsApiKeyWithoutRetainingSecret()
    {
        var command = PumasiCommandParser.ParsePmsArguments(new[] { "key", "super-secret" });

        Assert.Equal(PumasiCommandKind.ApiKeyRejected, command.Kind);
        Assert.Equal(string.Empty, command.Argument);
    }
}
