namespace Pumasi.Core.Commands;

public static class PumasiCommandParser
{
    private const string DefaultAskInstruction = "Plan safe farm chores.";

    public static PumasiCommand ParseChatInput(string input)
    {
        var parts = Split(input);
        if (parts.Length == 0)
            return PumasiCommand.None;

        var commandName = NormalizeCommandName(parts[0]);
        var args = parts.Skip(1).ToArray();
        return commandName switch
        {
            "pms" => ParsePmsArguments(args),
            "pms_ask" => CreateAsk(args, defaultToSafeChores: true),
            "pms_status" => new PumasiCommand(PumasiCommandKind.Status, string.Empty),
            "pms_scan" => new PumasiCommand(PumasiCommandKind.Scan, string.Empty),
            "pms_todo" => CreateTodo(args),
            "pms_work" => CreateWorkCategory(args),
            "pms_key" => new PumasiCommand(PumasiCommandKind.ApiKeyRejected, string.Empty),
            _ => PumasiCommand.None
        };
    }

    public static PumasiCommand ParsePmsArguments(IEnumerable<string> args)
    {
        var parts = args.Where(part => !string.IsNullOrWhiteSpace(part)).ToArray();
        if (parts.Length == 0)
            return new PumasiCommand(PumasiCommandKind.Help, string.Empty);

        var verb = parts[0].Trim().ToLowerInvariant();
        var rest = parts.Skip(1).ToArray();
        return verb switch
        {
            "ask" or "question" or "q" => CreateAsk(rest, defaultToSafeChores: true),
            "status" => new PumasiCommand(PumasiCommandKind.Status, string.Empty),
            "scan" => new PumasiCommand(PumasiCommandKind.Scan, string.Empty),
            "todo" or "todos" or "list" => CreateTodo(rest),
            "work" or "category" or "categories" or "config" => CreateWorkCategory(rest),
            "crops" or "crop" or "machines" or "machine" or "animals" or "animal" or "chests" or "chest" or "planting" or "plant" => CreateWorkCategory(parts),
            "help" or "?" => new PumasiCommand(PumasiCommandKind.Help, string.Empty),
            "key" or "apikey" or "api_key" or "gemini_key" => new PumasiCommand(PumasiCommandKind.ApiKeyRejected, string.Empty),
            _ => CreateAsk(parts, defaultToSafeChores: false)
        };
    }

    private static PumasiCommand CreateAsk(IReadOnlyCollection<string> parts, bool defaultToSafeChores)
    {
        var instruction = string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part))).Trim();
        if (instruction.Length == 0 && defaultToSafeChores)
            instruction = DefaultAskInstruction;

        return instruction.Length == 0
            ? new PumasiCommand(PumasiCommandKind.Help, string.Empty)
            : new PumasiCommand(PumasiCommandKind.Ask, instruction);
    }

    private static PumasiCommand CreateTodo(IReadOnlyCollection<string> parts)
    {
        return new PumasiCommand(
            PumasiCommandKind.Todo,
            string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part))).Trim());
    }

    private static PumasiCommand CreateWorkCategory(IReadOnlyCollection<string> parts)
    {
        return new PumasiCommand(
            PumasiCommandKind.WorkCategory,
            string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part))).Trim().ToLowerInvariant());
    }

    private static string NormalizeCommandName(string commandName)
    {
        var normalized = commandName.Trim().TrimStart('/').ToLowerInvariant();
        var underscoreIndex = normalized.LastIndexOf('_');
        if (normalized.StartsWith("p3ckolim.pms_", StringComparison.Ordinal) && underscoreIndex >= 0)
            return normalized[(underscoreIndex + 1)..] switch
            {
                "pms" => "pms",
                "ask" => "pms_ask",
                "status" => "pms_status",
                "scan" => "pms_scan",
                "todo" => "pms_todo",
                "work" => "pms_work",
                "key" => "pms_key",
                _ => normalized
            };

        return normalized;
    }

    private static string[] Split(string input)
    {
        return input
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
