using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SharpMetal.Foundation;
using SharpMetal.Metal;
using Silk.NET.Shaderc;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using CrossBackend = Silk.NET.SPIRV.Cross.Backend;
using CrossCompiler = Silk.NET.SPIRV.Cross.Compiler;
using CrossResult = Silk.NET.SPIRV.Cross.Result;
using ShadercCompiler = Silk.NET.Shaderc.Compiler;

namespace ElementEngine;

[SupportedOSPlatform("macos")]
internal unsafe sealed class MetalShaderCompiler : IDisposable
{
    private Shaderc _shaderc;
    private Cross _cross;
    private ShadercCompiler* _compiler;
    private CompileOptions* _compileOptions;
    private readonly bool _debug;
    private bool _disposed;

    public MetalShaderCompiler(bool debug)
    {
        _debug = debug;
        _shaderc = NativeShaderTools.CreateShaderc();

        try
        {
            _cross = NativeShaderTools.CreateSpirvCross();
            _compiler = _shaderc.CompilerInitialize();
            _compileOptions = _shaderc.CompileOptionsInitialize();

            if (_compiler == null || _compileOptions == null)
                throw new InvalidOperationException("Failed to initialize shaderc for Metal.");

            _shaderc.CompileOptionsSetSourceLanguage(_compileOptions, Silk.NET.Shaderc.SourceLanguage.Glsl);
            _shaderc.CompileOptionsSetTargetEnv(_compileOptions, TargetEnv.Vulkan, (uint)EnvVersion.Vulkan11);
            _shaderc.CompileOptionsSetOptimizationLevel(
                _compileOptions,
                debug ? OptimizationLevel.Zero : OptimizationLevel.Performance);
            if (debug)
                _shaderc.CompileOptionsSetGenerateDebugInfo(_compileOptions);
        }
        catch (Exception creationException)
        {
            var teardown = new TeardownCollector(creationException);

            var compileOptions = _compileOptions;
            _compileOptions = null;
            if (compileOptions != null)
                teardown.Run(() => _shaderc.CompileOptionsRelease(compileOptions));

            var compiler = _compiler;
            _compiler = null;
            if (compiler != null)
                teardown.Run(() => _shaderc.CompilerRelease(compiler));

            var cross = _cross;
            _cross = null;
            if (cross is not null)
                teardown.Run(cross.Dispose);

            var shaderc = _shaderc;
            _shaderc = null;
            if (shaderc is not null)
                teardown.Run(shaderc.Dispose);

            teardown.ThrowIfFailed("Metal shader compiler creation and cleanup both failed.");
            throw;
        }
    }

    public byte[] CompileGlsl(ShaderDescription description)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (description.Stage is not ShaderStages.Vertex and not ShaderStages.Fragment)
            throw new NotSupportedException($"Metal does not support the {description.Stage} shader stage in the current graphics contract.");
        if (description.ShaderBytes is null || description.ShaderBytes.Length == 0)
            throw new ArgumentException("Shader source cannot be empty.", nameof(description));
        if (string.IsNullOrWhiteSpace(description.EntryPoint))
            throw new ArgumentException("Shaders require an entry point.", nameof(description));

        var kind = description.Stage == ShaderStages.Vertex
            ? ShaderKind.VertexShader
            : ShaderKind.FragmentShader;

        fixed (byte* source = description.ShaderBytes)
        {
            var result = _shaderc.CompileIntoSpv(
                _compiler,
                source,
                (nuint)description.ShaderBytes.Length,
                kind,
                "ElementEngine.glsl",
                description.EntryPoint,
                _compileOptions);

            if (result == null)
                throw new InvalidOperationException("shaderc returned no Metal compilation result.");

            try
            {
                var status = _shaderc.ResultGetCompilationStatus(result);
                if (status != CompilationStatus.Success)
                    throw new InvalidOperationException($"GLSL compilation failed: {_shaderc.ResultGetErrorMessageS(result)}");

                var length = checked((int)_shaderc.ResultGetLength(result));
                if ((length & 3) != 0)
                    throw new InvalidOperationException("shaderc returned malformed SPIR-V bytecode.");

                var spirv = new byte[length];
                fixed (byte* destination = spirv)
                    Buffer.MemoryCopy(_shaderc.ResultGetBytes(result), destination, length, length);

                return spirv;
            }
            finally
            {
                _shaderc.ResultRelease(result);
            }
        }
    }

    public MetalCompiledShader CrossCompile(
        MTLDevice device,
        MetalShaderResource shader,
        ReadOnlySpan<MetalResourceBinding> bindings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Context* context = null;
        ParsedIr* parsedIr = null;
        CrossCompiler* compiler = null;
        CompilerOptions* compilerOptions = null;

        CheckCross(_cross.ContextCreate(&context), context, "create a SPIRV-Cross context");

        try
        {
            fixed (byte* bytes = shader.Spirv)
            {
                CheckCross(
                    _cross.ContextParseSpirv(context, (uint*)bytes, (nuint)(shader.Spirv.Length / sizeof(uint)), &parsedIr),
                    context,
                    "parse SPIR-V for Metal");
            }

            CheckCross(
                _cross.ContextCreateCompiler(context, CrossBackend.Msl, parsedIr, CaptureMode.TakeOwnership, &compiler),
                context,
                "create the Metal cross-compiler");
            parsedIr = null;

            CheckCross(
                _cross.CompilerCreateCompilerOptions(compiler, &compilerOptions),
                context,
                "create Metal compiler options");
            CheckCross(
                _cross.CompilerOptionsSetUint(compilerOptions, CompilerOption.MslVersion, 20300),
                context,
                "select Metal Shading Language 2.3");
            CheckCross(
                _cross.CompilerOptionsSetUint(compilerOptions, CompilerOption.MslPlatform, (uint)MslPlatform.Macos),
                context,
                "select the macOS Metal platform");
            CheckCross(
                _cross.CompilerOptionsSetBool(
                    compilerOptions,
                    CompilerOption.FixupDepthConvention,
                    shader.Options.FixClipSpaceZ ? (byte)1 : (byte)0),
                context,
                "configure Metal depth conversion");
            CheckCross(
                _cross.CompilerOptionsSetBool(
                    compilerOptions,
                    CompilerOption.FlipVertexY,
                    shader.Options.InvertVertexOutputY ? (byte)1 : (byte)0),
                context,
                "configure Metal vertex Y conversion");
            CheckCross(
                _cross.CompilerOptionsSetBool(compilerOptions, CompilerOption.ForceZeroInitializedVariables, 1),
                context,
                "configure Metal variable initialization");
            CheckCross(
                _cross.CompilerInstallCompilerOptions(compiler, compilerOptions),
                context,
                "install Metal compiler options");

            var executionModel = shader.Description.Stage == ShaderStages.Vertex
                ? ExecutionModel.Vertex
                : ExecutionModel.Fragment;

            foreach (var binding in bindings)
            {
                if ((binding.Stages & shader.Description.Stage) == 0)
                    continue;

                MslResourceBinding nativeBinding = default;
                _cross.MslResourceBindingInit(ref nativeBinding);
                nativeBinding.Stage = executionModel;
                nativeBinding.DescSet = binding.Slot;
                nativeBinding.Binding = binding.Element;

                switch (binding.Kind)
                {
                    case ResourceKind.UniformBuffer:
                        nativeBinding.MslBuffer = binding.NativeIndex;
                        break;
                    case ResourceKind.TextureReadOnly:
                        nativeBinding.MslTexture = binding.NativeIndex;
                        break;
                    case ResourceKind.Sampler:
                        nativeBinding.MslSampler = binding.NativeIndex;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(bindings));
                }

                CheckCross(
                    _cross.CompilerMslAddResourceBinding(compiler, ref nativeBinding),
                    context,
                    $"map Metal resource set {binding.Slot}, binding {binding.Element}");
            }

            byte* source = null;
            CheckCross(_cross.CompilerCompile(compiler, &source), context, "cross-compile SPIR-V to Metal");
            var metalSource = Marshal.PtrToStringUTF8((nint)source)
                ?? throw new InvalidOperationException("SPIRV-Cross returned empty Metal source.");
            var entryPoint = _cross.CompilerGetCleansedEntryPointNameS(
                compiler,
                shader.Description.EntryPoint,
                executionModel);

            using var compileOptions = new MTLCompileOptions
            {
                FastMathEnabled = !_debug,
                LanguageVersion = MTLLanguageVersion.Version23,
            };
            NSError error = default;
            var library = device.NewLibrary((NSString)metalSource, compileOptions, ref error);
            if (library.NativePtr == 0)
                throw new InvalidOperationException($"Metal shader compilation failed: {GetError(error)}");

            var function = library.NewFunction((NSString)entryPoint);
            if (function.NativePtr == 0)
            {
                library.Dispose();
                throw new InvalidOperationException($"Metal shader entry point '{entryPoint}' was not found.");
            }

            return new MetalCompiledShader(library, function);
        }
        finally
        {
            if (context != null)
                _cross.ContextDestroy(context);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var teardown = new TeardownCollector();

        var shaderc = _shaderc;
        var compileOptions = _compileOptions;
        _compileOptions = null;
        if (shaderc is not null && compileOptions != null)
            teardown.Run(() => shaderc.CompileOptionsRelease(compileOptions));

        var compiler = _compiler;
        _compiler = null;
        if (shaderc is not null && compiler != null)
            teardown.Run(() => shaderc.CompilerRelease(compiler));

        var cross = _cross;
        _cross = null;
        if (cross is not null)
            teardown.Run(cross.Dispose);

        _shaderc = null;
        if (shaderc is not null)
            teardown.Run(shaderc.Dispose);

        teardown.ThrowIfFailed("One or more errors occurred while disposing the Metal shader compiler.");
    }

    private void CheckCross(CrossResult result, Context* context, string operation)
    {
        if (result == CrossResult.Success)
            return;

        var details = context == null ? result.ToString() : _cross.ContextGetLastErrorStringS(context);
        throw new InvalidOperationException($"Failed to {operation}: {details}");
    }

    private static string GetError(NSError error)
    {
        if (error.NativePtr == 0)
            return "Metal did not provide an error description.";

        var description = error.LocalizedDescription;
        return description.NativePtr == 0 ? $"NSError {error.Code}" : description.ToString();
    }
}

[SupportedOSPlatform("macos")]
internal readonly record struct MetalCompiledShader(MTLLibrary Library, MTLFunction Function);
