using System;
using System.Collections.Generic;
using Silk.NET.Vulkan;
using VkBuffer = Silk.NET.Vulkan.Buffer;
using VkFramebuffer = Silk.NET.Vulkan.Framebuffer;
using VkImage = Silk.NET.Vulkan.Image;
using VkPipeline = Silk.NET.Vulkan.Pipeline;
using VkSampler = Silk.NET.Vulkan.Sampler;

namespace ElementEngine;

internal sealed class VulkanBufferResource
{
    public required VkBuffer Handle { get; init; }
    public required DeviceMemory Memory { get; init; }
    public required ulong Size { get; init; }
    public required BufferUsageFlags Usage { get; init; }
    public required MemoryPropertyFlags MemoryProperties { get; init; }
    public nint MappedAddress { get; set; }
}

internal sealed class VulkanTextureResource
{
    public required VkImage Handle { get; init; }
    public required DeviceMemory Memory { get; init; }
    public required TextureDescription Description { get; init; }
    public required Format Format { get; init; }
    public required ImageAspectFlags AspectMask { get; init; }
    public required MemoryPropertyFlags MemoryProperties { get; init; }
    public ImageLayout Layout { get; set; } = ImageLayout.Undefined;
    public nint MappedAddress { get; set; }
    public MapMode MappedMode { get; set; }
}

internal sealed class VulkanTextureViewResource
{
    public required ImageView Handle { get; init; }
    public required VulkanTextureResource Texture { get; init; }
}

internal sealed class VulkanSamplerResource
{
    public required VkSampler Handle { get; init; }
}

internal sealed class VulkanShaderResource
{
    public required ShaderModule Handle { get; init; }
    public required ShaderStages Stage { get; init; }
    public required string EntryPoint { get; init; }
}

internal sealed class VulkanResourceLayoutResource
{
    public required DescriptorSetLayout Handle { get; init; }
    public required DescriptorSetLayoutBinding[] Bindings { get; init; }
}

internal sealed class VulkanResourceSetResource
{
    public required DescriptorSet Handle { get; init; }
    public required VulkanResourceLayoutResource Layout { get; init; }
    public required VulkanTextureResource[] Textures { get; init; }
    public required ImageView[] OwnedViews { get; init; }
}

internal sealed class VulkanPipelineResource
{
    public required VkPipeline Handle { get; init; }
    public required PipelineLayout Layout { get; init; }
    public required RenderPass CompatibilityRenderPass { get; init; }
    public required OutputDescription Output { get; init; }
    public required bool ScissorTestEnabled { get; init; }
    public required VulkanResourceLayoutResource[] SetLayouts { get; init; }
    public required int VertexBufferCount { get; init; }
}

internal sealed class VulkanFramebufferResource
{
    public required VkFramebuffer Handle { get; init; }
    public required RenderPass RenderPass { get; init; }
    public required Extent2D Extent { get; init; }
    public required OutputDescription Output { get; init; }
    public required VulkanTextureResource[] ColorTargets { get; init; }
    public VulkanTextureResource DepthTarget { get; init; }
    public required ImageView[] OwnedViews { get; init; }
}

internal sealed class VulkanCommandState
{
    public const int MaximumBindings = 16;

    public required CommandBuffer CommandBuffer { get; init; }
    public required VulkanFrameContext Frame { get; init; }
    public VulkanBufferResource[] VertexBuffers { get; } = new VulkanBufferResource[MaximumBindings];
    public VulkanResourceSetResource[] ResourceSets { get; } = new VulkanResourceSetResource[MaximumBindings];
    public Dictionary<VulkanTextureResource, ImageLayout> OriginalTextureLayouts { get; } = [];
    public bool RenderPassActive { get; set; }
    public bool FramebufferSelected { get; set; }
    public bool SwapchainTouched { get; set; }
    public bool ViewportSet { get; set; }
    public bool ScissorSet { get; set; }
    public bool IsSwapchain { get; set; }
    public RenderPass RenderPass { get; set; }
    public VkFramebuffer Framebuffer { get; set; }
    public Extent2D Extent { get; set; }
    public OutputDescription Output { get; set; }
    public VulkanFramebufferResource OffscreenFramebuffer { get; set; }
    public VulkanPipelineResource Pipeline { get; set; }
    public VulkanBufferResource IndexBuffer { get; set; }
    public IndexFormat IndexFormat { get; set; }
    public ImageLayout OriginalSwapchainLayout { get; set; }
}

internal sealed class VulkanFrameContext
{
    public required CommandPool CommandPool { get; init; }
    public required CommandBuffer CommandBuffer { get; init; }
    public required Semaphore ImageAvailable { get; init; }
    public required Semaphore RenderFinished { get; init; }
    public required Fence InFlight { get; init; }
    public List<VulkanBufferResource> TransientBuffers { get; } = [];
    public ulong SubmittedSerial { get; set; }
    public VulkanBufferResource UploadArena { get; set; }
    public ulong UploadArenaOffset { get; set; }
}

internal readonly record struct VulkanSwapchainImage(
    Silk.NET.Vulkan.Image Image,
    ImageView View,
    VkFramebuffer Framebuffer,
    ImageLayout Layout);
