using System;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Shaderc;
using Silk.NET.SPIRV.Cross;
using CrossBackend = Silk.NET.SPIRV.Cross.Backend;
using CrossCompiler = Silk.NET.SPIRV.Cross.Compiler;
using CrossResult = Silk.NET.SPIRV.Cross.Result;
using ShadercCompiler = Silk.NET.Shaderc.Compiler;

namespace ElementEngine;

internal unsafe sealed class DirectX12ShaderCompiler : IDisposable
{
    private const uint D3DCompileDebug = 1;
    private const uint D3DCompileSkipOptimization = 4;
    private const uint D3DCompileEnableStrictness = 1 << 11;

    private readonly Shaderc _shaderc;
    private readonly Cross _cross;
    private readonly ShadercCompiler* _compiler;
    private readonly CompileOptions* _compileOptions;
    private readonly bool _debug;
    private bool _disposed;

    public DirectX12ShaderCompiler(bool debug)
    {
        _debug = debug;
        _shaderc = NativeShaderTools.CreateShaderc();

        try
        {
            _cross = NativeShaderTools.CreateSpirvCross();
            _compiler = _shaderc.CompilerInitialize();
            _compileOptions = _shaderc.CompileOptionsInitialize();

            if (_compiler == null || _compileOptions == null)
                throw new InvalidOperationException("Failed to initialize shaderc.");

            _shaderc.CompileOptionsSetSourceLanguage(_compileOptions, SourceLanguage.Glsl);
            _shaderc.CompileOptionsSetTargetEnv(_compileOptions, TargetEnv.Vulkan, (uint)EnvVersion.Vulkan11);
            _shaderc.CompileOptionsSetOptimizationLevel(
                _compileOptions,
                debug ? OptimizationLevel.Zero : OptimizationLevel.Performance);
        }
        catch
        {
            if (_compileOptions != null)
                _shaderc.CompileOptionsRelease(_compileOptions);
            if (_compiler != null)
                _shaderc.CompilerRelease(_compiler);

            _cross?.Dispose();
            _shaderc.Dispose();
            throw;
        }
    }

    public DirectX12Shader Compile(ShaderDescription description, CrossCompileOptions options)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (description.Stage is not ShaderStages.Vertex and not ShaderStages.Fragment)
            throw new NotSupportedException($"DirectX 12 does not support the {description.Stage} shader stage in the current graphics contract.");
        if (description.ShaderBytes is null || description.ShaderBytes.Length == 0)
            throw new ArgumentException("Shader source cannot be empty.", nameof(description));
        if (string.IsNullOrWhiteSpace(description.EntryPoint))
            throw new ArgumentException("DirectX 12 shaders require an entry point.", nameof(description));

        var spirv = CompileGlsl(description);
        var hlsl = CrossCompileHlsl(spirv, options);
        var bytecode = CompileHlsl(hlsl, description);
        return new DirectX12Shader(description.Stage, bytecode);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_compileOptions != null)
            _shaderc.CompileOptionsRelease(_compileOptions);
        if (_compiler != null)
            _shaderc.CompilerRelease(_compiler);

        _shaderc.Dispose();
        _cross.Dispose();
        _disposed = true;
    }

    private uint[] CompileGlsl(ShaderDescription description)
    {
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
                throw new InvalidOperationException("shaderc returned no compilation result.");

            try
            {
                var status = _shaderc.ResultGetCompilationStatus(result);
                if (status != CompilationStatus.Success)
                    throw new InvalidOperationException($"GLSL compilation failed: {_shaderc.ResultGetErrorMessageS(result)}");

                var byteLength = checked((int)_shaderc.ResultGetLength(result));
                if ((byteLength & 3) != 0)
                    throw new InvalidOperationException("shaderc returned malformed SPIR-V bytecode.");

                var words = new uint[byteLength / sizeof(uint)];
                fixed (uint* destination = words)
                    Buffer.MemoryCopy(_shaderc.ResultGetBytes(result), destination, byteLength, byteLength);

                return words;
            }
            finally
            {
                _shaderc.ResultRelease(result);
            }
        }
    }

    private string CrossCompileHlsl(uint[] spirv, CrossCompileOptions options)
    {
        Context* context = null;
        ParsedIr* parsedIr = null;
        CrossCompiler* compiler = null;
        CompilerOptions* compilerOptions = null;

        CheckCross(_cross.ContextCreate(&context), context, "create a SPIRV-Cross context");

        try
        {
            fixed (uint* words = spirv)
            {
                CheckCross(
                    _cross.ContextParseSpirv(context, words, (nuint)spirv.Length, &parsedIr),
                    context,
                    "parse SPIR-V");
            }

            CheckCross(
                _cross.ContextCreateCompiler(context, CrossBackend.Hlsl, parsedIr, CaptureMode.TakeOwnership, &compiler),
                context,
                "create the HLSL cross-compiler");
            parsedIr = null;

            CheckCross(
                _cross.CompilerCreateCompilerOptions(compiler, &compilerOptions),
                context,
                "create HLSL compiler options");
            CheckCross(
                _cross.CompilerOptionsSetUint(compilerOptions, CompilerOption.HlslShaderModel, 51),
                context,
                "select HLSL shader model 5.1");
            CheckCross(
                _cross.CompilerOptionsSetBool(
                    compilerOptions,
                    CompilerOption.FixupDepthConvention,
                    options.FixClipSpaceZ ? (byte)1 : (byte)0),
                context,
                "configure depth conversion");
            CheckCross(
                _cross.CompilerOptionsSetBool(
                    compilerOptions,
                    CompilerOption.FlipVertexY,
                    options.InvertVertexOutputY ? (byte)1 : (byte)0),
                context,
                "configure vertex Y conversion");
            CheckCross(
                _cross.CompilerOptionsSetBool(compilerOptions, CompilerOption.HlslUseEntryPointName, 1),
                context,
                "preserve the shader entry point");
            CheckCross(
                _cross.CompilerInstallCompilerOptions(compiler, compilerOptions),
                context,
                "install HLSL compiler options");

            byte* source = null;
            CheckCross(_cross.CompilerCompile(compiler, &source), context, "cross-compile SPIR-V to HLSL");
            return Marshal.PtrToStringUTF8((nint)source)
                ?? throw new InvalidOperationException("SPIRV-Cross returned an empty HLSL shader.");
        }
        finally
        {
            if (context != null)
                _cross.ContextDestroy(context);
        }
    }

    private byte[] CompileHlsl(string hlsl, ShaderDescription description)
    {
        var sourceBytes = Encoding.UTF8.GetBytes(hlsl);
        var entryPoint = Encoding.UTF8.GetBytes(description.EntryPoint + '\0');
        var target = Encoding.UTF8.GetBytes(
            (description.Stage == ShaderStages.Vertex ? "vs_5_1" : "ps_5_1") + '\0');
        var sourceName = Encoding.UTF8.GetBytes("ElementEngine.hlsl\0");
        ID3D10Blob* code = null;
        ID3D10Blob* errors = null;
        var flags = D3DCompileEnableStrictness;

        if (_debug)
            flags |= D3DCompileDebug | D3DCompileSkipOptimization;

        fixed (byte* source = sourceBytes)
        fixed (byte* entry = entryPoint)
        fixed (byte* profile = target)
        fixed (byte* name = sourceName)
        {
            var result = D3DCompile(
                source,
                (nuint)sourceBytes.Length,
                name,
                null,
                null,
                entry,
                profile,
                flags,
                0,
                &code,
                &errors);

            try
            {
                if (result < 0 || code == null)
                {
                    var message = errors == null
                        ? $"HRESULT 0x{result:X8}"
                        : Marshal.PtrToStringUTF8((nint)errors->GetBufferPointer(), checked((int)errors->GetBufferSize()));
                    throw new InvalidOperationException($"HLSL compilation failed: {message}");
                }

                var length = checked((int)code->GetBufferSize());
                var bytecode = new byte[length];
                fixed (byte* destination = bytecode)
                    Buffer.MemoryCopy(code->GetBufferPointer(), destination, length, length);

                return bytecode;
            }
            finally
            {
                if (errors != null)
                    errors->Release();
                if (code != null)
                    code->Release();
            }
        }
    }

    private void CheckCross(CrossResult result, Context* context, string operation)
    {
        if (result == CrossResult.Success)
            return;

        var details = context == null ? result.ToString() : _cross.ContextGetLastErrorStringS(context);
        throw new InvalidOperationException($"Failed to {operation}: {details}");
    }

    [DllImport("d3dcompiler_47.dll", CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
    private static extern int D3DCompile(
        void* sourceData,
        nuint sourceDataSize,
        byte* sourceName,
        void* defines,
        void* include,
        byte* entryPoint,
        byte* target,
        uint flags1,
        uint flags2,
        ID3D10Blob** code,
        ID3D10Blob** errorMessages);
}
