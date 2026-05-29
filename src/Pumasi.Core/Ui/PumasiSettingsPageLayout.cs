namespace Pumasi.Core.Ui;

public sealed record PumasiSettingsPageLayout(
    int ContentX,
    int ContentRight,
    int TitleY,
    int SubtitleY,
    int FirstRowY,
    int RowHeight,
    int CheckboxX,
    int CheckboxSize,
    int RowLabelX,
    int RowLabelMaxWidth,
    int FooterY,
    int FooterMaxWidth,
    int VisibleRows,
    int LastRowBottom,
    bool HasScrollbar,
    int ScrollbarX,
    int ScrollbarWidth,
    int ScrollbarTrackY,
    int ScrollbarTrackHeight)
{
    public int ScrollbarTrackBottom => ScrollbarTrackY + ScrollbarTrackHeight;
}

public sealed record PumasiSettingsScrollbarThumb(int X, int Y, int Width, int Height)
{
    public int Bottom => Y + Height;
}

public sealed record PumasiSettingsTabBounds(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
}

public static class PumasiSettingsPageLayoutFactory
{
    private const int ContentInset = 88;
    private const int TopClearance = 112;
    private const int TitleHeight = 48;
    private const int SubtitleHeight = 28;
    private const int TitleToRowsGap = 20;
    private const int RowHeight = 68;
    private const int CheckboxSize = 32;
    private const int CheckboxTextGap = 20;
    private const int ScrollbarWidth = 18;
    private const int ScrollbarGap = 22;
    private const int FooterInset = 88;
    private const int FooterHeight = 34;

    public static PumasiSettingsPageLayout Create(int x, int y, int width, int height, int rowCount)
    {
        var contentX = x + ContentInset;
        var contentRight = x + width - ContentInset;
        var titleY = y + TopClearance;
        var subtitleY = titleY + TitleHeight;
        var firstRowY = subtitleY + SubtitleHeight + TitleToRowsGap;
        var footerY = y + height - FooterInset;
        var availableRows = Math.Max(1, (footerY - firstRowY - FooterHeight) / RowHeight);
        var visibleRows = Math.Min(Math.Max(0, rowCount), availableRows);
        var hasScrollbar = rowCount > visibleRows;
        var rowLabelX = contentX + CheckboxSize + CheckboxTextGap;
        var scrollbarReserve = hasScrollbar ? ScrollbarWidth + ScrollbarGap : 0;
        var rowLabelMaxWidth = Math.Max(80, contentRight - rowLabelX - scrollbarReserve);
        var lastRowBottom = firstRowY + visibleRows * RowHeight;
        var scrollbarTrackHeight = Math.Max(RowHeight, lastRowBottom - firstRowY - 8);

        return new PumasiSettingsPageLayout(
            contentX,
            contentRight,
            titleY,
            subtitleY,
            firstRowY,
            RowHeight,
            contentX,
            CheckboxSize,
            rowLabelX,
            rowLabelMaxWidth,
            footerY,
            contentRight - contentX,
            visibleRows,
            lastRowBottom,
            hasScrollbar,
            contentRight - ScrollbarWidth,
            ScrollbarWidth,
            firstRowY,
            scrollbarTrackHeight);
    }
}

public static class PumasiSettingsScroll
{
    public static int ClampScrollIndex(int requested, int rowCount, int visibleRows)
    {
        var max = Math.Max(0, rowCount - Math.Max(0, visibleRows));
        return Math.Clamp(requested, 0, max);
    }

    public static PumasiSettingsScrollbarThumb CreateThumb(PumasiSettingsPageLayout layout, int rowCount, int scrollIndex)
    {
        if (!layout.HasScrollbar || rowCount <= 0)
            return new PumasiSettingsScrollbarThumb(layout.ScrollbarX, layout.ScrollbarTrackY, layout.ScrollbarWidth, layout.ScrollbarTrackHeight);

        var maxScroll = Math.Max(1, rowCount - layout.VisibleRows);
        var ratio = Math.Clamp(layout.VisibleRows / (double)rowCount, 0.05, 1.0);
        var thumbHeight = Math.Max(28, (int)Math.Round(layout.ScrollbarTrackHeight * ratio));
        thumbHeight = Math.Min(layout.ScrollbarTrackHeight, thumbHeight);
        var travel = Math.Max(0, layout.ScrollbarTrackHeight - thumbHeight);
        var y = layout.ScrollbarTrackY + (int)Math.Round(travel * (PumasiSettingsScroll.ClampScrollIndex(scrollIndex, rowCount, layout.VisibleRows) / (double)maxScroll));

        return new PumasiSettingsScrollbarThumb(layout.ScrollbarX, y, layout.ScrollbarWidth, thumbHeight);
    }

    public static int IndexFromTrackClick(PumasiSettingsPageLayout layout, int rowCount, int clickY)
    {
        if (!layout.HasScrollbar)
            return 0;

        var maxScroll = Math.Max(0, rowCount - layout.VisibleRows);
        if (maxScroll == 0)
            return 0;

        var relative = Math.Clamp(clickY - layout.ScrollbarTrackY, 0, Math.Max(1, layout.ScrollbarTrackHeight));
        var ratio = relative / (double)Math.Max(1, layout.ScrollbarTrackHeight);
        return ClampScrollIndex((int)Math.Round(maxScroll * ratio), rowCount, layout.VisibleRows);
    }
}

public static class PumasiSettingsTabLayoutFactory
{
    private const int ScreenMargin = 24;
    private const int MenuRightPadding = 24;
    private const int FallbackYOffset = 8;

    public static PumasiSettingsTabBounds Create(
        int menuX,
        int menuY,
        int menuWidth,
        int viewportWidth,
        int anchorRight,
        int anchorY,
        int anchorWidth,
        int anchorHeight)
    {
        var width = Math.Max(48, anchorWidth);
        var height = Math.Max(48, anchorHeight);
        var safeRight = Math.Min(viewportWidth - ScreenMargin, menuX + menuWidth - MenuRightPadding);
        var topX = anchorRight;

        if (topX + width <= safeRight)
            return new PumasiSettingsTabBounds(topX, anchorY, width, height);

        var fallbackX = Math.Max(menuX + ScreenMargin, safeRight - width);
        var fallbackY = anchorY + height + FallbackYOffset;
        return new PumasiSettingsTabBounds(fallbackX, fallbackY, width, height);
    }
}
