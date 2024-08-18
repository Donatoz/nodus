using System.Runtime.InteropServices;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan;


/// <summary>
/// Represents a managed Vulkan instance.
/// </summary>
public interface IVkInstance : IVkUnmanagedHook
{
    /// <summary>
    /// The wrapped, unmanaged Vulkan instance.
    /// </summary>
    /// <remarks>
    /// This property provides access to the underlying Vulkan instance object.
    /// </remarks>
    Instance WrappedInstance { get; }
    
    void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo);
}

/// <summary>
/// Represents the context information for creating a Vulkan instance.
/// </summary>
public interface IVkInstanceContext
{
    /// <summary>
    /// The name of the application.
    /// </summary>
    /// <remarks>
    /// This property provides the name of the application that is associated with the Vulkan instance.
    /// </remarks>
    string AppName { get; }

    /// <summary>
    /// The version of the application.
    /// </summary>
    /// <remarks>
    /// This property returns the version of the application. It is a positive integer value that represents the version number of the application.
    /// </remarks>
    uint AppVersion { get; }

    /// <summary>
    /// The name of the engine.
    /// </summary>
    /// <remarks>
    /// This property is part of the Vulkan rendering engine context and provides the name of the engine being used.
    /// </remarks>
    string EngineName { get; }

    /// <summary>
    /// The version of the engine.
    /// </summary>
    /// <remarks>
    /// This property provides the numeric version of the engine being used.
    /// </remarks>
    uint EngineVersion { get; }

    /// <summary>
    /// The version of the Vulkan API that the instance was created with.
    /// </summary>
    /// <remarks>
    /// This property represents the version of the Vulkan API that was used to create the instance.
    /// It is typically used to determine which features and extensions are supported by the instance.
    /// The version is specified as a single number composed of three components: major, minor, and patch.
    /// The major component indicates a major release, the minor component indicates a minor release, and the patch component indicates a bugfix release.
    /// </remarks>
    uint ApiVersion { get; }
}

public readonly struct VkInstanceContext(
    string appName,
    uint appVersion,
    string engineName,
    uint engineVersion,
    uint apiVersion)
    : IVkInstanceContext
{
    public string AppName { get; } = appName;
    public uint AppVersion { get; } = appVersion;
    public string EngineName { get; } = engineName;
    public uint EngineVersion { get; } = engineVersion;
    public uint ApiVersion { get; } = apiVersion;
}

public unsafe class VkInstance : VkObject, IVkInstance
{
    public Instance WrappedInstance { get; }

    public VkInstance(IVkContext vkContext, IVkInstanceContext instanceContext) : base(vkContext)
    {
        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi(instanceContext.AppName),
            ApplicationVersion = instanceContext.AppVersion,
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi(instanceContext.EngineName),
            EngineVersion = instanceContext.EngineVersion,
            ApiVersion = instanceContext.ApiVersion
        };

        var createInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,
            EnabledExtensionCount = (uint)vkContext.ExtensionsInfo.RequiredExtensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(vkContext.ExtensionsInfo.RequiredExtensions)
        };

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Extensions loaded:");
        vkContext.ExtensionsInfo.RequiredExtensions.ForEach(Console.WriteLine);
        Console.ResetColor();

        if (vkContext.LayerInfo != null)
        {
            createInfo.EnabledLayerCount = vkContext.LayerInfo.Value.EnabledLayersCount;
            createInfo.PpEnabledLayerNames = vkContext.LayerInfo.Value.EnabledLayerNames;

            var debugCreateInfo = new DebugUtilsMessengerCreateInfoEXT();
            PopulateDebugMessengerCreateInfo(ref debugCreateInfo);

            createInfo.PNext = &debugCreateInfo;
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }

        if (Context.Api.CreateInstance(in createInfo, null, out var instance) != Result.Success)
        {
            throw new Exception("Failed to create Vk instance.");
        }

        WrappedInstance = instance;
        
        Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
        Marshal.FreeHGlobal((nint)appInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
    }
    
    public virtual void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
    {
        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
    }
    
    protected virtual uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, 
        DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"Validation: {Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage)}");
        Console.ResetColor();
        
        return Vk.False;
    }
    
    public static implicit operator Instance(VkInstance instance)
    {
        return instance.WrappedInstance;
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            Context.Api.DestroyInstance(this, null);
        }
        
        base.Dispose(disposing);
    }
}