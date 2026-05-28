using Pumasi.Core.Ui;
using Xunit;

namespace Pumasi.Core.Tests.Ui;

public sealed class TodoOverlayLayoutTests
{
    [Fact]
    public void Create_StartsBelowTopHudForIdlePanel()
    {
        var panel = TodoOverlayLayout.Create(activeTodoCount: 0, lineHeight: 40, viewportWidth: 800, viewportHeight: 600);

        Assert.Equal(24, panel.X);
        Assert.Equal(320, panel.Y);
        Assert.Equal(2, panel.LineCount);
        Assert.Equal(116, panel.Height);
    }

    [Fact]
    public void Create_ReducesVisibleTodoRowsWhenViewportIsShort()
    {
        var panel = TodoOverlayLayout.Create(activeTodoCount: 8, lineHeight: 40, viewportWidth: 800, viewportHeight: 360);

        Assert.Equal(5, TodoOverlayLayout.GetVisibleTodoCapacity(lineHeight: 40, viewportHeight: 360));
        Assert.Equal(6, panel.LineCount);
        Assert.Equal(276, panel.Height);
        Assert.Equal(60, panel.Y);
        Assert.True(panel.Bottom <= 336);
    }
}
