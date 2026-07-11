using System;

namespace ElementEngine;

internal sealed class WaylandThreadGuard
{
    private readonly int _ownerThreadId = Environment.CurrentManagedThreadId;

    public void EnsureCurrentThread()
    {
        if (Environment.CurrentManagedThreadId != _ownerThreadId)
        {
            throw new InvalidOperationException(
                "Wayland window operations must run on the thread that created the window.");
        }
    }
}
