namespace Pumasi.Core.Configuration;

public sealed class WikiAnswerConfig
{
    public bool WikiAnswersEnabled { get; set; } = true;
    public string WikiBaseUrl { get; set; } = "https://ko.stardewvalleywiki.com";
    public int WikiMaxPages { get; set; } = 3;
    public int WikiContextCharacterLimit { get; set; } = 8000;
    public int WikiQuestionCooldownSeconds { get; set; } = 10;
}
