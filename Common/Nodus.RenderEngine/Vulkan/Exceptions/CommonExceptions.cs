namespace Nodus.RenderEngine.Vulkan;

public class VulkanException(string message) : Exception(message);

public class VulkanMemoryException(string message) : Exception(message);

public class VulkanRenderingException(string message) : VulkanException(message);

public class VulkanTaskException(string message) : VulkanException(message);