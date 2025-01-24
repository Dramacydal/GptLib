namespace GptLib;

public class History
{
    private List<HistoryEntry> _history { get; set; } = new();

    public int Length => _history.Count;

    public History Copy()
    {
        var copy = new History();
        copy._history = new(_history);

        return copy;
    }

    private readonly Locker _lock = new();

    public void Lock(Action<History> h)
    {
        using var l = _lock.Acquire();

        h(this);
    }

    public T Lock<T>(Func<History, T> h)
    {
        using var l = _lock.Acquire();

        return h(this);
    }

    public void Shrink(int index) => _history = _history.Take(index).ToList();

    public void RollbackLastQuestion()
    {
        var pos = GetFirstBefore(RoleType.User, _history.Count);
        if (pos != -1)
            Shrink(pos);
    }

    public int GetFirstAfter(RoleType role, int index)
    {
        for (var i = index + 1; i < _history.Count; i++)
            if (_history[i].Role == role)
                return i;

        return -1;
    }

    public int GetFirstBefore(RoleType role, int index)
    {
        for (var i = index - 1; i >= 0; --i)
            if (_history[i].Role == role)
                return i;

        return -1;
    }

    public void Reset() => _history.Clear();

    public void Add(IEnumerable<HistoryEntry> history) => _history.AddRange(history);

    public int Find(Func<HistoryEntry, bool> predicate)
    {
        for (var i = 0; i < _history.Count; ++i)
            if (predicate(_history[i]))
                return i;

        return -1;
    }

    public void RemoveAt(int index) => _history.RemoveAt(index);

    public List<HistoryEntry> Contents => _history;
}
