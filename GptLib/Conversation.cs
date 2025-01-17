namespace GptLib;

public class Conversation
{
    public Guid Guid { get; set; } = Guid.NewGuid();

    public DateTime Date { get; set; } = DateTime.Now;

    public List<HistoryEntry> History { get; set; } = new();
    
    public int Size => History.Count;
    
    public HistoryEntry? Last => History.Count > 0 ?  History[^1] : null;

    public HistoryEntry CreateEntry()
    {
        var entry = new HistoryEntry();
        History.Add(entry);

        return entry;
    }

    public void Shrink(int index)
    {
        History = History.Take(index).ToList();
    }

    public void RemoveLast()
    {
        History.RemoveAt(History.Count - 1);
    }
}
