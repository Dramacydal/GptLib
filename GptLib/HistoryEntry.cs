namespace GptLib;

public class HistoryEntry
{
    public RoleType Role { get; set; }

    public string Text { get; set; }

    public List<string> UploadedFiles { get; set; } = new();

    public DateTime Time { get; set; }
    
    public string Tag { get; set; }

    public bool Error { get; set; }
}
