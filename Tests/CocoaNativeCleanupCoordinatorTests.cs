using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class CocoaNativeCleanupCoordinatorTests
{
    [Fact]
    public void DefersCleanupUntilCallbackAndOutboundCallReturn()
    {
        var coordinator = new CocoaNativeCleanupCoordinator();

        coordinator.EnterOutboundCall();
        coordinator.EnterCallback();
        coordinator.RequestCleanup();

        Assert.False(coordinator.TryBeginCleanup());

        coordinator.ExitCallback();

        Assert.False(coordinator.TryBeginCleanup());

        coordinator.ExitOutboundCall();

        Assert.True(coordinator.TryBeginCleanup());
    }

    [Fact]
    public void ExplicitBoundaryCanDrainAfterAnExternalNativeCallbackReturns()
    {
        var coordinator = new CocoaNativeCleanupCoordinator();

        coordinator.EnterCallback();
        coordinator.RequestCleanup();
        coordinator.ExitCallback();

        Assert.True(coordinator.TryBeginCleanup());
    }

    [Fact]
    public void CleanupCanOnlyBeginAndCompleteOnce()
    {
        var coordinator = new CocoaNativeCleanupCoordinator();

        coordinator.RequestCleanup();

        Assert.True(coordinator.TryBeginCleanup());
        Assert.False(coordinator.TryBeginCleanup());

        coordinator.CompleteCleanup();

        Assert.True(coordinator.CleanupCompleted);
        Assert.False(coordinator.TryBeginCleanup());
        Assert.Throws<InvalidOperationException>(coordinator.CompleteCleanup);
    }

    [Fact]
    public void NestedCallbacksRemainDeferredUntilTheOutermostCallbackReturns()
    {
        var coordinator = new CocoaNativeCleanupCoordinator();

        coordinator.EnterCallback();
        coordinator.EnterCallback();
        coordinator.RequestCleanup();
        coordinator.ExitCallback();

        Assert.False(coordinator.TryBeginCleanup());

        coordinator.ExitCallback();

        Assert.True(coordinator.TryBeginCleanup());
    }
}
