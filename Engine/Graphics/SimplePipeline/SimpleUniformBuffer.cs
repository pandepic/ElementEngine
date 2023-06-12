using System;
using Veldrid;

namespace ElementEngine
{
    public class SimpleUniformBuffer<T> : ISimpleUniformBuffer, IDisposable where T : unmanaged
    {
        public string Name { get; set; }
        public T[] Data;

        public DeviceBuffer Buffer { get; set; }
        public ResourceLayout ResourceLayout { get; set; }
        public ResourceSet ResourceSet { get; set; }

        public readonly ShaderStages Stages;
        public readonly GraphicsDevice GraphicsDevice;

        protected bool _isDisposed = false;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            Buffer?.Dispose();
            ResourceLayout?.Dispose();
            ResourceSet?.Dispose();

            _isDisposed = true;
        }

        public unsafe SimpleUniformBuffer(GraphicsDevice graphicsDevice, string name, int size, ShaderStages stages)
        {
            Name = name;
            Data = new T[size];
            Stages = stages;
            GraphicsDevice = graphicsDevice;

            var dataSize = size * sizeof(T);
            // make sure the size is in multiples of 16
            var bufferSize = (dataSize / 16 + (dataSize % 16 > 0 ? 1 : 0)) * 16;
            
            Buffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)bufferSize, BufferUsage.UniformBuffer));
            Buffer.Name = Name;

            ResourceLayout = GraphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(new ResourceLayoutElementDescription(Name, ResourceKind.UniformBuffer, Stages)));
            ResourceSet = GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(ResourceLayout, Buffer));
        }

        public unsafe void SetValue(int index, T value, bool updateBuffer = false)
        {
            Data[index] = value;

            if (updateBuffer)
                GraphicsDevice.UpdateBuffer(Buffer, (uint)(index * sizeof(T)), Data[index]);
        }

        public void UpdateBufferImmediate()
        {
            GraphicsDevice.UpdateBuffer(Buffer, 0, Data);
        }

        public void UpdateBuffer(CommandList commandList)
        {
            commandList.UpdateBuffer(Buffer, 0, Data);
        }
    }
}
