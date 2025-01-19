namespace GptLib;

public class Conversation
{
    public Guid Guid { get; set; } = Guid.NewGuid();

    public string UsageContext { get; set; } = "default";
    
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

    public void RollbackLastQuestion()
    {
        var pos = GetFirstBefore(RoleType.User, History.Count);
        if (pos != -1)
            Shrink(pos);
    }

    public int GetFirstAfter(RoleType role, int index)
    {
        for (var i = index + 1; i < History.Count; i++)
            if (History[i].Role == role)
                return i;

        return -1;
    }

    public int GetFirstBefore(RoleType role, int index)
    {
        for (var i = index - 1; i >= 0; --i)
            if (History[i].Role == role)
                return i;

        return -1;
    }

    public void Reset() => History.Clear();
}
