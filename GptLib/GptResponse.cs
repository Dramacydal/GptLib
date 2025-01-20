namespace GptLib;

public class GptResponse
{
    public bool Success { get; set; }

    public HistoryEntry Question { get; set; }

    public HistoryEntry Answer { get; set; }
}
