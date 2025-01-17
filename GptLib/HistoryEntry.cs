namespace GptLib;

public class HistoryEntry
{
    public string Role { get; set; }

    public string Question { get; set; }

    public string Answer { get; set; }

    public List<string> UploadedFiles { get; set; } = new();

    public DateTime Time { get; set; }

    public bool Error { get; set; }
}
