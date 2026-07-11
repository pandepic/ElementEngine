using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace ElementEngine;

internal sealed class TeardownCollector
{
    private readonly List<Exception> _exceptions = [];

    public TeardownCollector()
    {
    }

    public TeardownCollector(Exception initialException)
    {
        Add(initialException);
    }

    public void Run(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            action();
        }
        catch (Exception exception)
        {
            Add(exception);
        }
    }

    public void ThrowIfFailed(string message)
    {
        if (_exceptions.Count == 0)
            return;

        if (_exceptions.Count == 1)
            ExceptionDispatchInfo.Capture(_exceptions[0]).Throw();

        throw new AggregateException(message, _exceptions);
    }

    private void Add(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is AggregateException aggregate)
            _exceptions.AddRange(aggregate.Flatten().InnerExceptions);
        else
            _exceptions.Add(exception);
    }
}
