using System.Runtime.Versioning;
using ElementEngine;
using Xunit;

namespace ElementEngine.Tests;

[SupportedOSPlatform("macos")]
public sealed class BackendConversionContractTests
{
    [Fact]
    public void VertexElementSizesStayIdenticalAcrossBackends()
    {
        foreach (var format in Enum.GetValues<VertexElementFormat>())
        {
            var directXSize = DirectX12Conversions.GetVertexElementSize(format);
            var vulkanSize = VulkanConversions.SizeInBytes(format);
            var metalSize = MetalConversions.GetVertexElementSize(format);

            Assert.Equal(directXSize, vulkanSize);
            Assert.Equal(directXSize, metalSize);
        }
    }

    [Fact]
    public void PixelStorageSizesStayIdenticalAcrossSupportedBackends()
    {
        foreach (var format in Enum.GetValues<PixelFormat>())
        {
            var expected = (uint)GraphicsHelper.GetPixelFormatBytesPerPixel(format);
            Assert.Equal(expected, VulkanConversions.BytesPerBlock(format));
            Assert.Equal(expected, MetalConversions.GetBytesPerPixel(format));

            if (VulkanConversions.IsCompressed(format))
            {
                Assert.Throws<NotSupportedException>(() => DirectX12Conversions.GetBytesPerPixel(format));
                continue;
            }

            Assert.Equal(expected, DirectX12Conversions.GetBytesPerPixel(format));
        }
    }

    [Fact]
    public void EveryFrontendEnumValueHasBackendConversions()
    {
        foreach (var comparison in Enum.GetValues<ComparisonKind>())
        {
            DirectX12Conversions.ToComparison(comparison);
            VulkanConversions.ToVulkan(comparison);
            MetalConversions.ToMetal(comparison);
        }

        foreach (var factor in Enum.GetValues<BlendFactor>())
        {
            DirectX12Conversions.ToBlend(factor);
            VulkanConversions.ToVulkan(factor);
            MetalConversions.ToMetal(factor);
        }

        foreach (var function in Enum.GetValues<BlendFunction>())
        {
            DirectX12Conversions.ToBlendOperation(function);
            VulkanConversions.ToVulkan(function);
            MetalConversions.ToMetal(function);
        }

        foreach (var cullMode in Enum.GetValues<FaceCullMode>())
        {
            DirectX12Conversions.ToCullMode(cullMode);
            VulkanConversions.ToVulkan(cullMode);
            MetalConversions.ToMetal(cullMode);
        }

        foreach (var topology in Enum.GetValues<PrimitiveTopology>())
        {
            DirectX12Conversions.ToPrimitiveTopology(topology);
            DirectX12Conversions.ToPrimitiveTopologyType(topology);
            VulkanConversions.ToVulkan(topology);
            MetalConversions.ToMetal(topology);
            MetalConversions.ToMetalClass(topology);
        }
    }

    [Fact]
    public void EveryPixelFormatHasNativeMappingsWhereSupported()
    {
        foreach (var format in Enum.GetValues<PixelFormat>())
        {
            VulkanConversions.ToVulkan(format);
            MetalConversions.ToMetal(format, true);

            if (VulkanConversions.IsCompressed(format))
                Assert.Throws<NotSupportedException>(() => DirectX12Conversions.ToDxgiFormat(format));
            else
                DirectX12Conversions.ToDxgiFormat(format);
        }
    }

    [Theory]
    [InlineData(0, 256, 0)]
    [InlineData(1, 256, 256)]
    [InlineData(256, 256, 256)]
    [InlineData(257, 256, 512)]
    [InlineData(17, 1, 17)]
    [InlineData(17, 0, 17)]
    public void MetalAlignmentRoundsUpWithoutDroppingData(
        int value,
        int alignment,
        int expected)
    {
        Assert.Equal((ulong)expected, MetalConversions.AlignUp((ulong)value, (ulong)alignment));
    }
}
