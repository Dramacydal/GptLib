namespace GptLib;

public class GptQuestion
{
    public string Role { get; set; }
    
    public string Text { get; set; }
    
    public string Tag { get; set; }

    public List<string> Files { get; set; } = new();
}