namespace Pumasi.Core.Ui;

public sealed record TodoOverlayPanel(int X, int Y, int Width, int Height, int Padding, int LineCount)
{
    public int TextX => X + Padding;
    public int TextY => Y + Padding;
    public int Bottom => Y + Height;
    public int InnerWidth => Width - Padding * 2;
}

public static class TodoOverlayLayout
{
    private const int Margin = 24;
    private const int DefaultX = 24;
    private const int DefaultY = 320;
    private const int PanelWidth = 520;
    private const int MinPanelWidth = 240;
    private const int Padding = 18;
    private const int MaxTodoRows = 8;

    public static TodoOverlayPanel Create(int activeTodoCount, int lineHeight, int viewportWidth, int viewportHeight)
    {
        if (lineHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(lineHeight), "Line height must be greater than zero.");

        var todoLines = Math.Max(1, Math.Min(activeTodoCount, GetVisibleTodoCapacity(lineHeight, viewportHeight)));
        var lineCount = 1 + todoLines;
        var availableWidth = Math.Max(MinPanelWidth, viewportWidth - Margin * 2);
        var width = Math.Min(PanelWidth, availableWidth);
        var height = Padding * 2 + lineHeight * lineCount;
        var maxX = Math.Max(Margin, viewportWidth - width - Margin);
        var maxY = Math.Max(Margin, viewportHeight - height - Margin);
        var x = Math.Clamp(DefaultX, Margin, maxX);
        var y = Math.Clamp(DefaultY, Margin, maxY);

        return new TodoOverlayPanel(x, y, width, height, Padding, lineCount);
    }

    public static int GetVisibleTodoCapacity(int lineHeight, int viewportHeight)
    {
        if (lineHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(lineHeight), "Line height must be greater than zero.");

        var availableHeight = Math.Max(lineHeight * 2 + Padding * 2, viewportHeight - Margin * 2);
        var lineCapacity = Math.Max(2, (availableHeight - Padding * 2) / lineHeight);

        return Math.Clamp(lineCapacity - 1, 1, MaxTodoRows);
    }
}
