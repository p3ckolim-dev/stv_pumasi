namespace StardewAIFarmHelper.Core.Tasks;

public sealed record TaskProposal(
    TaskType Type,
    TaskTarget Target,
    int Priority,
    string Reason,
    string Source = "system")
{
    public string Key => TaskKeyFactory.Create(Type, Target);
}
