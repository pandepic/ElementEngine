using Veldrid;

namespace ElementEngine
{
    public interface ISimpleUniformBuffer
    {
        public string Name { get; set; }

        public DeviceBuffer Buffer { get; set; }
        public ResourceLayout ResourceLayout { get; set; }
        public ResourceSet ResourceSet { get; set; }

        public void Dispose();
    }
}
