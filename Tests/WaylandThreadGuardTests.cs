using System;
using System.Threading;
using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

public sealed class WaylandThreadGuardTests
{
    [Fact]
    public void CreatingThreadIsAccepted()
    {
        var guard = new WaylandThreadGuard();

        guard.EnsureCurrentThread();
    }

    [Fact]
    public void DifferentThreadIsRejected()
    {
        var guard = new WaylandThreadGuard();
        Exception? exception = null;
        var thread = new Thread(() => exception = Record.Exception(guard.EnsureCurrentThread));

        thread.Start();
        thread.Join();

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("thread that created the window", invalidOperation.Message, StringComparison.Ordinal);
    }
}
