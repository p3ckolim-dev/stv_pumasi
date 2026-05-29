using Pumasi.Core.Knowledge;
using Xunit;

namespace Pumasi.Core.Tests.Knowledge;

public sealed class ConversationMemoryTests
{
    [Fact]
    public void Remember_PrunesToLimit()
    {
        var memory = new ConversationMemory(limit: 3);

        memory.Remember("user", "첫 번째");
        memory.Remember("assistant", "두 번째");
        memory.Remember("user", "세 번째");
        memory.Remember("assistant", "네 번째");

        Assert.Collection(
            memory.Turns,
            turn => Assert.Equal("두 번째", turn.Text),
            turn => Assert.Equal("세 번째", turn.Text),
            turn => Assert.Equal("네 번째", turn.Text));
    }

    [Fact]
    public void Restore_IgnoresBlankTurnsAndKeepsRecentEntries()
    {
        var memory = new ConversationMemory(limit: 2);
        var data = new ConversationMemorySaveData
        {
            Turns = new List<ConversationTurn>
            {
                new("user", "딸기 씨앗은 어디서 사?"),
                new("assistant", ""),
                new("assistant", "봄 달걀 축제에서 살 수 있어요.")
            }
        };

        memory.Restore(data);

        var saved = memory.ToSaveData();
        Assert.Collection(
            saved.Turns,
            turn => Assert.Equal("딸기 씨앗은 어디서 사?", turn.Text),
            turn => Assert.Equal("봄 달걀 축제에서 살 수 있어요.", turn.Text));
    }
}
