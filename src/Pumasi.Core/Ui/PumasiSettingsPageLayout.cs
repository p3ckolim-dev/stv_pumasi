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
    int LastRowBottom);

public static class PumasiSettingsPageLayoutFactory
{
    private const int ContentInset = 88;
    private const int TopClearance = 112;
    private const int TitleHeight = 48;
    private const int SubtitleHeight = 28;
    private const int TitleToRowsGap = 20;
    private const int RowHeight = 56;
    private const int CheckboxSize = 32;
    private const int CheckboxTextGap = 20;
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
        var rowLabelX = contentX + CheckboxSize + CheckboxTextGap;
        var rowLabelMaxWidth = Math.Max(80, contentRight - rowLabelX);
        var lastRowBottom = firstRowY + visibleRows * RowHeight;

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
            lastRowBottom);
    }
}
