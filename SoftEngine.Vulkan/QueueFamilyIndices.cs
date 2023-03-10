namespace SoftEngine.Vulkan
{
    public struct QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }

        public bool IsComplete() =>
            GraphicsFamily.HasValue;
    }
}