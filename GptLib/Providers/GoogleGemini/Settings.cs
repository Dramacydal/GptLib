namespace GptLib.Providers.GoogleGemini;

public class Settings : GptSettings
{
    public Dictionary<string, string> SafetySettings = new();
    
    public string ResponseMimeType { get; set; }

    public List<string> Instructions { get; set; } = new();
}