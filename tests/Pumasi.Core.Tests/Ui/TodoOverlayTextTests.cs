using Pumasi.Core.Ui;
using Xunit;

namespace Pumasi.Core.Tests.Ui;

public sealed class TodoOverlayTextTests
{
    [Fact]
    public void FormatTitle_UsesHelperNameWithoutRepeatingStatus()
    {
        var title = TodoOverlayText.FormatTitle("Pumasi");

        Assert.Equal("Pumasi", title);
    }

    [Fact]
    public void FormatTitle_UsesFallbackNameWhenHelperNameIsBlank()
    {
        var title = TodoOverlayText.FormatTitle(" ");

        Assert.Equal("pumasi", title);
    }
}
