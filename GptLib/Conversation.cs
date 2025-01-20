namespace GptLib;

public class Conversation
{
    public Guid Guid { get; set; } = Guid.NewGuid();

    public string UsageContext { get; set; } = "default";

    public DateTime Date { get; set; } = DateTime.Now;

    public History History { get; set; } = new();
}
    
