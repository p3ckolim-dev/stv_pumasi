namespace StardewAIFarmHelper.Core.Configuration;

public sealed class GeminiConfig
{
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public string Model { get; set; } = "gemini-2.5-flash";
    public string ApiKey { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxCallsPerDay { get; set; } = 20;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Model);

    public override string ToString()
    {
        var keyState = string.IsNullOrWhiteSpace(ApiKey) ? "not-set" : "set";
        return $"GeminiConfig(BaseUrl={BaseUrl}, Model={Model}, ApiKey={keyState}, TimeoutSeconds={TimeoutSeconds}, MaxCallsPerDay={MaxCallsPerDay})";
    }
}
