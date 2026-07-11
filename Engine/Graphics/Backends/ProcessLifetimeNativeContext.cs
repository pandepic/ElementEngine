using Silk.NET.Core.Contexts;
using System;
using System.Runtime.InteropServices;

namespace ElementEngine;

internal sealed class ProcessLifetimeNativeContext : INativeContext
{
    private readonly nint _library;

    public ProcessLifetimeNativeContext(params string[] libraryNames)
    {
        foreach (var libraryName in libraryNames)
        {
            // The assembly overload adds DllImport-style probing (app base directory,
            // NATIVE_DLL_SEARCH_DIRECTORIES); the plain overload is a bare dlopen, which
            // misses natives published next to the executable on Linux/macOS.
            if (NativeLibrary.TryLoad(libraryName, typeof(ProcessLifetimeNativeContext).Assembly, null, out _library))
                return;
        }

        throw new DllNotFoundException(
            $"Unable to load any native library candidate: {string.Join(", ", libraryNames)}.");
    }

    public nint GetProcAddress(string proc, int? slot = default)
    {
        return NativeLibrary.GetExport(_library, proc);
    }

    public bool TryGetProcAddress(string proc, out nint addr, int? slot = default)
    {
        return NativeLibrary.TryGetExport(_library, proc, out addr);
    }

    public void Dispose()
    {
    }
}
