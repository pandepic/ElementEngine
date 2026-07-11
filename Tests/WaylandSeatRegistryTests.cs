using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class WaylandSeatRegistryTests
{
    [Fact]
    public void RemovingBoundSeatMakesExistingFallbackAvailable()
    {
        var registry = new WaylandSeatRegistry();
        registry.Add(10, 7);
        registry.Add(20, 5);
        registry.MarkBound(10);

        Assert.True(registry.Remove(10));
        Assert.True(registry.TryGetAvailable(out var globalName, out var version));
        Assert.Equal(20u, globalName);
        Assert.Equal(5u, version);
    }

    [Fact]
    public void RemovingUnboundSeatPreservesCurrentBinding()
    {
        var registry = new WaylandSeatRegistry();
        registry.Add(10, 7);
        registry.Add(20, 5);
        registry.MarkBound(10);

        Assert.True(registry.Remove(20));
        Assert.Equal(10u, registry.BoundGlobalName);
        Assert.False(registry.TryGetAvailable(out _, out _));
    }

    [Fact]
    public void SeatAddedAfterRemovalCanBeSelected()
    {
        var registry = new WaylandSeatRegistry();
        registry.Add(10, 7);
        registry.MarkBound(10);
        registry.Remove(10);

        Assert.False(registry.TryGetAvailable(out _, out _));

        registry.Add(30, 6);

        Assert.True(registry.TryGetAvailable(out var globalName, out var version));
        Assert.Equal(30u, globalName);
        Assert.Equal(6u, version);
    }
}
