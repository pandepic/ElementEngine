using System;
using Silk.NET.Vulkan;

namespace ElementEngine;

internal sealed unsafe partial class VulkanGraphicsBackend
{
    public void Present()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_sync)
        {
            if (!_frameSubmitted)
                throw new InvalidOperationException("Submit graphics commands before presenting the Vulkan swapchain.");

            var frame = _frames[_currentFrame];
            if (_waitForFrameBeforePresent)
                WaitForFrame(frame);

            var waitSemaphore = frame.RenderFinished;
            var swapchain = _swapchain;
            var imageIndex = _currentImage;
            var presentInfo = new PresentInfoKHR
            {
                SType = StructureType.PresentInfoKhr,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = &waitSemaphore,
                SwapchainCount = 1,
                PSwapchains = &swapchain,
                PImageIndices = &imageIndex,
            };

            var result = _swapchainApi.QueuePresent(_presentQueue, &presentInfo);
            if (result is Result.ErrorOutOfDateKhr or Result.SuboptimalKhr)
                _swapchainDirty = true;
            else
                Check(result, "present the Vulkan swapchain");

            _frameAcquired = false;
            _frameSubmitted = false;
            _waitForFrameBeforePresent = false;
            _currentFrame = (_currentFrame + 1) % (uint)_frames.Length;

            if (_swapchainDirty && _window.FramebufferWidth > 0 && _window.FramebufferHeight > 0)
            {
                RecreateSwapchain(
                    (uint)_window.FramebufferWidth,
                    (uint)_window.FramebufferHeight);
            }
        }
    }

    private void CreateSwapchain(uint requestedWidth, uint requestedHeight)
    {
        _surfaceApi.GetPhysicalDeviceSurfaceCapabilities(_physicalDevice, _surface, out var capabilities);
        var surfaceFormat = ChooseSurfaceFormat();
        var presentMode = ChoosePresentMode();
        var extent = ChooseSwapchainExtent(capabilities, requestedWidth, requestedHeight);
        if ((capabilities.SupportedUsageFlags & ImageUsageFlags.ColorAttachmentBit) == 0)
            throw new NotSupportedException("The Vulkan surface does not support color-attachment swapchain images.");

        var imageCount = capabilities.MinImageCount + 1;
        if (capabilities.MaxImageCount > 0 && imageCount > capabilities.MaxImageCount)
            imageCount = capabilities.MaxImageCount;

        var queueFamilyIndices = stackalloc uint[2]
        {
            _graphicsQueueFamily,
            _presentQueueFamily,
        };
        var oldSwapchain = _swapchain;
        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = _surface,
            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            ImageSharingMode = _graphicsQueueFamily == _presentQueueFamily
                ? SharingMode.Exclusive
                : SharingMode.Concurrent,
            QueueFamilyIndexCount = _graphicsQueueFamily == _presentQueueFamily ? 0u : 2u,
            PQueueFamilyIndices = _graphicsQueueFamily == _presentQueueFamily ? null : queueFamilyIndices,
            PreTransform = capabilities.CurrentTransform,
            CompositeAlpha = ChooseCompositeAlpha(capabilities.SupportedCompositeAlpha),
            PresentMode = presentMode,
            Clipped = true,
            OldSwapchain = oldSwapchain,
        };

        Check(_swapchainApi.CreateSwapchain(_device, &createInfo, null, out var newSwapchain), "create the Vulkan swapchain");

        if (oldSwapchain.Handle != 0)
        {
            _swapchain = default;
            try
            {
                DestroySwapchainObjects(oldSwapchain);
            }
            catch (Exception replacementException)
            {
                var teardown = new TeardownCollector(replacementException);
                teardown.Run(() => _swapchainApi.DestroySwapchain(_device, newSwapchain, null));
                teardown.ThrowIfFailed("Vulkan swapchain replacement and cleanup both failed.");
                throw;
            }
        }

        _swapchain = newSwapchain;
        _swapchainFormat = surfaceFormat.Format;
        _swapchainExtent = extent;
        _swapchainOutputDescription = new OutputDescription([surfaceFormat.Format.ToElement()]);

        uint swapchainImageCount = 0;
        Check(
            _swapchainApi.GetSwapchainImages(_device, _swapchain, ref swapchainImageCount, null),
            "query Vulkan swapchain images");
        _swapchainImages = new Silk.NET.Vulkan.Image[swapchainImageCount];

        fixed (Silk.NET.Vulkan.Image* images = _swapchainImages)
        {
            Check(
                _swapchainApi.GetSwapchainImages(_device, _swapchain, ref swapchainImageCount, images),
                "query Vulkan swapchain images");
        }

        _swapchainImageViews = new ImageView[swapchainImageCount];
        _swapchainImageLayouts = new ImageLayout[swapchainImageCount];
        for (var index = 0; index < _swapchainImages.Length; index++)
        {
            _swapchainImageViews[index] = CreateImageView(
                _swapchainImages[index],
                _swapchainFormat,
                ImageAspectFlags.ColorBit,
                ImageViewType.Type2D,
                1,
                1);
            _swapchainImageLayouts[index] = ImageLayout.Undefined;
        }

        _swapchainRenderPass = CreateRenderPass(_swapchainOutputDescription);
        _swapchainFramebuffers = new Silk.NET.Vulkan.Framebuffer[swapchainImageCount];

        for (var index = 0; index < _swapchainImageViews.Length; index++)
        {
            var attachment = _swapchainImageViews[index];
            var framebufferInfo = new FramebufferCreateInfo
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = _swapchainRenderPass,
                AttachmentCount = 1,
                PAttachments = &attachment,
                Width = extent.Width,
                Height = extent.Height,
                Layers = 1,
            };
            Check(
                _vk.CreateFramebuffer(_device, &framebufferInfo, null, out _swapchainFramebuffers[index]),
                "create a Vulkan swapchain framebuffer");
        }

        _imageFences = new Fence[swapchainImageCount];
        _swapchainDirty = false;
    }

    private void RecreateSwapchain(uint width, uint height)
    {
        if (width == 0 || height == 0 || _device.Handle == 0)
        {
            _swapchainDirty = true;
            return;
        }

        Check(_vk.DeviceWaitIdle(_device), "wait before recreating the Vulkan swapchain");
        if (_frameSubmitted)
        {
            var renderFinished = _frames[_currentFrame].RenderFinished;
            var waitStage = PipelineStageFlags.AllCommandsBit;
            var submitInfo = new SubmitInfo
            {
                SType = StructureType.SubmitInfo,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = &renderFinished,
                PWaitDstStageMask = &waitStage,
            };
            Check(
                _vk.QueueSubmit(_graphicsQueue, 1, &submitInfo, default),
                "consume a pending Vulkan render semaphore before resizing");
            Check(
                _vk.QueueWaitIdle(_graphicsQueue),
                "wait after consuming a pending Vulkan render semaphore");
        }

        _frameAcquired = false;
        _frameSubmitted = false;
        _waitForFrameBeforePresent = false;
        CreateSwapchain(width, height);
    }

    private void DestroySwapchain()
    {
        var swapchain = _swapchain;
        _swapchain = default;
        DestroySwapchainObjects(swapchain);
    }

    private void DestroySwapchainObjects(SwapchainKHR swapchain)
    {
        var framebuffers = _swapchainFramebuffers;
        _swapchainFramebuffers = [];
        var renderPass = _swapchainRenderPass;
        _swapchainRenderPass = default;
        var imageViews = _swapchainImageViews;
        _swapchainImageViews = [];
        _swapchainImages = [];
        _swapchainImageLayouts = [];
        _imageFences = [];
        _swapchainFormat = default;
        _swapchainExtent = default;
        _swapchainOutputDescription = default;

        if (_device.Handle == 0)
            return;

        var teardown = new TeardownCollector();
        for (var index = framebuffers.Length - 1; index >= 0; index--)
        {
            var framebuffer = framebuffers[index];
            if (framebuffer.Handle != 0)
                teardown.Run(() => _vk.DestroyFramebuffer(_device, framebuffer, null));
        }

        if (renderPass.Handle != 0)
            teardown.Run(() => _vk.DestroyRenderPass(_device, renderPass, null));

        for (var index = imageViews.Length - 1; index >= 0; index--)
        {
            var view = imageViews[index];
            if (view.Handle != 0)
                teardown.Run(() => _vk.DestroyImageView(_device, view, null));
        }

        if (swapchain.Handle != 0 && _swapchainApi is not null)
            teardown.Run(() => _swapchainApi.DestroySwapchain(_device, swapchain, null));

        teardown.ThrowIfFailed("One or more errors occurred while destroying the Vulkan swapchain.");
    }

    private SurfaceFormatKHR ChooseSurfaceFormat()
    {
        uint count = 0;
        Check(
            _surfaceApi.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, ref count, null),
            "query Vulkan surface formats");
        var formats = new SurfaceFormatKHR[count];

        fixed (SurfaceFormatKHR* formatsPointer = formats)
        {
            Check(
                _surfaceApi.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, ref count, formatsPointer),
                "query Vulkan surface formats");
        }

        if (formats.Length == 1 && formats[0].Format == Format.Undefined)
        {
            return new SurfaceFormatKHR
            {
                Format = Format.B8G8R8A8Unorm,
                ColorSpace = formats[0].ColorSpace,
            };
        }

        if (TryFindFormat(formats, Format.B8G8R8A8Unorm, ColorSpaceKHR.SpaceSrgbNonlinearKhr, out var chosen))
            return chosen;

        // Prefer UNorm formats in a fixed order: shaders output gamma-encoded color, so an
        // sRGB swapchain (which re-encodes) is only a last resort, not driver-order luck.
        Span<Format> fallbackOrder = [Format.R8G8B8A8Unorm, Format.B8G8R8A8Srgb, Format.R8G8B8A8Srgb];
        foreach (var fallback in fallbackOrder)
        {
            if (TryFindFormat(formats, fallback, colorSpace: null, out chosen))
                return chosen;
        }

        throw new NotSupportedException("The Vulkan surface does not expose a supported 32-bit color format.");
    }

    private static bool TryFindFormat(
        ReadOnlySpan<SurfaceFormatKHR> formats,
        Format format,
        ColorSpaceKHR? colorSpace,
        out SurfaceFormatKHR result)
    {
        foreach (var candidate in formats)
        {
            if (candidate.Format == format && (colorSpace is null || candidate.ColorSpace == colorSpace))
            {
                result = candidate;
                return true;
            }
        }

        result = default;
        return false;
    }

    private PresentModeKHR ChoosePresentMode()
    {
        uint count = 0;
        Check(
            _surfaceApi.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface, ref count, null),
            "query Vulkan present modes");
        var modes = new PresentModeKHR[count];

        fixed (PresentModeKHR* modesPointer = modes)
        {
            Check(
                _surfaceApi.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface, ref count, modesPointer),
                "query Vulkan present modes");
        }

        if (_options.SyncToVerticalBlank)
            return PresentModeKHR.FifoKhr;

        foreach (var mode in modes)
        {
            if (mode == PresentModeKHR.MailboxKhr)
                return mode;
        }

        foreach (var mode in modes)
        {
            if (mode == PresentModeKHR.ImmediateKhr)
                return mode;
        }

        return PresentModeKHR.FifoKhr;
    }

    private static Extent2D ChooseSwapchainExtent(
        SurfaceCapabilitiesKHR capabilities,
        uint requestedWidth,
        uint requestedHeight)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
            return capabilities.CurrentExtent;

        return new Extent2D
        {
            Width = Math.Clamp(requestedWidth, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width),
            Height = Math.Clamp(requestedHeight, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height),
        };
    }

    private static CompositeAlphaFlagsKHR ChooseCompositeAlpha(CompositeAlphaFlagsKHR supported)
    {
        var choices = new[]
        {
            CompositeAlphaFlagsKHR.OpaqueBitKhr,
            CompositeAlphaFlagsKHR.PreMultipliedBitKhr,
            CompositeAlphaFlagsKHR.PostMultipliedBitKhr,
            CompositeAlphaFlagsKHR.InheritBitKhr,
        };

        foreach (var choice in choices)
        {
            if ((supported & choice) != 0)
                return choice;
        }

        throw new InvalidOperationException("The Vulkan surface exposes no supported composite-alpha mode.");
    }

    private RenderPass CreateRenderPass(OutputDescription output)
    {
        var colorFormats = output.ColorAttachments ?? [];
        if (colorFormats.Length == 0)
            throw new ArgumentException("A Vulkan render pass requires a color attachment.", nameof(output));

        var hasDepth = output.DepthAttachment.HasValue;
        var attachmentCount = colorFormats.Length + (hasDepth ? 1 : 0);
        var attachments = stackalloc AttachmentDescription[attachmentCount];
        var colorReferences = stackalloc AttachmentReference[colorFormats.Length];

        for (var index = 0; index < colorFormats.Length; index++)
        {
            attachments[index] = new AttachmentDescription
            {
                Format = colorFormats[index].ToVulkan(),
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Load,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.ColorAttachmentOptimal,
                FinalLayout = ImageLayout.ColorAttachmentOptimal,
            };
            colorReferences[index] = new AttachmentReference
            {
                Attachment = (uint)index,
                Layout = ImageLayout.ColorAttachmentOptimal,
            };
        }

        var depthReference = new AttachmentReference();
        if (hasDepth)
        {
            attachments[colorFormats.Length] = new AttachmentDescription
            {
                Format = output.DepthAttachment.Value.ToVulkan(),
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Load,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.Load,
                StencilStoreOp = AttachmentStoreOp.Store,
                InitialLayout = ImageLayout.DepthStencilAttachmentOptimal,
                FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
            };
            depthReference = new AttachmentReference
            {
                Attachment = (uint)colorFormats.Length,
                Layout = ImageLayout.DepthStencilAttachmentOptimal,
            };
        }

        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = (uint)colorFormats.Length,
            PColorAttachments = colorReferences,
            PDepthStencilAttachment = hasDepth ? &depthReference : null,
        };
        var dependency = new SubpassDependency
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            SrcAccessMask = AccessFlags.MemoryReadBit | AccessFlags.MemoryWriteBit,
            DstAccessMask = AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit |
                            AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.DepthStencilAttachmentWriteBit,
        };
        var createInfo = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = (uint)attachmentCount,
            PAttachments = attachments,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 1,
            PDependencies = &dependency,
        };

        Check(_vk.CreateRenderPass(_device, &createInfo, null, out var renderPass), "create a Vulkan render pass");
        return renderPass;
    }
}
