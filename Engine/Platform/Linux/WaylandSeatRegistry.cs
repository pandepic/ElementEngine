using System;
using System.Collections.Generic;

namespace ElementEngine;

internal sealed class WaylandSeatRegistry
{
    private readonly SortedDictionary<uint, uint> _globals = [];

    public uint? BoundGlobalName { get; private set; }

    public void Add(uint globalName, uint version) => _globals[globalName] = version;

    public bool Remove(uint globalName)
    {
        if (!_globals.Remove(globalName))
            return false;

        if (BoundGlobalName == globalName)
            BoundGlobalName = null;

        return true;
    }

    public bool TryGetAvailable(out uint globalName, out uint version)
    {
        if (BoundGlobalName.HasValue)
        {
            globalName = 0;
            version = 0;
            return false;
        }

        foreach (var global in _globals)
        {
            globalName = global.Key;
            version = global.Value;
            return true;
        }

        globalName = 0;
        version = 0;
        return false;
    }

    public void MarkBound(uint globalName)
    {
        if (!_globals.ContainsKey(globalName))
            throw new ArgumentOutOfRangeException(nameof(globalName));

        BoundGlobalName = globalName;
    }

    public void Clear()
    {
        _globals.Clear();
        BoundGlobalName = null;
    }
}
