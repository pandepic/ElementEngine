using System;

namespace ElementEngine;

internal sealed class CocoaNativeCleanupCoordinator
{
    private int _callbackDepth;
    private int _outboundCallDepth;
    private bool _cleanupRequested;
    private bool _cleanupStarted;

    public bool CleanupCompleted { get; private set; }

    public void EnterCallback()
    {
        _callbackDepth = checked(_callbackDepth + 1);
    }

    public void ExitCallback()
    {
        if (_callbackDepth == 0)
            throw new InvalidOperationException("No Cocoa callback is active.");

        _callbackDepth--;
    }

    public void EnterOutboundCall()
    {
        _outboundCallDepth = checked(_outboundCallDepth + 1);
    }

    public void ExitOutboundCall()
    {
        if (_outboundCallDepth == 0)
            throw new InvalidOperationException("No Cocoa outbound call is active.");

        _outboundCallDepth--;
    }

    public void RequestCleanup()
    {
        _cleanupRequested = true;
    }

    public bool TryBeginCleanup()
    {
        if (!_cleanupRequested ||
            _cleanupStarted ||
            _callbackDepth != 0 ||
            _outboundCallDepth != 0)
        {
            return false;
        }

        _cleanupStarted = true;
        return true;
    }

    public void CompleteCleanup()
    {
        if (!_cleanupStarted || CleanupCompleted)
            throw new InvalidOperationException("Cocoa native cleanup is not awaiting completion.");

        CleanupCompleted = true;
    }
}
