using Silk.NET.Core.Contexts;
using Silk.NET.SPIRV.Cross;
using System;
using System.IO;
using System.Runtime.InteropServices;
using ShadercApi = Silk.NET.Shaderc.Shaderc;

namespace ElementEngine;

internal static class NativeShaderTools
{
    private static readonly Lazy<ProcessLifetimeNativeContext> ShadercContext =
        new(() => CreateProcessLifetimeContext("shaderc_shared"));
    private static readonly Lazy<ProcessLifetimeNativeContext> SpirvCrossContext =
        new(() => CreateProcessLifetimeContext("spirv-cross"));

    public static ShadercApi CreateShaderc()
    {
        return new ShadercApi(ShadercContext.Value);
    }

    public static Cross CreateSpirvCross()
    {
        return new Cross(SpirvCrossContext.Value);
    }

    private static ProcessLifetimeNativeContext CreateProcessLifetimeContext(string baseName)
    {
        var libraryName = GetLibraryName(baseName);
        return new ProcessLifetimeNativeContext(
            new DefaultNativeContext(FindBundledLibrary(libraryName) ?? libraryName));
    }

    private static string FindBundledLibrary(string libraryName)
    {
        var runtimeIdentifier = GetPortableRuntimeIdentifier();
        if (runtimeIdentifier is null)
            return null;

        var path = Path.Combine(
            AppContext.BaseDirectory,
            "runtimes",
            runtimeIdentifier,
            "native",
            libraryName);
        return File.Exists(path) ? path : null;
    }

    private static string GetPortableRuntimeIdentifier()
    {
        var platform = OperatingSystem.IsWindows()
            ? "win"
            : OperatingSystem.IsLinux()
                ? "linux"
                : OperatingSystem.IsMacOS()
                    ? "osx"
                    : null;
        if (platform is null)
            return null;

        var architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => null,
        };
        return architecture is null ? null : $"{platform}-{architecture}";
    }

    private static string GetLibraryName(string baseName)
    {
        if (OperatingSystem.IsWindows())
            return $"{baseName}.dll";
        if (OperatingSystem.IsMacOS())
            return $"lib{baseName}.dylib";
        return $"lib{baseName}.so";
    }

    private sealed class ProcessLifetimeNativeContext(INativeContext context) : INativeContext
    {
        public nint GetProcAddress(string proc, int? slot = default)
        {
            return context.GetProcAddress(proc, slot);
        }

        public bool TryGetProcAddress(string proc, out nint addr, int? slot = default)
        {
            return context.TryGetProcAddress(proc, out addr, slot);
        }

        public void Dispose()
        {
            // API wrappers share this native library, which remains loaded until process exit.
        }
    }
}
