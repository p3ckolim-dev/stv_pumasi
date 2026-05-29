using Pumasi.Core.Ui;
using Xunit;

namespace Pumasi.Core.Tests.Ui;

public sealed class PumasiSettingsPageLayoutTests
{
    [Fact]
    public void Create_StartsTitleBelowGameMenuTabArea()
    {
        var layout = PumasiSettingsPageLayoutFactory.Create(x: 64, y: 128, width: 880, height: 680, rowCount: 8);

        Assert.True(layout.TitleY >= 228);
        Assert.True(layout.FirstRowY > layout.SubtitleY + 28);
    }

    [Fact]
    public void Create_KeepsRowsAndFooterInsideMenuBounds()
    {
        var layout = PumasiSettingsPageLayoutFactory.Create(x: 64, y: 128, width: 880, height: 680, rowCount: 12);

        Assert.True(layout.RowLabelMaxWidth > 200);
        Assert.True(layout.RowLabelX + layout.RowLabelMaxWidth <= 64 + 880 - 88);
        Assert.True(layout.LastRowBottom < layout.FooterY);
        Assert.True(layout.FooterY <= 128 + 680 - 88);
    }

    [Fact]
    public void Create_ReservesScrollbarWhenRowsOverflow()
    {
        var layout = PumasiSettingsPageLayoutFactory.Create(x: 64, y: 128, width: 880, height: 680, rowCount: 12);

        Assert.True(layout.HasScrollbar);
        Assert.True(layout.ScrollbarX > layout.RowLabelX);
        Assert.True(layout.RowLabelX + layout.RowLabelMaxWidth < layout.ScrollbarX);
        Assert.True(layout.ScrollbarTrackBottom < layout.FooterY);
    }

    [Theory]
    [InlineData(-1, 9, 6, 0)]
    [InlineData(0, 9, 6, 0)]
    [InlineData(2, 9, 6, 2)]
    [InlineData(8, 9, 6, 3)]
    public void ClampScrollIndex_ClampsToReachLastRow(int requested, int rowCount, int visibleRows, int expected)
    {
        Assert.Equal(expected, PumasiSettingsScroll.ClampScrollIndex(requested, rowCount, visibleRows));
    }

    [Fact]
    public void CreateTabBounds_AttachesToLeftSideOfTabRow()
    {
        var bounds = PumasiSettingsTabLayoutFactory.Create(
            menuX: 64,
            menuY: 128,
            menuWidth: 880,
            viewportWidth: 960,
            anchorLeft: 136,
            anchorY: 104,
            anchorWidth: 64,
            anchorHeight: 64);

        Assert.Equal(72, bounds.X);
        Assert.Equal(104, bounds.Y);
        Assert.Equal(136, bounds.Right);
    }

    [Fact]
    public void CreateTabBounds_ClampsLeftTabInsideViewport()
    {
        var bounds = PumasiSettingsTabLayoutFactory.Create(
            menuX: 0,
            menuY: 128,
            menuWidth: 880,
            viewportWidth: 960,
            anchorLeft: 48,
            anchorY: 104,
            anchorWidth: 64,
            anchorHeight: 64);

        Assert.Equal(24, bounds.X);
        Assert.Equal(104, bounds.Y);
    }

    [Fact]
    public void SettingsTypography_UsesScaledSmallFontForDescriptions()
    {
        Assert.Equal(PumasiFontRole.Small, PumasiSettingsTypography.DescriptionFont);
        Assert.InRange(PumasiSettingsTypography.DescriptionScale, 0.5f, 0.9f);
    }
}
