using System.Text;
using ElementEngine;
using Silk.NET.Shaderc;
using Xunit;

namespace ElementEngine.Tests;

public sealed unsafe class NativeShaderToolsTests
{
    [Fact]
    public void DisposingOneShadercWrapperDoesNotUnloadSharedLibrary()
    {
        NativeShaderTools.CreateShaderc().Dispose();

        var shaderc = NativeShaderTools.CreateShaderc();
        var compiler = shaderc.CompilerInitialize();
        var options = shaderc.CompileOptionsInitialize();
        CompilationResult* result = null;

        try
        {
            Assert.NotEqual(0, (nint)compiler);
            Assert.NotEqual(0, (nint)options);

            const string source = "#version 450\nvoid main() { gl_Position = vec4(0.0); }";
            var sourceBytes = Encoding.UTF8.GetBytes(source);
            fixed (byte* sourcePointer = sourceBytes)
            {
                result = shaderc.CompileIntoSpv(
                    compiler,
                    sourcePointer,
                    (nuint)sourceBytes.Length,
                    ShaderKind.VertexShader,
                    "NativeShaderToolsTests.glsl",
                    "main",
                    options);
            }

            Assert.NotEqual(0, (nint)result);
            Assert.Equal(CompilationStatus.Success, shaderc.ResultGetCompilationStatus(result));
            Assert.True(shaderc.ResultGetLength(result) > 0);
        }
        finally
        {
            if (result != null)
                shaderc.ResultRelease(result);
            if (options != null)
                shaderc.CompileOptionsRelease(options);
            if (compiler != null)
                shaderc.CompilerRelease(compiler);
            shaderc.Dispose();
        }
    }
}
