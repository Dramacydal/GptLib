namespace GptLib;

public class GptSettings
{
    public double? Temperature;
    
    public List<string> Instructions { get; set; } = new();
}
