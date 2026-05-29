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
        var layout = PumasiSettingsPageLayoutFactory.Create(x: 64, y: 128, width: 880, height: 680, rowCount: 8);

        Assert.True(layout.RowLabelMaxWidth > 200);
        Assert.True(layout.RowLabelX + layout.RowLabelMaxWidth <= 64 + 880 - 88);
        Assert.True(layout.LastRowBottom < layout.FooterY);
        Assert.True(layout.FooterY <= 128 + 680 - 88);
    }
}
