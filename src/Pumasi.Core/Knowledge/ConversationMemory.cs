namespace Pumasi.Core.Knowledge;

public sealed class ConversationMemorySaveData
{
    public List<ConversationTurn> Turns { get; set; } = new();
}

public sealed class ConversationMemory
{
    private readonly int limit;
    private readonly List<ConversationTurn> turns = new();

    public ConversationMemory(int limit)
    {
        this.limit = Math.Max(1, limit);
    }

    public IReadOnlyList<ConversationTurn> Turns => turns;

    public void Remember(string role, string text)
    {
        if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(text))
            return;

        turns.Add(new ConversationTurn(role.Trim(), text.Trim()));
        Prune();
    }

    public void Restore(ConversationMemorySaveData? data)
    {
        turns.Clear();
        if (data is null)
            return;

        foreach (var turn in data.Turns)
            Remember(turn.Role, turn.Text);
    }

    public void Clear()
    {
        turns.Clear();
    }

    public ConversationMemorySaveData ToSaveData()
    {
        return new ConversationMemorySaveData
        {
            Turns = turns.ToList()
        };
    }

    private void Prune()
    {
        while (turns.Count > limit)
            turns.RemoveAt(0);
    }
}
